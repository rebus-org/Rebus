@echo off
if [%1] == [] goto NO_VERSION

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe scripts\build.proj /v:m /t:build;pushNugetPackages /p:Version=%1
goto DONE

:NO_VERSION
echo Syntax: push_packages ^<version^>

:DONE