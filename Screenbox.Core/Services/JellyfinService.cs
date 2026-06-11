#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Windows.Data.Json;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services;

public sealed class JellyfinService : IJellyfinService
{
    private const string ClientName = "Screenbox";
    private const string ClientVersion = "1.0.0";

    private readonly ISettingsService _settingsService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly HttpClient _httpClient = new();

    public JellyfinService(ISettingsService settingsService, MediaViewModelFactory mediaFactory)
    {
        _settingsService = settingsService;
        _mediaFactory = mediaFactory;
    }

    public bool IsConnected => GetConnection().IsConfigured;

    public JellyfinConnection GetConnection()
    {
        return new JellyfinConnection(_settingsService.JellyfinServerUrl, _settingsService.JellyfinAccessToken, _settingsService.JellyfinUserId, _settingsService.JellyfinDeviceId);
    }

    public async Task<bool> AuthenticateAsync(string serverUrl, string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(username)) return false;

        serverUrl = serverUrl.Trim().TrimEnd('/');
        var request = new HttpRequestMessage(HttpMethod.Post, $"{serverUrl}/Users/AuthenticateByName");
        ApplyAuthorizationHeader(request, null);
        request.Content = new StringContent($"{{\"Username\":\"{EscapeJson(username)}\",\"Pw\":\"{EscapeJson(password)}\"}}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;

        JsonObject root = JsonObject.Parse(await response.Content.ReadAsStringAsync());
        string token = root.GetNamedString("AccessToken", string.Empty);
        string userId = root.GetNamedObject("User", new JsonObject()).GetNamedString("Id", string.Empty);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId)) return false;

        _settingsService.JellyfinServerUrl = serverUrl;
        _settingsService.JellyfinUserId = userId;
        _settingsService.JellyfinAccessToken = token;
        return true;
    }

    public void Disconnect()
    {
        _settingsService.JellyfinServerUrl = string.Empty;
        _settingsService.JellyfinAccessToken = string.Empty;
        _settingsService.JellyfinUserId = string.Empty;
    }

    public async Task<MusicLibrary> FetchMusicAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();
        if (!connection.IsConfigured) return MusicLibrary.Empty;

        List<MediaViewModel> songs = await FetchItemsAsync(connection, "Audio", cancellationToken);
        var albumFactory = new AlbumViewModelFactory();
        var artistFactory = new ArtistViewModelFactory();
        foreach (MediaViewModel song in songs)
        {
            song.IsFromLibrary = true;
            albumFactory.AddSong(song);
            artistFactory.AddSong(song);
            song.Album = albumFactory.SongsToAlbums[song];
            song.Artists = artistFactory.SongsToArtists[song].ToArray();
        }

        return new MusicLibrary(songs, albumFactory.Albums, artistFactory.Artists, albumFactory.UnknownAlbum, artistFactory.UnknownArtist);
    }

    public async Task<VideosLibrary> FetchVideosAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();
        if (!connection.IsConfigured) return VideosLibrary.Empty;

        List<MediaViewModel> videos = await FetchItemsAsync(connection, "Movie,Episode,Video", cancellationToken);
        foreach (MediaViewModel video in videos) video.IsFromLibrary = true;
        return new VideosLibrary(videos);
    }

    public Task ReportPlaybackStartAsync(JellyfinMediaSource source, long positionTicks, CancellationToken cancellationToken = default)
    {
        return PostSessionAsync("/Sessions/Playing", source, positionTicks, false, cancellationToken);
    }

    public Task ReportPlaybackProgressAsync(JellyfinMediaSource source, long positionTicks, bool isPaused, CancellationToken cancellationToken = default)
    {
        return PostSessionAsync("/Sessions/Playing/Progress", source, positionTicks, isPaused, cancellationToken);
    }

    public Task ReportPlaybackStoppedAsync(JellyfinMediaSource source, long positionTicks, CancellationToken cancellationToken = default)
    {
        return PostSessionAsync("/Sessions/Playing/Stopped", source, positionTicks, false, cancellationToken);
    }

    private async Task<List<MediaViewModel>> FetchItemsAsync(JellyfinConnection connection, string includeItemTypes, CancellationToken cancellationToken)
    {
        string url = $"{connection.ServerUrl}/Users/{connection.UserId}/Items?Recursive=true&IncludeItemTypes={Uri.EscapeDataString(includeItemTypes)}&Fields=DateCreated,Genres,MediaSources,Overview,ParentId,PrimaryImageAspectRatio,ProductionYear,RunTimeTicks,Studios,AlbumArtist,Artists,Album,IndexNumber,SeriesName&SortBy=SortName&SortOrder=Ascending";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuthorizationHeader(request, connection.AccessToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        JsonObject root = JsonObject.Parse(await response.Content.ReadAsStringAsync());
        JsonArray items = root.GetNamedArray("Items", new JsonArray());
        List<MediaViewModel> result = new(items.Count);
        foreach (IJsonValue value in items)
        {
            if (value.ValueType != JsonValueType.Object) continue;
            MediaViewModel? item = MapItem(connection, value.GetObject());
            if (item != null) result.Add(item);
        }

        return result;
    }

    private MediaViewModel? MapItem(JellyfinConnection connection, JsonObject item)
    {
        string itemId = item.GetNamedString("Id", string.Empty);
        if (string.IsNullOrWhiteSpace(itemId)) return null;

        string type = item.GetNamedString("Type", string.Empty);
        MediaPlaybackType mediaType = type.Equals("Audio", StringComparison.OrdinalIgnoreCase) ? MediaPlaybackType.Music : MediaPlaybackType.Video;
        string name = item.GetNamedString("Name", string.Empty);
        TimeSpan duration = TimeSpan.FromTicks((long)item.GetNamedNumber("RunTimeTicks", 0));
        var source = new JellyfinMediaSource(itemId, connection.ServerUrl, connection.AccessToken, name, mediaType, duration, item.GetNamedString("ParentId", string.Empty));
        var info = new MediaInfo(mediaType, name, (uint)item.GetNamedNumber("ProductionYear", 0), duration);

        if (mediaType == MediaPlaybackType.Music)
        {
            info.MusicProperties.Album = item.GetNamedString("Album", string.Empty);
            info.MusicProperties.AlbumArtist = item.GetNamedString("AlbumArtist", string.Empty);
            info.MusicProperties.Artist = string.Join(", ", GetStringArray(item, "Artists"));
            info.MusicProperties.Genre = string.Join(", ", GetStringArray(item, "Genres"));
            info.MusicProperties.TrackNumber = (uint)item.GetNamedNumber("IndexNumber", 0);
        }

        MediaViewModel vm = _mediaFactory.Create(source, info);
        vm.Name = name;
        vm.Caption = mediaType == MediaPlaybackType.Music ? info.MusicProperties.Artist : item.GetNamedString("SeriesName", string.Empty);
        vm.AltCaption = vm.Caption;
        return vm;
    }

    private async Task PostSessionAsync(string path, JellyfinMediaSource source, long positionTicks, bool isPaused, CancellationToken cancellationToken)
    {
        string json = $"{{\"ItemId\":\"{EscapeJson(source.ItemId)}\",\"PositionTicks\":{Math.Max(0, positionTicks)},\"IsPaused\":{(isPaused ? "true" : "false")},\"PlayMethod\":\"DirectStream\"}}";
        var request = new HttpRequestMessage(HttpMethod.Post, source.ServerUrl + path);
        ApplyAuthorizationHeader(request, source.AccessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            LogService.Log(e);
        }
    }

    private void ApplyAuthorizationHeader(HttpRequestMessage request, string? accessToken)
    {
        string auth = $"MediaBrowser Client=\"{ClientName}\", Device=\"Windows\", DeviceId=\"{_settingsService.JellyfinDeviceId}\", Version=\"{ClientVersion}\"";
        if (!string.IsNullOrWhiteSpace(accessToken)) auth += $", Token=\"{accessToken}\"";
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(auth);
    }

    private static IEnumerable<string> GetStringArray(JsonObject obj, string name)
    {
        if (!obj.ContainsKey(name) || obj[name].ValueType != JsonValueType.Array) return Array.Empty<string>();
        return obj.GetNamedArray(name).Select(v => v.ValueType == JsonValueType.String ? v.GetString() : string.Empty).Where(s => !string.IsNullOrEmpty(s));
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
