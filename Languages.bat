set BATDIR=%~dp0
cd %BATDIR%

set TOOL="Omnius\Omnius.Tools\bin\Debug\Omnius.Tools.exe"
IF NOT EXIST %TOOL% exit

call %TOOL% "Languages" "Amoeba.Interface.Windows\Resources\Languages"
