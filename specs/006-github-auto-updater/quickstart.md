# Quickstart: Implementing GitHub Auto-Updater

## 1. API Integration
The core logic resides in `Services/UpdateService.cs`.

```csharp
public async Task<UpdateRelease?> CheckForUpdatesAsync()
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", "ToneAndBeatsUpdater");
    var json = await client.GetStringAsync("https://api.github.com/repos/HostilityMusic/ToneAndBeats/releases/latest");
    // Parse TagName and Assets
}
```

## 2. UI Integration
Add a button to `AboutWindow.xaml`:

```xml
<Button Content="BUSCAR ACTUALIZACIONES" 
        Click="OnCheckUpdatesClick" 
        Style="{StaticResource BrutalistButton}"/>
```

## 3. The "Silent" Handover
When the user clicks "REINICIAR Y ACTUALIZAR":

1. Start `powershell.exe` with a command like:
   `Start-Sleep -s 2; Move-Item "temp_new.exe" "original.exe" -Force; Start-Process "original.exe"`
2. Exit current application: `Application.Current.Shutdown();`

## 4. Testing
Mock the GitHub API response or change your local version to `1.1.0` in `.csproj` to trigger a fake "Update Found" scenario.
