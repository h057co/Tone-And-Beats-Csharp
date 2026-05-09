namespace AudioAnalyzer.Models;

public enum DependencyStatus
{
    Idle,
    Downloading,
    Extracting,
    Ready,
    Error
}

public class DependencyInfo
{
    public string Name { get; set; } = "FFmpeg";
    public string DownloadUrl { get; set; } = "";
    public string TargetFolder { get; set; } = "";
    public DependencyStatus Status { get; set; } = DependencyStatus.Idle;
    public double Progress { get; set; }
    public string ErrorMessage { get; set; } = "";
}
