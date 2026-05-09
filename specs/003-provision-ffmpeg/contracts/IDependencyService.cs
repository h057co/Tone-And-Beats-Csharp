namespace AudioAnalyzer.Interfaces;

public interface IDependencyService
{
    bool IsFFmpegAvailable();
    Task DownloadFFmpegAsync(IProgress<double> progress);
    string GetFFmpegPath();
}
