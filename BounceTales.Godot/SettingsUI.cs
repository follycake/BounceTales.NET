using Godot;
using Godot.Collections;
using Gd = Godot;

namespace BounceTales.Godot;

public partial class SettingsUI : Control
{
    public const string ConfigPath = "user://settings.json";
    
	public override void _Ready()
	{
		GetWindow().Title = "Settings";
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
        GetWindow().Size = new Gd.Vector2I(RMIDlet.DefaultScreenWidth, RMIDlet.DefaultScreenHeight) * data["Scale"].AsInt32();
        DisplayServer.WindowSetVsyncMode(data["EnableVSync"].AsBool() ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        GetWindow().MoveToCenter();
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
