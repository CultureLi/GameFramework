@echo off
setlocal EnableDelayedExpansion

REM run.bat 所在目录 = AndroidProject
set "ANDROID_PROJECT_DIR=%~dp0"

REM Unity Plugins Android 目录
for %%i in ("%ANDROID_PROJECT_DIR%..\Client\Assets\Plugins\Android") do (
    set "UNITY_PLUGIN_PATH=%%~fi"
)

echo AndroidProject: %ANDROID_PROJECT_DIR%
echo Unity Plugin Dir: %UNITY_PLUGIN_PATH%
echo.

REM 模块列表
set MODULES=testLib customLib

if not exist "%UNITY_PLUGIN_PATH%\" (
    echo Creating Unity Plugins Android directory...
    mkdir "%UNITY_PLUGIN_PATH%" || (
        echo Failed to create Unity Plugin directory
        pause
        exit /b 1
    )
)

for %%m in (%MODULES%) do (
    echo =============== Building %%m ===============

    call gradlew :%%m:build
    echo Gradle exit code: !ERRORLEVEL!

    if errorlevel 1 (
        echo Gradle failed for %%m
        pause
        exit /b 1
    )

    REM jar 路径（注意：无引号）
    set "JAR_PATH=%ANDROID_PROJECT_DIR%%%m\build\intermediates\compile_library_classes_jar\release\classes.jar"

    echo JAR_PATH: !JAR_PATH!

    if exist "!JAR_PATH!" (
        copy /Y "!JAR_PATH!" "%UNITY_PLUGIN_PATH%\%%m.jar"
        echo Copied %%m.jar
    ) else (
        echo JAR not found for %%m
        pause
        exit /b 1
    )

    echo.
)

echo 🎉 All modules processed successfully!
pause
