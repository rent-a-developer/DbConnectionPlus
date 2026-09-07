# Performance

DbConnectionPlus is designed to have a minimal performance and allocation overhead compared to using `DbCommand` 
manually.  

All benchmarks are performed using SQLite in-memory databases, which is a worst-case scenario for DbConnectionPlus 
because the overhead of using DbConnectionPlus is more noticeable when the executed SQL statements are very fast.

The entity-querying categories are additionally measured as a Native AOT compiled binary, because DbConnectionPlus
selects its materializer on `RuntimeFeature.IsDynamicCodeSupported` and the reflection path behind that switch is
the one a Native AOT consumer runs. Only `Query_Entities`, `Query_ValueTuples` and `TemporaryTable_ComplexObjects`
reach that branch; every other category runs identical code on both runtimes, so measuring it twice would only
compare RyuJIT with ILC. The table below is the JIT snapshot. See
[benchmarks/DbConnectionPlus.Benchmarks/README.md](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/benchmarks/DbConnectionPlus.Benchmarks/README.md) for the
two-job summary and for which categories have a Dapper competitor under Native AOT at all.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i9-12900K 3.19GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.303
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JIT    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  AOT    : .NET 10.0.11, X64 NativeAOT x86-64-v3

Server=True  InvocationCount=Default  IterationTime=300ms  
MaxIterationCount=20  UnrollFactor=16  WarmupCount=3  

```
| Method                                         | Job | Toolchain         | Mean         | Error       | StdDev      | Ratio        | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---- |------------------ |-------------:|------------:|------------:|-------------:|--------:|--------:|-------:|----------:|------------:|
| **DeleteEntities_Command**                         | **JIT** | **Default**           |   **136.627 μs** |   **1.2107 μs** |   **1.1324 μs** |     **baseline** |        **** |  **0.7813** |      **-** |   **68556 B** |            **** |
| DeleteEntities_Dapper                          | JIT | Default           |   168.392 μs |   1.8021 μs |   1.5975 μs | 1.23x slower |   0.02x |  1.2500 |      - |  133269 B |  1.94x more |
| DeleteEntities_DbConnectionPlus                | JIT | Default           |   167.147 μs |   1.0246 μs |   0.9584 μs | 1.22x slower |   0.01x |  1.0417 |      - |  116876 B |  1.70x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **DeleteEntity_Command**                           | **JIT** | **Default**           |     **1.464 μs** |   **0.0115 μs** |   **0.0102 μs** |     **baseline** |        **** |  **0.0078** |      **-** |     **769 B** |            **** |
| DeleteEntity_Dapper                            | JIT | Default           |     1.972 μs |   0.0148 μs |   0.0131 μs | 1.35x slower |   0.01x |  0.0263 |      - |    1705 B |  2.22x more |
| DeleteEntity_DbConnectionPlus                  | JIT | Default           |     1.749 μs |   0.0186 μs |   0.0165 μs | 1.19x slower |   0.01x |  0.0170 |      - |    1249 B |  1.62x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteNonQuery_Command**                        | **JIT** | **Default**           |     **1.353 μs** |   **0.0168 μs** |   **0.0140 μs** |     **baseline** |        **** |  **0.0133** |      **-** |     **768 B** |            **** |
| ExecuteNonQuery_Dapper                         | JIT | Default           |     1.551 μs |   0.0145 μs |   0.0129 μs | 1.15x slower |   0.01x |  0.0153 |      - |    1072 B |  1.40x more |
| ExecuteNonQuery_DbConnectionPlus               | JIT | Default           |     1.697 μs |   0.0057 μs |   0.0048 μs | 1.25x slower |   0.01x |  0.0280 |      - |    1608 B |  2.09x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteReader_Command**                          | **JIT** | **Default**           |   **281.423 μs** |   **1.7947 μs** |   **1.4987 μs** |     **baseline** |        **** |  **6.3406** |      **-** |  **411084 B** |            **** |
| ExecuteReader_Dapper                           | JIT | Default           |   282.375 μs |   2.5789 μs |   2.4123 μs | 1.00x slower |   0.01x |  5.8140 |      - |  411116 B |  1.00x more |
| ExecuteReader_DbConnectionPlus                 | JIT | Default           |   280.036 μs |   2.4280 μs |   2.2711 μs | 1.01x faster |   0.01x |  6.0976 |      - |  411724 B |  1.00x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteScalar_Command**                          | **JIT** | **Default**           |     **1.914 μs** |   **0.0116 μs** |   **0.0103 μs** |     **baseline** |        **** |  **0.0188** |      **-** |    **1120 B** |            **** |
| ExecuteScalar_Dapper                           | JIT | Default           |     2.166 μs |   0.0142 μs |   0.0111 μs | 1.13x slower |   0.01x |  0.0215 |      - |    1424 B |  1.27x more |
| ExecuteScalar_DbConnectionPlus                 | JIT | Default           |     2.286 μs |   0.0171 μs |   0.0151 μs | 1.19x slower |   0.01x |  0.0307 |      - |    2088 B |  1.86x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Exists_Command**                                 | **JIT** | **Default**           |     **1.625 μs** |   **0.0106 μs** |   **0.0088 μs** |     **baseline** |        **** |  **0.0161** |      **-** |    **1000 B** |            **** |
| Exists_Dapper                                  | JIT | Default           |     1.847 μs |   0.0071 μs |   0.0060 μs | 1.14x slower |   0.01x |  0.0367 |      - |    1336 B |  1.34x more |
| Exists_DbConnectionPlus                        | JIT | Default           |     2.040 μs |   0.0238 μs |   0.0186 μs | 1.26x slower |   0.01x |  0.0338 |      - |    1944 B |  1.94x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **InsertEntities_Command**                         | **JIT** | **Default**           | **1,084.779 μs** |   **5.2549 μs** |   **4.1027 μs** |     **baseline** |        **** | **18.7500** |      **-** | **1129093 B** |            **** |
| InsertEntities_Dapper                          | JIT | Default           | 1,089.550 μs |  14.3120 μs |  12.6873 μs | 1.00x slower |   0.01x | 14.8148 |      - | 1247818 B |  1.11x more |
| InsertEntities_DbConnectionPlus                | JIT | Default           | 1,184.102 μs |   7.7167 μs |   6.8406 μs | 1.09x slower |   0.01x | 19.5313 |      - | 1139668 B |  1.01x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **InsertEntity_Command**                           | **JIT** | **Default**           |     **8.668 μs** |   **0.0370 μs** |   **0.0309 μs** |     **baseline** |        **** |  **0.1447** |      **-** |    **8480 B** |            **** |
| InsertEntity_Dapper                            | JIT | Default           |    14.258 μs |   0.0960 μs |   0.0851 μs | 1.64x slower |   0.01x |  0.2872 |      - |   17608 B |  2.08x more |
| InsertEntity_DbConnectionPlus                  | JIT | Default           |     9.003 μs |   0.0555 μs |   0.0492 μs | 1.04x slower |   0.01x |  0.1217 |      - |    8024 B |  1.06x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Parameter_Command**                              | **JIT** | **Default**           |     **3.390 μs** |   **0.0078 μs** |   **0.0061 μs** |     **baseline** |        **** |  **0.0452** |      **-** |    **2952 B** |            **** |
| Parameter_Dapper                               | JIT | Default           |     5.279 μs |   0.0373 μs |   0.0312 μs | 1.56x slower |   0.01x |  0.2117 |      - |    5016 B |  1.70x more |
| Parameter_DbConnectionPlus                     | JIT | Default           |     5.898 μs |   0.0504 μs |   0.0447 μs | 1.74x slower |   0.01x |  0.3523 |      - |    7376 B |  2.50x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Dynamic_Command**                          | **JIT** | **Default**           |   **302.573 μs** |   **3.1295 μs** |   **2.6133 μs** |     **baseline** |        **** | **15.1210** | **1.0081** |  **532528 B** |            **** |
| Query_Dynamic_Dapper                           | JIT | Default           |   214.474 μs |   1.1667 μs |   1.0913 μs | 1.41x faster |   0.01x |  0.7267 |      - |   73880 B |  7.21x less |
| Query_Dynamic_DbConnectionPlus                 | JIT | Default           |   276.257 μs |   1.8022 μs |   1.5049 μs | 1.10x faster |   0.01x |  2.7174 |      - |  131944 B |  4.04x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Entities_Command**                         | **JIT** | **Default**           |   **281.692 μs** |   **2.1036 μs** |   **1.8648 μs** |     **baseline** |        **** |  **7.1023** |      **-** |  **411084 B** |            **** |
| Query_Entities_Dapper                          | JIT | Default           |   234.856 μs |   0.8877 μs |   0.8303 μs | 1.20x faster |   0.01x |  0.7806 |      - |   74105 B |  5.55x less |
| Query_Entities_DbConnectionPlus                | JIT | Default           |   245.137 μs |   0.8514 μs |   0.7547 μs | 1.15x faster |   0.01x |  0.8244 |      - |   64025 B |  6.42x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Entities_Command**                         | **AOT** | **Latest ILCompiler** |   **305.151 μs** |   **4.2016 μs** |   **3.5085 μs** |     **baseline** |        **** |  **7.0565** |      **-** |  **411091 B** |            **** |
| Query_Entities_Dapper_Aot                      | AOT | Latest ILCompiler |   245.874 μs |   1.4414 μs |   1.2778 μs | 1.24x faster |   0.02x |  2.4351 |      - |   60969 B |  6.74x less |
| Query_Entities_DbConnectionPlus                | AOT | Latest ILCompiler |   321.910 μs |   3.0612 μs |   2.7137 μs | 1.06x slower |   0.01x |  1.0593 |      - |   91244 B |  4.51x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Scalars_Command**                          | **JIT** | **Default**           |    **81.212 μs** |   **0.2861 μs** |   **0.2389 μs** |     **baseline** |        **** |  **0.2717** |      **-** |   **17288 B** |            **** |
| Query_Scalars_Dapper                           | JIT | Default           |   111.449 μs |   0.6049 μs |   0.5051 μs | 1.37x slower |   0.01x |  0.3720 |      - |   36976 B |  2.14x more |
| Query_Scalars_DbConnectionPlus                 | JIT | Default           |   109.890 μs |   0.4992 μs |   0.4670 μs | 1.35x slower |   0.01x |  0.3655 |      - |   32480 B |  1.88x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_ValueTuples_Command**                      | **JIT** | **Default**           |    **98.529 μs** |   **0.5243 μs** |   **0.4905 μs** |     **baseline** |        **** |  **0.6649** |      **-** |   **47801 B** |            **** |
| Query_ValueTuples_Dapper                       | JIT | Default           |   131.177 μs |   0.5286 μs |   0.4945 μs | 1.33x slower |   0.01x |  1.3193 |      - |   71297 B |  1.49x more |
| Query_ValueTuples_DbConnectionPlus             | JIT | Default           |   130.361 μs |   1.5104 μs |   1.4128 μs | 1.32x slower |   0.02x |  0.9021 |      - |   53137 B |  1.11x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_ValueTuples_Command**                      | **AOT** | **Latest ILCompiler** |   **110.324 μs** |   **0.4156 μs** |   **0.3684 μs** |     **baseline** |        **** |  **0.7267** |      **-** |   **47790 B** |            **** |
| Query_ValueTuples_DbConnectionPlus             | AOT | Latest ILCompiler |   175.588 μs |   1.3776 μs |   1.2212 μs | 1.59x slower |   0.01x |  1.1682 |      - |   84376 B |  1.77x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ComplexObjects_Command**          | **JIT** | **Default**           | **2,728.061 μs** |  **14.5332 μs** |  **13.5944 μs** |     **baseline** |        **** | **46.8750** |      **-** | **3388440 B** |            **** |
| TemporaryTable_ComplexObjects_Dapper           | JIT | Default           | 1,918.002 μs |  34.1135 μs |  28.4863 μs | 1.42x faster |   0.02x | 16.6667 |      - | 1731239 B |  1.96x less |
| TemporaryTable_ComplexObjects_DbConnectionPlus | JIT | Default           | 2,169.543 μs |  39.4448 μs |  36.8967 μs | 1.26x faster |   0.02x | 17.8571 |      - | 1580135 B |  2.14x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ComplexObjects_Command**          | **AOT** | **Latest ILCompiler** | **3,180.703 μs** |  **23.5846 μs** |  **20.9072 μs** |     **baseline** |        **** | **52.0833** |      **-** | **3388726 B** |            **** |
| TemporaryTable_ComplexObjects_DbConnectionPlus | AOT | Latest ILCompiler | 2,650.714 μs |  13.9937 μs |  13.0897 μs | 1.20x faster |   0.01x | 23.4375 |      - | 1648405 B |  2.06x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ScalarValues_Command**            | **JIT** | **Default**           | **4,946.405 μs** |  **34.7151 μs** |  **27.1033 μs** |     **baseline** |        **** | **20.8333** |      **-** | **1493512 B** |            **** |
| TemporaryTable_ScalarValues_Dapper             | JIT | Default           | 5,993.998 μs | 117.4418 μs | 115.3436 μs | 1.21x slower |   0.02x | 39.2157 |      - | 3175374 B |  2.13x more |
| TemporaryTable_ScalarValues_DbConnectionPlus   | JIT | Default           | 5,844.893 μs |  38.7475 μs |  32.3559 μs | 1.18x slower |   0.01x | 38.4615 |      - | 2696352 B |  1.81x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **UpdateEntities_Command**                         | **JIT** | **Default**           |   **532.109 μs** |   **2.0057 μs** |   **1.8762 μs** |     **baseline** |        **** |  **6.9444** |      **-** |  **566049 B** |            **** |
| UpdateEntities_Dapper                          | JIT | Default           |   586.049 μs |   4.1645 μs |   3.8954 μs | 1.10x slower |   0.01x | 11.6054 |      - |  663867 B |  1.17x more |
| UpdateEntities_DbConnectionPlus                | JIT | Default           |   587.406 μs |   3.3649 μs |   2.8098 μs | 1.10x slower |   0.01x |  9.7656 |      - |  571057 B |  1.01x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **UpdateEntity_Command**                           | **JIT** | **Default**           |     **9.143 μs** |   **0.1013 μs** |   **0.0846 μs** |     **baseline** |        **** |  **0.1225** |      **-** |    **8551 B** |            **** |
| UpdateEntity_Dapper                            | JIT | Default           |    10.748 μs |   0.0497 μs |   0.0465 μs | 1.18x slower |   0.01x |  0.1789 |      - |   12031 B |  1.41x more |
| UpdateEntity_DbConnectionPlus                  | JIT | Default           |     9.630 μs |   0.0560 μs |   0.0468 μs | 1.05x slower |   0.01x |  0.1277 |      - |    8055 B |  1.06x less |

## Running the benchmarks
```shell
pwsh -File scripts/benchmarks.ps1
```

Anything after the script name is forwarded to BenchmarkDotNet, e.g. `--filter *Query_Entities*`. The Native AOT
job needs a C++ toolchain (MSVC on Windows, `clang` and `zlib1g-dev` on Linux); the script also puts `vswhere.exe`
on `PATH`, without which the native link step fails with a misleading `MSB3073`.
