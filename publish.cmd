SET nugetversion=1.0.3
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.nupkg -source nuget.org
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.nupkg -source Github
pause
