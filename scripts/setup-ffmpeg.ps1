$ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$destDir = Join-Path $PSScriptRoot "..\dependencies\ffmpeg"
$tempZip = Join-Path $env:TEMP "ffmpeg-essentials.zip"
$tempExtract = Join-Path $env:TEMP "ffmpeg-extract"

Write-Host "--- FFmpeg Provisioning Script ---" -ForegroundColor Cyan

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

Write-Host "Downloading FFmpeg from Gyan.dev..."
Invoke-WebRequest -Uri $ffmpegUrl -OutFile $tempZip

Write-Host "Extracting binaries..."
if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
Expand-Archive -Path $tempZip -DestinationPath $tempExtract

$binPath = Get-ChildItem -Path $tempExtract -Filter "ffmpeg.exe" -Recurse | Select-Object -First 1 -ExpandProperty DirectoryName

if ($binPath) {
    Write-Host "Copying binaries to $destDir..."
    Copy-Item -Path (Join-Path $binPath "ffmpeg.exe") -Destination $destDir -Force
    Copy-Item -Path (Join-Path $binPath "ffprobe.exe") -Destination $destDir -Force
    Write-Host "Done! FFmpeg is now available in dependencies/ffmpeg/" -ForegroundColor Green
} else {
    Write-Error "Could not find ffmpeg.exe in the downloaded archive."
}

# Cleanup
Remove-Item $tempZip -Force
Remove-Item $tempExtract -Recurse -Force
