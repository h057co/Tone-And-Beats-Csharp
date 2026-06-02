using System;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;
using NAudio.Wave;

namespace AudioAnalyzer.Services;

public interface IPlaybackController
{
    void Play();
    void Pause();
    void Stop();
    void Seek(double positionInSeconds);
    void SetVolume(float volume);
    void UpdatePosition();
    void LoadFile(string filePath);
    void UnloadFile();
    AudioFileInfo? GetAudioFileInfo();
    void Cleanup();
    
    TimeSpan Duration { get; }
    TimeSpan Position { get; }
    PlaybackState State { get; }
    
    event Action<string>? StatusChanged;
    event Action<string, string>? PositionTextChanged;
}

public class PlaybackController : IPlaybackController
{
    private readonly IAudioPlayerService _audioPlayerService;

    public event Action<string>? StatusChanged;
    public event Action<string, string>? PositionTextChanged; // (PositionText, DurationText)

    public PlaybackController(IAudioPlayerService audioPlayerService)
    {
        _audioPlayerService = audioPlayerService;
        _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    public TimeSpan Duration => _audioPlayerService.Duration;
    public TimeSpan Position => _audioPlayerService.Position;
    public PlaybackState State => _audioPlayerService.State;

    public void LoadFile(string filePath)
    {
        _audioPlayerService.LoadFile(filePath);
    }

    public void UnloadFile()
    {
        _audioPlayerService.UnloadFile();
    }

    public AudioFileInfo? GetAudioFileInfo()
    {
        return _audioPlayerService.GetAudioFileInfo();
    }

    public void Play()
    {
        _audioPlayerService.Play();
        StatusChanged?.Invoke("Reproduciendo...");
    }

    public void Pause()
    {
        _audioPlayerService.Pause();
        StatusChanged?.Invoke("En pausa.");
    }

    public void Stop()
    {
        _audioPlayerService.Stop();
        StatusChanged?.Invoke("Detenido.");
        UpdatePositionDisplay();
    }

    public void Seek(double positionInSeconds)
    {
        if (_audioPlayerService.Duration.TotalSeconds > 0)
        {
            positionInSeconds = Math.Max(0, Math.Min(_audioPlayerService.Duration.TotalSeconds, positionInSeconds));
            var newPosition = TimeSpan.FromSeconds(positionInSeconds);
            _audioPlayerService.Seek(newPosition);
            
            PositionTextChanged?.Invoke(FormatTime(newPosition), FormatTime(_audioPlayerService.Duration));
        }
    }

    public void SetVolume(float volume)
    {
        _audioPlayerService.SetVolume(volume);
    }

    public void UpdatePosition()
    {
        if (_audioPlayerService.State == NAudio.Wave.PlaybackState.Playing)
        {
            UpdatePositionDisplay();
        }
    }

    private void OnPlaybackStateChanged(object? sender, NAudio.Wave.PlaybackState state)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            switch (state)
            {
                case NAudio.Wave.PlaybackState.Playing:
                    StatusChanged?.Invoke("Reproduciendo...");
                    break;
                case NAudio.Wave.PlaybackState.Stopped:
                    StatusChanged?.Invoke("Detenido.");
                    UpdatePositionDisplay();
                    break;
                case NAudio.Wave.PlaybackState.Paused:
                    StatusChanged?.Invoke("En pausa.");
                    UpdatePositionDisplay();
                    break;
            }
        });
    }

    private void UpdatePositionDisplay()
    {
        PositionTextChanged?.Invoke(FormatTime(_audioPlayerService.Position), FormatTime(_audioPlayerService.Duration));
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
    }

    public void Cleanup()
    {
        _audioPlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }
}
