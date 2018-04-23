@ECHO OFF
IF "%1" == "" GOTO :help

CALL build.cmd
dotnet nuget push build\artifacts\*.nupkg -s https://api.nuget.org/v3/index.json %*
GOTO :EOF

:help
ECHO Usage: publish [nuget push options, including -k api-key-here]