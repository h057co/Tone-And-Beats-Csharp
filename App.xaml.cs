using System.Windows;
using System.Windows.Threading;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Services;
using AudioAnalyzer.Themes;
using AudioAnalyzer.ViewModels;

namespace AudioAnalyzer;

public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            if (args.Name.StartsWith("ToneAndBeatsByHostility,"))
            {
                return System.Reflection.Assembly.GetExecutingAssembly();
            }
            return null;
        };
    }

    private MainViewModel? _viewModel;
    private DispatcherTimer? _positionTimer;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Create services (in a real app, use a DI container like Microsoft.Extensions.DependencyInjection)
        IDependencyService dependencyService = new DependencyService();
        var essentiaWrapper = new EssentiaWrapper(dependencyService);
        IAudioPlayerService audioPlayerService = new AudioPlayerService();
        IBpmDetectorService bpmDetectorService = new BpmDetector(essentiaWrapper);
        IKeyDetector keyDetectorService = new KeyDetector();
        IWaveformAnalyzerService waveformAnalyzerService = new WaveformAnalyzer();
        IFilePickerService filePickerService = new FilePickerService();
        IMessageBoxService messageBoxService = new MessageBoxService();
        ILoudnessAnalyzerService loudnessAnalyzerService = new LoudnessAnalyzer(dependencyService);
        IToneGeneratorService toneGeneratorService = new ToneGeneratorService();
        IUpdateService updateService = new UpdateService();
        IAudioAnalysisPipeline analysisPipeline = new AudioAnalysisPipeline(
            bpmDetectorService,
            keyDetectorService,
            waveformAnalyzerService,
            loudnessAnalyzerService);

        // Create ViewModel with injected dependencies (DIP)
        _viewModel = new MainViewModel(
            audioPlayerService,
            bpmDetectorService,
            keyDetectorService,
            waveformAnalyzerService,
            filePickerService,
            messageBoxService,
            loudnessAnalyzerService,
            analysisPipeline,
            toneGeneratorService,
            dependencyService,
            updateService);

        // Create and show MainWindow
        var mainWindow = new MainWindow
        {
            DataContext = _viewModel
        };

        mainWindow.Show();

        // Perform background update check
        Task.Run(async () =>
        {
            var update = await updateService.CheckForUpdatesAsync();
            if (update != null)
            {
                LoggerService.Log($"[Updater] New version available: {update.Version}");
                // In a future phase, we could show a banner in MainWindow here
            }
        });

        // Initialize theme after window is loaded
        ThemeManager.Initialize();

        // Setup position timer for playback sync
        SetupPositionTimer();
        _positionTimer?.Start();
    }

    private void SetupPositionTimer()
    {
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _positionTimer.Tick += (s, e) =>
        {
            _viewModel?.UpdatePosition();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _positionTimer?.Stop();
        _viewModel?.Cleanup();
        base.OnExit(e);
    }
}