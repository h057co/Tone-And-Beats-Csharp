using System;

namespace AudioAnalyzer.Models
{
    public record UpdateInfo(
        string Version,
        string ReleaseNotes,
        string DownloadUrl,
        string HtmlUrl,
        DateTime PublishedAt
    );
}
