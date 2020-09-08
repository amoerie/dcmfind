SET nugetversion=1.0.0
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.nupkg -source nuget.org
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.symbols.nupkg -source nuget.org
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.nupkg -source Github
nuget push ./DcmFind/nupkg/DcmFind.%nugetversion%.symbols.nupkg -source Github
pause
