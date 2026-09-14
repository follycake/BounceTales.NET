using Godot;
using Godot.Collections;
using Gd = Godot;

namespace BounceTales.Godot.UI;

public partial class SettingsUI : Control
{
    public const string ConfigPath = "user://settings.json";
    
	public override void _Ready()
    {
        Window window = GetWindow();
        window.Title = "Settings";
        window.Size = new Gd.Vector2I(RMIDlet.DefaultScreenWidth * 2, RMIDlet.DefaultScreenHeight * 2);
        window.MoveToCenter();
        window.SetContentScaleMode(Window.ContentScaleModeEnum.CanvasItems);
        
#if GODOT_ANDROID
        GetNode<CheckButton>("%EnableTouchControls").ButtonPressed = true;
#endif
        
        OptionButton locale = GetNode<OptionButton>("%Locale");
        locale.Clear();
        foreach (string str in StringManager.LocaleList)
            locale.AddItem(str);
        locale.Select(System.Array.IndexOf(StringManager.LocaleList, "en-US"));
        Load();
    }

    public static Dictionary LoadSettings()
    {
        return Json.ParseString(FileAccess.GetFileAsString(ConfigPath)).AsGodotDictionary();
    }

    private void Load()
    {
        if (!FileAccess.FileExists(ConfigPath))
            return;
        Dictionary data = LoadSettings();
        foreach (Node node in GetTree().GetNodesInGroup("config"))
        {
            if (!data.ContainsKey(node.Name))
                continue;
            StringName property = GetBindProperty(node);
            node.Set(property, data[node.Name]);
        }
    }

    private Dictionary Save()
    {
        Dictionary data = [];
        foreach (Node node in GetTree().GetNodesInGroup("config"))
        {
            StringName property = GetBindProperty(node);
            data[node.Name] = node.Get(property);
        }
        using FileAccess f = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        f.StoreString(Json.Stringify(data, "\t"));
        return data;
    }

    static StringName GetBindProperty(Node node)
    {
        if (node.IsClass(nameof(LineEdit)))
            return LineEdit.PropertyName.Text;
        if (node.IsClass(nameof(OptionButton)))
            return OptionButton.PropertyName.Selected;
        if (node.IsClass(nameof(BaseButton)))
            return BaseButton.PropertyName.ButtonPressed;
        if (node.IsClass(nameof(Range)))
            return Range.PropertyName.Value;
        return null;
    }

    public static void OpenUserFolder()
    {
        OS.ShellShowInFileManager(ProjectSettings.GlobalizePath("user://"));
    }
    
    public void Play()
    {
        Dictionary data = Save();
        Window window = GetWindow();
        window.Size = new Gd.Vector2I(RMIDlet.DefaultScreenWidth, RMIDlet.DefaultScreenHeight) * data["Scale"].AsInt32();
        window.MoveToCenter();
        window.SetContentScaleMode(Window.ContentScaleModeEnum.Disabled);
        DisplayServer.WindowSetVsyncMode(data["EnableVSync"].AsBool() ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        GetTree().ChangeSceneToFile("res://game.tscn");
    }

	public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Save();
            GetTree().Quit();
        }
    }
}
