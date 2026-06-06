# 检查 Ext 编译层是否 reintroduce DbDataReader Mapper 路径
$root = Join-Path $PSScriptRoot ".."
$patterns = @("BuildMapper", "SetRunQuery", "InitPreambles")
$hits = @()
foreach ($pat in $patterns) {
    $found = rg -l $pat $root --glob "*.cs" 2>$null | Where-Object { $_ -notmatch "\\core\\" -and $_ -notmatch "EntityVisitCompiler" }
    if ($found) { $hits += $found }
}
if ($hits.Count -gt 0) {
    Write-Error "Compile/Execute boundary violation suspects: $($hits -join ', ')"
    exit 1
}
Write-Host "Compile/Execute boundary check passed."
exit 0
