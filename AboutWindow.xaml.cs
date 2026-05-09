using System.Diagnostics;
using System.Reflection;
using System.Windows;
using AudioAnalyzer.Helpers;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;
using AudioAnalyzer.Services;
using AudioAnalyzer.Themes;

namespace AudioAnalyzer;

public partial class AboutWindow : Window
{
    private readonly IUpdateService? _updateService;
    private UpdateInfo? _currentUpdate;

    public AboutWindow() : this(null) { }

    public AboutWindow(IUpdateService? updateService)
    {
        _updateService = updateService;
        LoggerService.Log("AboutWindow - Constructor iniciado");
        InitializeComponent();
        
        // Cap initial height to available screen work area (accounts for taskbar)
        var workArea = SystemParameters.WorkArea;
        if (Height > workArea.Height)
        {
            Height = workArea.Height;
            LoggerService.Log($"AboutWindow - Height capped to {Height} (screen work area: {workArea.Height})");
        }
        
        LoadEmbeddedImages();
        SetVersionText();
        ThemeManager.ThemeChanged += (s, e) => Dispatcher.BeginInvoke(LoadEmbeddedImages);

        if (_updateService != null)
        {
            CheckForUpdatesInternal();
        }

        LoggerService.Log("AboutWindow - Constructor completado");
    }

    private async void CheckForUpdatesInternal()
    {
        if (_updateService == null) return;

        UpdateStatusText.Text = "ESTADO: BUSCANDO...";
        UpdateStatusText.Foreground = System.Windows.Media.Brushes.White;
        UpdateActionButton.IsEnabled = false;

        try
        {
            _currentUpdate = await _updateService.CheckForUpdatesAsync();

            if (_currentUpdate != null)
            {
                UpdateStatusText.Text = $"ESTADO: ACTUALIZACIÓN DISPONIBLE (v{_currentUpdate.Version})";
                UpdateStatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                UpdateDetailsText.Text = "NUEVA VERSIÓN DETECTADA EN GITHUB";
                UpdateActionButton.Content = "[ DESCARGAR ]";
            }
            else
            {
                UpdateStatusText.Text = "ESTADO: AL DÍA";
                UpdateStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                UpdateDetailsText.Text = "ESTÁS UTILIZANDO LA ÚLTIMA VERSIÓN";
                UpdateActionButton.Content = "[ RE-COMPROBAR ]";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "ESTADO: ERROR DE CONEXIÓN";
            UpdateStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            UpdateDetailsText.Text = "NO SE PUDO CONECTAR CON GITHUB";
            LoggerService.Log($"AboutWindow.CheckForUpdatesInternal - Error: {ex.Message}");
        }
        finally
        {
            UpdateActionButton.IsEnabled = true;
        }
    }

    private async void UpdateActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUpdate == null)
        {
            CheckForUpdatesInternal();
            return;
        }

        if (UpdateActionButton.Content.ToString() == "[ REINICIAR ]")
        {
            _updateService?.ApplyUpdateAndRestart(_downloadedPath);
            return;
        }

        await StartDownloadFlow();
    }

    private string _downloadedPath = "";

    private async Task StartDownloadFlow()
    {
        if (_updateService == null || _currentUpdate == null) return;

        UpdateActionButton.IsEnabled = false;
        UpdateProgressArea.Visibility = Visibility.Visible;
        UpdateDetailsText.Text = "DESCARGANDO NUEVO EJECUTABLE...";

        try
        {
            var progress = new Progress<double>(p =>
            {
                UpdateProgressBar.Value = p;
                UpdateProgressText.Text = $"DESCARGANDO: {p:F0}%";
            });

            _downloadedPath = await _updateService.DownloadUpdateAsync(progress);

            UpdateStatusText.Text = "ESTADO: LISTO PARA INSTALAR";
            UpdateStatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            UpdateDetailsText.Text = "DESCARGA COMPLETADA CON ÉXITO";
            UpdateActionButton.Content = "[ REINICIAR ]";
            UpdateProgressArea.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "ESTADO: ERROR EN DESCARGA";
            UpdateDetailsText.Text = "FALLÓ LA DESCARGA DEL ARCHIVO";
            LoggerService.Log($"AboutWindow.StartDownloadFlow - Error: {ex.Message}");
        }
        finally
        {
            UpdateActionButton.IsEnabled = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void SetVersionText()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            if (version != null)
            {
                VersionText.Text = $"Versión {version.Major}.{version.Minor}.{version.Build}";
                LoggerService.Log($"AboutWindow - Versión detectada: {VersionText.Text}");
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log($"AboutWindow - Error al obtener versión: {ex.Message}");
            VersionText.Text = "Versión 1.1.0"; // Fallback
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LoggerService.Log("AboutWindow.CloseButton_Click - Cerrando ventana");
        Close();
        LoggerService.Log("AboutWindow.CloseButton_Click - Ventana cerrada");
    }

    private void KoFiButton_Click(object sender, RoutedEventArgs e)
    {
        LoggerService.Log("AboutWindow.KoFiButton_Click - Abriendo ko-fi");
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://ko-fi.com/hostilityme",
            UseShellExecute = true
        });
        LoggerService.Log("AboutWindow.KoFiButton_Click - Navegador abierto");
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }

    private void QrImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        QrOverlay.Opacity = 0;
        QrOverlay.Visibility = Visibility.Visible;
        
        var animation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        QrOverlay.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void QrOverlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var animation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        
        animation.Completed += (s, a) => 
        {
            QrOverlay.Visibility = Visibility.Collapsed;
        };
        
        QrOverlay.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    /// <summary>
    /// Carga todas las imágenes incrustadas (EmbeddedResources) desde el assembly.
    /// Utiliza EmbeddedResourceHelper para centralizar la lógica de carga.
    /// </summary>
    private void LoadEmbeddedImages()
    {
        var qr = EmbeddedResourceHelper.LoadImage("qrdonaciones.png");
        if (qr != null)
        {
            if (FindName("QrImage") is System.Windows.Controls.Image qrImg) qrImg.Source = qr;
            if (FindName("QrOverlayImage") is System.Windows.Controls.Image qrOverlayImg) qrOverlayImg.Source = qr;
            LoggerService.Log("AboutWindow.LoadEmbeddedImages - ✓ QR image loaded (Main & Overlay)");
        }

        var currentTheme = ThemeManager.CurrentTheme;
        var logoFile = (currentTheme == "Light" || currentTheme == "iOS Light") 
            ? "HOST_NEGRO.png" 
            : "HOST_BLANCO.png";
        var logo = EmbeddedResourceHelper.LoadImage(logoFile);
        if (logo != null && FindName("LogoImageAbout") is System.Windows.Controls.Image logoImg)
        {
            logoImg.Source = logo;
            LoggerService.Log("AboutWindow.LoadEmbeddedImages - ✓ Logo image loaded");
        }
    }
}
