#requires -Version 5.1
<#
.SYNOPSIS
    Restore VRChat blueprintIds from .blueprint-id.local into the corresponding scene files.

.DESCRIPTION
    Reads .blueprint-id.local (which Save-BlueprintId.ps1 produces) and writes each id
    back into the matching scene file's "blueprintId:" field. Idempotent: a scene whose
    value already matches the cache is left alone.

.EXAMPLE
    pwsh scripts/Restore-BlueprintId.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { throw 'Not in a git repository (git rev-parse --show-toplevel failed).' }
$repoRoot = $repoRoot.Trim()

$cachePath = Join-Path $repoRoot '.blueprint-id.local'
if (-not (Test-Path -LiteralPath $cachePath)) {
    throw ".blueprint-id.local not found at $cachePath - run Save-BlueprintId.ps1 first."
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$lines = [System.IO.File]::ReadAllLines($cachePath, $utf8)

$processedCount = 0
foreach ($rawLine in $lines) {
    $line = $rawLine.Trim()
    if (-not $line -or $line.StartsWith('#')) { continue }

    $eq = $line.IndexOf('=')
    if ($eq -lt 1) {
        Write-Warning "Invalid line in .blueprint-id.local: $rawLine"
        continue
    }

    $sceneRel = ($line.Substring(0, $eq).Trim()) -replace '\\', '/'
    $blueprintId = $line.Substring($eq + 1).Trim()

    if (-not $blueprintId) {
        Write-Warning "Empty blueprintId for $sceneRel - skipped."
        continue
    }

    $sceneAbs = Join-Path $repoRoot $sceneRel
    if (-not (Test-Path -LiteralPath $sceneAbs)) {
        Write-Warning "Scene file not found, skipping: $sceneRel"
        continue
    }

    $content = [System.IO.File]::ReadAllText($sceneAbs, $utf8)

    # Match both empty and populated blueprintId lines.
    $pattern = '(?m)^(?<indent>[ \t]*)blueprintId:[ \t]*(?<existing>\S*)[ \t]*$'
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        Write-Warning "No blueprintId field found in $sceneRel"
        continue
    }

    $existing = $match.Groups['existing'].Value
    if ($existing -eq $blueprintId) {
        Write-Host "Already restored: $sceneRel"
        $processedCount++
        continue
    }
    if ($existing -and $existing -ne $blueprintId) {
        Write-Warning "blueprintId in $sceneRel differs from cache (scene: $existing, cache: $blueprintId) - overwriting with cache value."
    }

    $replaced = [regex]::Replace($content, $pattern, ('${indent}blueprintId: ' + $blueprintId))
    [System.IO.File]::WriteAllText($sceneAbs, $replaced, $utf8)
    Write-Host "Restored blueprintId in $sceneRel"
    $processedCount++
}

if ($processedCount -eq 0) {
    Write-Warning "No entries processed from .blueprint-id.local"
}
