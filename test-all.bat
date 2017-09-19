@echo off
dotnet test Tests\Tests.csproj /p:Platform=x32
dotnet test Tests\Tests.csproj /p:Platform=x64