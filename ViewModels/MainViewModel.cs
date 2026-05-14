using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AudioAnalyzer.Commands;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;
using AudioAnalyzer.Services;
using System.Reflection;
using AudioAnalyzer.Themes;

namespace AudioAnalyzer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly IBpmDetectorService _bpmDetectorService;
    private readonly IKeyDetector _keyDetectorService;
    private readonly IWaveformAnalyzerService _waveformAnalyzerService;
    private readonly IFilePickerService _filePickerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILoudnessAnalyzerService _loudnessAnalyzerService;
    private readonly IAudioAnalysisPipeline _audioAnalysisPipeline;
    private readonly IToneGeneratorService _toneGeneratorService;
    private readonly IUpdateService _updateService;
    private readonly MetadataWriter _metadataWriter;
    private readonly System.Windows.Threading.DispatcherTimer _bpmTimer;

    private string _fileName = "No file selected";
    private bool _isFileSelected = false;
    private string _positionText = "00:00";
    private string _durationText = "00:00";
    private string _bpmText = "--";
    private string _alternativeBpmText = "";
    private string _keyText = "--";
    private string _modeText = "";
    private string _bpmConfidence = "";
    private string _keyConfidence = "";
    private string _tuningText = "";
    private string _statusText = "Ready";
    private string _statusState = "Normal";
    private double _analysisProgress;
    private bool _isAnalysisProgressVisible;
    private bool _arePlaybackControlsEnabled;
    private bool _isAnalyzeButtonEnabled;
    private bool _isSaveMetadataEnabled;
    private bool _isAnalyzingInProgress = false;
    private string? _pendingFilePath = null;
    private WaveformData? _waveformData;
    private RelayCommand? _browseCommand;
    private RelayCommand? _playCommand;
    private RelayCommand? _pauseCommand;
    private RelayCommand? _stopCommand;
    private RelayCommand? _saveMetadataCommand;
    private RelayCommand? _analyzeCommand;
    private RelayCommand? _cycleThemeCommand;
    private RelayCommand? _openUrlCommand;
    private double _originalBpm;
    private double _displayBpm;
    private BpmAnalysisResult? _bpmAnalysisResult;
    private RelayCommand? _swapBpmCommand;
    private double _originalAlternativeBpm;  // Almacena el BPM alternativo original detectado
    private bool _hasSwappedBpm;              // Tracking: true si el usuario ha intercambiado
    private BpmRangeProfile _selectedBpmProfile = BpmRangeProfile.Auto;
    private string _audioFileType = "";
    private string _sampleRateText = "";
    private string _bitDepthText = "";
    private string _channelsText = "";
    private string _bitrateText = "";
    private string _bitrateModeText = "";
    private AudioFileInfo? _currentAudioInfo;
    private int _keyIndex = -1;
    private string _alternativeKey = "";
    private string _alternativeMode = "";
    private string _originalKey = "";
    private string _originalMode = "";
    private bool _hasSwappedKey = false;
    private bool _showRelativeKey = false;
    private LoudnessResult? _loudnessResult;
    private bool _isLoudnessVisible = false;
    private double _keyConfidenceValue = 0;
    private bool _isTuningOff = false;
    private bool _isKeyboardOverlayVisible = false;
    private bool _isPlayingScale = false;
    private RelayCommand? _toggleKeyboardCommand;
    private RelayCommand? _playScaleCommand;
    private RelayCommand? _closeKeyboardCommand;
    private RelayCommand? _toggleKeyDisplayCommand;
    private RelayCommand? _swapKeyCommand;
    private readonly IDependencyService _dependencyService;

    // Dependency Properties
    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    private bool _isDependencyMissing;
    public bool IsDependencyMissing
    {
        get => _isDependencyMissing;
        set => SetProperty(ref _isDependencyMissing, value);
    }

    public string AppVersion { get; }

    public MainViewModel(
        IAudioPlayerService audioPlayerService,
        IBpmDetectorService bpmDetectorService,
        IKeyDetector keyDetectorService,
        IWaveformAnalyzerService waveformAnalyzerService,
        IFilePickerService filePickerService,
        IMessageBoxService messageBoxService,
        ILoudnessAnalyzerService loudnessAnalyzerService,
        IAudioAnalysisPipeline audioAnalysisPipeline,
        IToneGeneratorService toneGeneratorService,
        IDependencyService dependencyService,
        IUpdateService updateService)
    {
        _audioPlayerService = audioPlayerService;
        _bpmDetectorService = bpmDetectorService;
        _keyDetectorService = keyDetectorService;
        _waveformAnalyzerService = waveformAnalyzerService;
        _filePickerService = filePickerService;
        _messageBoxService = messageBoxService;
        _loudnessAnalyzerService = loudnessAnalyzerService;
        _audioAnalysisPipeline = audioAnalysisPipeline;
        _toneGeneratorService = toneGeneratorService;
        _dependencyService = dependencyService;
        _updateService = updateService;
        _metadataWriter = new MetadataWriter();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.2.0";

        _bpmTimer = new System.Windows.Threading.DispatcherTimer();
        _bpmTimer.Tick += (s, e) => _toneGeneratorService.Trigger();

        ResolveDependenciesCommand = new RelayCommand(async () => await ResolveDependenciesAsync());

        // Check dependencies on startup
        if (!_dependencyService.IsFFmpegAvailable())
        {
            IsDependencyMissing = true;
        }

        _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    public RelayCommand ResolveDependenciesCommand { get; }
    public IUpdateService UpdateService => _updateService;

    private async Task ResolveDependenciesAsync()
    {
        if (IsDownloading) return;

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            
            var progress = new Progress<double>(p => DownloadProgress = p);
            await _dependencyService.DownloadFFmpegAsync(progress);
            
            IsDependencyMissing = false;
            _messageBoxService.ShowInfo("FFmpeg dependencies resolved successfully.", "Success");
        }
        catch (Exception ex)
        {
            LoggerService.Log("MainViewModel.ResolveDependenciesAsync - Error: " + ex.Message);
            _messageBoxService.ShowError("Failed to download FFmpeg: " + ex.Message, "Error");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    /// <summary>
    /// Semantic flag: true when a valid audio file is loaded.
    /// The View uses this with DataTrigger to switch FileNameForeground color.
    /// </summary>
    public bool IsFileSelected
    {
        get => _isFileSelected;
        set => SetProperty(ref _isFileSelected, value);
    }

    public bool IsRelativeModeActive
    {
        get => _showRelativeKey;
        set
        {
            if (_showRelativeKey != value)
            {
                _showRelativeKey = value;
                OnPropertyChanged(nameof(IsRelativeModeActive));
                OnPropertyChanged(nameof(KeyDisplayText));
                OnPropertyChanged(nameof(CurrentTonicIndex));
                OnPropertyChanged(nameof(ScaleNotes));
            }
        }
    }

    public string PositionText
    {
        get => _positionText;
        set => SetProperty(ref _positionText, value);
    }

    public string DurationText
    {
        get => _durationText;
        set => SetProperty(ref _durationText, value);
    }

    public string AudioFileType
    {
        get => _audioFileType;
        set => SetProperty(ref _audioFileType, value);
    }

    public string SampleRateText
    {
        get => _sampleRateText;
        set => SetProperty(ref _sampleRateText, value);
    }

    public string BitDepthText
    {
        get => _bitDepthText;
        set => SetProperty(ref _bitDepthText, value);
    }

    public string ChannelsText
    {
        get => _channelsText;
        set => SetProperty(ref _channelsText, value);
    }

    public string BitrateText
    {
        get => _bitrateText;
        set => SetProperty(ref _bitrateText, value);
    }

    public string BitrateModeText
    {
        get => _bitrateModeText;
        set => SetProperty(ref _bitrateModeText, value);
    }

    public string AudioInfoSummary => BuildAudioInfoSummary();

    private string BuildAudioInfoSummary()
    {
        if (_currentAudioInfo == null)
            return "No file loaded";

        var parts = new List<string>();

        parts.Add(_currentAudioInfo.FileType ?? "Unknown");

        if (_currentAudioInfo.SampleRate > 0)
            parts.Add($"{_currentAudioInfo.SampleRate} Hz");

        if (_currentAudioInfo.BitDepth > 0)
            parts.Add($"{_currentAudioInfo.BitDepth}-bit");

        if (_currentAudioInfo.Bitrate > 0)
        {
            var bitrateInfo = $"{_currentAudioInfo.Bitrate} kbps";
            if (!string.IsNullOrEmpty(_currentAudioInfo.BitrateMode))
                bitrateInfo += $" {_currentAudioInfo.BitrateMode}";
            parts.Add(bitrateInfo);
        }

        if (_currentAudioInfo.Channels > 0)
            parts.Add(_currentAudioInfo.ChannelsDisplay);

        return string.Join(" • ", parts);
    }

    public string FooterText => BuildFooterText();

    private string BuildFooterText()
    {
        if (_currentAudioInfo == null)
            return "READY TO ANALYZE";

        return $"SAMPLE RATE: {_currentAudioInfo.SampleRate} Hz | BIT DEPTH: {_currentAudioInfo.BitDepth}-bit | CHANNELS: {_currentAudioInfo.ChannelsDisplay}";
    }

    private string _headroomText = "-. - dB";
    public string HeadroomText
    {
        get => _headroomText;
        set => SetProperty(ref _headroomText, value);
    }

    public string BpmText
    {
        get => _bpmText;
        set
        {
            if (SetProperty(ref _bpmText, value))
                OnPropertyChanged(nameof(BpmDisplayText));
        }
    }

    public string BpmDisplayText
    {
        get
        {
            if (_displayBpm <= 0) return _bpmText;
            return _displayBpm.ToString("F1");
        }
    }

    public string AlternativeBpmText
    {
        get => _alternativeBpmText;
        set => SetProperty(ref _alternativeBpmText, value);
    }

    public Dictionary<BpmRangeProfile, string> AvailableBpmProfiles { get; } = new Dictionary<BpmRangeProfile, string>
    {
        { BpmRangeProfile.Auto, "Auto (Recomendado)" },
        { BpmRangeProfile.Low_50_100, "Low (50 - 100 BPM)" },
        { BpmRangeProfile.Mid_75_150, "Mid (75 - 150 BPM)" },
        { BpmRangeProfile.High_100_200, "High (100 - 200 BPM)" },
        { BpmRangeProfile.VeryHigh_150_300, "Very High (150 - 300 BPM)" }
    };

    public BpmRangeProfile SelectedBpmProfile
    {
        get => _selectedBpmProfile;
        set => SetProperty(ref _selectedBpmProfile, value);
    }

    /// <summary>
    /// Semantic flag: true when BPM has been swapped.
    /// The View uses this with DataTrigger to apply modified styling.
    /// </summary>
    public bool IsBpmModified => _hasSwappedBpm;

    /// <summary>
    /// Retorna true cuando hay un BPM alternativo válido para intercambiar.
    /// </summary>
    public bool CanSwapBpm => _originalAlternativeBpm > 0 && _originalAlternativeBpm != _originalBpm;

    /// <summary>
    /// Retorna true cuando el usuario ha activado el intercambio BPM.
    /// La View usa esto para mostrar el botón ⇄ en estado "activo".
    /// </summary>
    public bool IsSwapped => _hasSwappedBpm;

    /// <summary>
    /// Retorna true cuando el BPM fue intercambiado (swap), distinto de ajuste ×2/÷2.
    /// La View usa esto para aplicar BpmSwappedBrush en lugar de BpmModifiedBrush.
    /// </summary>
    public bool IsBpmSwapped => _hasSwappedBpm;

    public string KeyText
    {
        get => _keyText;
        set => SetProperty(ref _keyText, value);
    }

    public string ModeText
    {
        get => _modeText;
        set => SetProperty(ref _modeText, value);
    }

    public string BpmConfidence
    {
        get => _bpmConfidence;
        set => SetProperty(ref _bpmConfidence, value);
    }

    public string KeyConfidence
    {
        get => _keyConfidence;
        set => SetProperty(ref _keyConfidence, value);
    }

    public string TuningText
    {
        get => _tuningText;
        set => SetProperty(ref _tuningText, value);
    }

    public LoudnessResult? LoudnessResult
    {
        get => _loudnessResult;
        set
        {
            SetProperty(ref _loudnessResult, value);
            OnPropertyChanged(nameof(LoudnessIntegratedDisplay));
            OnPropertyChanged(nameof(LoudnessLraDisplay));
            OnPropertyChanged(nameof(LoudnessTruePeakDisplay));
            OnPropertyChanged(nameof(LoudnessIntegratedLevel));
            OnPropertyChanged(nameof(LoudnessTruePeakLevel));
        }
    }

    public bool IsLoudnessVisible
    {
        get => _isLoudnessVisible;
        set => SetProperty(ref _isLoudnessVisible, value);
    }

    public string LoudnessIntegratedDisplay => _loudnessResult?.IntegratedDisplay ?? "--";
    public string LoudnessLraDisplay => _loudnessResult?.LraDisplay ?? "--";
    public string LoudnessTruePeakDisplay => _loudnessResult?.TruePeakDisplay ?? "--";

    /// <summary>
    /// Semantic level for LUFS Integrated: "Good", "Warning", "Danger", or "None".
    /// The View uses LevelToBrushConverter to map this to the appropriate color.
    /// </summary>
    public string LoudnessIntegratedLevel
    {
        get
        {
            if (_loudnessResult == null) return "None";
            if (_loudnessResult.HasError) return "Error";
            if (!_loudnessResult.IsValid) return "None";

            if (_loudnessResult.IntegratedLufs >= -12)
                return "Danger";   // Too loud

            if (_loudnessResult.IntegratedLufs >= -16)
                return "Warning";  // Caution

            return "Good";         // OK
        }
    }

    /// <summary>
    /// Semantic level for True Peak: "Good", "Warning", "Danger", or "None".
    /// The View uses LevelToBrushConverter to map this to the appropriate color.
    /// </summary>
    public string LoudnessTruePeakLevel
    {
        get
        {
            if (_loudnessResult == null) return "None";
            if (_loudnessResult.HasError) return "Error";
            if (_loudnessResult.TruePeak == 0) return "None";

            if (_loudnessResult.TruePeak >= 0)
                return "Danger";   // Clipping

            if (_loudnessResult.TruePeak > -1)
                return "Warning";  // Close to clipping

            return "Good";         // OK
        }
    }

    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public bool[] ScaleNotes => CalculateScaleNotes();

    private bool[] CalculateScaleNotes()
    {
        var notes = new bool[12];
        if (_keyIndex < 0 || string.IsNullOrEmpty(_modeText))
            return notes;

        // Determine current mode and root index (accounting for relative key)
        string currentMode = _modeText;
        int currentRoot = _keyIndex;

        if (_showRelativeKey)
        {
            if (_modeText == "Major")
            {
                currentMode = "Minor";
                currentRoot = (_keyIndex + 9) % 12;
            }
            else
            {
                currentMode = "Major";
                currentRoot = (_keyIndex + 3) % 12;
            }
        }

        // Scale patterns (semitones from root)
        int[] pattern = currentMode == "Major" 
            ? new[] { 0, 2, 4, 5, 7, 9, 11 } 
            : new[] { 0, 2, 3, 5, 7, 8, 10 };

        foreach (int interval in pattern)
        {
            notes[(currentRoot + interval) % 12] = true;
        }

        return notes;
    }

    public int CurrentTonicIndex
    {
        get
        {
            if (_keyIndex < 0) return -1;
            if (!_showRelativeKey) return _keyIndex;

            // Major -> Relative Minor (-3 semitones)
            // Minor -> Relative Major (+3 semitones)
            if (_modeText == "Major")
                return (_keyIndex - 3 + 12) % 12;
            else
                return (_keyIndex + 3) % 12;
        }
    }

    public string KeyDisplayText
    {
        get
        {
            if (string.IsNullOrEmpty(_keyText) || _keyText == "--")
                return "--";
            if (_showRelativeKey)
                return CalculateRelativeKey();
            return $"{_keyText} {_modeText}";
        }
    }

    private string CalculateRelativeKey()
    {
        if (_keyIndex < 0 || string.IsNullOrEmpty(_modeText))
            return "--";

        int relativeIndex;
        string relativeMode;

        if (_modeText == "Major")
        {
            relativeIndex = (_keyIndex - 3 + 12) % 12;
            relativeMode = "Minor";
        }
        else
        {
            relativeIndex = (_keyIndex + 3) % 12;
            relativeMode = "Major";
        }

        return $"{NoteNames[relativeIndex]} {relativeMode}";
    }

    public void ToggleKeyDisplay()
    {
        if (string.IsNullOrEmpty(_keyText) || _keyText == "--") return;
        
        IsRelativeModeActive = !IsRelativeModeActive;

        if (IsPlayingScale)
        {
            StartScalePlayback(); // Restart to follow new tonic/scale
        }
    }

    public void SwapKeyValues()
    {
        if (string.IsNullOrEmpty(_alternativeKey)) return;

        if (_hasSwappedKey)
        {
            KeyText = _originalKey;
            ModeText = _originalMode;
            _hasSwappedKey = false;
        }
        else
        {
            KeyText = _alternativeKey;
            ModeText = _alternativeMode;
            _hasSwappedKey = true;
        }

        _showRelativeKey = false; // Reset to absolute view
        _keyIndex = Array.IndexOf(NoteNames, KeyText);
        OnPropertyChanged(nameof(KeyDisplayText));
        OnPropertyChanged(nameof(CurrentTonicIndex));
        OnPropertyChanged(nameof(ScaleNotes));
        
        if (IsPlayingScale)
        {
            StartScalePlayback(); // Restart with new Key
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Semantic state: "Normal", "Success", or "Error".
    /// The View uses DataTriggers to map these to StatusForegroundBrush, StatusSuccessBrush, StatusErrorBrush.
    /// </summary>
    public string StatusState
    {
        get => _statusState;
        set => SetProperty(ref _statusState, value);
    }

    public double AnalysisProgress
    {
        get => _analysisProgress;
        set => SetProperty(ref _analysisProgress, value);
    }

    public bool IsAnalysisProgressVisible
    {
        get => _isAnalysisProgressVisible;
        set => SetProperty(ref _isAnalysisProgressVisible, value);
    }

    public bool ArePlaybackControlsEnabled
    {
        get => _arePlaybackControlsEnabled;
        set
        {
            if (SetProperty(ref _arePlaybackControlsEnabled, value))
            {
                PlayCommand.RaiseCanExecuteChanged();
                PauseCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsKeyboardOverlayVisible
    {
        get => _isKeyboardOverlayVisible;
        set 
        {
            if (SetProperty(ref _isKeyboardOverlayVisible, value))
            {
                // If closing overlay, stop scale playback
                if (!value && IsPlayingScale)
                {
                    IsPlayingScale = false;
                }
            }
        }
    }

    public bool IsPlayingScale
    {
        get => _isPlayingScale;
        set 
        {
            if (SetProperty(ref _isPlayingScale, value))
            {
                if (_isPlayingScale)
                {
                    StartScalePlayback();
                }
                else
                {
                    StopScalePlayback();
                }
            }
        }
    }

    private void StartScalePlayback()
    {
        if (_displayBpm <= 0 || _keyIndex < 0)
        {
            IsPlayingScale = false;
            return;
        }

        // Apply ducking (-6dB ≈ 0.5 volume)
        _audioPlayerService.SetVolume(0.5f);

        _toneGeneratorService.StartScalePlayback(_displayBpm, ScaleNotes, CurrentTonicIndex);
        
        // Interval = 60,000 / BPM (ms per beat)
        double intervalMs = 60000.0 / _displayBpm;
        _bpmTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _bpmTimer.Start();
    }

    private void StopScalePlayback()
    {
        _bpmTimer.Stop();
        _toneGeneratorService.StopScalePlayback();

        // Restore volume
        _audioPlayerService.SetVolume(1.0f);
    }

    public RelayCommand ToggleKeyboardCommand => _toggleKeyboardCommand ??= new RelayCommand(() => IsKeyboardOverlayVisible = !IsKeyboardOverlayVisible);
    public RelayCommand CloseKeyboardCommand => _closeKeyboardCommand ??= new RelayCommand(() => IsKeyboardOverlayVisible = false);
    public RelayCommand PlayScaleCommand => _playScaleCommand ??= new RelayCommand(() => IsPlayingScale = !IsPlayingScale);
    public RelayCommand ToggleKeyDisplayCommand => _toggleKeyDisplayCommand ??= new RelayCommand(ToggleKeyDisplay);
    public RelayCommand SwapKeyCommand => _swapKeyCommand ??= new RelayCommand(SwapKeyValues);

    public bool IsAnalyzeButtonEnabled
    {
        get => _isAnalyzeButtonEnabled;
        set => SetProperty(ref _isAnalyzeButtonEnabled, value);
    }

    private double _waveformPosition;
    public double WaveformPosition
    {
        get => _waveformPosition;
        set => SetProperty(ref _waveformPosition, value);
    }

    public WaveformData? WaveformData
    {
        get => _waveformData;
        set => SetProperty(ref _waveformData, value);
    }

    public double KeyConfidenceValue
    {
        get => _keyConfidenceValue;
        set => SetProperty(ref _keyConfidenceValue, value);
    }

    public bool IsTuningOff
    {
        get => _isTuningOff;
        set => SetProperty(ref _isTuningOff, value);
    }

    private bool _hasAnalysisResults;
    public bool HasAnalysisResults
    {
        get => _hasAnalysisResults;
        set => SetProperty(ref _hasAnalysisResults, value);
    }

    public string? FilePath { get; private set; }

    public RelayCommand BrowseCommand
    {
        get => _browseCommand ??= new RelayCommand(ExecuteBrowse, () => true);
        private set => _browseCommand = value;
    }
    public RelayCommand PlayCommand
    {
        get => _playCommand ??= new RelayCommand(ExecutePlay, 
            () => ArePlaybackControlsEnabled && CurrentPlaybackState != NAudio.Wave.PlaybackState.Playing);
        private set => _playCommand = value;
    }
    public RelayCommand PauseCommand
    {
        get => _pauseCommand ??= new RelayCommand(ExecutePause, 
            () => ArePlaybackControlsEnabled && CurrentPlaybackState == NAudio.Wave.PlaybackState.Playing);
        private set => _pauseCommand = value;
    }
    public RelayCommand StopCommand
    {
        get => _stopCommand ??= new RelayCommand(ExecuteStop, 
            () => ArePlaybackControlsEnabled && CurrentPlaybackState != NAudio.Wave.PlaybackState.Stopped);
        private set => _stopCommand = value;
    }
    public RelayCommand AnalyzeCommand 
    {
        get => _analyzeCommand ??= new RelayCommand(
            async () => { if (!string.IsNullOrEmpty(FilePath)) await ExecuteAnalyzeAsync(); },
            () => !string.IsNullOrEmpty(FilePath) && !_isAnalyzingInProgress);
        private set => _analyzeCommand = value;
    }

    public RelayCommand CycleThemeCommand
    {
        get => _cycleThemeCommand ??= new RelayCommand(() => ThemeManager.CycleTheme());
        private set => _cycleThemeCommand = value;
    }

    public RelayCommand OpenUrlCommand
    {
        get => _openUrlCommand ??= new RelayCommand(urlObj => 
        {
            if (urlObj is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"OpenUrlCommand failed: {ex.Message}");
                }
            }
        });
        private set => _openUrlCommand = value;
    }

    public RelayCommand SwapBpmCommand => _swapBpmCommand ??= new RelayCommand(SwapBpmValues, () => CanSwapBpm);

    public bool IsAnalyzingInProgress
    {
        get => _isAnalyzingInProgress;
        private set
        {
            if (SetProperty(ref _isAnalyzingInProgress, value))
            {
                OnPropertyChanged(nameof(IsAnalyzingInProgress));
                UpdateTransportCommands();
            }
        }
    }

    private NAudio.Wave.PlaybackState _currentPlaybackState = NAudio.Wave.PlaybackState.Stopped;
    public NAudio.Wave.PlaybackState CurrentPlaybackState
    {
        get => _currentPlaybackState;
        private set
        {
            if (SetProperty(ref _currentPlaybackState, value))
            {
                UpdateTransportCommands();
            }
        }
    }

    private void UpdateTransportCommands()
    {
        PlayCommand.RaiseCanExecuteChanged();
        PauseCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
    }

    private string _analysisStageText = "";
    public string AnalysisStageText
    {
        get => _analysisStageText;
        set => SetProperty(ref _analysisStageText, value);
    }

    public bool IsSaveMetadataEnabled
    {
        get => _isSaveMetadataEnabled;
        set
        {
            if (SetProperty(ref _isSaveMetadataEnabled, value))
            {
                SaveMetadataCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand SaveMetadataCommand
    {
        get => _saveMetadataCommand ??= new RelayCommand(ExecuteSaveMetadata, () => IsSaveMetadataEnabled);
        private set => _saveMetadataCommand = value;
    }

    private void ExecuteBrowse()
    {
        var filter = "Audio Files|*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.aac;*.aiff;*.wma";
        var filePath = _filePickerService.OpenFile(filter, "Select Audio File");

        if (!string.IsNullOrEmpty(filePath))
        {
            LoadAudioFile(filePath);
        }
    }

    public void LoadAudioFile(string filePath)
    {
        try
        {
            if (!_filePickerService.ValidateAudioFile(filePath))
            {
                _messageBoxService.ShowError("Archivo no válido. Seleccione un archivo de audio válido (MP3, WAV, OGG, FLAC, M4A, AAC, AIFF, WMA).");
                StatusText = "Archivo no válido.";
                return;
            }

            if (_isAnalyzingInProgress)
            {
                if (_pendingFilePath != null)
                {
                    StatusText = "Ya hay archivo en cola. Solo se permite uno.";
                    return;
                }
                
                _pendingFilePath = filePath;
                
                try
                {
                    _audioPlayerService.Stop();
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"Warning: AudioPlayer Stop failed during file queue - {ex.Message}");
                }
                
                try
                {
                    _audioPlayerService.UnloadFile();
                }
                catch (Exception ex)
                {
                    LoggerService.Log($"Warning: AudioPlayer Unload failed during file queue - {ex.Message}");
                }
                
                FilePath = null;
                FileName = "Archivo en cola";
                IsFileSelected = false;
                
                StatusText = "Análisis en proceso. Archivo en cola.";
                return;
            }

            FilePath = filePath;
            HasAnalysisResults = false;
            _audioPlayerService.LoadFile(filePath);

            FileName = Path.GetFileName(filePath);
            IsFileSelected = true;

            ArePlaybackControlsEnabled = true;
            IsAnalyzeButtonEnabled = true;

            var audioInfo = _audioPlayerService.GetAudioFileInfo();
            LoggerService.Log($"LoadAudioFile() - audioInfo es null: {audioInfo == null}");
            
            if (audioInfo != null)
            {
                LoggerService.Log($"LoadAudioFile() - Asignando: Type={audioInfo.FileType}, SR={audioInfo.SampleRate}, BD={audioInfo.BitDepth}, Ch={audioInfo.Channels}, BR={audioInfo.Bitrate}, BRM={audioInfo.BitrateMode}");

                _currentAudioInfo = audioInfo;
                AudioFileType = audioInfo.FileType;
                SampleRateText = audioInfo.SampleRateDisplay;
                BitDepthText = audioInfo.BitDepthDisplay;
                ChannelsText = audioInfo.ChannelsDisplay;
                BitrateText = audioInfo.BitrateDisplay;
                BitrateModeText = audioInfo.BitrateModeDisplay;

                // SetProperty ya notifica - solo AudioInfoSummary necesita notificación manual (es calculated)
                OnPropertyChanged(nameof(AudioInfoSummary));

                StatusText = $"Audio: {audioInfo.FileType} | {audioInfo.SampleRateDisplay} | {audioInfo.BitDepthDisplay} | {audioInfo.BitrateDisplay} | {audioInfo.BitrateModeDisplay} | {audioInfo.ChannelsDisplay}";
            }
            else
            {
                LoggerService.Log("LoadAudioFile() - audioInfo es null, asignando valores vacios");

                _currentAudioInfo = null;
                AudioFileType = "";
                SampleRateText = "";
                BitDepthText = "";
                ChannelsText = "";
                BitrateText = "";
                BitrateModeText = "";

                OnPropertyChanged(nameof(AudioInfoSummary));
            }

            BpmText = "--";
            KeyText = "--";
            ModeText = "";
            BpmConfidence = "";
            KeyConfidence = "";
            WaveformData = null;
            
            _originalBpm = 0;
            _displayBpm = 0;
            _bpmAnalysisResult = null;
            _originalAlternativeBpm = 0;
            _hasSwappedBpm = false;
            _keyIndex = -1;
            _showRelativeKey = false;
            OnPropertyChanged(nameof(BpmDisplayText));
            OnPropertyChanged(nameof(IsBpmModified));
            OnPropertyChanged(nameof(CanSwapBpm));
            OnPropertyChanged(nameof(KeyDisplayText));

            UpdatePositionDisplay();
            StatusText = "Archivo cargado. Listo para analizar.";
            StatusState = "Normal";
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowError($"Error al cargar archivo: {ex.Message}");
            StatusText = "Error al cargar archivo.";
        }
    }

    private void ExecutePlay()
    {
        _audioPlayerService.Play();
        StatusText = "Reproduciendo...";
    }

    private void ExecutePause()
    {
        _audioPlayerService.Pause();
        StatusText = "En pausa.";
    }

    private void ExecuteStop()
    {
        _audioPlayerService.Stop();
        WaveformPosition = 0;
        UpdatePositionDisplay();
        StatusText = "Detenido.";
    }

    private async Task ExecuteAnalyzeAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        try
        {
            _audioPlayerService.Stop();
        }
        catch (Exception ex)
        {
            LoggerService.Log($"Warning: AudioPlayer Stop failed before analysis - {ex.Message}");
        }

        IsAnalyzingInProgress = true;
        AnalysisStageText = ">_ INITIALIZING AUDIO ENGINE...";
        IsAnalyzeButtonEnabled = false;
        IsAnalysisProgressVisible = true;
        AnalysisProgress = 0;

        BpmText = "...";
        KeyText = "...";
        ModeText = "";
        StatusText = "Analizando audio...";

        try
        {
            AnalysisProgress = 10;
            AnalysisStageText = ">_ DECODING AUDIO STREAM...";
            StatusText = "Analizando audio...";
            
            var progressReporter = new Progress<int>(p =>
            {
                // Clamp progress to 99 to leave room for the final 100% completion step
                AnalysisProgress = Math.Min(p, 99);
                if (p < 30) AnalysisStageText = ">_ INITIALIZING AUDIO PIPELINE...";
                else if (p < 99) AnalysisStageText = ">_ RUNNING PARALLEL ANALYSIS (BPM, KEY, LUFS, WAVEFORM)...";
                else AnalysisStageText = ">_ FINALIZING RESULTS...";
            });
            
            // Perform full pipeline analysis (now includes consolidated BPM)
            var report = await _audioAnalysisPipeline.AnalyzeAudioAsync(FilePath, progressReporter, SelectedBpmProfile);
            _bpmAnalysisResult = report.BpmResult;
            
            if (_bpmAnalysisResult != null)
            {
                LoggerService.Log($"ExecuteAnalyze - Essentia BPM: {report.Bpm} (Reinterpreted: {_bpmAnalysisResult.IsReinterpreted})");
            }

            BpmText = report.Bpm > 0 ? report.Bpm.ToString("F1") : "--";
            AlternativeBpmText = report.AlternativeBpm > 0 && report.AlternativeBpm != report.Bpm ? $"Alt: {report.AlternativeBpm:F0} BPM" : "";
            BpmConfidence = report.Bpm > 0 ? "Detected" : "";
            if (report.Bpm > 0)
            {
                _originalBpm = report.Bpm;
                _displayBpm = report.Bpm;
                _originalAlternativeBpm = report.AlternativeBpm;
                _hasSwappedBpm = false;
                OnPropertyChanged(nameof(BpmDisplayText));
                OnPropertyChanged(nameof(IsBpmModified));
                OnPropertyChanged(nameof(CanSwapBpm));
                SwapBpmCommand.RaiseCanExecuteChanged();
            }

            KeyText = report.Key != "Unknown" ? report.Key : "--";
            ModeText = report.Mode != "" ? report.Mode : "";
            _originalKey = report.Key;
            _originalMode = report.Mode;
            _alternativeKey = report.AlternativeKey;
            _alternativeMode = report.AlternativeMode;
            _hasSwappedKey = false;
            
            KeyConfidence = report.KeyConfidence > 0 ? $"Confidence: {report.KeyConfidence:P0}" : "";
            KeyConfidenceValue = report.KeyConfidence * 100;
            TuningText = report.Key != "Unknown" ? $"Tuning: {report.TuningOffset:+0.0;-0.0;0} cents" : "";
            IsTuningOff = report.Key != "Unknown" && Math.Abs(report.TuningOffset) > 10;

            if (report.Key != "Unknown")
            {
                _keyIndex = Array.IndexOf(NoteNames, report.Key);
                _showRelativeKey = false;
                OnPropertyChanged(nameof(KeyDisplayText));
                OnPropertyChanged(nameof(ScaleNotes));
            }

            LoudnessResult = report.Loudness;
            IsLoudnessVisible = true;
            WaveformData = report.Waveform;

            AnalysisProgress = 100;
            AnalysisStageText = ">_ ANALYSIS COMPLETE";
            HasAnalysisResults = true;
            HeadroomText = $"{report.Loudness.TruePeak:F1} dB";
            StatusText = "¡Análisis completo!";
            LoggerService.Log("ExecuteAnalyze - Analisis completo");
        }
        catch (Exception ex)
        {
            StatusText = $"Error en análisis: {ex.Message}";
            LoggerService.Log($"ExecuteAnalyze - Error: {ex.Message}");
        }
        finally
        {
            IsAnalyzeButtonEnabled = true;
            IsAnalysisProgressVisible = false;
            
            bool hasResults = BpmText != "..." && BpmText != "--" && 
                             KeyText != "..." && KeyText != "--";
            IsSaveMetadataEnabled = hasResults && !string.IsNullOrEmpty(FilePath);

            IsAnalyzingInProgress = false;
            AnalysisStageText = "";

            if (!string.IsNullOrEmpty(_pendingFilePath))
            {
                var nextFile = _pendingFilePath;
                _pendingFilePath = null;
                LoadAudioFile(nextFile);
                await ExecuteAnalyzeAsync();
            }
        }
    }

    private void ExecuteSaveMetadata()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        
        if (!double.TryParse(BpmText, out double bpm)) return;
        if (string.IsNullOrEmpty(KeyText) || KeyText == "--") return;
        
        var (hasMetadata, currentBpm, currentKey) = _metadataWriter.GetCurrentMetadata(FilePath);
        
        string message;
        if (hasMetadata)
        {
            message = $"El archivo ya tiene metadata:\nBPM actual: {currentBpm}\nKey actual: {currentKey}\n\n¿Deseas sobrescribir los valores?";
        }
        else
        {
            message = $"¿Guardar metadata en el archivo?\n\nBPM: {bpm}\nKey: {KeyText} {ModeText}";
        }
        
        var result = _messageBoxService.ShowConfirmation(message, "Guardar Metadata");
        
        if (result)
        {
            _audioPlayerService.Stop();
            _audioPlayerService.UnloadFile();
            
            var (success, msg) = _metadataWriter.WriteMetadata(FilePath, bpm, KeyText, ModeText);
            StatusText = msg;
            
            if (success && !string.IsNullOrEmpty(FilePath))
            {
                _audioPlayerService.LoadFile(FilePath);
                StatusText = msg + " (Archivo recargado)";
            }
        }
        else
        {
            StatusText = "Guardado de metadata cancelado.";
        }
    }

    private void OnPlaybackStateChanged(object? sender, NAudio.Wave.PlaybackState state)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CurrentPlaybackState = state;
            switch (state)
            {
                case NAudio.Wave.PlaybackState.Playing:
                    StatusText = "Reproduciendo...";
                    break;
                case NAudio.Wave.PlaybackState.Stopped:
                    StatusText = "Detenido.";
                    UpdatePositionDisplay();
                    break;
                case NAudio.Wave.PlaybackState.Paused:
                    StatusText = "En pausa.";
                    UpdatePositionDisplay();
                    break;
            }
        });
    }

    public void UpdatePosition()
    {
        if (_audioPlayerService.State == NAudio.Wave.PlaybackState.Playing)
        {
            UpdatePositionDisplay();
            WaveformPosition = _audioPlayerService.Position.TotalSeconds;
        }
    }

    public async void ExecuteAnalyzeCommand()
    {
        try { await ExecuteAnalyzeAsync(); }
        catch (Exception ex) { LoggerService.Log($"ExecuteAnalyzeCommand - Unhandled: {ex.Message}"); }
    }

    private void UpdatePositionDisplay()
    {
        PositionText = FormatTime(_audioPlayerService.Position);
        DurationText = FormatTime(_audioPlayerService.Duration);
    }

    public void SeekToPosition(double positionInSeconds)
    {
        if (_audioPlayerService != null && _audioPlayerService.Duration.TotalSeconds > 0)
        {
            // Clamp position to valid range
            positionInSeconds = Math.Max(0, Math.Min(_audioPlayerService.Duration.TotalSeconds, positionInSeconds));
            
            var newPosition = TimeSpan.FromSeconds(positionInSeconds);
            _audioPlayerService.Seek(newPosition);
            PositionText = FormatTime(newPosition);
            WaveformPosition = positionInSeconds;
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
    }

    public void HandleDragEnter(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && _filePickerService.ValidateAudioFile(files[0]))
            {
                e.Effects = DragDropEffects.Copy;
                StatusText = "Suelta el archivo de audio aquí";
                StatusState = "Success";
                return;
            }
        }
        e.Effects = DragDropEffects.None;
        StatusText = "Formato no válido - Solo archivos de audio";
        StatusState = "Error";
    }

    public void HandleDragLeave()
    {
        StatusText = "Listo";
        StatusState = "Normal";
    }

    public void HandleDrop(DragEventArgs e)
    {
        StatusText = "Listo";
        StatusState = "Normal";

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && _filePickerService.ValidateAudioFile(files[0]))
            {
                LoadAudioFile(files[0]);
            }
            else
            {
                StatusText = "Formato no válido - Solo archivos de audio";
                StatusState = "Error";
            }
        }
    }

    /// <summary>
    /// Intercambia el BPM principal con el BPM alternativo.
    /// Primer click: muestra el alternativo como principal.
    /// Segundo click: restaura el valor original.
    /// </summary>
    public void SwapBpmValues()
    {
        if (!CanSwapBpm) return;
 
        if (_hasSwappedBpm)
        {
            // Segundo click: restaurar al BPM original detectado
            _displayBpm = _originalBpm;
            _hasSwappedBpm = false;
            _alternativeBpmText = $"Alt: {_originalAlternativeBpm:F0} BPM";
        }
        else
        {
            // Primer click: mostrar el BPM alternativo como principal
            _displayBpm = _originalAlternativeBpm;
            _hasSwappedBpm = true;
            _alternativeBpmText = $"Original: {_originalBpm:F0} BPM";
        }
 
        OnPropertyChanged(nameof(BpmDisplayText));
        OnPropertyChanged(nameof(AlternativeBpmText));
        OnPropertyChanged(nameof(IsBpmModified));
        OnPropertyChanged(nameof(IsSwapped));
        OnPropertyChanged(nameof(IsBpmSwapped));
 
        if (IsPlayingScale)
        {
            StartScalePlayback(); // Restart with new BPM
        }
    }

    public void Cleanup()
    {
        StopScalePlayback();
        _audioPlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
        _audioPlayerService.Dispose();
        _toneGeneratorService.Dispose();
    }
}