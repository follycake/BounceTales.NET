using MeltySynth;

namespace BounceTales.Platform.Software;

public class MeltySynthProvider(Synthesizer synth) : IAudioProvider
{
    public Synthesizer Synth { get; } = synth;
    public MidiFileSequencer Sequencer { get; } = new(synth);

    public virtual void Initialize()
    {
    }

    public virtual void Update()
    {
    }

    public void SetVolumeLevel(int level)
    {
        Synth.MasterVolume = level / 100f;
    }

    public void Play(byte[] midiData, bool loop)
    {
        using MemoryStream stream = new(midiData);
        Sequencer.Play(new MidiFile(stream), loop);
    }

    public void Stop()
    {
        Sequencer.Stop();
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
