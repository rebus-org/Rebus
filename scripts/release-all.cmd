@echo off

set version=%1

set build=%~dp0\build-all.cmd
set push=%~dp0\push.cmd



REM == BUILD EVERYTHING ==

call %build% %1
if %ERRORLEVEL% neq 0 (
  echo Could not build all packages.
  exit /b 1
)




REM == PUSH PACKAGES ==

call %push% %1
if %ERRORLEVEL% neq 0 (
  echo Could not push packages.
  exit /b 1
)
