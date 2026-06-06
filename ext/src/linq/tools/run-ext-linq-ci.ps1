# Ext LINQ CI 本地/流水线入口：Compile/Execute 边界 + TestLinq 全量
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")

Write-Host "== Compile/Execute boundary =="
& (Join-Path $PSScriptRoot "check-compile-execute-boundary.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== TestLinq net6.0 =="
Push-Location $repoRoot
try {
    dotnet test (Join-Path $repoRoot "Tests\TestLinq.csproj") -f net6.0
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
