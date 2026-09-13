using BounceTales.Platform;
using BounceTales.Platform.Software;
using Godot;
using Godot.Collections;
using MeltySynth;
using System;
using System.Runtime.InteropServices;
using Gd = Godot;

namespace BounceTales.Godot;

public partial class Game : Node
{
    public static event Action PreUpdate;
    public static ShaderMaterial ClipMaterial { get; private set; }
    
    public static readonly KeyCode[] Keys =
    [
        KeyCode.UP,
        KeyCode.DOWN,
        KeyCode.LEFT,
        KeyCode.RIGHT,
        KeyCode.SOFTKEY_RIGHT,
        KeyCode.SOFTKEY_LEFT,
        KeyCode.SOFTKEY_MIDDLE,
        KeyCode.STAR,
        KeyCode.POUND,
        KeyCode.NUM0,
        KeyCode.NUM1,
        KeyCode.NUM2,
        KeyCode.NUM3,
        KeyCode.NUM4,
        KeyCode.NUM5,
        KeyCode.NUM6,
        KeyCode.NUM7,
        KeyCode.NUM8,
        KeyCode.NUM9
    ];
    
    private RMIDlet _midlet;
    private GodotGraphicsProvider _graphicsProvider;
    private MeltySynthProvider _synth;
    private AudioStreamGeneratorPlayback _playback;
    private float[] _audioBuffer;
    private bool _quitEntirely;
    
    public override void _Ready()
    {
        GetWindow().Title = "Bounce Tales";
        ClipMaterial = GD.Load<ShaderMaterial>("res://clip.tres");
        AudioStreamPlayer musicPlayer = GetNode<AudioStreamPlayer>("%MusicPlayer");
        
        _midlet = new RMIDlet();
        GodotSystemProvider systemProvider = new();
        _midlet.System = systemProvider;
        _graphicsProvider = new GodotGraphicsProvider(this);
        _midlet.Graphics = _graphicsProvider;
        
        SoundFont soundFont;
        void UseEmbeddedSoundFont()
        {
            using System.IO.MemoryStream stream = new(FileAccess.GetFileAsBytes("res://Chaos_Bank.sf2"));
            soundFont = new SoundFont(stream);
        }
        
        if (FileAccess.FileExists(SettingsUI.ConfigPath))
        {
            Dictionary data = SettingsUI.LoadSettings();
            GetNode<Control>("%TouchControls").Visible = data["EnableTouchControls"].AsBool();
            _graphicsProvider.Scale = Vector2.One * data["Scale"].AsInt32();
            systemProvider.Locale = StringManager.LocaleList[data["Locale"].AsInt32()];
            systemProvider.EnableCheats = data["EnableCheats"].AsBool();
            systemProvider.DebugOverlay = data["DebugOverlay"].AsBool();
            systemProvider.ObjectDrawDebug = data["ObjectDrawDebug"].AsBool();
            string jarPath = data["JarPath"].AsString();
            if (!string.IsNullOrWhiteSpace(jarPath))
                systemProvider.JarPath = jarPath;
            string soundFontPath = data["SoundFontPath"].AsString();
            if (!string.IsNullOrWhiteSpace(soundFontPath))
                soundFont = new SoundFont(data["SoundFontPath"].AsString());
            else
                UseEmbeddedSoundFont();
        }
        else
            UseEmbeddedSoundFont();
        
        GetNode<Control>("%Softkeys").CustomMinimumSize = new Vector2(0f, _graphicsProvider.Scale.Y * 16f);
        foreach (Node node in GetNode<Control>("%Keys").GetChildren())
            ((Control)node).CustomMinimumSize = new Vector2(32f, 32f) * _graphicsProvider.Scale;
        
        _synth = new MeltySynthProvider(new Synthesizer(soundFont, (int)((AudioStreamGenerator)musicPlayer.Stream).MixRate));
        _midlet.Audio = _synth;
        _midlet.Start();
        
        musicPlayer.Play();
        _playback = (AudioStreamGeneratorPlayback)musicPlayer.GetStreamPlayback();
        _audioBuffer = new float[_playback.GetFramesAvailable() * 2];
    }

    public override void _Process(double delta)
    {
        PreUpdate?.Invoke();
        if (!_midlet.Update())
        {
            Quit();
            return;
        }
        int audioFrames = _playback.GetFramesAvailable();
        if (audioFrames > 0)
        {
            Span<float> samples = _audioBuffer.AsSpan()[..(audioFrames * 2)];
            _synth.Sequencer.RenderInterleaved(samples);
            _playback.PushBuffer(MemoryMarshal.Cast<float, Vector2>(samples));
        }
        foreach (KeyCode key in Keys)
        {
            string name = key.ToString();
            if (Input.IsActionJustPressed(name))
                GameRuntime.PressKey(key);
            if (Input.IsActionJustReleased(name))
                GameRuntime.ReleaseKey(key);
        }
        if (Input.IsActionJustPressed("scale_down"))
            SetScale(Math.Clamp((int)(_graphicsProvider.Scale.X - 1), 1, 5));
        if (Input.IsActionJustPressed("scale_up"))
            SetScale(Math.Clamp((int)(_graphicsProvider.Scale.X + 1), 1, 5));
    }

    public void SetScale(int scale)
    {
        int w = _graphicsProvider.ScreenWidth;
        int h = _graphicsProvider.ScreenHeight;
        _graphicsProvider.Scale = new Vector2(scale, scale);
        _graphicsProvider.ScreenWidth = w;
        _graphicsProvider.ScreenHeight = h;
        GetWindow().MoveToCenter();
    }

    public void Quit()
    {
        SceneTree tree = GetTree();
        Dispose();
        if (_quitEntirely)
            tree.Quit();
        else
            tree.ChangeSceneToFile("res://settings_ui.tscn");
    }

    public override void _ExitTree()
    {
        Dispose();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _quitEntirely = true;
            _midlet.RequestQuit();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _midlet?.Dispose();
        _midlet = null;
        base.Dispose(disposing);
    }
}
