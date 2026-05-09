using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services
{
    public class UpdateService : IUpdateService
    {
        private const string RepoOwner = "h057co";
        private const string RepoName = "Tone-And-Beats-Csharp";
        private const string GitHubApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ToneAndBeats", "1.2.0"));
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString()?.TrimStart('v');
                if (string.IsNullOrEmpty(tagName)) return null;

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
                
                if (IsNewerVersion(tagName, currentVersion))
                {
                    var assets = root.GetProperty("assets");
                    var exeAsset = assets.EnumerateArray()
                        .FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);

                    if (exeAsset.ValueKind == JsonValueKind.Undefined) return null;

                    return new UpdateInfo(
                        Version: tagName,
                        ReleaseNotes: root.GetProperty("body").GetString() ?? "",
                        DownloadUrl: exeAsset.GetProperty("browser_download_url").GetString() ?? "",
                        HtmlUrl: root.GetProperty("html_url").GetString() ?? "",
                        PublishedAt: root.GetProperty("published_at").GetDateTime()
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return null;
        }

        public async Task<string> DownloadUpdateAsync(IProgress<double> progress)
        {
            var updateInfo = await CheckForUpdatesAsync();
            if (updateInfo == null) throw new InvalidOperationException("No update available to download.");

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"ToneAndBeats_Update_{updateInfo.Version}.exe");

            using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    int read;

                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes != -1)
                        {
                            progress.Report((double)totalRead / totalBytes * 100);
                        }
                    }
                }
            }

            return tempFilePath;
        }

        public void ApplyUpdateAndRestart(string downloadedFilePath)
        {
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExe)) return;

            var pid = Process.GetCurrentProcess().Id;
            
            // Script de PowerShell para esperar a que el proceso actual muera, reemplazar el archivo y reiniciar.
            var script = $@"
$pid = {pid}
$source = '{downloadedFilePath}'
$dest = '{currentExe}'

while (Get-Process -Id $pid -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 100 }}

Move-Item -Path $source -Destination $dest -Force
Start-Process -FilePath $dest
";

            var scriptPath = Path.Combine(Path.GetTempPath(), "apply_update.ps1");
            File.WriteAllText(scriptPath, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Environment.Exit(0);
        }

        private bool IsNewerVersion(string latestVersion, string? currentVersion)
        {
            if (string.IsNullOrEmpty(currentVersion)) return true;
            if (Version.TryParse(latestVersion, out var latest) && Version.TryParse(currentVersion, out var current))
            {
                return latest > current;
            }
            return false;
        }
    }
}
