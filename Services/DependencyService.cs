using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class DependencyService : IDependencyService
{
    private const string FFmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    private const string EssentiaUrl = "https://essentia.upf.edu/extractors/essentia-extractors-v2.1_beta5-356-g673b6a14-win-i686/essentia_streaming_extractor_music.exe";
    private readonly string _baseDir;
    private readonly string _ffmpegDir;
    private readonly string _essentiaDir;

    public DependencyService()
    {
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _ffmpegDir = Path.Combine(_baseDir, "ffmpeg");
        _essentiaDir = Path.Combine(_baseDir, "essentia");
    }

    public bool IsFFmpegAvailable()
    {
        var path = GetFFmpegPath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    public string GetFFmpegPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(_ffmpegDir, "ffmpeg.exe"),
            Path.Combine(_baseDir, "ffmpeg.exe"),
            "ffmpeg.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path)) return path;
        }

        return string.Empty;
    }

    public bool IsEssentiaAvailable()
    {
        var path = GetEssentiaPath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    public string GetEssentiaPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(_baseDir, "dependencies", "Essentia", "essentia_streaming_extractor_music.exe"),
            Path.Combine(_essentiaDir, "streaming_extractor_music.exe"),
            Path.Combine(_baseDir, "streaming_extractor_music.exe"),
            "essentia_streaming_extractor_music.exe",
            "streaming_extractor_music.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path)) return path;
        }

        return string.Empty;
    }

    public async Task DownloadFFmpegAsync(IProgress<double> progress)
    {
        await DownloadAndExtractAsync(FFmpegUrl, "ffmpeg_download.zip", _ffmpegDir, "ffmpeg.exe", progress);
    }

    public async Task DownloadEssentiaAsync(IProgress<double> progress)
    {
        await DownloadAndExtractAsync(EssentiaUrl, "essentia_download.zip", _essentiaDir, "streaming_extractor_music.exe", progress);
    }

    private async Task DownloadAndExtractAsync(string url, string zipName, string targetDir, string mainExe, IProgress<double> progress)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), zipName);
        var tempExtract = Path.Combine(Path.GetTempPath(), "extract_" + Guid.NewGuid().ToString("N"));

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            var totalRead = 0L;
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (totalBytes != -1 && progress != null)
                {
                    progress.Report((double)totalRead / totalBytes * 100);
                }
            }

            fileStream.Close();

            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            
            await Task.Run(() => 
            {
                if (url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    // Direct executable download
                    File.Copy(tempZip, Path.Combine(targetDir, mainExe), true);
                    return;
                }

                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
                ZipFile.ExtractToDirectory(tempZip, tempExtract);
                
                var files = Directory.GetFiles(tempExtract, mainExe, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    File.Copy(files[0], Path.Combine(targetDir, mainExe), true);
                    
                    // Copy accompanying dlls/binaries if any
                    var allFiles = Directory.GetFiles(tempExtract, "*.*", SearchOption.AllDirectories);
                    foreach(var file in allFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(file, Path.Combine(targetDir, fileName), true);
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException($"{mainExe} not found in downloaded archive.");
                }
            });
        }
        finally
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
        }
    }
}
