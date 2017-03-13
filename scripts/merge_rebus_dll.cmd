@echo off

set targetdir=%1
set currentdir=%~dp0

if "%targetdir%"=="" (
    echo TargetDir was not passed as an argument.
    echo Please pass the TargetDir variable from the build script to 
    echo this script.
    goto exit_fail
)

set maindll=%targetdir%\Rebus.dll
set tempdll=%targetdir%\Rebus_original.dll
set jsondll=%targetdir%\Newtonsoft.Json.dll
set mergetargetdir=%targetdir%
set mergetargetdll=%mergetargetdir%\Rebus.dll
set ilmerge=%currentdir%\..\tools\ilmerge\ilmerge.exe

if not exist "%ilmerge%" (
    echo Could not find ILMerge here:
    echo.
    echo %ilmerge%
    echo.
    goto exit_fail
)

if exist "%tempdll%" (
    echo Deleting old %tempdll%
    del "%tempdll%" /Q
    if %ERRORLEVEL% neq 0 (
        echo IlMerge failed.
        exit /b 1
    )
)

echo Renaming "%maindll%" to "%tempdll%"
move "%maindll%" "%tempdll%"
if %ERRORLEVEL% neq 0 (
    echo IlMerge failed.
    exit /b 1
)

if exist "%mergetargetdll%" del "%mergetargetdll%"
if not exist "%mergetargetdir%" mkdir "%mergetargetdir%"

echo.
echo Merging
echo     %tempdll%
echo     %jsondll%
echo into
echo    %mergetargetdll%
echo.

"%ilmerge%" "%tempdll%" "%jsondll%" /targetplatform:"v4,%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5" /internalize /out:"%mergetargetdll%"
if %ERRORLEVEL% neq 0 (
    echo IlMerge failed.
    exit /b 1
)


goto success

:exit_fail

echo IlMerge failed.
exit /b 1


:success