$projectName = "DcmFind"
$projectPath = Resolve-Path (Join-Path $PSScriptRoot "./$projectName/")
$csProjPath = Resolve-Path (Join-Path $projectPath "$projectName.csproj")

[xml]$csproj = Get-Content $csprojPath

$version = $csproj.Project.PropertyGroup.Version

Write-Host "Packing version $version"

dotnet pack $csprojPath --configuration Release

$nupkgFile = Resolve-Path (Join-Path "$projectPath/nupkg" "$projectName.$version.nupkg")

Write-Host "Publishing NuGet package file"

nuget push $nupkgFile -skipduplicate -source NuGet
nuget push $nupkgFile -skipduplicate -source Github