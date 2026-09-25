param(
    [Parameter(Mandatory=$true)][string]$Godot,
    [string]$OutputDirectory = "builds/windows-alpha"
)
$ErrorActionPreference = 'Stop'
# The console forwarding launcher can keep waiting on inherited build-server
# handles after export. Launch the engine directly and capture its logs here.
if($Godot.EndsWith('_console.exe',[StringComparison]::OrdinalIgnoreCase)) {
    $directEngine=$Godot.Substring(0,$Godot.Length-'_console.exe'.Length)+'.exe'
    if(Test-Path -LiteralPath $directEngine) { $Godot=$directEngine }
}
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath((Join-Path $repo $OutputDirectory))
if(-not $output.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Package output must be inside this repository.' }
if(Test-Path -LiteralPath $output) { throw 'Choose an empty output directory to avoid mixing builds.' }
$template = Join-Path $repo 'TestResults/export-tools/windows_release_x86_64.exe'
if(-not (Test-Path -LiteralPath $template)) { throw 'Extract the matching official Godot .NET Windows export templates into TestResults/export-tools first. See docs/superpowers/private-alpha-testing.md.' }
New-Item -ItemType Directory -Path $output | Out-Null
$log = Join-Path $output 'export.log'
$err = Join-Path $output 'export-error.log'
$arguments = @('--headless','--path',('"'+(Join-Path $repo 'godot')+'"'),'--export-release','"Windows Private Alpha"',('"'+(Join-Path $output 'MangakaStudio.exe')+'"'),'--quit')
$process = Start-Process -FilePath $Godot -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput $log -RedirectStandardError $err -PassThru
# Wait for the exporter, not the persistent MSBuild server it can spawn.
if(-not $process.WaitForExit(180000)) { $process.Kill(); throw "Export timed out; see $log and $err" }
if($process.ExitCode -ne 0) { throw "Export failed; see $log and $err" }
Copy-Item -LiteralPath (Join-Path $repo 'docs/superpowers/private-alpha-testing.md') -Destination (Join-Path $output 'START-HERE.md')
Copy-Item -LiteralPath (Join-Path $repo 'docs/superpowers/private-alpha-credits.txt') -Destination (Join-Path $output 'CREDITS.txt')
# Engine-generated complete third-party notices are emitted by the export smoke below.
$smoke = Join-Path $repo 'TestResults/package-check'
New-Item -ItemType Directory -Path $smoke -Force | Out-Null
$arguments = @('--headless','--','--alpha-smoke',('"--alpha-output='+$smoke+'"'))
$process = Start-Process -FilePath (Join-Path $output 'MangakaStudio.exe') -ArgumentList $arguments -WorkingDirectory $output -WindowStyle Hidden -RedirectStandardOutput (Join-Path $smoke 'stdout.log') -RedirectStandardError (Join-Path $smoke 'stderr.log') -Wait -PassThru
if($process.ExitCode -ne 0) { throw "Exported game checks failed; see $smoke" }
Copy-Item -LiteralPath (Join-Path $smoke 'GODOT-LICENSES.txt') -Destination $output
$runtimeDirectory = Join-Path $output 'data_MangakaGame_windows_x86_64'
$runtime = Get-Content -LiteralPath (Join-Path $runtimeDirectory 'MangakaGame.runtimeconfig.json') -Raw | ConvertFrom-Json
$runtimeVersion = ($runtime.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
$packages = if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget/packages'}
$runtimePackage = Join-Path $packages ('microsoft.netcore.app.runtime.win-x64/'+$runtimeVersion)
foreach($name in @('LICENSE.TXT','THIRD-PARTY-NOTICES.TXT')){Copy-Item -LiteralPath (Join-Path $runtimePackage $name) -Destination (Join-Path $runtimeDirectory $name)}
foreach($file in @($log,$err)){Move-Item -LiteralPath $file -Destination (Join-Path $smoke ([IO.Path]::GetFileName($file))) -Force}
$archive = $output+'.zip'
if(Test-Path -LiteralPath $archive) { throw 'Archive already exists; choose a new output directory.' }
Compress-Archive -LiteralPath $output -DestinationPath $archive
Get-FileHash -LiteralPath $archive -Algorithm SHA256
