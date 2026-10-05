param(
    [Parameter(Mandatory=$true)][string]$Source,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\godot\Assets\Sfx')
)
# Cuts the CC0 source recordings into the game's sound effects (Q67, record docs/superpowers/sound-effects-completion.md).
# -Source is a folder holding the files exactly as downloaded: Freesound HQ previews (<id>_<user>-hq.ogg) and the
# unzipped Kenney packs (kenney_interface-sounds, kenney_rpg-audio). Every source and its licence is listed in
# godot/Assets/Sfx/README.md.
$ErrorActionPreference = 'Stop'
$found = Get-Command ffmpeg -ErrorAction SilentlyContinue
if(-not $found) { throw 'ffmpeg was not found. Install it with: winget install Gyan.FFmpeg' }
$ffmpeg = $found.Source
$inv = [Globalization.CultureInfo]::InvariantCulture
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
# Output, source, start and length in seconds (0 length keeps the whole file), target mean loudness in dB.
$cuts = @(
    @('pencil-01', '376705_3302313-hq.ogg', 0.8, 1.6, -27),
    @('pencil-02', '376705_3302313-hq.ogg', 5.3, 1.6, -27),
    @('pencil-03', '376705_3302313-hq.ogg', 10.5, 1.6, -27),
    @('pencil-04', '278159_3409809-hq.ogg', 0.3, 1.5, -27),
    @('pencil-05', '278159_3409809-hq.ogg', 3.0, 1.5, -27),
    @('page-01', 'kenney_rpg-audio\Audio\bookFlip1.ogg', 0, 0, -33),
    @('page-02', 'kenney_rpg-audio\Audio\bookFlip3.ogg', 0, 0, -33),
    @('click', 'kenney_interface-sounds\Audio\click_001.ogg', 0, 0, -31),
    @('phone-buzz', '515295_9159316-hq.ogg', 0, 0, -28)
)
function Measure-Mean([string]$file) {
    $line = & $ffmpeg -hide_banner -nostats -i $file -af volumedetect -f null - 2>&1 | Select-String 'mean_volume: ([-0-9.]+) dB'
    if(-not $line) { throw "Could not measure $file" }
    [double]::Parse($line.Matches[0].Groups[1].Value, $inv)
}
$temp = Join-Path ([IO.Path]::GetTempPath()) 'mangaka-sfx'
New-Item -ItemType Directory -Force -Path $temp | Out-Null
foreach($cut in $cuts) {
    $name, $file, $start, $length, $target = $cut
    $in = Join-Path $Source $file
    if(-not (Test-Path -LiteralPath $in)) { throw "Missing source $in" }
    $raw = Join-Path $temp "$name.wav"
    $trim = if($length -gt 0) { @('-ss', $start.ToString($inv), '-t', $length.ToString($inv)) } else { @() }
    # Short fades so a cut never starts or ends with a click.
    $fades = if($length -gt 0) { "afade=t=in:d=0.05,afade=t=out:st=$(($length - 0.15).ToString($inv)):d=0.15" } else { 'anull' }
    # The pencil recordings are quiet, so their hiss is reduced before they are raised to the target level.
    if($name.StartsWith('pencil')) { $fades = "afftdn=nr=12:nf=-50,$fades" }
    & $ffmpeg -hide_banner -loglevel error -y @trim -i $in -af $fades -ac 1 -ar 44100 $raw
    if($LASTEXITCODE -ne 0) { throw "ffmpeg failed cutting $name" }
    $gain = ($target - (Measure-Mean $raw)).ToString('0.##', $inv)
    $out = Join-Path $OutputDirectory "$name.ogg"
    & $ffmpeg -hide_banner -loglevel error -y -i $raw -af "volume=$($gain)dB,alimiter=limit=0.7:level=false" -c:a libvorbis -q:a 5 $out
    if($LASTEXITCODE -ne 0) { throw "ffmpeg failed encoding $name" }
    Write-Output "$name.ogg  gain $gain dB"
}
# The room tone loops: the last two seconds cross-fade into the start, so the join is seamless.
$room = Join-Path $Source '135097_658546-hq.ogg'
$roomRaw = Join-Path $temp 'room-tone.wav'
& $ffmpeg -hide_banner -loglevel error -y -i $room -filter_complex "[0:a]atrim=2:32,asetpts=PTS-STARTPTS[a];[0:a]atrim=0:2,asetpts=PTS-STARTPTS[b];[a][b]acrossfade=d=2:c1=tri:c2=tri" -ac 1 -ar 44100 $roomRaw
if($LASTEXITCODE -ne 0) { throw 'ffmpeg failed looping the room tone' }
$roomGain = (-42 - (Measure-Mean $roomRaw)).ToString('0.##', $inv)
& $ffmpeg -hide_banner -loglevel error -y -i $roomRaw -af "volume=$($roomGain)dB" -c:a libvorbis -q:a 4 (Join-Path $OutputDirectory 'room-tone.ogg')
if($LASTEXITCODE -ne 0) { throw 'ffmpeg failed encoding the room tone' }
Write-Output "room-tone.ogg  gain $roomGain dB"
Remove-Item -Recurse -Force $temp
