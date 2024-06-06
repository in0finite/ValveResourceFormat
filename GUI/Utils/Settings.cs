using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using ValveKeyValue;

namespace GUI.Utils
{
    static class Settings
    {
        private const int SettingsFileCurrentVersion = 8;
        private const int RecentFilesLimit = 20;

        [Flags]
        public enum QuickPreviewFlags : int
        {
            Enabled = 1 << 0,
            AutoPlaySounds = 1 << 1,
        }

        public class AppConfig
        {
            public List<string> GameSearchPaths { get; set; } = [];
            public string OpenDirectory { get; set; } = string.Empty;
            public string SaveDirectory { get; set; } = string.Empty;
            public List<string> BookmarkedFiles { get; set; } = [];
            public List<string> RecentFiles { get; set; } = new(RecentFilesLimit);
            public Dictionary<string, float[]> SavedCameras { get; set; } = [];
            public int MaxTextureSize { get; set; }
            public int FieldOfView { get; set; }
            public int AntiAliasingSamples { get; set; }
            public int WindowTop { get; set; }
            public int WindowLeft { get; set; }
            public int WindowWidth { get; set; }
            public int WindowHeight { get; set; }
            public int WindowState { get; set; } = (int)FormWindowState.Normal;
            public float Volume { get; set; }
            public int Vsync { get; set; }
            public int DisplayFps { get; set; }
            public int QuickFilePreview { get; set; }
            public int OpenExplorerOnStart { get; set; }
            public int _VERSION_DO_NOT_MODIFY { get; set; }
        }

        public static string SettingsFolder { get; private set; }
        private static string SettingsFilePath;

        public static AppConfig Config { get; set; } = new AppConfig();

        public static event EventHandler RefreshCamerasOnSave;
        public static void InvokeRefreshCamerasOnSave() => RefreshCamerasOnSave.Invoke(null, null);

        public static string GpuRendererAndDriver;

        public static void Load()
        {
            SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Source2Viewer");
            SettingsFilePath = Path.Combine(SettingsFolder, "settings.vdf");

            Directory.CreateDirectory(SettingsFolder);

            // Before 2023-09-08, settings were saved next to the executable
            var legacySettings = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "settings.txt");

            if (File.Exists(legacySettings) && !File.Exists(SettingsFilePath))
            {
                Log.Info(nameof(Settings), $"Moving '{legacySettings}' to '{SettingsFilePath}'.");

                File.Move(legacySettings, SettingsFilePath);
            }

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    using var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read);
                    Config = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<AppConfig>(stream, KVSerializerOptions.DefaultOptions);
                }
            }
            catch (Exception e)
            {
                Log.Error(nameof(Settings), $"Failed to parse '{SettingsFilePath}', is it corrupted?{Environment.NewLine}{e}");

                try
                {
                    var corruptedPath = Path.ChangeExtension(SettingsFilePath, $".corrupted-{DateTimeOffset.Now.ToUnixTimeSeconds()}.txt");
                    File.Move(SettingsFilePath, corruptedPath);

                    Log.Error(nameof(Settings), $"Corrupted '{Path.GetFileName(SettingsFilePath)}' has been renamed to '{Path.GetFileName(corruptedPath)}'.");

                    Save();
                }
                catch
                {
                    //
                }
            }

            var currentVersion = Config._VERSION_DO_NOT_MODIFY;

            if (currentVersion > SettingsFileCurrentVersion)
            {
                var result = MessageBox.Show(
                    $"Your current settings.vdf has a higher version ({currentVersion}) than currently supported ({SettingsFileCurrentVersion}). You likely ran an older version of Source 2 Viewer and your settings may get reset.\n\nDo you want to continue?",
                    "Source 2 Viewer downgraded",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    Environment.Exit(1);
                    return;
                }
            }

            Config.SavedCameras ??= [];
            Config.BookmarkedFiles ??= [];
            Config.RecentFiles ??= new(RecentFilesLimit);

            if (string.IsNullOrEmpty(Config.OpenDirectory) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Config.OpenDirectory = Path.Join(GetSteamPath(), "steamapps", "common");
            }

            if (Config.MaxTextureSize <= 0)
            {
                Config.MaxTextureSize = 1024;
            }
            else if (Config.MaxTextureSize > 10240)
            {
                Config.MaxTextureSize = 10240;
            }

            if (Config.FieldOfView <= 0)
            {
                Config.FieldOfView = 60;
            }
            else if (Config.FieldOfView >= 120)
            {
                Config.FieldOfView = 120;
            }

            Config.AntiAliasingSamples = Math.Clamp(Config.AntiAliasingSamples, 0, 64);
            Config.Volume = Math.Clamp(Config.Volume, 0f, 1f);

            if (currentVersion < 2) // version 2: added anti aliasing samples
            {
                Config.AntiAliasingSamples = 8;
            }

            if (currentVersion < 3) // version 3: added volume
            {
                Config.Volume = 0.5f;
            }

            if (currentVersion < 4) // version 4: added vsync
            {
                Config.Vsync = 1;
            }

            if (currentVersion < 5) // version 5: added display fps
            {
                Config.DisplayFps = 1;
            }

            if (currentVersion != SettingsFileCurrentVersion)
            {
                Log.Info(nameof(Settings), $"Settings version changed: {currentVersion} -> {SettingsFileCurrentVersion}");
            }

            Config._VERSION_DO_NOT_MODIFY = SettingsFileCurrentVersion;
        }

        public static void Save()
        {
            var tempFile = Path.GetTempFileName();

            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(stream, Config, nameof(ValveResourceFormat));
            }

            File.Move(tempFile, SettingsFilePath, overwrite: true);
        }

        public static void TrackRecentFile(string path)
        {
            Config.RecentFiles.Remove(path);
            Config.RecentFiles.Add(path);

            if (Config.RecentFiles.Count > RecentFilesLimit)
            {
                Config.RecentFiles.RemoveRange(0, Config.RecentFiles.Count - RecentFilesLimit);
            }

            Save();
        }

        public static void ClearRecentFiles()
        {
            Config.RecentFiles.Clear();
            Save();
        }

        public static string GetSteamPath()
        {
            try
            {
                using var key =
                    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam") ??
                    Registry.LocalMachine.OpenSubKey("SOFTWARE\\Valve\\Steam");

                if (key?.GetValue("SteamPath") is string steamPath)
                {
                    return Path.GetFullPath(steamPath);
                }
            }
            catch
            {
                // Don't care about registry exceptions
            }

            return string.Empty;
        }
    }
}
