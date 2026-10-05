using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using MangakaSim;
using Steamworks;

namespace MangakaGame;

/// <summary>Steam connection for achievements (Q68, Steamworks.NET). The game runs the same without Steam:
/// when Steam is off, missing or not running, every unlock stays recorded in the save and reaches Steam on a later run.</summary>
public sealed class SteamAchievements : IAchievementSink
{
    /// <summary>Valve's free Spacewar test app. Used for development until Mangaka Days has its own app ID.</summary>
    public const uint TestAppId = 480;
    /// <summary>The real app ID once the Steam Direct fee is paid. Zero keeps packaged builds away from Steam.</summary>
    public const uint ReleaseAppId = 0;

    public bool Connected { get; private set; }
    public uint AppId { get; private set; }
    public string Status { get; private set; } = "Steam is off for this run.";
    public Action<string>? Log;

    private readonly HashSet<string> _unknown = new();
    private bool _storePending;
    private double _storeRetry;

    /// <summary>Decides whether this run talks to Steam. Returns false when Steam relaunches the game itself,
    /// in which case this copy should quit at once.</summary>
    public bool Start(IReadOnlyCollection<string> args)
    {
        var exported = OS.HasFeature("template");
        var wanted = args.Contains("--steam") || args.Contains("--steam-smoke");
        uint appId;
        if (ReleaseAppId != 0 && exported && !args.Contains("--no-steam")) appId = ReleaseAppId;
        else if (wanted) appId = ReleaseAppId != 0 ? ReleaseAppId : TestAppId;
        else return true;
        try
        {
            UseBundledLibrary();
            if (!Packsize.Test() || !DllCheck.Test()) { Status = "The Steam library does not match this build."; return true; }
            // A store copy started outside Steam is relaunched through Steam so ownership and the overlay work.
            if (appId == ReleaseAppId && exported && SteamAPI.RestartAppIfNecessary(new AppId_t(appId))) return false;
            // Development runs name the app here instead of a steam_appid.txt beside the executable.
            if (!exported || appId != ReleaseAppId) { OS.SetEnvironment("SteamAppId", appId.ToString()); OS.SetEnvironment("SteamGameId", appId.ToString()); }
            if (!SteamAPI.Init()) { Status = "Steam is not running or not signed in."; return true; }
        }
        catch (Exception ex) // A missing or broken steam_api64.dll must never stop the game from starting.
        {
            Status = "The Steam library could not be loaded."; Log?.Invoke($"steam {ex.GetType().Name}: {ex.Message}");
            return true;
        }
        Connected = true; AppId = appId;
        Status = appId == TestAppId ? "Connected to Steam with Valve's test app (480)." : "Connected to Steam.";
        Log?.Invoke($"steam connected app {appId}");
        return true;
    }

    private static bool _resolverSet;
    /// <summary>Godot loads game code in its own load context, which does not search the build folder for native files,
    /// so steam_api64.dll is looked up in the places this project puts it.</summary>
    private static void UseBundledLibrary()
    {
        if (_resolverSet) return;
        _resolverSet = true;
        string?[] folders = [Path.GetDirectoryName(OS.GetExecutablePath()), Path.GetDirectoryName(typeof(SteamAchievements).Assembly.Location),
            OS.HasFeature("template") ? null : ProjectSettings.GlobalizePath("res://ThirdParty/Steamworks.NET")];
        NativeLibrary.SetDllImportResolver(typeof(SteamAPI).Assembly, (name, _, _) =>
        {
            if (!name.StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)) return IntPtr.Zero;
            foreach (var folder in folders)
                if (!string.IsNullOrEmpty(folder) && NativeLibrary.TryLoad(Path.Combine(folder, "steam_api64.dll"), out var handle)) return handle;
            return IntPtr.Zero;
        });
    }

    /// <summary>Steam loads the player's achievements shortly after start; until then unlocks wait and are retried.</summary>
    public bool StatsReady => Connected && SteamUserStats.GetNumAchievements() > 0;

    public bool Unlock(string key)
    {
        if (!Connected) return true;
        if (!SteamUserStats.GetAchievement(key, out var done))
        {
            if (!StatsReady) return false;
            // The app has no achievement by this name (always so on test app 480). Nothing to retry.
            if (_unknown.Add(key)) Log?.Invoke($"steam has no achievement {key} in app {AppId}");
            return true;
        }
        if (done) return true;
        if (!SteamUserStats.SetAchievement(key)) return false;
        _storePending = true;
        Log?.Invoke($"steam unlocked {key}");
        return true;
    }

    /// <summary>Called every frame: runs Steam callbacks and sends new unlocks, which shows Steam's own pop-up.</summary>
    public void Update(double delta)
    {
        if (!Connected) return;
        SteamAPI.RunCallbacks();
        if (!_storePending) return;
        _storeRetry -= delta;
        if (_storeRetry > 0) return;
        if (SteamUserStats.StoreStats()) _storePending = false;
        else _storeRetry = 2;
    }

    public void Stop()
    {
        if (!Connected) return;
        if (_storePending) SteamUserStats.StoreStats();
        SteamAPI.Shutdown();
        Connected = false;
    }
}
