```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.6 (9.0.6, 9.0.625.26613), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 9.0.6 (9.0.6, 9.0.625.26613), X64 RyuJIT x86-64-v3


```
| Method                            | Mean            | Error           | StdDev          | Rank | Gen0   | Allocated |
|---------------------------------- |----------------:|----------------:|----------------:|-----:|-------:|----------:|
| &#39;Aes Encrypt Only&#39;                |        795.0 ns |         2.14 ns |         2.00 ns |    1 | 0.0658 |     416 B |
| &#39;Aes Encrypt With Key Generation&#39; |      1,432.7 ns |         3.22 ns |         2.86 ns |    2 | 0.1335 |     848 B |
| &#39;Rsa Encrypt Only&#39;                |     16,899.9 ns |       113.38 ns |       100.51 ns |    3 | 0.0305 |     280 B |
| &#39;Rsa Encrypt With Key Generation&#39; | 62,013,218.9 ns | 1,822,215.67 ns | 5,315,489.04 ns |    4 |      - |    2147 B |
