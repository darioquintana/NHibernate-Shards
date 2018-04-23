MD build\artifacts
DEL /F /S /Q build\artifacts\*.*
dotnet clean src\NHibernate.Shards.Test -c Release
dotnet test src\NHibernate.Shards.Test -c Release
dotnet pack src\NHibernate.Shards -c Release --include-source --include-symbols -o ..\..\build\artifacts