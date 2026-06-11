#nullable enable

using System;
using System.Linq;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed class SettingsService : ISettingsService
{
    private static IPropertySet SettingsStorage => ApplicationData.Current.LocalSettings.Values;

    private const string GeneralThemeKey = "General/Theme";
    private const string PlayerAutoResizeKey = "Player/AutoResize";
    private const string PlayerShowControlsKey = "Player/ShowControls";
    private const string PlayerControlsHideDelayKey = "Player/ControlsHideDelay";
    private const string PlayerLivelyPathKey = "Player/Lively/Path";
    private const string LibrariesUseIndexerKey = "Libraries/UseIndexer";
    private const string LibrariesSearchRemovableStorageKey = "Libraries/SearchRemovableStorage";
    private const string GeneralShowRecent = "General/ShowRecent";
    private const string GeneralEnqueueAllInFolder = "General/EnqueueAllInFolder";
    private const string GeneralRestorePlaybackPosition = "General/RestorePlaybackPosition";
    private const string AdvancedModeKey = "Advanced/IsEnabled";
    private const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
    private const string AdvancedMultipleInstancesKey = "Advanced/MultipleInstances";
    private const string GlobalArgumentsKey = "Values/GlobalArguments";
    private const string PersistentVolumeKey = "Values/Volume";
    private const string MaxVolumeKey = "Values/MaxVolume";
    private const string PersistentRepeatModeKey = "Values/RepeatMode";
    private const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";
    private const string PlayerShowChaptersKey = "Player/ShowChapters";
    private const string PrivacyPersistPlaybackPosition = "Privacy/PersistPlaybackPosition";

    private const string PlayerRewindStepKey = "Player/RewindStep";
    private const string PlayerFastForwardStepKey = "Player/FastForwardStep";
    private const string PlayerGestureTapKey = "Player/Gesture/Tap";
    private const string PlayerGestureSwipeUpKey = "Player/Gesture/SwipeUp";
    private const string PlayerGestureSwipeDownKey = "Player/Gesture/SwipeDown";
    private const string PlayerGestureSwipeLeftKey = "Player/Gesture/SwipeLeft";
    private const string PlayerGestureSwipeRightKey = "Player/Gesture/SwipeRight";
    private const string PlayerGestureSlideVerticalKey = "Player/Gesture/SlideVertical";
    private const string PlayerGestureSlideHorizontalKey = "Player/Gesture/SlideHorizontal";
    private const string PlayerGesturePressAndHoldKey = "Player/Gesture/PressAndHold";
    private const string JellyfinServerUrlKey = "Jellyfin/ServerUrl";
    private const string JellyfinUsernameKey = "Jellyfin/Username";
    private const string JellyfinUserIdKey = "Jellyfin/UserId";
    private const string JellyfinDeviceIdKey = "Jellyfin/DeviceId";
    private const string JellyfinVaultResource = "Screenbox/Jellyfin";

    public bool UseIndexer
    {
        get => GetValue<bool>(LibrariesUseIndexerKey);
        set => SetValue(LibrariesUseIndexerKey, value);
    }

    public ThemeOption Theme
    {
        get => (ThemeOption)GetValue<int>(GeneralThemeKey);
        set => SetValue(GeneralThemeKey, (int)value);
    }

    public PlayerAutoResizeOption PlayerAutoResize
    {
        get => (PlayerAutoResizeOption)GetValue<int>(PlayerAutoResizeKey);
        set => SetValue(PlayerAutoResizeKey, (int)value);
    }

    public int PersistentVolume
    {
        get => GetValue<int>(PersistentVolumeKey);
        set => SetValue(PersistentVolumeKey, value);
    }

    public string PersistentSubtitleLanguage
    {
        get => GetValue<string>(PersistentSubtitleLanguageKey) ?? string.Empty;
        set => SetValue(PersistentSubtitleLanguageKey, value);
    }

    public int MaxVolume
    {
        get => GetValue<int>(MaxVolumeKey);
        set => SetValue(MaxVolumeKey, value);
    }

    public bool ShowRecent
    {
        get => GetValue<bool>(GeneralShowRecent);
        set => SetValue(GeneralShowRecent, value);
    }

    public bool EnqueueAllFilesInFolder
    {
        get => GetValue<bool>(GeneralEnqueueAllInFolder);
        set => SetValue(GeneralEnqueueAllInFolder, value);
    }

    public bool RestorePlaybackPosition
    {
        get => GetValue<bool>(GeneralRestorePlaybackPosition);
        set => SetValue(GeneralRestorePlaybackPosition, value);
    }

    public bool PlayerShowControls
    {
        get => GetValue<bool>(PlayerShowControlsKey);
        set => SetValue(PlayerShowControlsKey, value);
    }

    public int PlayerControlsHideDelay
    {
        get => GetValue<int>(PlayerControlsHideDelayKey);
        set => SetValue(PlayerControlsHideDelayKey, value);
    }

    public bool SearchRemovableStorage
    {
        get => GetValue<bool>(LibrariesSearchRemovableStorageKey);
        set => SetValue(LibrariesSearchRemovableStorageKey, value);
    }

    public MediaPlaybackAutoRepeatMode PersistentRepeatMode
    {
        get => (MediaPlaybackAutoRepeatMode)GetValue<int>(PersistentRepeatModeKey);
        set => SetValue(PersistentRepeatModeKey, (int)value);
    }

    public string GlobalArguments
    {
        get => GetValue<string>(GlobalArgumentsKey) ?? string.Empty;
        set => SetValue(GlobalArgumentsKey, SanitizeArguments(value));
    }

    public bool AdvancedMode
    {
        get => GetValue<bool>(AdvancedModeKey);
        set => SetValue(AdvancedModeKey, value);
    }

    public VideoUpscaleOption VideoUpscale
    {
        get => (VideoUpscaleOption)GetValue<int>(AdvancedVideoUpscaleKey);
        set => SetValue(AdvancedVideoUpscaleKey, (int)value);
    }

    public bool UseMultipleInstances
    {
        get => GetValue<bool>(AdvancedMultipleInstancesKey);
        set => SetValue(AdvancedMultipleInstancesKey, value);
    }

    public string LivelyActivePath
    {
        get => GetValue<string>(PlayerLivelyPathKey) ?? string.Empty;
        set => SetValue(PlayerLivelyPathKey, value);
    }

    public bool PlayerShowChapters
    {
        get => GetValue<bool>(PlayerShowChaptersKey);
        set => SetValue(PlayerShowChaptersKey, value);
    }

    public bool PersistPlaybackPosition
    {
        get => GetValue<bool>(PrivacyPersistPlaybackPosition);
        set => SetValue(PrivacyPersistPlaybackPosition, value);
    }

    public int PlayerRewindStep
    {
        get => GetValue<int>(PlayerRewindStepKey);
        set => SetValue(PlayerRewindStepKey, value);
    }

    public int PlayerFastForwardStep
    {
        get => GetValue<int>(PlayerFastForwardStepKey);
        set => SetValue(PlayerFastForwardStepKey, value);
    }

    public PlaybackActionKind PlayerGestureTap
    {
        get => (PlaybackActionKind)GetValue<int>(PlayerGestureTapKey);
        set => SetValue(PlayerGestureTapKey, (int)value);
    }

    public PlaybackActionKind PlayerGestureSwipeUp
    {
        get => (PlaybackActionKind)GetValue<int>(PlayerGestureSwipeUpKey);
        set => SetValue(PlayerGestureSwipeUpKey, (int)value);
    }

    public PlaybackActionKind PlayerGestureSwipeDown
    {
        get => (PlaybackActionKind)GetValue<int>(PlayerGestureSwipeDownKey);
        set => SetValue(PlayerGestureSwipeDownKey, (int)value);
    }

    public PlaybackActionKind PlayerGestureSwipeLeft
    {
        get => (PlaybackActionKind)GetValue<int>(PlayerGestureSwipeLeftKey);
        set => SetValue(PlayerGestureSwipeLeftKey, (int)value);
    }

    public PlaybackActionKind PlayerGestureSwipeRight
    {
        get => (PlaybackActionKind)GetValue<int>(PlayerGestureSwipeRightKey);
        set => SetValue(PlayerGestureSwipeRightKey, (int)value);
    }

    public bool PlayerGestureSlideVertical
    {
        get => GetValue<bool>(PlayerGestureSlideVerticalKey);
        set => SetValue(PlayerGestureSlideVerticalKey, value);
    }

    public bool PlayerGestureSlideHorizontal
    {
        get => GetValue<bool>(PlayerGestureSlideHorizontalKey);
        set => SetValue(PlayerGestureSlideHorizontalKey, value);
    }

    public bool PlayerGesturePressAndHold
    {
        get => GetValue<bool>(PlayerGesturePressAndHoldKey);
        set => SetValue(PlayerGesturePressAndHoldKey, value);
    }

    public string JellyfinServerUrl
    {
        get => GetValue<string>(JellyfinServerUrlKey) ?? string.Empty;
        set => SetValue(JellyfinServerUrlKey, value?.TrimEnd('/') ?? string.Empty);
    }

    public string JellyfinUsername
    {
        get => GetValue<string>(JellyfinUsernameKey) ?? string.Empty;
        set => SetValue(JellyfinUsernameKey, value ?? string.Empty);
    }

    public string JellyfinAccessToken
    {
        get => GetPasswordFromVault(JellyfinVaultResource, JellyfinUserId) ?? string.Empty;
        set => SetPasswordInVault(JellyfinVaultResource, JellyfinUserId, value ?? string.Empty);
    }

    public string JellyfinUserId
    {
        get => GetValue<string>(JellyfinUserIdKey) ?? string.Empty;
        set => SetValue(JellyfinUserIdKey, value ?? string.Empty);
    }

    public string JellyfinDeviceId
    {
        get => GetValue<string>(JellyfinDeviceIdKey) ?? string.Empty;
        set => SetValue(JellyfinDeviceIdKey, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value);
    }

    public SettingsService()
    {
        SetDefault(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
        SetDefault(PlayerShowControlsKey, true);
        SetDefault(PlayerControlsHideDelayKey, 3);
        SetDefault(PersistentVolumeKey, 100);
        SetDefault(MaxVolumeKey, 100);
        SetDefault(LibrariesUseIndexerKey, true);
        SetDefault(LibrariesSearchRemovableStorageKey, true);
        SetDefault(GeneralShowRecent, true);
        SetDefault(PersistentRepeatModeKey, (int)MediaPlaybackAutoRepeatMode.None);
        SetDefault(AdvancedModeKey, false);
        SetDefault(AdvancedVideoUpscaleKey, (int)VideoUpscaleOption.Linear);
        SetDefault(AdvancedMultipleInstancesKey, false);
        SetDefault(GlobalArgumentsKey, string.Empty);
        SetDefault(PlayerShowChaptersKey, true);
        SetDefault(PrivacyPersistPlaybackPosition, true);
        SetDefault(PlayerRewindStepKey, 5);
        SetDefault(PlayerFastForwardStepKey, 5);
        SetDefault(PlayerGestureTapKey, (int)PlaybackActionKind.PlayPause);
        SetDefault(PlayerGestureSwipeUpKey, (int)PlaybackActionKind.IncreaseVolume);
        SetDefault(PlayerGestureSwipeDownKey, (int)PlaybackActionKind.DecreaseVolume);
        SetDefault(PlayerGestureSwipeLeftKey, (int)PlaybackActionKind.Rewind);
        SetDefault(PlayerGestureSwipeRightKey, (int)PlaybackActionKind.FastForward);
        SetDefault(PlayerGestureSlideVerticalKey, true);
        SetDefault(PlayerGestureSlideHorizontalKey, true);
        SetDefault(PlayerGesturePressAndHoldKey, true);
        SetDefault(JellyfinServerUrlKey, string.Empty);
        SetDefault(JellyfinUsernameKey, string.Empty);
        SetDefault(JellyfinUserIdKey, string.Empty);
        SetDefault(JellyfinDeviceIdKey, Guid.NewGuid().ToString());

        // Device family specific overrides
        if (SystemInformation.IsXbox)
        {
            SetValue(PlayerShowControlsKey, true);
            SetValue(PlayerGestureTapKey, (int)PlaybackActionKind.None);
            SetValue(PlayerGestureSwipeUpKey, (int)PlaybackActionKind.None);
            SetValue(PlayerGestureSwipeDownKey, (int)PlaybackActionKind.None);
            SetValue(PlayerGestureSwipeLeftKey, (int)PlaybackActionKind.None);
            SetValue(PlayerGestureSwipeRightKey, (int)PlaybackActionKind.None);
            SetValue(PlayerGestureSlideVerticalKey, false);
            SetValue(PlayerGestureSlideHorizontalKey, false);
            SetValue(PlayerGesturePressAndHoldKey, false);
        }
    }

    private static T? GetValue<T>(string key)
    {
        if (SettingsStorage.TryGetValue(key, out object value))
        {
            return (T)value;
        }

        return default;
    }

    private static void SetValue<T>(string key, T value)
    {
        SettingsStorage[key] = value;
    }

    private static void SetDefault<T>(string key, T value)
    {
        if (SettingsStorage.ContainsKey(key) && SettingsStorage[key] is T) return;
        SettingsStorage[key] = value;
    }

    private static string? GetPasswordFromVault(string resource, string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        try
        {
            PasswordCredential credential = new PasswordVault().Retrieve(resource, userName);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void SetPasswordInVault(string resource, string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName)) return;
        try
        {
            PasswordVault vault = new();
            foreach (PasswordCredential credential in vault.FindAllByResource(resource).Where(c => c.UserName == userName))
            {
                vault.Remove(credential);
            }

            if (!string.IsNullOrWhiteSpace(password)) vault.Add(new PasswordCredential(resource, userName, password));
        }
        catch (Exception)
        {
            // Keep non-secret connection state even if PasswordVault is unavailable.
        }
    }

    private static string SanitizeArguments(string raw)
    {
        string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith('-') && s != "--").ToArray();
        return string.Join(' ', args);
    }
}
