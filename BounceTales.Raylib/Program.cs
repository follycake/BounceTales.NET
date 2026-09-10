using System.Diagnostics;
using BounceTales.Platform.Software;
using MeltySynth;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace BounceTales.Raylib;

internal static class Program
{
    public const int SampleRate = 44100;
    public const int AudioBufferSize = 4096;
    
    private static readonly Dictionary<KeyboardKey, KeyCode> _keyMap = new()
    {
        [KeyboardKey.Up] = KeyCode.UP,
        [KeyboardKey.Down] = KeyCode.DOWN,
        [KeyboardKey.Left] = KeyCode.LEFT,
        [KeyboardKey.Right] = KeyCode.RIGHT,

        [KeyboardKey.W] = KeyCode.UP,
        [KeyboardKey.S] = KeyCode.DOWN,
        [KeyboardKey.A] = KeyCode.LEFT,
        [KeyboardKey.D] = KeyCode.RIGHT,

        [KeyboardKey.Z] = KeyCode.SOFTKEY_LEFT,
        [KeyboardKey.X] = KeyCode.SOFTKEY_MIDDLE,
        [KeyboardKey.C] = KeyCode.SOFTKEY_RIGHT,

        [KeyboardKey.Space] = KeyCode.SOFTKEY_MIDDLE,
        [KeyboardKey.Enter] = KeyCode.SOFTKEY_MIDDLE,
        [KeyboardKey.Escape] = KeyCode.SOFTKEY_RIGHT,

        [KeyboardKey.Q] = KeyCode.STAR,
        [KeyboardKey.E] = KeyCode.POUND,
        
        [KeyboardKey.Zero] = KeyCode.NUM0,
        [KeyboardKey.One] = KeyCode.NUM1,
        [KeyboardKey.Two] = KeyCode.NUM2,
        [KeyboardKey.Three] = KeyCode.NUM3,
        [KeyboardKey.Four] = KeyCode.NUM4,
        [KeyboardKey.Five] = KeyCode.NUM5,
        [KeyboardKey.Six] = KeyCode.NUM6,
        [KeyboardKey.Seven] = KeyCode.NUM7,
        [KeyboardKey.Eight] = KeyCode.NUM8,
        [KeyboardKey.Nine] = KeyCode.NUM9
    };
    
    private static void Main()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());

        RaylibGraphicsProvider graphicsProvider = new();
        Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
        Rl.InitWindow(graphicsProvider.WindowWidth, graphicsProvider.WindowHeight, "Bounce Tales");
        
        Rl.SetExitKey(KeyboardKey.Delete);
        
        Rl.InitAudioDevice();
        Rl.SetAudioStreamBufferSizeDefault(AudioBufferSize);
        
        short[] buffer = new short[2 * AudioBufferSize];
        AudioStream stream = Rl.LoadAudioStream(SampleRate, 16, 2);
        Rl.PlayAudioStream(stream);
        
        using RMIDlet midlet = new();
        MeltySynthProvider synth = new(new Synthesizer("Chaos_Bank.sf2", SampleRate));
        midlet.Graphics = graphicsProvider;
        midlet.Audio = synth;
        midlet.Start();
        while (midlet.Update())
        {
            if (Rl.IsKeyPressed(KeyboardKey.O))
            {
                graphicsProvider.Scale = Math.Clamp(graphicsProvider.Scale - 1, 1, 5);
                Rl.SetWindowSize(graphicsProvider.WindowWidth, graphicsProvider.WindowHeight);
            }
            if (Rl.IsKeyPressed(KeyboardKey.P))
            {
                graphicsProvider.Scale = Math.Clamp(graphicsProvider.Scale + 1, 1, 5);
                Rl.SetWindowSize(graphicsProvider.WindowWidth, graphicsProvider.WindowHeight);
            }

            if (Rl.IsAudioStreamProcessed(stream))
            {
                synth.Sequencer.RenderInterleavedInt16(buffer);
                Rl.UpdateAudioStream(stream, buffer, AudioBufferSize);
            }

            foreach (KeyValuePair<KeyboardKey, KeyCode> pair in _keyMap)
            {
                if (Rl.IsKeyPressed(pair.Key))
                    GameRuntime.PressKey(pair.Value);
                if (Rl.IsKeyReleased(pair.Key))
                    GameRuntime.ReleaseKey(pair.Value);
            }

            if (Rl.WindowShouldClose())
                midlet.RequestQuit();
        }
        Rl.StopAudioStream(stream);
        Rl.UnloadAudioStream(stream);
        Rl.CloseAudioDevice();
        Rl.CloseWindow();
    }
}
