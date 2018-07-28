@ECHO OFF
IF "%1" == "" GOTO :help

CALL build.cmd
REN build\artifacts\*.symbols.nupkg *.nupkg$
dotnet nuget push build\artifacts\*.nupkg -s https://api.nuget.org/v3/index.json %*

REN build\artifacts\*.nupkg$ *.nupkg
dotnet nuget push build\artifacts\*.symbols.nupkg -s https://nuget.smbsrc.net %*


GOTO :EOF

:help
ECHO Usage: publish [nuget push options, including -k api-key-here]