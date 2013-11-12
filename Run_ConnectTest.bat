set BATDIR=%~dp0
cd %BATDIR%

set target="Amoeba\bin\Debug\Core"

md "Amoeba\bin\Test"
rd /s /q "Amoeba\bin\Test\Core"
xcopy %target% "Amoeba\bin\Test\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Test\Core\Amoeba.exe"  "Amoeba\bin\Test\Core"
