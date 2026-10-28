```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i9-12900KF 3.19GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.302
  [Host]    : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                      | Mean        | Error      | StdDev     | Gen0      | Gen1      | Allocated |
|---------------------------- |------------:|-----------:|-----------:|----------:|----------:|----------:|
| EFCore_SingleInsert_Async   | 4,463.20 ms |  46.658 ms |  68.390 ms | 3000.0000 | 2000.0000 |  58.62 MB |
| SqlSugar_SingleInsert_Async | 4,086.30 ms |  35.392 ms |  48.445 ms | 1000.0000 |         - |  25.28 MB |
| LiteOrm_SingleInsert_Async  | 3,927.04 ms |  50.083 ms |  71.827 ms |         - |         - |   5.27 MB |
| Dapper_SingleInsert_Async   | 4,024.62 ms | 133.036 ms | 186.498 ms |         - |         - |   5.47 MB |
| FreeSql_SingleInsert_Async  | 4,179.67 ms |  63.002 ms |  92.347 ms | 1000.0000 |         - |  20.26 MB |
| MooSql_SingleInsert_Async   | 4,131.13 ms |  51.958 ms |  76.159 ms | 1000.0000 |         - |  23.34 MB |
| EFCore_SingleUpdate_Async   | 5,517.94 ms | 157.455 ms | 215.527 ms | 3000.0000 | 2000.0000 |  46.71 MB |
| SqlSugar_SingleUpdate_Async | 5,430.72 ms | 174.037 ms | 260.491 ms | 2000.0000 | 1000.0000 |  30.67 MB |
| LiteOrm_SingleUpdate_Async  | 5,471.73 ms | 160.576 ms | 235.370 ms |         - |         - |   5.01 MB |
| Dapper_SingleUpdate_Async   | 5,385.08 ms | 124.718 ms | 182.810 ms |         - |         - |    4.3 MB |
| FreeSql_SingleUpdate_Async  | 5,309.53 ms | 137.730 ms | 193.078 ms | 1000.0000 |         - |  22.83 MB |
| MooSql_SingleUpdate_Async   | 5,447.49 ms | 169.438 ms | 248.360 ms | 1000.0000 |         - |  27.12 MB |
| EFCore_SingleUpsert_Async   | 5,326.32 ms |  81.770 ms | 122.389 ms | 3000.0000 | 1000.0000 |  55.35 MB |
| SqlSugar_SingleUpsert_Async | 5,471.14 ms |  98.189 ms | 140.819 ms | 5000.0000 | 1000.0000 |  88.28 MB |
| LiteOrm_SingleUpsert_Async  | 5,201.77 ms |  97.542 ms | 139.893 ms |         - |         - |    7.4 MB |
| Dapper_SingleUpsert_Async   | 4,754.07 ms |  61.510 ms |  88.216 ms |         - |         - |   6.09 MB |
| FreeSql_SingleUpsert_Async  | 5,068.09 ms |  96.227 ms | 144.029 ms | 1000.0000 |         - |   20.3 MB |
| MooSql_SingleUpsert_Async   | 5,065.26 ms |  92.407 ms | 135.449 ms | 2000.0000 | 1000.0000 |  38.85 MB |
| EFCore_SingleGet_Async      |   351.86 ms |  17.178 ms |  24.636 ms | 3000.0000 | 1000.0000 |  47.61 MB |
| SqlSugar_SingleGet_Async    |   282.73 ms |   7.343 ms |   9.803 ms | 3000.0000 |         - |  50.89 MB |
| LiteOrm_SingleGet_Async     |   105.49 ms |   2.609 ms |   3.824 ms |  250.0000 |         - |   3.82 MB |
| Dapper_SingleGet_Async      |    97.02 ms |   2.194 ms |   3.283 ms |  250.0000 |         - |   6.08 MB |
| FreeSql_SingleGet_Async     |   248.32 ms |   7.618 ms |  11.402 ms | 1000.0000 |         - |  18.62 MB |
| MooSql_SingleGet_Async      |   164.24 ms |   3.168 ms |   4.742 ms |  666.6667 |         - |   12.4 MB |
