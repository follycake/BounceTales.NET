namespace BounceTales.Microedition.Lcdui;

public abstract class Displayable
{
    public virtual int GetWidth()
    {
        return GameRuntime.MidLet.Graphics.ScreenWidth;
    }

    public virtual int GetHeight()
    {
        return GameRuntime.MidLet.Graphics.ScreenHeight;
    }
}
