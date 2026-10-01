param(
    [Parameter(Mandatory=$true)][string]$Source,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\godot\Assets\Music')
)
# Converts Suno downloads (WAV or MP3) into loudness-matched OGG files for the game (spec 2026-09-28).
$ErrorActionPreference = 'Stop'
$prefixes = 'title-','day-','night-','studio-','convention-','deadline-','good-news-','setback-'
# Works in Windows PowerShell 5.1 and PowerShell 7 (final review).
$found = Get-Command ffmpeg -ErrorAction SilentlyContinue
if(-not $found) { throw 'ffmpeg was not found. Install it with: winget install Gyan.FFmpeg' }
$ffmpeg = $found.Source
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$converted = 0
foreach($file in Get-ChildItem -LiteralPath $Source -File | Where-Object { $_.Extension -in '.wav','.mp3' }) {
    $name = $file.BaseName.ToLowerInvariant()
    if(-not ($prefixes | Where-Object { $name.StartsWith($_) })) { Write-Warning "Skipped $($file.Name): the name must start with one of $($prefixes -join ', ')"; continue }
    $out = Join-Path $OutputDirectory ($name + '.ogg')
    # A 1.5-second fade at the very end so every track finishes smoothly when it plays once in the game.
    $probe = Join-Path (Split-Path $found.Source) 'ffprobe.exe'
    $duration = [double]::Parse((& $probe -v error -show_entries format=duration -of csv=p=0 $file.FullName), [Globalization.CultureInfo]::InvariantCulture)
    $fadeStart = [Math]::Max(0, $duration - 1.5).ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
    & $ffmpeg -hide_banner -loglevel error -y -i $file.FullName -af "loudnorm=I=-16:TP=-1.5:LRA=11,afade=t=out:st=$($fadeStart):d=1.5" -ar 44100 -c:a libvorbis -q:a 5 $out
    if($LASTEXITCODE -ne 0) { throw "ffmpeg failed on $($file.Name)" }
    $converted++
    Write-Output "Converted $($file.Name) -> $out"
}
Write-Output "$converted file(s) converted."
