# SessionStart hook: stdout is added to Codex developer context at the start of a session.
# Shows branch, uncommitted changes, recent commits, Unity editor status and docs/progress.md.

$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.Encoding]::UTF8

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root

Write-Output "=== Project-Chilseok session start ==="

$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
    Write-Output "Branch: $branch"

    $changes = @(git --no-optional-locks status --porcelain 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Uncommitted changes: unavailable (git status failed)."
    } else {
        Write-Output "Uncommitted changes: $($changes.Count) file(s)"
        if ($changes.Count -gt 0) {
            $changes | Select-Object -First 15 | ForEach-Object { Write-Output "  $_" }
            if ($changes.Count -gt 15) { Write-Output "  ..." }
        }
    }

    Write-Output "Recent commits:"
    git log --oneline -n 5 2>$null | ForEach-Object { Write-Output "  $_" }
} else {
    Write-Output "git not found on PATH."
}

# Unity project / editor status (quality-gate.ps1 needs both)
$versionFile = Join-Path $root 'ProjectSettings/ProjectVersion.txt'
if (Test-Path $versionFile) {
    $line = Get-Content $versionFile | Where-Object { $_ -like 'm_EditorVersion:*' } | Select-Object -First 1
    Write-Output "Unity project: $line"
    if (Test-Path (Join-Path $root 'Temp/UnityLockfile')) {
        Write-Output "Unity editor appears to be OPEN on this project (batch tests will be INCOMPLETE)."
    }
} else {
    Write-Output "WARNING: ProjectSettings/ProjectVersion.txt not found. Unity project is not created yet (progress.md Phase 0)."
}

$progress = Join-Path (Join-Path $root 'docs') 'progress.md'
if (Test-Path $progress) {
    Write-Output ""
    Write-Output "=== docs/progress.md ==="
    Get-Content -Path $progress -Encoding UTF8 -TotalCount 80 | ForEach-Object { Write-Output $_ }
}

Write-Output ""
Write-Output "Reminder: read AGENTS.md rules. Plan before code. One phase per session. Run unity-quality-gate before saying done."
exit 0
