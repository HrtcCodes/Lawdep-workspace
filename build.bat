@echo off
chcp 65001 >nul
cd /d "%~dp0"

setlocal enabledelayedexpansion
set FILES=

for /r "src" %%f in (*.cs) do (
    set FILES=!FILES! "%%f"
)

if "!FILES!"=="" (
    echo [ОШИБКА] Файлы .cs в папке src не найдены!
    pause
    exit /b 1
)

echo Найдены файлы для компиляции:
echo !FILES!
echo.

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe ^
  /out:WorkSpace.exe ^
  /optimize+ ^
  /utf8output ^
  /win32icon:app.ico ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Web.Extensions.dll ^
  /reference:System.Data.dll ^
  !FILES!

if %errorlevel% neq 0 (
    echo.
    echo [ОШИБКА] Сборка не удалась!
    pause
    exit /b %errorlevel%
)

echo.
echo [OK] Успешно собрано: WorkSpace.exe
