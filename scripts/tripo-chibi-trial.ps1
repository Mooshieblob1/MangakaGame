param([ValidateSet('Create','Poll','RigCheck','RigStatus')][string]$Action='Poll', [ValidateSet('Mangaka','Helper','Mom','Dad')][string]$Character='Mangaka')
$ErrorActionPreference='Stop'
$tripoDirectory=Join-Path $PSScriptRoot '../TestResults/tripo-chibi-trial'
if($Character -eq 'Helper') { $tripoDirectory=Join-Path $tripoDirectory 'helper-chan' }
if($Character -in @('Mom','Dad')) { $tripoDirectory=Join-Path $tripoDirectory $Character.ToLowerInvariant() }
$tripoModelName=if($Character -eq 'Helper'){'helper-chan.glb'}elseif($Character -in @('Mom','Dad')){$Character.ToLowerInvariant()+'.glb'}else{'chibi-mangaka.glb'}
New-Item -ItemType Directory -Force -Path $tripoDirectory | Out-Null
$tripoTaskFile=Join-Path $tripoDirectory 'task.json'
$tripoKey=[Environment]::GetEnvironmentVariable('TRIPO_API_KEY','User')
if([string]::IsNullOrWhiteSpace($tripoKey)) { $tripoKey=$env:TRIPO_API_KEY }
if([string]::IsNullOrWhiteSpace($tripoKey)) { throw 'TRIPO_API_KEY is not available.' }
$tripoHeaders=@{Authorization="Bearer $($tripoKey.Trim())"}
try {
    if($Action -eq 'Create') {
        if(Test-Path -LiteralPath $tripoTaskFile) {
            $tripoPrevious=Get-Content -LiteralPath $tripoTaskFile -Raw | ConvertFrom-Json
            if($tripoPrevious.status -ne 'rejected') { throw 'A trial already exists. Poll the existing task instead of spending credits again.' }
        }
        $tripoBalance=Invoke-RestMethod -Uri 'https://openapi.tripo3d.ai/v3/account/balance' -Headers $tripoHeaders -TimeoutSec 30
        $tripoCost=if($Character -ne 'Mangaka'){30}else{20}
        if($tripoBalance.code -ne 0 -or $tripoBalance.data.balance -lt $tripoCost) { throw "At least $tripoCost available API credits are needed for this trial." }
        $tripoRequest=@{
            model='v3.1-20260211'
            prompt='A single tiny chibi adult manga artist for a cozy isometric game, exactly 2.5 heads tall. Oversized rounded head, very short stubby legs, small torso, simple mitten hands. Short black hair, teal cardigan over a cream shirt, charcoal trousers, simple dark shoes. Cute clean anime toy style, no mouth, large simple eyes. Complete full body, neutral symmetrical A-pose, arms slightly away from body, feet flat and separated. No props, no pedestal, no background. Solid colors, matte materials, clean simple shapes readable at a tiny screen size. Suitable for rigging and seated desk poses.'
            negative_prompt='long legs, realistic adult proportions, tall body, tiny head, mouth, pedestal, base, scenery, furniture, accessories, handheld items, text, extra limbs, merged feet'
            face_limit=8000; texture=$true; pbr=$true; texture_quality='standard'
            geometry_quality='standard'; smart_low_poly=$false; quad=$false; generate_parts=$false
            model_seed=19960401; image_seed=19960401; texture_seed=19960401
        }
        $tripoEndpoint='text-to-model'
        if($Character -ne 'Mangaka') {
            $tripoImage=Get-Item -LiteralPath (Join-Path $tripoDirectory 'chibi-reference.png')
            $tripoUpload=Invoke-RestMethod -Uri 'https://openapi.tripo3d.ai/v3/files' -Method Post -Headers $tripoHeaders -Form @{file=$tripoImage} -TimeoutSec 90
            if($tripoUpload.code -ne 0 -or -not $tripoUpload.data.file_token) { throw 'Image upload did not return a file token.' }
            $tripoRequest=@{
                model='v3.1-20260211'; input=$tripoUpload.data.file_token
                face_limit=12000; texture=$true; pbr=$true; texture_quality='standard'
                geometry_quality='standard'; smart_low_poly=$false; quad=$false; generate_parts=$false
                model_seed=19960402; texture_seed=19960402; texture_alignment='original_image'
                orientation='align_image'; enable_image_autofix=$false
            }
            $tripoEndpoint='image-to-model'
        }
        $tripoJson=$tripoRequest | ConvertTo-Json -Depth 8
        $tripoJson | Set-Content -LiteralPath (Join-Path $tripoDirectory 'request.json') -Encoding utf8
        # Preserve an uncertain submission rather than automatically retrying a billable POST.
        @{status='submitting';created_at=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath $tripoTaskFile -Encoding utf8
        $tripoReply=Invoke-RestMethod -Uri "https://openapi.tripo3d.ai/v3/generation/$tripoEndpoint" -Method Post -Headers $tripoHeaders -ContentType 'application/json' -Body $tripoJson -TimeoutSec 90
        if($tripoReply.code -ne 0 -or -not $tripoReply.data.task_id) { throw "Tripo rejected submission (code $($tripoReply.code))." }
        @{task_id=$tripoReply.data.task_id;status='submitted';created_at=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath $tripoTaskFile -Encoding utf8
        Write-Output "Trial submitted: $($tripoReply.data.task_id)"
    } elseif($Action -eq 'RigCheck') {
        $tripoRigFile=Join-Path $tripoDirectory 'rig-check.json'
        if(Test-Path -LiteralPath $tripoRigFile) { throw 'Rig check already exists. Use RigStatus.' }
        $tripoSaved=Get-Content -LiteralPath $tripoTaskFile -Raw | ConvertFrom-Json
        if($tripoSaved.status -ne 'success') { throw 'Download the successful model before checking riggability.' }
        $tripoReply=Invoke-RestMethod -Uri 'https://openapi.tripo3d.ai/v3/animations/rig-check' -Method Post -Headers $tripoHeaders -ContentType 'application/json' -Body (@{input=$tripoSaved.task_id}|ConvertTo-Json) -TimeoutSec 45
        if($tripoReply.code -ne 0 -or -not $tripoReply.data.task_id) { throw 'Rig check did not return a task ID.' }
        @{task_id=$tripoReply.data.task_id;status='submitted'} | ConvertTo-Json | Set-Content -LiteralPath $tripoRigFile -Encoding utf8
        Write-Output 'Free riggability check submitted.'
    } elseif($Action -eq 'RigStatus') {
        $tripoRigFile=Join-Path $tripoDirectory 'rig-check.json'
        $tripoSaved=Get-Content -LiteralPath $tripoRigFile -Raw | ConvertFrom-Json
        $tripoReply=Invoke-RestMethod -Uri "https://openapi.tripo3d.ai/v3/tasks/$($tripoSaved.task_id)" -Headers $tripoHeaders -TimeoutSec 45
        if($tripoReply.code -ne 0) { throw 'Rig check status query failed.' }
        $tripoResult=$tripoReply.data
        $tripoRig=@{task_id=$tripoSaved.task_id;status=$tripoResult.status;output=$tripoResult.output;credits_consumed=$tripoResult.credits_consumed}
        $tripoRig | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $tripoRigFile -Encoding utf8
        $tripoRig | ConvertTo-Json -Depth 8
    } else {
        $tripoSaved=Get-Content -LiteralPath $tripoTaskFile -Raw | ConvertFrom-Json
        if(-not $tripoSaved.task_id) { throw 'Submission outcome is uncertain; check Tripo task history before retrying.' }
        $tripoReply=Invoke-RestMethod -Uri "https://openapi.tripo3d.ai/v3/tasks/$($tripoSaved.task_id)" -Headers $tripoHeaders -TimeoutSec 45
        if($tripoReply.code -ne 0) { throw "Tripo query failed (code $($tripoReply.code))." }
        $tripoResult=$tripoReply.data
        Write-Output "Status: $($tripoResult.status); progress: $($tripoResult.progress); credits: $($tripoResult.credits_consumed)"
        if($tripoResult.status -eq 'success') {
            $tripoModelUrl=$tripoResult.output.model_url
            if(-not $tripoModelUrl) { throw 'Successful task returned no model_url.' }
            # Signed output URLs are used only for download, not printed or committed.
            Invoke-WebRequest -Uri $tripoModelUrl -OutFile (Join-Path $tripoDirectory $tripoModelName) -TimeoutSec 90
            @{task_id=$tripoSaved.task_id;status='success';credits_consumed=$tripoResult.credits_consumed;model=$tripoModelName} | ConvertTo-Json | Set-Content -LiteralPath $tripoTaskFile -Encoding utf8
            Write-Output "Downloaded $tripoModelName"
        }
    }
} catch {
    # Never print HTTP headers, credentials, or full request/response objects.
    if($_.Exception.Response) {
        $tripoStatus=[int]$_.Exception.Response.StatusCode
        if($Action -eq 'Create' -and $tripoStatus -in @(400,401,403,422,429)) {
            @{status='rejected';http_status=$tripoStatus;created_at=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath $tripoTaskFile -Encoding utf8
        }
        Write-Output "Tripo HTTP failure: $tripoStatus"; exit 1
    }
    throw
} finally { $tripoHeaders.Clear();$tripoKey=$null }
