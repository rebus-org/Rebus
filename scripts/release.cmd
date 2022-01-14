@echo off

set scriptsdir=%~dp0
set root=%scriptsdir%\..
set deploydir=%root%\deploy
set project=%1
set version=%2

if "%project%"=="" (
	echo Please invoke the build script with a project name as its first argument.
	echo.
	goto exit_fail
)

if "%version%"=="" (
	echo Please invoke the build script with a version as its second argument.
	echo.
	goto exit_fail
)

set Version=%version%

if exist "%deploydir%" (
	rd "%deploydir%" /s/q
)

pushd %root%

dotnet restore --interactive
if %ERRORLEVEL% neq 0 (
	popd
 	goto exit_fail
)

dotnet pack "%root%/Rebus" -c Release -o "%deploydir%" -p:PackageVersion=%version%;Version=%version% --no-restore
if %ERRORLEVEL% neq 0 (
	popd
 	goto exit_fail
)

call scripts\push.cmd "%version%"

popd






goto exit_success
:exit_fail
exit /b 1
:exit_success