using BounceTales.Platform;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace BounceTales.Raylib;

public class RaylibGraphicsProvider : BasicGraphicsProvider
{
    public int Scale { get; set; } = 1;
    public int WindowWidth => ScreenWidth * Scale;
    public int WindowHeight => ScreenHeight * Scale;

    public override void FlushGraphics()
    {
        ScreenWidth *= Scale;
        ScreenHeight *= Scale;
        base.FlushGraphics();
        ((RaylibGraphics)_graphics)._scale = Scale;
        RaylibGraphics.EndDrawing();

        Rl.BeginDrawing();
        Rl.ClearBackground(Color.Black);

        Texture2D tex = ((RaylibRenderImage)_renderTarget)._texture;
        Rl.DrawTextureRec(tex, new Rectangle(0f, 0f, tex.Width, -tex.Height), System.Numerics.Vector2.Zero, Color.White);

        //Rl.DrawFPS(0, 0);
        Rl.EndDrawing();

        ScreenWidth = Rl.GetScreenWidth() / Scale;
        ScreenHeight = Rl.GetScreenHeight() / Scale;
    }

    public override Microedition.Lcdui.Image CreateImage(ReadOnlySpan<Microedition.Lcdui.Color32> data, int width, int height)
    {
        return new RaylibImage(data, width, height);
    }

    public override Microedition.Lcdui.Image CreateRenderImage(int width, int height)
    {
        return new RaylibRenderImage(width, height);
    }
}
