# Quality gate for Project-Chilseok.
# Runs Unity in batch mode: compile, EditMode tests, PlayMode tests.
# Prints a human summary and, as the LAST line, "GATE_RESULT: {json}".
# Exit codes: 0 = PASS, 1 = FAIL, 2 = bad arguments, 3 = INCOMPLETE.
# Read-only for the project: writes only under Logs/QualityGate/.
# Messages are ASCII on purpose (Windows PowerShell 5.1).

param(
    [string[]]$Checks = @('compile', 'editmode', 'playmode'),
    [string]$UnityPath = '',
    [int]$TimeoutMinutes = 20
)

$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root

# Allow "-Checks compile,editmode" passed as one string.
$Checks = @($Checks | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ -ne '' })
$valid = @('compile', 'editmode', 'playmode')
foreach ($c in $Checks) {
    if ($valid -notcontains $c) {
        Write-Output "Unknown check '$c'. Valid: compile, editmode, playmode"
        exit 2
    }
}

$results = [ordered]@{}
foreach ($c in $valid) { if ($Checks -contains $c) { $results[$c] = [ordered]@{ status = 'INCOMPLETE'; summary = 'not run'; errors = @() } } }

function Finish {
    $overall = 'PASS'
    foreach ($k in $results.Keys) { if ($results[$k].status -eq 'INCOMPLETE') { $overall = 'INCOMPLETE' } }
    foreach ($k in $results.Keys) { if ($results[$k].status -eq 'FAIL') { $overall = 'FAIL' } }

    Write-Output ""
    Write-Output "GATE: $overall"
    foreach ($k in $results.Keys) {
        Write-Output ("- {0,-8}: {1} - {2}" -f $k, $results[$k].status, $results[$k].summary)
        foreach ($e in $results[$k].errors) { Write-Output "    $e" }
    }
    $json = [ordered]@{ result = $overall; checks = $results; logs = 'Logs/QualityGate' } | ConvertTo-Json -Compress -Depth 6
    Write-Output "GATE_RESULT: $json"
    switch ($overall) { 'PASS' { exit 0 } 'FAIL' { exit 1 } default { exit 3 } }
}

function SetAll([string]$status, [string]$summary) {
    foreach ($k in @($results.Keys)) { $results[$k].status = $status; $results[$k].summary = $summary }
}

# 1) Unity project present?
$versionFile = Join-Path $root 'ProjectSettings/ProjectVersion.txt'
if (-not (Test-Path $versionFile)) {
    SetAll 'INCOMPLETE' 'ProjectSettings/ProjectVersion.txt not found (Unity project not created yet)'
    Finish
}
$editorVersion = ((Get-Content $versionFile | Where-Object { $_ -like 'm_EditorVersion:*' } | Select-Object -First 1) -replace 'm_EditorVersion:\s*', '').Trim()

# 2) Find the Unity editor executable.
$candidates = @()
if ($UnityPath) { $candidates += $UnityPath }
if ($env:UNITY_EDITOR_PATH) { $candidates += $env:UNITY_EDITOR_PATH }
if ($editorVersion) {
    $candidates += "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
    $candidates += "/Applications/Unity/Hub/Editor/$editorVersion/Unity.app/Contents/MacOS/Unity"
    $candidates += "$HOME/Unity/Hub/Editor/$editorVersion/Editor/Unity"
}
$unity = $null
foreach ($p in $candidates) { if ($p -and (Test-Path $p)) { $unity = $p; break } }
if (-not $unity) {
    SetAll 'INCOMPLETE' "Unity $editorVersion editor not found. Set UNITY_EDITOR_PATH or pass -UnityPath."
    Finish
}

# 3) Editor already open on this project? Batch mode cannot open the same project.
$lock = Join-Path $root 'Temp/UnityLockfile'
if (Test-Path $lock) {
    $locked = $false
    try {
        $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $fs.Close()
    } catch { $locked = $true }
    if ($locked) {
        SetAll 'INCOMPLETE' 'Unity editor is open on this project. Close it and rerun (do not kill it).'
        Finish
    }
}

$logDir = Join-Path $root 'Logs/QualityGate'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

function RunUnity([string[]]$unityArgs, [string]$name) {
    $quoted = $unityArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    $p = Start-Process -FilePath $unity -ArgumentList ($quoted -join ' ') -PassThru -WindowStyle Hidden
    $null = $p.Handle   # ExitCode stays empty in PS 5.1 unless the handle is touched before waiting
    $done = $p.WaitForExit($TimeoutMinutes * 60 * 1000)
    if (-not $done) {
        # Our own batch process only; never touch the user's editor.
        try { $p.Kill() } catch { }
        return -999
    }
    return $p.ExitCode
}

function CompileErrors([string]$logPath) {
    if (-not (Test-Path $logPath)) { return @() }
    return @(Get-Content $logPath | Where-Object { $_ -match ': error CS\d+' } | Select-Object -Unique | Select-Object -First 20)
}

$compileFailed = $false

# 4) Compile
if ($results.Contains('compile')) {
    $log = Join-Path $logDir "$stamp-compile-editor.log"
    Write-Output "[gate] compile: Unity $editorVersion batch mode ..."
    $code = RunUnity @('-batchmode', '-nographics', '-quit', '-projectPath', $root, '-logFile', $log) 'compile'
    $errs = CompileErrors $log
    if ($code -eq -999) {
        $results['compile'].status = 'INCOMPLETE'; $results['compile'].summary = "timeout after $TimeoutMinutes min"
    } elseif ($errs.Count -gt 0) {
        $results['compile'].status = 'FAIL'; $results['compile'].summary = "$($errs.Count) compile error(s)"; $results['compile'].errors = $errs
        $compileFailed = $true
    } elseif ($code -ne 0) {
        $results['compile'].status = 'INCOMPLETE'; $results['compile'].summary = "Unity exited with code $code (see $log)"
    } else {
        $results['compile'].status = 'PASS'; $results['compile'].summary = 'no compile errors'
    }
}

# 5) Tests
function RunTests([string]$platform, [string]$key) {
    if (-not $results.Contains($key)) { return }
    if ($compileFailed) {
        $results[$key].status = 'INCOMPLETE'; $results[$key].summary = 'skipped: compile failed'
        return
    }
    $log = Join-Path $logDir "$stamp-$key-editor.log"
    $xml = Join-Path $logDir "$stamp-$key-results.xml"
    Write-Output "[gate] ${key}: running $platform tests ..."
    $code = RunUnity @('-batchmode', '-nographics', '-projectPath', $root, '-runTests', '-testPlatform', $platform, '-testResults', $xml, '-logFile', $log) $key
    if ($code -eq -999) {
        $results[$key].status = 'INCOMPLETE'; $results[$key].summary = "timeout after $TimeoutMinutes min"
        return
    }
    if (-not (Test-Path $xml)) {
        $errs = CompileErrors $log
        if ($errs.Count -gt 0) {
            $results[$key].status = 'FAIL'; $results[$key].summary = 'compile errors before tests'; $results[$key].errors = $errs
        } else {
            $results[$key].status = 'INCOMPLETE'; $results[$key].summary = "no results file (exit $code, see $log)"
        }
        return
    }
    [xml]$doc = Get-Content $xml -Raw
    $run = $doc.'test-run'
    $total = [int]$run.total; $passed = [int]$run.passed; $failed = [int]$run.failed; $skipped = [int]$run.skipped
    $failures = @()
    foreach ($tc in $doc.SelectNodes("//test-case[@result='Failed']")) {
        $msg = ''
        $node = $tc.SelectSingleNode('failure/message')
        if ($node) { $msg = ($node.InnerText -split "`n")[0].Trim() }
        $failures += "$($tc.fullname) - $msg"
    }
    if ($failed -gt 0) {
        $results[$key].status = 'FAIL'; $results[$key].summary = "$failed failed / $total total"; $results[$key].errors = @($failures | Select-Object -First 20)
    } elseif ($total -eq 0) {
        $results[$key].status = 'PASS'; $results[$key].summary = '0 tests found'
    } else {
        $results[$key].status = 'PASS'; $results[$key].summary = "$passed passed, $skipped skipped / $total total"
    }
}

RunTests 'EditMode' 'editmode'
RunTests 'PlayMode' 'playmode'

Finish
