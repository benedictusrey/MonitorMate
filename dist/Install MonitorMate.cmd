@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-MonitorMate.ps1"
if errorlevel 1 pause
