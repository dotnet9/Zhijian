@echo off
setlocal enabledelayedexpansion

set "project_paths=src\Zhijian"
set "platforms=win-x64 linux-x64 osx-x64 osx-arm64"

call "%~dp0publishbase.bat" "%project_paths%" "%platforms%"
