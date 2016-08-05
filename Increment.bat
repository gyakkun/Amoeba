set LIBRARY_INCREMENT="C:\Local\Projects\Alliance-Network\Library\Increment.bat"
IF EXIST %LIBRARY_INCREMENT% call %LIBRARY_INCREMENT%

set BATDIR=%~dp0
cd %BATDIR%

set TOOL="C:\Local\Projects\Alliance-Network\Library\Library.Tools\bin\Debug\Library.Tools.exe"
IF NOT EXIST %TOOL% exit

call %TOOL% "Increment" Amoeba.Wpf\Amoeba.Wpf.csproj Properties\AssemblyInfo.cs
