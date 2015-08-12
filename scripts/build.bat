@echo off

echo Rebus 2 build script

if "%1%"=="" (
  echo.
  echo Please specify a version as an argument!
  echo.
  goto exit
)

echo Building version %1% 

msbuild "%~dp0\build.proj" /t:package /p:Version=%1%

:exit