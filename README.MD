DICOM FIND
----------

Find DICOM files on your disk.

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
