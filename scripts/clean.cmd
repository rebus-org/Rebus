@echo off

set reporoot=%~dp0\..
set destination=%reporoot%\deploy

if exist "%destination%" (
  rd "%destination%" /s/q
  if %ERRORLEVEL% neq 0 (
    echo Could not clean up %destination%
    goto exit_fail
  )


)

goto exit





:exit_fail

echo An error occurred.
exit /b 1




:exit

