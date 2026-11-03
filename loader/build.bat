@echo off
setlocal
set VCVARS=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat
if not exist "%VCVARS%" set VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat

call "%VCVARS%" >nul || (echo [!] vcvars64 not found & exit /b 1)

cd /d "%~dp0"
del /q kerix.exe kerix.obj >nul 2>&1
cl /nologo /W3 /O2 /GS- /D_UNICODE /DUNICODE /LD /Fe:KerixLoader.dll bootstrap.c /link user32.lib kernel32.lib || exit /b 1
del /q bootstrap.obj >nul 2>&1
echo [+] built: KerixLoader.dll
