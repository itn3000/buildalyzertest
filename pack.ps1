Push-Location BuildalyzerTest.CustomTask
dotnet pack -c Release -o ../nupkg
Pop-Location

Push-Location -Path .\dotnet-buildalyzer-test
dotnet pack -c Release -o ../nupkg
Pop-Location