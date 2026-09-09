@echo off
set DIR=%~dp0
where gradle >nul 2>&1
if %ERRORLEVEL%==0 (
  gradle %*
) else (
  echo Use Android Studio: File - Open this folder, then Run.
  echo Or install Gradle 8.2 and run: gradle assembleDebug
)
