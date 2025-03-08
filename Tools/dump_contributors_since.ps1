#!/usr/bin/env pwsh

    [cmdletbinding()]

param(
    [Parameter(Mandatory=$true)]
    [DateTime]$since,

    [Nullable[DateTime]]$until);

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $(join-path $scriptDir contribs_shared.ps1)

$contentCP = & "$PSScriptRoot\dump_commits_since.ps1" -repo Corvinella-Project/space-station-14 -since $since -until $until

$contribs = ($contentCP) `
    | Select-Object -ExpandProperty author `
    | Select-Object -ExpandProperty login -Unique `
    | Where-Object { -not $ignore[$_] }`
    | ForEach-Object { if($replacements[$_] -eq $null){ $_ } else { $replacements[$_] }} `
    | Sort-Object `

$contribs = $contribs -join ", "
Write-Host $contribs
Write-Host "Total commit count is $($engine.Length + $content.Length)"
