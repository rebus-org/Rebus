@echo off

set scriptsdir=%~dp0
set root=%scriptsdir%\..
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
 	goto exit_fail
)

dotnet restore
if %ERRORLEVEL% neq 0 (
	popd
 	goto exit_fail
)

dotnet msbuild "/p:Configuration=Release;Version=%version%" "%root%\Rebus.Tests.Contracts"
if %ERRORLEVEL% neq 0 (
	popd
 	goto exit_fail
)

popd






goto exit_success
:exit_fail
exit /b 1
:exit_success