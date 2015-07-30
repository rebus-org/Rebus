@echo off

echo Rebus 2 push packages script

msbuild "%~dp0/build.proj" /t:push