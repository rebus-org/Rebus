@echo off

set reporoot=%~dp0\..
set aversion=%reporoot%\tools\aversion\aversion
set projectdir=%1

if "%version%"=="" (
    "%aversion%" patch -ver 1.0.0 -in %projectdir%\Properties\AssemblyInfo.cs -out %projectdir%\Properties\AssemblyInfo_Patch.cs -token $version$
) else (
    "%aversion%" patch -ver %version% -in %projectdir%\Properties\AssemblyInfo.cs -out %projectdir%\Properties\AssemblyInfo_Patch.cs -token $version$
)

