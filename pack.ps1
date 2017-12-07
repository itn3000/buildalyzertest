Push-Location -Path .\dotnet-buildalyzer-test
dotnet pack -c Release -o ../nupkg
Pop-Location