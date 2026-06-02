using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Services;
using AudioAnalyzer.Themes;
using AudioAnalyzer.ViewModels;

namespace AudioAnalyzer;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private DispatcherTimer? _positionTimer;

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

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IDependencyService, DependencyService>();
        services.AddSingleton<EssentiaWrapper>();
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
        services.AddSingleton<IBpmDetectorService, BpmDetector>();
        services.AddSingleton<IKeyDetector, KeyDetector>();
        services.AddSingleton<IWaveformAnalyzerService, WaveformAnalyzer>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<ILoudnessAnalyzerService, LoudnessAnalyzer>();
        services.AddSingleton<IToneGeneratorService, ToneGeneratorService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IAudioAnalysisPipeline, AudioAnalysisPipeline>();
        services.AddSingleton<IKeyDisplayService, KeyDisplayService>();
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        services.AddSingleton<IPlaybackController, PlaybackController>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Setup static logger instance for legacy access
        LoggerService.Instance = _serviceProvider.GetRequiredService<ILoggerService>();

        // Resolve MainViewModel
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        // Resolve and show MainWindow
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = viewModel;
        mainWindow.Show();

        // Perform background update check
        var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
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
        SetupPositionTimer(viewModel);
        _positionTimer?.Start();
    }

    private void SetupPositionTimer(MainViewModel viewModel)
    {
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _positionTimer.Tick += (s, ev) =>
        {
            viewModel.UpdatePosition();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _positionTimer?.Stop();
        
        // Ensure MainViewModel is cleaned up
        var viewModel = _serviceProvider.GetService<MainViewModel>();
        viewModel?.Cleanup();
        
        base.OnExit(e);
    }
}