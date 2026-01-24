REM This script deletes all bin and obj folders

@echo off

set "ParentDir=%CD%\.."

for %%f in (bin obj) do (
    for /f "delims=" %%d in ('dir /ad /b /s "%ParentDir%\%%f" 2^>nul') do (
        echo Deleting folder: %%d
        rd /s /q "%%d"
    )
)