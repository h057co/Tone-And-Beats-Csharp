using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services;

/// <summary>
/// Orchestrates the complete audio analysis workflow.
/// Loads audio once, distributes to all analyzers, and refines results.
/// </summary>
public class AudioAnalysisPipeline : IAudioAnalysisPipeline
{
    private readonly IBpmDetectorService _bpmDetector;
    private readonly IKeyDetector _keyDetector;
    private readonly IWaveformAnalyzerService _waveformAnalyzer;
    private readonly ILoudnessAnalyzerService _loudnessAnalyzer;

    public AudioAnalysisPipeline(
        IBpmDetectorService bpmDetector,
        IKeyDetector keyDetector,
        IWaveformAnalyzerService waveformAnalyzer,
        ILoudnessAnalyzerService loudnessAnalyzer)
    {
        _bpmDetector = bpmDetector ?? throw new ArgumentNullException(nameof(bpmDetector));
        _keyDetector = keyDetector ?? throw new ArgumentNullException(nameof(keyDetector));
        _waveformAnalyzer = waveformAnalyzer ?? throw new ArgumentNullException(nameof(waveformAnalyzer));
        _loudnessAnalyzer = loudnessAnalyzer ?? throw new ArgumentNullException(nameof(loudnessAnalyzer));
    }

    public async Task<AudioAnalysisReport> AnalyzeAudioAsync(string filePath, IProgress<int>? progress = null, BpmRangeProfile profile = BpmRangeProfile.Auto)
    {
        var report = new AudioAnalysisReport();

        try
        {
            LoggerService.Log($"AudioAnalysisPipeline - Starting analysis: {filePath}");
            progress?.Report(0);

            // === STEP 1 & 2: Start parallel tasks immediately ===
            // File-based analyzers can start without waiting for audio loading
            // Variables to track individual progress
            int currentBpmProg = 0, currentKeyProg = 0, currentWaveProg = 0, currentLoudProg = 0;
            object progressLock = new object();

            void UpdateOverallProgress()
            {
                lock (progressLock)
                {
                    // Calculate average progress of the 4 tasks (0 to 100 each)
                    int avgProgress = (currentBpmProg + currentKeyProg + currentWaveProg + currentLoudProg) / 4;
                    // Map this average (0-100) to the remaining 10-90% of the overall progress
                    int overallProgress = 10 + (int)(avgProgress * 0.8);
                    progress?.Report(overallProgress);
                }
            }

            var bpmProgress = new Progress<int>(p => { currentBpmProg = p; UpdateOverallProgress(); });
            var loudnessProgress = new Progress<int>(p => { currentLoudProg = p; UpdateOverallProgress(); });

            // BPM and Loudness only need the filePath
            var bpmTask = _bpmDetector.DetectFullAnalysisAsync(filePath, bpmProgress, profile);
            var loudnessTask = _loudnessAnalyzer.AnalyzeAsync(filePath, loudnessProgress);

            // Audio loading task (needed for Key and Waveform)
            var audioProvider = new AudioDataProvider();
            var audioLoadTask = Task.Run(() => audioProvider.LoadMono(filePath));

            // Wait for audio loading to start Key and Waveform analysis
            var (monoSamples, sampleRate) = await audioLoadTask;
            LoggerService.Log($"AudioAnalysisPipeline - Audio loaded: {monoSamples.Length} samples @ {sampleRate}Hz");

            var keyProgress = new Progress<int>(p => { currentKeyProg = p; UpdateOverallProgress(); });
            var waveformProgress = new Progress<int>(p => { currentWaveProg = p; UpdateOverallProgress(); });

            var keyTask = _keyDetector.DetectKeyAsync(monoSamples, sampleRate, keyProgress);
            var waveformTask = _waveformAnalyzer.AnalyzeAsync(monoSamples, sampleRate, null, waveformProgress);

            BpmAnalysisResult? bpmResult = null;
            KeyDetectionResult? keyResult = null;
            WaveformData? waveform = null;
            LoudnessResult loudness = new();

            try { bpmResult = await bpmTask; }
            catch (Exception ex) { LoggerService.Log($"AudioAnalysisPipeline - BPM detection failed: {ex.Message}"); }

            // bpmTask results are handled above

            try { keyResult = await keyTask; }
            catch (Exception ex) { LoggerService.Log($"AudioAnalysisPipeline - Key detection failed: {ex.Message}"); }

            try { waveform = await waveformTask; }
            catch (Exception ex) { LoggerService.Log($"AudioAnalysisPipeline - Waveform analysis failed: {ex.Message}"); }

            try { loudness = await loudnessTask; }
            catch (Exception ex) { LoggerService.Log($"AudioAnalysisPipeline - Loudness analysis failed: {ex.Message}"); }

            progress?.Report(90);

            // Note: Waveform refinement step was removed because WaveformAnalyzer 
            // currently doesn't use the globalBpm parameter in AnalyzeFromSamples,
            // so running it a second time was redundant and wasted CPU time.

            progress?.Report(95);

            // === STEP 4: Build report ===
            if (bpmResult != null)
            {
                report.Bpm = bpmResult.PrimaryBpm;
                report.AlternativeBpm = bpmResult.AlternateBpms.FirstOrDefault();
                report.BpmResult = bpmResult;
            }
            
            if (keyResult != null)
            {
                var parts = keyResult.Key.Split(' ');
                report.Key = parts[0];
                report.Mode = parts.Length > 1 ? parts[1] : "";
                
                if (!string.IsNullOrEmpty(keyResult.AlternativeKey))
                {
                    var altParts = keyResult.AlternativeKey.Split(' ');
                    report.AlternativeKey = altParts[0];
                    report.AlternativeMode = altParts.Length > 1 ? altParts[1] : "";
                }

                report.KeyConfidence = keyResult.Confidence;
                report.TuningOffset = keyResult.TuningOffsetCents;
            }

            report.Waveform = waveform;
            report.Loudness = loudness;

            LoggerService.Log($"AudioAnalysisPipeline - Analysis complete: BPM={report.Bpm}/{report.AlternativeBpm}, Key={report.Key}/{report.Mode}, Valid={report.IsValid}");
            progress?.Report(100);
        }
        catch (Exception ex)
        {
            LoggerService.Log($"AudioAnalysisPipeline - Fatal error: {ex.Message}");
        }

        return report;
    }
}
