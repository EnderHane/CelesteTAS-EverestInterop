using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace CelesteStudio;

public class Settings {
    private const string Path = "Celeste Studio.json";
    public static Settings Instance { get; private set; } = new();
    private static bool saving;
    private static FileSystemWatcher watcher;

    [JsonInclude] public int LocationX = 100;
    [JsonInclude] public int LocationY = 100;

    [JsonIgnore]
    public Point Location {
        get => new(LocationX, LocationY);
        set {
            LocationX = value.X;
            LocationY = value.Y;
        }
    }

    [JsonInclude] public int Width = 400;
    [JsonInclude] public int Height = 800;

    [JsonIgnore]
    public Size Size {
        get => new(Width, Height);
        set {
            Width = value.Width;
            Height = value.Height;
        }
    }

    [JsonInclude] public string FontName = "Courier New";
    [JsonInclude] public float FontSize = 14.25f;
    [JsonInclude] public byte FontStyle;

    [JsonIgnore]
    public Font Font {
        get {
            try {
                return new Font(new FontFamily(FontName), FontSize, (FontStyle) FontStyle);
            } catch {
                FontName = "Courier New";
                FontSize = 14.25f;
                FontStyle = 0;
                return new Font(new FontFamily(FontName), FontSize, (FontStyle) FontStyle);
            }
        }
        set {
            FontName = value.FontFamily.Name;
            FontSize = value.Size;
            FontStyle = (byte) value.Style;
        }
    }

    [JsonInclude] public bool SendInputsToCeleste = true;
    [JsonInclude] public bool ShowGameInfo = true;
    [JsonInclude] public bool AutoRemoveMutuallyExclusiveActions = true;
    [JsonInclude] public bool AlwaysOnTop = false;
    [JsonInclude] public bool AutoBackupEnabled = true;
    [JsonInclude] public int AutoBackupRate = 1;
    [JsonInclude] public int AutoBackupCount = 100;
    [JsonInclude] public bool FindMatchCase;

    [JsonInclude] public string LastFileName = "";

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [JsonInclude] public List<string> RecentFiles = new();

    [JsonInclude][JsonPropertyName("Theme")] public string Theme_ = ThemesType.Light.ToString();

    [JsonIgnore]
    public ThemesType ThemesType {
        get {
            int index = typeof(ThemesType).GetEnumNames().ToList().IndexOf(Theme_);
            if (index == -1) {
                index = 0;
            }

            return (ThemesType) index;
        }

        set => Theme_ = value.ToString();
    }

    public static void StartWatcher() {
        watcher = new();
        watcher.Path = Directory.GetCurrentDirectory();
        watcher.Filter = System.IO.Path.GetFileName(Path);
        watcher.Changed += (_, _) => {
            if (!saving && File.Exists(Path)) {
                Thread.Sleep(100);
                try {
                    Studio.Instance.Invoke(Load);
                } catch {
                    // ignore
                }
            }
        };

        try {
            watcher.EnableRaisingEvents = true;
        } catch {
            watcher.Dispose();
            watcher = null;
        }
    }

    public static void StopWatcher() {
        watcher?.Dispose();
        watcher = null;
    }

    private static readonly JsonSerializerOptions Option_ = new() {
        WriteIndented = true,
    };

    public static void Load() {
        if (File.Exists(Path)) {
            try {
                string jsonString = File.ReadAllText(Path);
                Dictionary<string, JsonElement> conf = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
                Instance = conf["Settings"].Deserialize<Settings>();
                Themes.Load(
                    conf["LightThemes"].Deserialize<LightTheme>(),
                    conf["DarkThemes"].Deserialize<DarkTheme>(),
                    conf["CustomThemes"].Deserialize<CustomTheme>()
                );
            } catch {
                // ignore
            }
        } else {
            Save();
        }
    }

    public static void Save() {
        saving = true;

        try {
            using StreamWriter writer = File.CreateText(Path);
            string jsonString = JsonSerializer.Serialize(new Dictionary<string, object> {
                { "Settings", Instance },
                { "LightThemes", Themes.Light },
                { "DarkThemes", Themes.Dark },
                { "CustomThemes", Themes.Custom },
            }, Option_);
            writer.Write(jsonString);
        } catch {
            // ignore
        }

        saving = false;
    }
}