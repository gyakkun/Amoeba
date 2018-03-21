setx DOTNET_CLI_TELEMETRY_OPTOUT 1

set BATDIR=%~dp0
cd %BATDIR%

cd "Amoeba.Daemon"
dotnet publish --configuration Release --runtime win-x64

cd %BATDIR%
rd /Q /S "Publish"
mkdir "Publish"

cd %BATDIR%
xcopy "Amoeba.Daemon\bin\Release\netcoreapp2.0\win-x64\publish" "Publish\Core\Daemon" /D /S /R /Y /I /K

cd %BATDIR%
xcopy "Amoeba.Interface\Wpf\bin\Release\Interface" "Publish\Core\Interface" /D /S /R /Y /I /K

cd %BATDIR%
cd "Publish"
del /Q /S *.pdb
del /Q /S *.so
