<#
.SYNOPSIS
    Refreshes the upstream blobatar assets this repository ships verbatim.

.DESCRIPTION
    Downloads the published npm package and copies its built motion.css, gaze.css and gaze.js into the
    Blazor library's wwwroot with an attribution header, and copies the golden fixture and the
    cross-language reference vectors from a clone of the upstream repository into the test project.

    Run it after bumping the upstream version the port tracks, then run the tests: a changed golden
    fixture means the renderer has to be re-ported before the tests pass again.

.PARAMETER NpmVersion
    The npm package version whose dist/ assets to ship.

.PARAMETER UpstreamRef
    The git ref of https://github.com/Alain00/blobatar to take the fixtures from.
#>
[CmdletBinding()]
param(
    [string] $NpmVersion = "2.7.0",
    [string] $UpstreamRef = "44bdffda5c956563eed11dacfc4bb982e5f333f7"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("blobatar-sync-" + [guid]::NewGuid().ToString("n"))
New-Item -ItemType Directory -Path $work | Out-Null

try {
    $tarball = Join-Path $work "blobatar.tgz"
    Invoke-WebRequest -Uri "https://registry.npmjs.org/blobatar/-/blobatar-$NpmVersion.tgz" -OutFile $tarball
    tar -xzf $tarball -C $work
    $dist = Join-Path $work "package/dist"
    $wwwroot = Join-Path $root "src/Blobatar.Blazor/wwwroot"

    $header = "https://github.com/Alain00/blobatar | MIT License, Copyright (c) Alain00 | shipped verbatim by Blobatar.Blazor"
    "/*! blobatar motion.css $NpmVersion | $header */`n" + (Get-Content (Join-Path $dist "motion.css") -Raw) | Set-Content (Join-Path $wwwroot "motion.css") -NoNewline
    "/*! blobatar gaze.css $NpmVersion | $header */`n" + (Get-Content (Join-Path $dist "gaze.css") -Raw) | Set-Content (Join-Path $wwwroot "gaze.css") -NoNewline
    $js = (Get-Content (Join-Path $dist "gaze.js")) | Where-Object { $_ -notmatch '^//# ' }
    ("// blobatar gaze.js $NpmVersion | $header (source map comments removed)") + "`n" + ($js -join "`n") + "`n" | Set-Content (Join-Path $wwwroot "gaze.js") -NoNewline
    Write-Host "wwwroot assets updated from blobatar@$NpmVersion"

    $clone = Join-Path $work "upstream"
    git clone --quiet https://github.com/Alain00/blobatar.git $clone
    git -C $clone checkout --quiet $UpstreamRef
    $fixtures = Join-Path $root "tests/Blobatar.Tests/Fixtures"
    Copy-Item (Join-Path $clone "packages/blobatar/test/golden/gen2.txt") (Join-Path $fixtures "gen2.txt")
    Copy-Item (Join-Path $clone "packages/flutter/test/fixtures/reference-vectors.json") (Join-Path $fixtures "reference-vectors.json")
    Write-Host "fixtures updated from $UpstreamRef"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
