#!/usr/bin/env pwsh

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $(Join-Path $scriptDir "contribs_shared.ps1")

if ($null -eq $env:GITHUB_TOKEN)
{
    throw "A GitHub API token is required to run this script properly without being rate limited. If you're a user, generate a personal access token and use that. If you're running this in a GitHub action, make sure you expose the GITHUB_TOKEN secret as an environment variable."
}

function load_contribs([string] $repo)
{
    $qParams = @{
        "per_page" = 100
        "anon" = 1
        "state" = "closed"
    }

    $headers = @{
        Authorization="Bearer $env:GITHUB_TOKEN"
    }

    $url = "https://api.github.com/repos/{0}/pulls" -f $repo

    $contributors = @()

    while ($null -ne $url)
    {
        $resp = Invoke-WebRequest -Uri $url -Body $qParams -Headers $headers

        $linkHeader = $resp.Headers["Link"]
        if ($linkHeader -and $linkHeader -match '<([^>]+)>; rel="next"')
        {
            $url = $resp.RelationLink.next
        }
        else
        {
            $url = $null
        }

        $prs = $resp.Content | ConvertFrom-Json

        foreach ($pr in $prs)
        {
            $contributors += $pr.user.login

            foreach ($assignee in $pr.assignees)
            {
                $contributors += $assignee.login
            }

            foreach ($reviewer in $pr.requested_reviewers)
            {
                $contributors += $reviewer.login
            }
        }
    }

    $contributors += "Sh18RW"

    $uniqueContributors = $contributors | Sort-Object -Unique

    return $uniqueContributors
}

$contentCPJson = load_contribs("Corvinella-Project/space-station-14")

$contentCPJson -join ", "
