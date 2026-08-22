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
        [KeyboardKey.E] = KeyCode.POUND
    };
    
    private static void Main()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
        
        Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
        Rl.InitWindow(RMIDlet.DefaultScreenWidth, RMIDlet.DefaultScreenHeight, "Bounce Tales");
        
        Rl.SetExitKey(KeyboardKey.Delete);
        
        Rl.InitAudioDevice();
        Rl.SetAudioStreamBufferSizeDefault(AudioBufferSize);
        
        short[] buffer = new short[2 * AudioBufferSize];
        AudioStream stream = Rl.LoadAudioStream(SampleRate, 16, 2);
        Rl.PlayAudioStream(stream);
        
        using RMIDlet midlet = new();
        MeltySynthProvider synth = new(new Synthesizer("GeneralUser-GS.sf2", SampleRate));
        midlet.Audio = synth;
        midlet.Start();
        while (true)
        {
            midlet.Graphics.ScreenWidth = Rl.GetScreenWidth();
            midlet.Graphics.ScreenHeight = Rl.GetScreenHeight();
            if (!midlet.Update())
                break;

            if (Rl.IsAudioStreamProcessed(stream))
            {
                synth.Sequencer.RenderInterleavedInt16(buffer);
                Rl.UpdateAudioStream(stream, buffer, AudioBufferSize);
            }

            Rl.BeginDrawing();
            SoftwareImage image = ((SoftwareGraphicsProvider)midlet.Graphics).ScreenImage;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Microedition.Lcdui.Color color = image[x, y];
                    Rl.DrawPixel(x, y, new Color(color.R, color.G, color.B, color.A));
                }
            }
            Rl.EndDrawing();

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
