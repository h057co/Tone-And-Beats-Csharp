using System;
using System.Threading.Tasks;

namespace AudioAnalyzer.Interfaces;

public interface IDependencyService
{
    bool IsFFmpegAvailable();
    Task DownloadFFmpegAsync(IProgress<double> progress);
    string GetFFmpegPath();

    bool IsEssentiaAvailable();
    Task DownloadEssentiaAsync(IProgress<double> progress);
    string GetEssentiaPath();
}
