# PreToolUse for Codex apply_patch. Keep diagnostics ASCII for Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)

function Block([string]$reason) {
    [Console]::Error.WriteLine("[protect-files] BLOCKED: $reason")
    [Console]::Error.WriteLine('Follow AGENTS.md and ADR-0003. Use Unity editor tooling for serialized assets.')
    exit 2
}

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    Block 'Invalid hook input.'
}

if ($payload.hook_event_name -ne 'PreToolUse' -or $payload.tool_name -ne 'apply_patch') { exit 0 }

$patch = $payload.tool_input.command
if ($patch -isnot [string] -or [string]::IsNullOrWhiteSpace($payload.cwd)) {
    Block 'Missing patch or session working directory.'
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$rootWithSep = $root.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
$protectedFolders = @('Assets/ThirdParty', 'Packages', 'ProjectSettings', 'Library', 'UserSettings', 'Temp')
$protectedExtensions = @(
    '.meta', '.unity', '.prefab', '.controller', '.asset', '.inputactions',
    '.anim', '.mat', '.overridecontroller', '.mask', '.playable', '.signal'
)

# Check each file header and both sides of a move. Hunk lines start with '+', '-' or ' '.
$headers = [regex]::Matches($patch, '(?m)^\*\*\* (?:Add File|Update File|Delete File|Move to):[ \t]*(?<path>[^\r\n]+)')
if ($headers.Count -eq 0) { Block 'No patch file headers were found.' }

foreach ($header in $headers) {
    $path = $header.Groups['path'].Value.Trim()
    try {
        $full = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine([string]$payload.cwd, $path))
    } catch {
        Block 'Invalid patch path.'
    }

    if (-not $full.StartsWith($rootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) { continue }

    $relative = $full.Substring($rootWithSep.Length).Replace('\', '/')
    foreach ($folder in $protectedFolders) {
        if ($relative -eq $folder -or $relative.StartsWith("$folder/", [System.StringComparison]::OrdinalIgnoreCase)) {
            Block "Protected folder: $folder."
        }
    }

    $extension = [System.IO.Path]::GetExtension($relative).ToLowerInvariant()
    if ($protectedExtensions -contains $extension) { Block "Unity serialized asset: $extension." }
}

exit 0
