#requires -Version 5.1
<#
.SYNOPSIS
Creates the approved Project-Chilseok backlog from production/backlog-draft.md.
.DESCRIPTION
Run from your own GitHub CLI session after reviewing the draft. Existing labels,
milestones and matching issues are reused. Changes to existing issues are manual.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$utf8 = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

$repository = 'Folktale-Studio/Project-Chilseok'
$repoEndpoint = "repos/$repository"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$draftPath = Join-Path $root 'production/backlog-draft.md'
$labelColors = [ordered]@{
    logic = '1D76DB'
    level = '0E8A16'
    render = '5319E7'
    cinematic = 'B60205'
    art = 'D93F0B'
    asset = 'FBCA04'
    qa = '0075CA'
    infra = '006B75'
    'nice-to-have' = 'C5DEF5'
}
# 서울 마감 시간(23:59:59)을 GitHub API의 UTC로 바꾼 값이다.
$dueDates = [ordered]@{
    '11/22' = '2026-11-22T14:59:59Z'
    '11/23' = '2026-11-23T14:59:59Z'
    '11/24' = '2026-11-24T14:59:59Z'
}

function Read-Backlog {
    $source = [IO.File]::ReadAllText($draftPath, $utf8)
    $headings = [regex]::Matches($source, '(?m)^## (?<id>B[0-9]{2,}) (?<title>[^\r\n]+)\r?$')
    if ($headings.Count -eq 0) { throw "No Bxx issue headings in $draftPath." }

    $known = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $titles = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for ($i = 0; $i -lt $headings.Count; $i++) {
        $heading = $headings[$i]
        $id = $heading.Groups['id'].Value
        $title = $heading.Groups['title'].Value.Trim()
        if ($known.ContainsKey($id)) { throw "Duplicate draft ID $id." }
        if (-not $title -or -not $titles.Add($title)) { throw "Empty or duplicate title at $id." }

        $start = $heading.Index + $heading.Length
        $end = if ($i + 1 -lt $headings.Count) { $headings[$i + 1].Index } else { $source.Length }
        $section = $source.Substring($start, $end - $start)
        $bodyStart = $section.IndexOf('### 목적', [StringComparison]::Ordinal)
        if ($bodyStart -lt 0) { throw "$id is missing the purpose section." }

        $fields = @{}
        $header = $section.Substring(0, $bodyStart)
        $entries = [regex]::Matches($header, '(?m)^- (?<key>템플릿|선행|단계|라벨|마일스톤): (?<value>[^\r\n]+)\r?$')
        foreach ($entry in $entries) {
            $key = $entry.Groups['key'].Value
            if ($fields.ContainsKey($key)) { throw "$id repeats a metadata field." }
            $fields[$key] = $entry.Groups['value'].Value.Trim()
        }
        if ($fields.Count -ne 5) { throw "$id is missing required metadata." }
        if ($fields['템플릿'] -notin @('feature', 'zone', 'asset', 'tuning', 'bug')) { throw "$id has an invalid template." }
        if ($fields['단계'] -notin @('자동', '승인', '결정')) { throw "$id has an invalid work stage." }

        $milestone = $fields['마일스톤']
        if (-not $dueDates.Contains($milestone)) { throw "$id has an invalid milestone." }
        $labels = @($fields['라벨'] -split ',' | ForEach-Object { $_.Trim() })
        foreach ($label in $labels) {
            if (-not $labelColors.Contains($label)) { throw "$id has an unknown label: $label." }
        }

        $dependencies = @()
        if ($fields['선행'] -ne '—') {
            $dependencies = @($fields['선행'] -split ',' | ForEach-Object { $_.Trim() })
            if (@($dependencies | Sort-Object -Unique).Count -ne $dependencies.Count) {
                throw "$id lists a dependency more than once."
            }
            foreach ($dependency in $dependencies) {
                if (-not $known.ContainsKey($dependency)) { throw "$id depends on missing or later issue $dependency." }
                if ([string]::CompareOrdinal($known[$dependency], $milestone) -gt 0) {
                    throw "$id is due before its dependency $dependency."
                }
            }
        }

        $body = $section.Substring($bodyStart).Trim() -replace '(?m)^### ', '## '
        $completion = [regex]::Match($body, '(?ms)^## 완료 조건\r?\n(?<items>.*?)(?=^## |\z)')
        if (-not $completion.Success -or $completion.Groups['items'].Value -notmatch '(?m)^- \[ \] \S') {
            throw "$id is missing an unchecked completion criterion."
        }
        if ($body -notmatch '(?m)^## QA 유형\r?$' -or $body -notmatch '(?m)^## 범위 밖\r?$') {
            throw "$id is missing a QA type or out-of-scope section."
        }

        $known.Add($id, $milestone)
        [pscustomobject]@{
            Id = $id
            Title = $title
            Dependencies = $dependencies
            Stage = $fields['단계']
            Labels = $labels
            Milestone = $milestone
            Body = $body
        }
    }
}

function Get-GitHubCollection([string]$endpoint) {
    $output = & $gh api --hostname github.com --paginate --slurp $endpoint
    if ($LASTEXITCODE -ne 0) { throw "GitHub listing failed: $endpoint." }

    # 바깥 배열을 속성에 두어 PowerShell의 파이프라인 배열 펼침을 피한다.
    $envelope = ('{"pages":' + ($output -join [char]10) + '}') | ConvertFrom-Json
    foreach ($page in $envelope.pages) {
        foreach ($item in $page) { $item }
    }
}

function New-GitHubResource([string]$endpoint, [hashtable]$payload) {
    $json = $payload | ConvertTo-Json -Compress -Depth 6
    $output = $json | & $gh api --hostname github.com --method POST $endpoint --input -
    if ($LASTEXITCODE -ne 0) { throw "GitHub creation failed: $endpoint." }
    return ($output -join [char]10) | ConvertFrom-Json
}

function Get-ExistingIssue($draft, [array]$issues) {
    $marker = "<!-- chilseok-backlog: $($draft.Id) -->"
    $sameId = @($issues | Where-Object { $_.body -and ([string]$_.body).Contains($marker) })
    $sameTitle = @($issues | Where-Object { [string]::Equals($_.title, $draft.Title, [StringComparison]::Ordinal) })
    if ($sameId.Count -gt 1 -or $sameTitle.Count -gt 1) {
        throw "$($draft.Id) has multiple GitHub matches. Resolve them before running again."
    }
    if ($sameId.Count -eq 1) {
        if ($sameTitle.Count -eq 1 -and $sameTitle[0].number -ne $sameId[0].number) {
            throw "$($draft.Id) matches different GitHub issues by ID and title."
        }
        return $sameId[0]
    }
    if ($sameTitle.Count -eq 1) {
        if ([string]$sameTitle[0].body -match '<!-- chilseok-backlog: B[0-9]{2,} -->') {
            throw "$($draft.Id) matches the title of a different backlog ID."
        }
        return $sameTitle[0]
    }
}

$drafts = @(Read-Backlog)
$ghCommand = Get-Command gh -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $ghCommand) { throw 'Install GitHub CLI (gh) and authenticate in your own terminal.' }
$gh = $ghCommand.Source

$issues = @(Get-GitHubCollection "$repoEndpoint/issues?state=all&per_page=100" | Where-Object { -not $_.pull_request })
$existing = @{}
$usedNumbers = [Collections.Generic.HashSet[int]]::new()
foreach ($draft in $drafts) {
    $match = Get-ExistingIssue $draft $issues
    if ($match) {
        if (-not $usedNumbers.Add([int]$match.number)) { throw "More than one draft resolves to #$($match.number)." }
        $existing[$draft.Id] = $match
    }
}

$labels = @(Get-GitHubCollection "$repoEndpoint/labels?per_page=100")
$milestones = @(Get-GitHubCollection "$repoEndpoint/milestones?state=all&per_page=100")
$milestoneNumbers = @{}
foreach ($title in $dueDates.Keys) {
    $matches = @($milestones | Where-Object { $_.title -eq $title })
    if ($matches.Count -gt 1) { throw "Duplicate GitHub milestones titled $title." }
    if ($matches.Count -eq 1) { $milestoneNumbers[$title] = [int]$matches[0].number }
}

foreach ($label in $labelColors.Keys) {
    if ($labels.name -notcontains $label) {
        $null = New-GitHubResource "$repoEndpoint/labels" @{ name = $label; color = $labelColors[$label] }
        Write-Output "Created label $label"
    }
}
foreach ($title in $dueDates.Keys) {
    if (-not $milestoneNumbers.ContainsKey($title)) {
        $milestone = New-GitHubResource "$repoEndpoint/milestones" @{ title = $title; due_on = $dueDates[$title] }
        if ([int]$milestone.number -lt 1) { throw "GitHub did not return a milestone number for $title." }
        $milestoneNumbers[$title] = [int]$milestone.number
        Write-Output "Created milestone $title"
    }
}

$numbers = @{}
$createdCount = 0
$lf = [string][char]10
foreach ($draft in $drafts) {
    $id = $draft.Id
    if ($existing.ContainsKey($id)) {
        $numbers[$id] = [int]$existing[$id].number
        Write-Output "$id reused #$($numbers[$id]) $($existing[$id].html_url)"
        continue
    }

    $references = @(foreach ($dependency in $draft.Dependencies) { "#$($numbers[$dependency])" })
    $dependencyText = if ($references.Count) { $references -join ', ' } else { '없음' }
    $body = @(
        "<!-- chilseok-backlog: $id -->"
        "선행: $dependencyText"
        "단계: $($draft.Stage)"
        ''
        $draft.Body
    ) -join ($lf + $lf)
    $issue = New-GitHubResource "$repoEndpoint/issues" @{
        title = $draft.Title
        body = $body
        labels = @($draft.Labels)
        milestone = $milestoneNumbers[$draft.Milestone]
    }
    if ([int]$issue.number -lt 1) { throw "GitHub did not return an issue number for $id. Check before retrying." }
    $numbers[$id] = [int]$issue.number
    $createdCount++
    Write-Output "$id created #$($numbers[$id]) $($issue.html_url)"
}

Write-Output "Backlog registered: $createdCount created, $($existing.Count) reused."
