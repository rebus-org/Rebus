@echo off

set scriptsdir=%~dp0
set root=%scriptsdir%\..
set deploydir=%root%\deploy
set version=%1

if "%version%"=="" (
	echo Please invoke the build script with a version as its single argument.
	echo.
	goto exit_fail
)

set Version=%version%

pushd %root%

git status

echo.
echo Are you sure you want to git clean -dxf and stuff?
echo.
pause

git clean -dxf
if %ERRORLEVEL% neq 0 (
	popd
	echo Could not clean.
 	goto exit_fail
)

dotnet restore --interactive
if %ERRORLEVEL% neq 0 (
	popd
	echo Could not restore NuGet packages.
 	goto exit_fail
)

dotnet pack "%root%/Rebus.Tests.Contracts" -c Release -o "%deploydir%" -p:PackageVersion=%version%;Version=%version% --no-restore
if %ERRORLEVEL% neq 0 (
	popd
	echo Could pack Rebus.Tests.Contracts
 	goto exit_fail
)

call scripts\push.cmd "%version%"

popd






goto exit_success

:exit_fail
exit /b 1


:exit_success