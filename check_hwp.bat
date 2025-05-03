@echo off
title 한글 프로그램 설치 및 COM 등록 확인

echo 한글 프로그램 설치 및 COM 등록 확인 스크립트
echo ---------------------------------------
echo.

rem 한글 설치 경로 확인
set "HWP_PATH_1=C:\Program Files (x86)\HNC\HOffice9"
set "HWP_PATH_2=C:\Program Files\HNC\HOffice9"
set "HWP_PATH_3=C:\Program Files (x86)\Hnc\Office\Hwp80"
set "HWP_PATH_4=C:\Program Files\Hnc\Office\Hwp80"

set "HWP_INSTALLED=0"

if exist "%HWP_PATH_1%\HOffice100\Bin\HwpApp.exe" (
    echo 한글 2018/2020/2022가 "%HWP_PATH_1%"에 설치되어 있습니다.
    set "HWP_INSTALLED=1"
    set "HWP_PATH=%HWP_PATH_1%\HOffice100\Bin"
) else if exist "%HWP_PATH_2%\HOffice100\Bin\HwpApp.exe" (
    echo 한글 2018/2020/2022가 "%HWP_PATH_2%"에 설치되어 있습니다.
    set "HWP_INSTALLED=1"
    set "HWP_PATH=%HWP_PATH_2%\HOffice100\Bin"
) else if exist "%HWP_PATH_3%\Bin\Hwp.exe" (
    echo 한글 2010이 "%HWP_PATH_3%"에 설치되어 있습니다.
    set "HWP_INSTALLED=1"
    set "HWP_PATH=%HWP_PATH_3%\Bin"
) else if exist "%HWP_PATH_4%\Bin\Hwp.exe" (
    echo 한글 2010이 "%HWP_PATH_4%"에 설치되어 있습니다.
    set "HWP_INSTALLED=1"
    set "HWP_PATH=%HWP_PATH_4%\Bin"
) else (
    echo 한글 프로그램을 찾을 수 없습니다.
    echo 한글 프로그램이 설치되어 있지 않거나 일반적인 설치 경로가 아닌 곳에 설치되어 있습니다.
    echo.
    echo 다음 단계:
    echo 1. 한글 프로그램을 설치하세요.
    echo 2. 설치 후 이 스크립트를 다시 실행하세요.
    echo.
    pause
    exit /b 1
)

echo.
echo COM 객체 등록 확인 중...

rem 관리자 권한 확인
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 경고: 관리자 권한으로 실행되지 않았습니다.
    echo COM 객체 등록을 위해 관리자 권한이 필요합니다.
    echo 이 스크립트를 마우스 오른쪽 버튼으로 클릭하여 "관리자 권한으로 실행"을 선택하세요.
    echo.
    pause
    exit /b 1
)

echo 한글 COM 객체를 등록합니다...
echo 이 작업은 몇 분 정도 소요될 수 있습니다.

cd /d "%HWP_PATH%"

if exist "HwpApp.exe" (
    echo HwpApp.exe /regserver 실행 중...
    HwpApp.exe /regserver
) else if exist "Hwp.exe" (
    echo Hwp.exe /regserver 실행 중...
    Hwp.exe /regserver
)

echo.
echo 등록이 완료되었습니다. Visual Studio를 재시작하고 프로젝트를 다시 빌드하세요.
echo.
pause 