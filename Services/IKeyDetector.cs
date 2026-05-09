namespace AudioAnalyzer.Services;

public record KeyDetectionResult(
    string Key, 
    double Confidence, 
    double TuningOffsetCents,
    string AlternativeKey = ""
);

public interface IKeyDetector
{
    /// <summary>
    /// Detects the musical key of an audio file.
    /// </summary>
    Task<KeyDetectionResult> DetectKeyAsync(string filePath, IProgress<int>? progress = null);
    
    /// <summary>
    /// Detects key from pre-loaded mono samples.
    /// </summary>
    Task<KeyDetectionResult> DetectKeyAsync(float[] monoSamples, int sampleRate, IProgress<int>? progress = null);
}
