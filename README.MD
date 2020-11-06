🔍 DICOM FIND
----------
[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fdcmfind%2Fbadge%3Fref%3Dmaster&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/dcmfind/goto?ref=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DcmFind?label=DcmFind&style=for-the-badge)](https://www.nuget.org/packages/DcmFind)

Find DICOM files on your disk.

Installation
------------

```
    dotnet tool install dcmfind --global
```

Usage
-----

```powershell
    dcmfind --query "AccessionNumber=abc"
```

Options 

```
  -d, --directory      (Default: .) Search for *.dcm files in this directory

  -f, --filePattern    (Default: *.dcm) Only query files that satisfy this file
                       pattern

  -r, --recursive      (Default: true) Search recursively in nested directories

  -l, --limit          (Default: 100) Limit results and stop finding after this
                       many results

  -q, --query          (Default: ) The query that should be applied

  --help               Display this help screen.

  --version            Display version information.
```

Query is the only required parameter, and supports the following syntax:

```
--query "(0008,0050)=abc"
--query "AccessionNumber=abc"
--query "StudyDate>=2020-09-08"
--query "StudyDate>2020-09-08"
--query "StudyDate<2020-09-08"
--query "StudyDate<=2020-09-08"
--query "AccessionNumber!=abc"
```
