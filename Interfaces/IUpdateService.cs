using System;
using System.Threading.Tasks;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Checks GitHub for a newer version than the current assembly version.
        /// </summary>
        /// <returns>Update information if a newer version exists, otherwise null.</returns>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>
        /// Downloads the update asset in the background.
        /// </summary>
        /// <param name="progress">Progress reporter for the download status (0-100).</param>
        /// <returns>Path to the downloaded temporary file.</returns>
        Task<string> DownloadUpdateAsync(IProgress<double> progress);

        /// <summary>
        /// Launches a sidecar process to replace the current executable and restarts the app.
        /// </summary>
        /// <param name="downloadedFilePath">Path to the new executable.</param>
        void ApplyUpdateAndRestart(string downloadedFilePath);
    }
}
