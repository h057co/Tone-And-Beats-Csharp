# Contract: IKeyDetector

## Service Interface

```csharp
namespace AudioAnalyzer.Services;

public interface IKeyDetector
{
    /// <summary>
    /// Detects the musical key of an audio file.
    /// </summary>
    /// <param name="filePath">Path to the audio file.</param>
    /// <returns>A KeyDetectionResult containing the key, confidence, and tuning offset.</returns>
    Task<KeyDetectionResult> DetectKeyAsync(string filePath);
    
    /// <summary>
    /// Detects the global tuning offset of the audio.
    /// </summary>
    /// <param name="samples">PCM audio samples.</param>
    /// <param name="sampleRate">Sample rate of the audio.</param>
    /// <returns>Offset in semitones (e.g., -0.32).</returns>
    double DetectTuningOffset(float[] samples, int sampleRate);
}
```

## Data Transfer Objects

```csharp
public record KeyDetectionResult(
    string Key, 
    double Confidence, 
    double TuningOffsetCents
);
```
