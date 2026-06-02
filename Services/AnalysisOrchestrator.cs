using System;
using System.Threading.Tasks;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services;

public interface IAnalysisOrchestrator
{
    Task<AudioAnalysisReport> RunAnalysisAsync(
        string filePath,
        BpmRangeProfile bpmProfile,
        Action<double, string> progressCallback);
}

public class AnalysisOrchestrator : IAnalysisOrchestrator
{
    private readonly IAudioAnalysisPipeline _pipeline;

    public AnalysisOrchestrator(IAudioAnalysisPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<AudioAnalysisReport> RunAnalysisAsync(
        string filePath, 
        BpmRangeProfile bpmProfile, 
        Action<double, string> progressCallback)
    {
        var progressReporter = new Progress<int>(p =>
        {
            var pCapped = Math.Min(p, 99);
            string stage = pCapped switch
            {
                < 30 => ">_ INITIALIZING AUDIO PIPELINE...",
                < 99 => ">_ RUNNING PARALLEL ANALYSIS (BPM, KEY, LUFS, WAVEFORM)...",
                _ => ">_ FINALIZING RESULTS..."
            };
            progressCallback(pCapped, stage);
        });

        return await _pipeline.AnalyzeAudioAsync(filePath, progressReporter, bpmProfile);
    }
}
