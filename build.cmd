dotnet clean
dotnet test src\NHibernate.Shards.Test -c Release
dotnet pack src\NHibernate.Shards -c Release --include-source --include-symbols