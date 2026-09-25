param([ValidateSet('Create','Poll')][string]$Action='Poll', [ValidateSet('Helper','Mom','Dad')][string]$Character='Helper')
$ErrorActionPreference='Stop'
$folder=if($Character -eq 'Helper'){'helper-chan'}else{$Character.ToLowerInvariant()}
$dir=Join-Path $PSScriptRoot ('../TestResults/tripo-chibi-trial/'+$folder)
$modelName=$folder+'-rigged.glb'
$record=Join-Path $dir 'rig-task.json'
$key=[Environment]::GetEnvironmentVariable('TRIPO_API_KEY','User')
if([string]::IsNullOrWhiteSpace($key)){$key=$env:TRIPO_API_KEY}
if([string]::IsNullOrWhiteSpace($key)){throw 'TRIPO_API_KEY is unavailable.'}
$headers=@{Authorization="Bearer $($key.Trim())"}
try {
    if($Action -eq 'Create') {
        if(Test-Path -LiteralPath $record){throw 'Rig submission already recorded; poll it instead.'}
        $source=Get-Content (Join-Path $dir 'task.json') -Raw | ConvertFrom-Json
        $check=Get-Content (Join-Path $dir 'rig-check.json') -Raw | ConvertFrom-Json
        if($source.status -ne 'success' -or !$check.output.riggable){throw 'Successful model and rig check required.'}
        $balance=Invoke-RestMethod 'https://openapi.tripo3d.ai/v3/account/balance' -Headers $headers
        if($balance.code -ne 0 -or $balance.data.balance -lt 25){throw 'Insufficient balance for the approved 25-credit rig.'}
        $body=@{input=$source.task_id;model='v1.0-20240301';rig_type='biped';spec='mixamo';out_format='glb'} | ConvertTo-Json
        $body | Set-Content (Join-Path $dir 'rig-request.json') -Encoding utf8
        @{status='submitting';created_at=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content $record -Encoding utf8
        $reply=Invoke-RestMethod 'https://openapi.tripo3d.ai/v3/animations/rig' -Method Post -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec 90
        if($reply.code -ne 0 -or !$reply.data.task_id){throw 'Rig submission returned no task ID; inspect account before retrying.'}
        @{task_id=$reply.data.task_id;status='submitted'} | ConvertTo-Json | Set-Content $record -Encoding utf8
        Write-Output "Rig submitted: $($reply.data.task_id)"
    } else {
        $saved=Get-Content $record -Raw | ConvertFrom-Json
        if(!$saved.task_id){throw 'Uncertain submission; do not resubmit.'}
        $reply=Invoke-RestMethod "https://openapi.tripo3d.ai/v3/tasks/$($saved.task_id)" -Headers $headers -TimeoutSec 45
        if($reply.code -ne 0){throw 'Task query failed.'}
        $result=$reply.data
        Write-Output "Status: $($result.status); progress: $($result.progress); credits: $($result.credits_consumed)"
        if($result.status -eq 'success') {
            if(!$result.output.model_url){throw 'No rigged model URL.'}
            Invoke-WebRequest $result.output.model_url -OutFile (Join-Path $dir $modelName) -TimeoutSec 90
            @{task_id=$saved.task_id;status='success';credits_consumed=$result.credits_consumed;model=$modelName} | ConvertTo-Json | Set-Content $record -Encoding utf8
            $balance=Invoke-RestMethod 'https://openapi.tripo3d.ai/v3/account/balance' -Headers $headers
            Write-Output "Downloaded rig. Available credits: $($balance.data.balance); frozen: $($balance.data.frozen)"
        }
    }
} catch {
    if($_.Exception.Response){Write-Output "Tripo HTTP failure: $([int]$_.Exception.Response.StatusCode)";exit 1}
    throw
} finally {$headers.Clear();$key=$null}
