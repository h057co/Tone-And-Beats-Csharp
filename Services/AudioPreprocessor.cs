using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioAnalyzer.Services;

public class AudioPreprocessor
{
    /// <summary>
    /// Resamples the input audio to 44.1kHz Mono, as required by the Essentia RhythmExtractor2013.
    /// </summary>
    public async Task<string?> PrepareForEssentiaAsync(string inputFilePath)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_prep.wav");

        try
        {
            await Task.Run(() =>
            {
                using var reader = new AudioFileReader(inputFilePath);
                
                // Segment extraction: start at 30s if the track is long enough
                if (reader.TotalTime.TotalSeconds > 45)
                {
                    // For tracks < 90s, ensure we don't seek past the end
                    double startSec = Math.Min(30, reader.TotalTime.TotalSeconds - 15);
                    reader.CurrentTime = TimeSpan.FromSeconds(startSec);
                }
                
                // Resample to 44100
                var resampler = new WdlResamplingSampleProvider(reader, 44100);
                
                // Convert to Mono if needed
                var mono = resampler.ToMono();

                // Take up to 60 seconds of audio
                var segment = mono.Take(TimeSpan.FromSeconds(60));

                // Write to WAV
                WaveFileWriter.CreateWaveFile16(tempPath, segment);
            });

            return tempPath;
        }
        catch (Exception ex)
        {
            LoggerService.Log($"AudioPreprocessor.PrepareForEssentiaAsync failed: {ex.Message}");
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return null;
        }
    }
}
