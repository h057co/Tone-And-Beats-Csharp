namespace AudioAnalyzer.Interfaces;

public interface IToneGeneratorService : IDisposable
{
    void Start(double frequency);
    void Trigger();
    void Stop();
    void SetFrequency(double frequency);
    void SetVolume(float volume);
    
    // New scale playback support
    void StartScalePlayback(double bpm, bool[] scaleNotes);
    void UpdateBpm(double bpm);
    void StopScalePlayback();
}
