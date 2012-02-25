set BATDIR=%~dp0
cd %BATDIR%

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 2\Core\Amoeba.exe"  "Amoeba\bin\Debug 2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 3\Core\Amoeba.exe"  "Amoeba\bin\Debug 3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 4\Core\Amoeba.exe"  "Amoeba\bin\Debug 4\Core"
