@echo off

set /P VERSION=Enter version: 

echo Verify version: %VERSION%

set /P PROMPT=Y/y to confirm version: 

if [%PROMPT%] == [Y] goto :GO
if [%PROMPT%] == [y] goto :GO
goto :DONE

:GO
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe scripts\build.proj /v:m /t:build;pushNugetPackages /p:Version=%VERSION%

if %errorlevel% neq 0 goto :DONE
git tag %VERSION%
echo Tagged commit with tag '%VERSION%' - push tags to origin with 'git push --tags'.

:DONE
@prompt $p$g