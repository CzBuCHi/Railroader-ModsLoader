@echo off

if "%~1"=="" (
    dotnet stryker --threshold-high 100 --threshold-low 95 -r progress -r json -r html -o
) else (
    dotnet stryker --threshold-high 100 --threshold-low 95 -r progress -r json -r html -o -m %*
)