@echo off
setlocal
set GAME=C:\Program Files (x86)\Steam\steamapps\common\Unturned
set REF=%GAME%\Unturned_Data\Managed
set FA=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8
set CSC=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe

cd /d "%~dp0"

"%CSC%" -nologo -target:library -platform:anycpu -langversion:7.3 -nostdlib+ -optimize+ ^
  -r:"%FA%\mscorlib.dll" -r:"%FA%\System.dll" -r:"%FA%\System.Core.dll" ^
  -r:"%REF%\netstandard.dll" ^
  -r:"%REF%\Assembly-CSharp.dll" ^
  -r:"%REF%\SDG.NetTransport.dll" ^
  -r:"%REF%\UnityEngine.CoreModule.dll" ^
  -r:"%REF%\UnityEngine.PhysicsModule.dll" ^
  -r:"%REF%\UnityEngine.IMGUIModule.dll" ^
  -r:"%REF%\UnityEngine.InputLegacyModule.dll" ^
  -r:"%REF%\UnityEngine.TextRenderingModule.dll" ^
  -r:"libs\Vault.dll" ^
  -r:"%REF%\com.rlabrecque.steamworks.net.dll" ^
  -out:Kerix.dll src\Loader.cs src\KerixMain.cs src\Ui.cs src\Menu.cs src\Esp.cs src\Aimbot.cs src\Misc.cs src\Config.cs src\Chams.cs src\Binds.cs src\Minimap.cs src\Freecam.cs src\Movement.cs src\Hud.cs src\VehicleSteer.cs src\Tooltips.cs src\Theme.cs || exit /b 1

echo [+] built: Kerix.dll
