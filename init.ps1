<#
.SYNOPSIS
    Prepares a machine to build and test this project.
.PARAMETER Localization
    Install the MicroBuild localization plugin for loc builds on desktop machines.
.PARAMETER Signing
    Install the MicroBuild signing plugin for test-signed builds on desktop machines.
#>
Param(
    [Parameter()]
    [switch]$Localization,
    [Parameter()]
    [switch]$Signing
)

Push-Location $PSScriptRoot
try {
    $toolsPath = "$PSScriptRoot\tools"

    # First restore NuProj packages since the solution restore depends on NuProj evaluation succeeding.
    gci "$PSScriptRoot\src\project.json" -rec |? { $_.FullName -imatch 'nuget' } |% {
        & "$toolsPath\Restore-NuGetPackages.ps1" -Path $_
    }

    & "$toolsPath\Restore-NuGetPackages.ps1" -Path "$PSScriptRoot\src"

    $MicroBuildPackageSource = 'https://devdiv.pkgs.visualstudio.com/DefaultCollection/_packaging/MicroBuildToolset/nuget/v3/index.json'
    if ($Localization) {
        Write-Host "Installing MicroBuild localization plugin"
        & "$toolsPath\Install-NuGetPackage.ps1" MicroBuild.Plugins.Localization -source $MicroBuildPackageSource
        $env:LocType = "Pseudo"
        $env:LocLanguages = "VS"
    }

    if ($Signing) {
        Write-Host "Installing MicroBuild signing plugin"
        & "$toolsPath\Install-NuGetPackage.ps1" MicroBuild.Plugins.Signing -source $MicroBuildPackageSource
        $env:SignType = "Test"
    }

    Write-Host "Successfully restored all dependencies" -ForegroundColor Green
}
catch {
    Write-Error "Aborting script due to error"
    exit $lastexitcode
}
finally {
    Pop-Location
}