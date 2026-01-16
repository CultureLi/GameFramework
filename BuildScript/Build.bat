::@echo off
setlocal EnableDelayedExpansion

:: 输入参数
set Version=%1
set BuildAddressable=%2
set TargetPlatform=%3
set CleanProject=%4

:: 环境变量
set PROJECT_DIR=E:\Work\BuildDir\AutoBuild
set GIT_URL=https://github.com/CultureLi/GameFramework.git

set BUILD_LOG_NAME=unity_build.log

:: 输出开始信息
echo Version at %Version%
echo BuildAddressable: %BuildAddressable%
echo TargetPlatform: %TargetPlatform%

:: -----------------------------
:: 处理工程目录
:: -----------------------------
if /I "%CleanProject%"=="true" (
    echo [Clean and Clone] CleanProject=true
    if exist "%PROJECT_DIR%" (
        echo Deleting old folder: %PROJECT_DIR%
        rmdir /S /Q "%PROJECT_DIR%"
    )
    echo Cloning repository...
    git clone %GIT_URL% "%PROJECT_DIR%"
    if errorlevel 1 (
        echo  Git clone failed!
        exit /b 1
    )
) else (
    if exist "%PROJECT_DIR%" (
        echo [Pull] Updating existing repository: %PROJECT_DIR%
        pushd "%PROJECT_DIR%"
        git reset --hard
        git clean -fd
        git pull
        if errorlevel 1 (
            echo  Git pull failed!
            popd
            exit /b 1
        )
        popd
    ) else (
        echo [Clone] Repository does not exist, cloning...
        git clone %GIT_URL% "%PROJECT_DIR%"
        if errorlevel 1 (
            echo  Git clone failed!
            exit /b 1
        )
    )
)

:: -----------------------------
:: 执行 Unity 构建
:: -----------------------------
echo [Build Unity Project]
"C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe" ^
    -batchmode ^
    -projectPath "%PROJECT_DIR%\Client" ^
    -executeMethod AutoBuild.BuildByCommandLine ^
    -version %Version% ^
    -buildAddressable %BuildAddressable% ^
    -targetPlatform %TargetPlatform% ^
    -quit ^
    -logFile %BUILD_LOG_NAME%

:: 检查 Unity 构建是否成功
if errorlevel 1 (
    echo Unity build failed!
    exit /b 1
)

:: 输出日志路径
echo Build log saved as %BUILD_LOG_NAME%
exit /b 0
