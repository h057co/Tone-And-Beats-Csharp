# Quickstart: Keyboard Overlay & Tone Generator

Integration guide for the 12-note chromatic keyboard and rhythmic playback.

## 1. Keyboard Layout (XAML)
The piano is implemented as a 12-slot container. White keys use a `UniformGrid` (Columns=7), and black keys are positioned using a layered `Grid` with margins.

```xml
<!-- Example Highlight Binding -->
<Rectangle Fill="{Binding HighlightedNotes[0], Converter={StaticResource KeyToBrushConverter}}" />
```

## 2. Scale Playback (C#)
To trigger the tone generator:

```csharp
// In MainViewModel.cs
public void ToggleScalePlayback()
{
    if (IsPlaying)
    {
        _toneService.StartPlayback(CurrentBpm, CurrentScaleNotes);
    }
    else
    {
        _toneService.StopPlayback();
    }
}
```

## 3. FFmpeg Check
Ensure `ffmpeg.exe` is in the `ffmpeg/` folder. The application checks this at startup:

```csharp
bool isAvailable = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe"));
```
