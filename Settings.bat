set BATDIR=%~dp0
cd %BATDIR%

set TOOL="Omnius\Omnius.Tools\bin\Debug\Omnius.Tools.exe"

IF EXIST %TOOL% call %TOOL% "Settings" "Amoeba\Properties\Settings.cs"
