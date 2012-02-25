set BATDIR=%~dp0
cd %BATDIR%
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "increment" %1 %2
