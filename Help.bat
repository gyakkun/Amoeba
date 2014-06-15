set BATDIR=%~dp0
cd %BATDIR%

robocopy Help Amoeba\bin\Debug\Core\Help /mir /NP
robocopy Help Amoeba\bin\Release\Core\Help /mir /NP