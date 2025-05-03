@echo off
echo Start Single EXE Build...
echo.

:: 빌드 환경 설정
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set Configuration=Release

:: Visual Studio MSBuild 경로 설정 (COM 참조 지원)
set MSBUILD_PATH=
for %%i in (2022 2019 2017) do (
    if exist "C:\Program Files\Microsoft Visual Studio\%%i\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\%%i\Community\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "C:\Program Files\Microsoft Visual Studio\%%i\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\%%i\Professional\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "C:\Program Files\Microsoft Visual Studio\%%i\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\%%i\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\%%i\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%i\Community\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\%%i\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%i\Professional\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\%%i\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%i\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        goto :found_msbuild
    )
)

:: MSBuild가 발견되지 않은 경우 확인
if "%MSBUILD_PATH%" == "" (
    echo Visual Studio MSBuild not found.
    echo Please check if Visual Studio 2017, 2019 or 2022 is installed.
    pause
    exit /b 1
)

:found_msbuild
echo Visual Studio MSBuild 경로: %MSBUILD_PATH%

:: 이전 빌드 결과 정리
if exist "bin\SingleExe" rd /s /q "bin\SingleExe"
md "bin\SingleExe"

:: 프로젝트 빌드 및 발행
echo Building and publishing project with MSBuild...
"%MSBUILD_PATH%" HwpToPdf.csproj /t:Publish /p:Configuration=%Configuration% /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false /p:PublishDir=bin\SingleExe

:: 결과 표시
echo.
if %ERRORLEVEL% EQU 0 (
    echo Build successful! Single EXE file created.
    echo Path: %CD%\bin\SingleExe\HwpToPdf.exe
) else (
    echo Build failed! Please check the error.
)

echo.
pause 