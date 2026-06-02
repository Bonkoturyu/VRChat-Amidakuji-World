#requires -Version 5.1
<#
.SYNOPSIS
    Save the VRChat blueprintId from a scene file into a local cache (.blueprint-id.local),
    then clear the value in the scene file (sets it to an empty string).

.DESCRIPTION
    The VRChat blueprintId is account-specific and should not be committed to the repository.
    Run this script before "git commit" to move the id to .blueprint-id.local (which is gitignored).
    Run Restore-BlueprintId.ps1 after commit to put the id back into the scene file.

.PARAMETER SceneFile
    Path to the target scene file, relative to the repository root.
    Default: Assets/Scenes/VRCDefaultWorldScene.unity

.EXAMPLE
    pwsh scripts/Save-BlueprintId.ps1
#>
[CmdletBinding()]
param(
    [string]$SceneFile = 'Assets/Scenes/VRCDefaultWorldScene.unity'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) { throw 'Not in a git repository (git rev-parse --show-toplevel failed).' }
$repoRoot = $repoRoot.Trim()

$normalizedScene = $SceneFile -replace '\\', '/'
$sceneAbs = Join-Path $repoRoot $normalizedScene
if (-not (Test-Path -LiteralPath $sceneAbs)) {
    throw "Scene file not found: $sceneAbs"
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$content = [System.IO.File]::ReadAllText($sceneAbs, $utf8)

# Capture indent and id. Use [ \t]* (not \s*) so that CRLF is preserved. \r? handles CRLF scene files.
$pattern = '(?m)^(?<indent>[ \t]*)blueprintId:[ \t]*(?<id>wrld_[\w-]+)[ \t]*\r?$'
$match = [regex]::Match($content, $pattern)

if (-not $match.Success) {
    Write-Host "No blueprintId to save in $normalizedScene (field is empty or absent)."
    exit 0
}

$blueprintId = $match.Groups['id'].Value
Write-Host "Found blueprintId in ${normalizedScene}: $blueprintId"

# #10: 形式検証。捕捉(=クリア対象)の regex はあえて広いまま(wrld_[\w-]+)にする。
# 厳格 UUID regex で捕捉自体を絞ると、誤編集された不正値を取りこぼして "No blueprintId to
# save" で素通りし、ID を scene に残したままコミット → 漏洩、という逆方向の事故になる。
# よってクリアは必ず行い、「捕捉した値が正規の wrld_<UUID> か」を警告でのみ知らせる。
$canonicalBlueprintId = '^wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
if ($blueprintId -notmatch $canonicalBlueprintId) {
    Write-Warning "blueprintId '$blueprintId' は正規の wrld_<UUID> 形式ではありません。scene が誤編集されていないか確認してください(安全のためクリア処理は続行します)。"
}

# Update .blueprint-id.local (replace existing line for this scene, or append).
$cachePath = Join-Path $repoRoot '.blueprint-id.local'
$lines = @()
if (Test-Path -LiteralPath $cachePath) {
    $lines = [System.IO.File]::ReadAllLines($cachePath, $utf8)
}

$found = $false
$newLines = New-Object System.Collections.Generic.List[string]
foreach ($line in $lines) {
    if ($line -match "^\s*$([regex]::Escape($normalizedScene))\s*=") {
        $newLines.Add("$normalizedScene=$blueprintId")
        $found = $true
    } else {
        $newLines.Add($line)
    }
}
if (-not $found) {
    if ($newLines.Count -eq 0) {
        $newLines.Add('# VRChat Blueprint IDs - local cache (gitignored).')
        $newLines.Add('# Format: <scene-file-relative-path>=<blueprintId>')
    }
    $newLines.Add("$normalizedScene=$blueprintId")
}

[System.IO.File]::WriteAllLines($cachePath, $newLines, $utf8)
Write-Host "Saved to .blueprint-id.local"

# Clear the blueprintId line in the scene file, preserving indent and CRLF.
$replaced = [regex]::Replace($content, $pattern, '${indent}blueprintId: ')
# #10: 置換が実際に効いたか検証する。マッチしたのに内容が変わらない場合、ID は未クリアのまま
# コミットされ漏洩する。非 0 終了(throw + $ErrorActionPreference='Stop')で失敗を顕在化させる。
if ($replaced -eq $content) {
    throw "blueprintId のクリアに失敗しました(置換後も scene の内容が変化していません): $normalizedScene"
}
[System.IO.File]::WriteAllText($sceneAbs, $replaced, $utf8)
Write-Host "Cleared blueprintId in $normalizedScene"
