@echo off

if "%~1"=="" (
    dotnet stryker --threshold-high 100 --threshold-low 95 -r progress -r json -r html
) else (
    dotnet stryker --threshold-high 100 --threshold-low 95 -r progress -r json -r html -m %*
)

StrykerReportTool\bin\Debug\StrykerReportTool.exe StrykerOutput %*

start StrykerOutput\mutation-report.html