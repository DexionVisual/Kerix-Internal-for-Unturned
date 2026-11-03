@echo off
setlocal
set GAME=C:\Program Files (x86)\Steam\steamapps\common\Unturned

echo === [1/2] KerixLoader.dll (native mono bootstrap) ===
cmd /c "%~dp0loader\build.bat" || exit /b 1

echo === [2/2] Kerix.dll (managed cheat) ===
cmd /c "%~dp0cheat\build.bat" || exit /b 1

copy /y "%~dp0cheat\Kerix.dll" "%GAME%\Kerix.dll" >nul || (echo [!] copy to game failed & exit /b 1)
copy /y "%~dp0cheat\libs\Vault.dll" "%GAME%\Vault.dll" >nul || (echo [!] copy vault failed & exit /b 1)
echo [+] Kerix.dll + Vault.dll copied to game root
echo.
echo Готово:
echo   в игре должны лежать  Kerix.dll + Vault.dll
echo   инжектить в Unturned.exe  KerixLoader.dll (лежит в loader\)
echo   INSERT - меню, END - ESP
