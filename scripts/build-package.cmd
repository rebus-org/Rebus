@echo off

REM --------------------------------------------------------------------------
REM This script is an adaptation of the other build script
REM --------------------------------------------------------------------------

set name=%1
set version=%2
set reporoot=%~dp0\..

if "%name%"=="" (
  echo Please remember to specify the name to build as an argument.
  goto exit_fail
)

if "%version%"=="" (
  echo Please remember to specify which version to build as an argument.
  goto exit_fail
)

set msbuild=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe

if not exist "%msbuild%" (
  echo Could not find MSBuild here:
  echo.
  echo   "%msbuild%"
  echo.
  goto exit_fail
)

set proj=%reporoot%\%name%\%name%.csproj

if not exist "%proj%" (
  echo Could not find project to build here:
  echo.
  echo    "%proj%"
  echo.
  goto exit_fail
)

set nuget=%reporoot%\tools\NuGet\NuGet.exe

if not exist "%nuget%" (
  echo Could not find NuGet here:
  echo.
  echo    "%nuget%"
  echo.
  goto exit_fail
)

set ilmerge=%reporoot%\tools\ilmerge\ilmerge.exe

if not exist "%ilmerge%" (
  echo Could not find IlMerge here:
  echo.
  echo    "%ilmerge%"
  echo.
  goto exit_fail
)

set destination=%reporoot%\deploy

if not exist "%destination%" (
  mkdir "%destination%"
  if %ERRORLEVEL% neq 0 (
    echo Could not create %destination%
    goto exit_fail
  )
)

set assemblyinfo=%reporoot%\%name%\Properties\GeneratedAssemblyInfo.cs


"%msbuild%" "%proj%" /p:Configuration=Release /t:rebuild
if %ERRORLEVEL% neq 0 (
  echo Build failed - error %ERRORLEVEL%.
  goto exit_fail
)

REM HACK - merge Newtonsoft into Rebus.dll
if "%name%"=="Rebus" (
  if exist "%reporoot%\Rebus\bin\Release\merged" rd "%reporoot%\Rebus\bin\Release\merged" /s/q
  mkdir "%reporoot%\Rebus\bin\Release\merged"

  "%ilmerge%" "%reporoot%\Rebus\bin\Release\Rebus.dll" "%reporoot%\Rebus\bin\Release\Newtonsoft.Json.dll" /targetplatform:"v4,%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5" /internalize /out:"%reporoot%\Rebus\bin\Release\merged\Rebus.dll"
  if %ERRORLEVEL% neq 0 (
    echo IlMerge failed.
    goto exit_fail
  )
)

"%nuget%" pack "%name%\%name%.nuspec" -OutputDirectory "%destination%" -Version %version%
if %ERRORLEVEL% neq 0 (
  echo NuGet pack failed.
  goto exit_fail
)

goto exit




:exit_fail

echo An error occurred.
exit /b 1



:exit