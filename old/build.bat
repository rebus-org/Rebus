@echo off
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe scripts\build.proj /v:m /p:Version=%1