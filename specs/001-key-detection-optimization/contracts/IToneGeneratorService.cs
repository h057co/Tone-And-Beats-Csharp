namespace AudioAnalyzer.Interfaces;

public interface IToneGeneratorService
{
    void Start(double frequency, double bpm);
    void Stop();
    void SetFrequency(double frequency);
    void SetBpm(double bpm);
    bool IsPlaying { get; }
}
