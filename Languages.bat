set BATDIR=%~dp0
cd %BATDIR%

set TOOL="C:\Local\Projects\Alliance-Network\Library\Library.Tools\bin\Debug\Library.Tools.exe"
IF EXIST %TOOL% call %TOOL% "Languages" "Amoeba.Wpf\Properties\LanguagesManager.cs" "Amoeba.Wpf\Languages"
