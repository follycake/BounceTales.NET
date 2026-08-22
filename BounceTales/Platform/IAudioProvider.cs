namespace BounceTales.Platform;

public interface IAudioProvider : IProvider
{
    void SetVolumeLevel(int level);
    void Play(byte[] midiData, bool loop);
    void Update();
    void Stop();
}
