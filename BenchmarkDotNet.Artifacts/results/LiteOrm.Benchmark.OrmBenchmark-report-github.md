```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i9-12900KF 3.19GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.302
  [Host]    : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=0  

```
| Method              | BatchCount | Mean         | Error | Gen0       | Gen1      | Gen2      | Allocated    |
|-------------------- |----------- |-------------:|------:|-----------:|----------:|----------:|-------------:|
| **MooSql_Insert_Async** | **10**         |     **8.412 ms** |    **NA** |          **-** |         **-** |         **-** |    **213.99 KB** |
| **MooSql_Insert_Async** | **100**        |    **19.610 ms** |    **NA** |          **-** |         **-** |         **-** |   **2080.91 KB** |
| **MooSql_Insert_Async** | **1000**       |   **142.176 ms** |    **NA** |  **1000.0000** |         **-** |         **-** |  **20741.87 KB** |
| **MooSql_Insert_Async** | **10000**      | **1,028.201 ms** |    **NA** | **14000.0000** | **7000.0000** | **1000.0000** | **207321.72 KB** |
