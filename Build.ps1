$ErrorActionPreference = "Stop";

dotnet publish $(Join-Path $PSScriptRoot "\src\TooltipFix") --output $(Join-Path $PSScriptRoot "\src\bin\") --configuration Release

Write-Output "Built Files to '$(Join-Path $PSScriptRoot "\src\bin")'";
