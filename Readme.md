### Overview 

Contains a new `Map<'Key, 'Value>` implementation drastically improving performance
Please note that the abstract data type implemented here is the same as for the current F# `Map<'Key, 'Value>` with some internal differences:

1. It uses explicit subtyping and virtual methods instead of pattern-matching for case-distinction in the nodes
2. It uses a *mutable* add-implementation during build (for operations like `ofSeq`, `ofList` and `ofArray`) significantly increasing performance
3. It maintains a count for all inner nodes, which doesn't cost too much memory and allows for `count` to be in `O(1)` and several other 
   things like `Map.tryItem : index : int -> map : Map<'a, 'b> -> option<'a * 'b>` in `O(log N)`
   
Please note that the implementation is work in progress and I intend to add several combinators since the implementation (when not adopted into FSharp.Core) will
be used in [FSharp.Data.Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive) as storage for `IndexList<'T>`.

### Benchmarks (preliminary)

Running with `FSharp.Core` version `5.0.0`

#### Bottom Line
* Almost all operations tested so far are about `1.2x` - `4x` faster than for the current F# Map
* The `ofArray` does not only perform better but also allocates way less garbage (see GC stats in benchmark)
* `ofSeq` and `ofList` for very small counts (approx. `<=5`) are a little worse, but that can be worked around 
* `toSeq` is more or less identical in speed which was more or less expected, due to its heavy use of virtual methods.
```
// * Summary *

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
Intel Core i7-4930K CPU 3.40GHz (Haswell), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
```

|                         Method | Count |             Mean |           Error |          StdDev |           Median |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|------------------------------- |------ |-----------------:|----------------:|----------------:|-----------------:|---------:|---------:|--------:|----------:|
|                    Map_ofArray |     1 |        21.226 ns |       0.4937 ns |       0.4848 ns |        20.994 ns |   0.0102 |        - |       - |      64 B |
|                 MapNew_ofArray |     1 |        15.826 ns |       0.3891 ns |       0.5194 ns |        15.856 ns |   0.0089 |        - |       - |      56 B |
|                     Map_ofList |     1 |        33.815 ns |       0.7616 ns |       0.9353 ns |        33.327 ns |   0.0140 |        - |       - |      88 B |
|                  MapNew_ofList |     1 |        13.701 ns |       0.3558 ns |       0.3955 ns |        13.827 ns |   0.0089 |        - |       - |      56 B |
|                      Map_ofSeq |     1 |        38.474 ns |       0.8441 ns |       0.9031 ns |        38.051 ns |   0.0140 |        - |       - |      88 B |
|                   MapNew_ofSeq |     1 |        49.260 ns |       0.8439 ns |       0.9718 ns |        48.896 ns |   0.0204 |        - |       - |     128 B |
|                    Map_toArray |     1 |        32.754 ns |       0.6987 ns |       0.6535 ns |        32.500 ns |   0.0140 |        - |       - |      88 B |
|                 MapNew_toArray |     1 |        21.307 ns |       0.4329 ns |       0.3380 ns |        21.437 ns |   0.0127 |        - |       - |      80 B |
|                     Map_toList |     1 |        16.866 ns |       0.4223 ns |       0.4336 ns |        17.011 ns |   0.0089 |        - |       - |      56 B |
|                  MapNew_toList |     1 |        12.269 ns |       0.3317 ns |       0.3687 ns |        12.030 ns |   0.0089 |        - |       - |      56 B |
|                  Map_enumerate |     1 |        59.879 ns |       1.2295 ns |       1.3665 ns |        60.289 ns |   0.0191 |        - |       - |     120 B |
|               MapNew_enumerate |     1 |        44.506 ns |       0.8792 ns |       0.8224 ns |        44.173 ns |   0.0063 |        - |       - |      40 B |
|                 Map_toSeq_enum |     1 |       146.198 ns |       2.8765 ns |       3.4242 ns |       144.251 ns |   0.0470 |        - |       - |     296 B |
|              MapNew_toSeq_enum |     1 |       167.624 ns |       3.2915 ns |       4.7206 ns |       167.852 ns |   0.0408 |        - |       - |     256 B |
|            Map_containsKey_all |     1 |         8.523 ns |       0.2032 ns |       0.2570 ns |         8.377 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |     1 |         7.232 ns |       0.0536 ns |       0.0419 ns |         7.218 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |     1 |         7.759 ns |       0.1914 ns |       0.1880 ns |         7.623 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     1 |         5.893 ns |       0.1544 ns |       0.1778 ns |         5.798 ns |        - |        - |       - |         - |
|                    Map_tryFind |     1 |        12.679 ns |       0.2842 ns |       0.4076 ns |        12.469 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     1 |         8.607 ns |       0.2429 ns |       0.2028 ns |         8.594 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |     1 |         9.143 ns |       0.0148 ns |       0.0115 ns |         9.143 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     1 |         5.024 ns |       0.1902 ns |       0.1780 ns |         5.097 ns |        - |        - |       - |         - |
|                 Map_remove_all |     1 |        15.056 ns |       0.3859 ns |       0.3963 ns |        14.798 ns |   0.0063 |        - |       - |      40 B |
|              MapNew_remove_all |     1 |        14.200 ns |       0.2609 ns |       0.2037 ns |        14.209 ns |   0.0051 |        - |       - |      32 B |
|                     Map_exists |     1 |        13.935 ns |       0.3153 ns |       0.3754 ns |        13.923 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     1 |        13.326 ns |       0.3013 ns |       0.4779 ns |        13.317 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |     1 |        14.006 ns |       0.3032 ns |       0.4251 ns |        13.680 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     1 |        13.454 ns |       0.3006 ns |       0.4855 ns |        13.427 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |     1 |        14.164 ns |       0.3122 ns |       0.2920 ns |        14.013 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     1 |        13.592 ns |       0.3031 ns |       0.4629 ns |        13.825 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |     2 |        34.975 ns |       0.2463 ns |       0.1923 ns |        34.948 ns |   0.0178 |        - |       - |     112 B |
|                 MapNew_ofArray |     2 |        38.697 ns |       0.8240 ns |       0.8093 ns |        39.003 ns |   0.0166 |        - |       - |     104 B |
|                     Map_ofList |     2 |        51.213 ns |       0.9811 ns |       0.9177 ns |        50.847 ns |   0.0216 |        - |       - |     136 B |
|                  MapNew_ofList |     2 |       128.418 ns |       2.6147 ns |       3.9135 ns |       127.223 ns |   0.0534 |        - |       - |     336 B |
|                      Map_ofSeq |     2 |        55.369 ns |       0.9963 ns |       0.9320 ns |        54.883 ns |   0.0216 |        - |       - |     136 B |
|                   MapNew_ofSeq |     2 |       120.547 ns |       2.4740 ns |       3.7780 ns |       120.746 ns |   0.0534 |        - |       - |     336 B |
|                    Map_toArray |     2 |        62.207 ns |       1.3207 ns |       1.7631 ns |        62.409 ns |   0.0242 |        - |       - |     152 B |
|                 MapNew_toArray |     2 |        35.433 ns |       0.6277 ns |       0.5872 ns |        35.596 ns |   0.0178 |        - |       - |     112 B |
|                     Map_toList |     2 |        40.201 ns |       0.8363 ns |       0.7823 ns |        40.524 ns |   0.0178 |        - |       - |     112 B |
|                  MapNew_toList |     2 |        25.446 ns |       0.5605 ns |       0.5756 ns |        25.427 ns |   0.0179 |        - |       - |     112 B |
|                  Map_enumerate |     2 |       115.375 ns |       2.2089 ns |       2.1695 ns |       115.004 ns |   0.0381 |        - |       - |     240 B |
|               MapNew_enumerate |     2 |        64.047 ns |       1.3276 ns |       2.0669 ns |        64.012 ns |   0.0126 |        - |       - |      80 B |
|                 Map_toSeq_enum |     2 |       224.213 ns |       4.5200 ns |       4.8363 ns |       223.673 ns |   0.0701 |        - |       - |     440 B |
|              MapNew_toSeq_enum |     2 |       224.859 ns |       3.3039 ns |       3.0905 ns |       222.804 ns |   0.0508 |        - |       - |     320 B |
|            Map_containsKey_all |     2 |        31.282 ns |       0.5655 ns |       0.6512 ns |        31.366 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |     2 |        20.559 ns |       0.4382 ns |       0.5542 ns |        20.203 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |     2 |        17.580 ns |       0.3787 ns |       0.4209 ns |        17.464 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     2 |         9.340 ns |       0.1368 ns |       0.1213 ns |         9.288 ns |        - |        - |       - |         - |
|                    Map_tryFind |     2 |        22.796 ns |       0.4564 ns |       0.4270 ns |        22.560 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     2 |        12.654 ns |       0.3086 ns |       0.2887 ns |        12.498 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |     2 |        18.259 ns |       0.4413 ns |       0.4334 ns |        18.169 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     2 |         9.398 ns |       0.1748 ns |       0.1365 ns |         9.415 ns |        - |        - |       - |         - |
|                 Map_remove_all |     2 |        62.788 ns |       1.3383 ns |       1.6436 ns |        61.526 ns |   0.0166 |        - |       - |     104 B |
|              MapNew_remove_all |     2 |        44.349 ns |       0.2449 ns |       0.1912 ns |        44.297 ns |   0.0140 |        - |       - |      88 B |
|                     Map_exists |     2 |        27.000 ns |       0.5723 ns |       0.7238 ns |        26.522 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     2 |        18.573 ns |       0.3929 ns |       0.4204 ns |        18.327 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |     2 |        26.374 ns |       0.5377 ns |       0.5029 ns |        26.290 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     2 |        18.508 ns |       0.4046 ns |       0.3974 ns |        18.279 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |     2 |        27.563 ns |       0.5725 ns |       0.9081 ns |        27.129 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     2 |        18.727 ns |       0.4011 ns |       0.5355 ns |        18.703 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |     5 |       221.874 ns |       4.3958 ns |       4.5142 ns |       221.327 ns |   0.0598 |        - |       - |     376 B |
|                 MapNew_ofArray |     5 |       182.264 ns |       3.6533 ns |       4.3490 ns |       180.745 ns |   0.0432 |        - |       - |     272 B |
|                     Map_ofList |     5 |       343.918 ns |       6.7506 ns |       6.3145 ns |       339.387 ns |   0.0863 |        - |       - |     544 B |
|                  MapNew_ofList |     5 |       310.471 ns |       6.2666 ns |       6.9653 ns |       312.746 ns |   0.0801 |        - |       - |     504 B |
|                      Map_ofSeq |     5 |       292.267 ns |       5.7975 ns |       5.4230 ns |       292.915 ns |   0.0710 |        - |       - |     448 B |
|                   MapNew_ofSeq |     5 |       552.889 ns |      11.0569 ns |      12.7331 ns |       562.113 ns |   0.1221 |        - |       - |     768 B |
|                    Map_toArray |     5 |       129.693 ns |       0.1720 ns |       0.1343 ns |       129.668 ns |   0.0548 |        - |       - |     344 B |
|                 MapNew_toArray |     5 |        64.148 ns |       1.3654 ns |       1.8228 ns |        65.104 ns |   0.0331 |        - |       - |     208 B |
|                     Map_toList |     5 |        98.381 ns |       1.9675 ns |       2.0204 ns |        98.585 ns |   0.0446 |        - |       - |     280 B |
|                  MapNew_toList |     5 |        68.641 ns |       1.3927 ns |       1.8109 ns |        68.810 ns |   0.0446 |        - |       - |     280 B |
|                  Map_enumerate |     5 |       241.964 ns |       4.7700 ns |       6.2023 ns |       242.763 ns |   0.0763 |        - |       - |     480 B |
|               MapNew_enumerate |     5 |       116.589 ns |       0.8109 ns |       0.7188 ns |       116.416 ns |   0.0317 |        - |       - |     200 B |
|                 Map_toSeq_enum |     5 |       408.419 ns |       6.7165 ns |       5.9540 ns |       407.090 ns |   0.1197 |        - |       - |     752 B |
|              MapNew_toSeq_enum |     5 |       442.986 ns |       7.5342 ns |       7.0475 ns |       438.411 ns |   0.0815 |        - |       - |     512 B |
|            Map_containsKey_all |     5 |       104.930 ns |       1.4066 ns |       1.0982 ns |       105.165 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |     5 |        59.491 ns |       1.2231 ns |       1.6742 ns |        58.937 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |     5 |        28.628 ns |       0.6028 ns |       0.9023 ns |        28.241 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     5 |        11.675 ns |       0.1985 ns |       0.1658 ns |        11.749 ns |        - |        - |       - |         - |
|                    Map_tryFind |     5 |        30.836 ns |       0.6828 ns |       0.9116 ns |        30.502 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     5 |        17.334 ns |       0.4240 ns |       0.5047 ns |        17.334 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |     5 |        40.073 ns |       0.7385 ns |       0.7253 ns |        40.138 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     5 |        10.760 ns |       0.0147 ns |       0.0138 ns |        10.758 ns |        - |        - |       - |         - |
|                 Map_remove_all |     5 |       268.487 ns |       5.4373 ns |       6.2616 ns |       264.521 ns |   0.0672 |        - |       - |     424 B |
|              MapNew_remove_all |     5 |       306.449 ns |       6.1039 ns |       6.5311 ns |       306.292 ns |   0.0558 |        - |       - |     352 B |
|                     Map_exists |     5 |        39.583 ns |       0.7740 ns |       0.7602 ns |        39.471 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     5 |        27.721 ns |       0.5523 ns |       0.5672 ns |        27.826 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |     5 |        50.174 ns |       1.0428 ns |       1.0242 ns |        49.992 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     5 |        27.148 ns |       0.4837 ns |       0.4525 ns |        27.348 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |     5 |        52.843 ns |       0.8001 ns |       0.7093 ns |        52.471 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     5 |        31.840 ns |       0.6597 ns |       0.7597 ns |        31.909 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |     7 |       545.463 ns |      10.5452 ns |      10.8292 ns |       541.980 ns |   0.1173 |        - |       - |     736 B |
|                 MapNew_ofArray |     7 |       317.856 ns |       6.4387 ns |       8.1429 ns |       319.862 ns |   0.0687 |        - |       - |     432 B |
|                     Map_ofList |     7 |       490.791 ns |       9.7485 ns |      13.0140 ns |       490.675 ns |   0.1097 |        - |       - |     688 B |
|                  MapNew_ofList |     7 |       374.638 ns |       0.9654 ns |       0.8062 ns |       374.396 ns |   0.0930 |        - |       - |     584 B |
|                      Map_ofSeq |     7 |       642.049 ns |      12.1435 ns |      11.3590 ns |       647.348 ns |   0.1402 |        - |       - |     880 B |
|                   MapNew_ofSeq |     7 |       463.088 ns |       7.2641 ns |       6.7948 ns |       459.950 ns |   0.0992 |        - |       - |     624 B |
|                    Map_toArray |     7 |       171.178 ns |       3.4090 ns |       4.6663 ns |       170.640 ns |   0.0751 |        - |       - |     472 B |
|                 MapNew_toArray |     7 |        79.534 ns |       1.4362 ns |       1.3434 ns |        79.234 ns |   0.0433 |        - |       - |     272 B |
|                     Map_toList |     7 |       130.834 ns |       2.5806 ns |       3.0721 ns |       131.098 ns |   0.0625 |        - |       - |     392 B |
|                  MapNew_toList |     7 |       107.391 ns |       2.0180 ns |       1.8876 ns |       107.989 ns |   0.0625 |   0.0001 |       - |     392 B |
|                  Map_enumerate |     7 |       313.044 ns |       6.1105 ns |       7.5043 ns |       309.659 ns |   0.0954 |        - |       - |     600 B |
|               MapNew_enumerate |     7 |       176.136 ns |       3.4207 ns |       4.4479 ns |       176.174 ns |   0.0446 |        - |       - |     280 B |
|                 Map_toSeq_enum |     7 |       476.361 ns |       9.4022 ns |      12.8698 ns |       484.761 ns |   0.1268 |        - |       - |     800 B |
|              MapNew_toSeq_enum |     7 |       537.299 ns |      10.7177 ns |      15.0247 ns |       540.530 ns |   0.1011 |        - |       - |     640 B |
|            Map_containsKey_all |     7 |       173.812 ns |       3.4031 ns |       4.1793 ns |       172.972 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |     7 |       101.859 ns |       1.9434 ns |       1.8178 ns |       102.679 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |     7 |        25.769 ns |       0.5496 ns |       0.6109 ns |        25.719 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     7 |        13.202 ns |       0.2177 ns |       0.2036 ns |        13.248 ns |        - |        - |       - |         - |
|                    Map_tryFind |     7 |        39.159 ns |       0.8415 ns |       1.0334 ns |        38.857 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     7 |         9.241 ns |       0.0253 ns |       0.0198 ns |         9.243 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |     7 |        28.436 ns |       0.6175 ns |       0.5776 ns |        28.200 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     7 |        13.274 ns |       0.3407 ns |       0.2845 ns |        13.159 ns |        - |        - |       - |         - |
|                 Map_remove_all |     7 |       482.193 ns |       8.0494 ns |       8.2662 ns |       479.556 ns |   0.1116 |        - |       - |     704 B |
|              MapNew_remove_all |     7 |       589.105 ns |      11.8624 ns |      15.8359 ns |       588.511 ns |   0.0925 |        - |       - |     584 B |
|                     Map_exists |     7 |        70.210 ns |       1.4190 ns |       1.6341 ns |        71.094 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     7 |        33.279 ns |       0.6833 ns |       0.8392 ns |        32.981 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |     7 |        51.808 ns |       0.8490 ns |       0.7941 ns |        51.429 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     7 |        33.776 ns |       0.6849 ns |       0.7612 ns |        33.588 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |     7 |        67.677 ns |       1.3742 ns |       1.8345 ns |        67.661 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     7 |        33.305 ns |       0.6900 ns |       0.8474 ns |        33.289 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |    10 |     1,117.669 ns |      22.3292 ns |      26.5814 ns |     1,113.141 ns |   0.2193 |        - |       - |    1384 B |
|                 MapNew_ofArray |    10 |       550.311 ns |      10.4880 ns |      11.6574 ns |       541.881 ns |   0.0992 |        - |       - |     624 B |
|                     Map_ofList |    10 |     1,045.089 ns |      20.6839 ns |      35.1228 ns |     1,054.163 ns |   0.2003 |        - |       - |    1264 B |
|                  MapNew_ofList |    10 |       651.910 ns |      13.1943 ns |      13.5496 ns |       661.115 ns |   0.1230 |        - |       - |     776 B |
|                      Map_ofSeq |    10 |     1,047.130 ns |      15.8623 ns |      14.0615 ns |     1,041.468 ns |   0.2079 |        - |       - |    1312 B |
|                   MapNew_ofSeq |    10 |       748.079 ns |      14.0826 ns |      14.4618 ns |       741.427 ns |   0.1297 |        - |       - |     816 B |
|                    Map_toArray |    10 |       249.307 ns |       4.9752 ns |       6.8101 ns |       244.400 ns |   0.1054 |        - |       - |     664 B |
|                 MapNew_toArray |    10 |       124.644 ns |       2.5359 ns |       3.2071 ns |       126.125 ns |   0.0587 |        - |       - |     368 B |
|                     Map_toList |    10 |       187.209 ns |       3.7243 ns |       3.8245 ns |       185.269 ns |   0.0892 |   0.0002 |       - |     560 B |
|                  MapNew_toList |    10 |       165.455 ns |       3.2952 ns |       3.5259 ns |       166.737 ns |   0.0892 |   0.0002 |       - |     560 B |
|                  Map_enumerate |    10 |       438.760 ns |       8.3669 ns |       8.5922 ns |       435.590 ns |   0.1335 |        - |       - |     840 B |
|               MapNew_enumerate |    10 |       246.041 ns |       4.8382 ns |       7.6739 ns |       243.409 ns |   0.0634 |        - |       - |     400 B |
|                 Map_toSeq_enum |    10 |       706.860 ns |      13.4883 ns |      12.6170 ns |       698.125 ns |   0.1955 |        - |       - |    1232 B |
|              MapNew_toSeq_enum |    10 |       724.271 ns |      11.6154 ns |      10.8650 ns |       721.835 ns |   0.1326 |        - |       - |     832 B |
|            Map_containsKey_all |    10 |       266.662 ns |       0.2363 ns |       0.1845 ns |       266.715 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |    10 |       168.924 ns |       3.4130 ns |       3.7936 ns |       169.608 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |    10 |        37.450 ns |       0.2867 ns |       0.2239 ns |        37.383 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    10 |        16.845 ns |       0.2154 ns |       0.1682 ns |        16.794 ns |        - |        - |       - |         - |
|                    Map_tryFind |    10 |        48.099 ns |       0.9558 ns |       0.8941 ns |        48.493 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    10 |        17.838 ns |       0.4228 ns |       0.5193 ns |        17.833 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |    10 |        30.460 ns |       0.6747 ns |       0.8773 ns |        30.159 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    10 |        17.148 ns |       0.4222 ns |       0.5185 ns |        17.069 ns |        - |        - |       - |         - |
|                 Map_remove_all |    10 |       826.050 ns |      16.4491 ns |      16.1552 ns |       817.787 ns |   0.1898 |        - |       - |    1192 B |
|              MapNew_remove_all |    10 |       897.698 ns |      17.7282 ns |      16.5830 ns |       905.779 ns |   0.1459 |        - |       - |     920 B |
|                     Map_exists |    10 |        97.802 ns |       1.7939 ns |       1.6780 ns |        97.144 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |    10 |        51.386 ns |       1.0655 ns |       1.0465 ns |        51.162 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |    10 |        88.666 ns |       1.7909 ns |       1.9905 ns |        88.559 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |    10 |        53.274 ns |       1.0560 ns |       0.9878 ns |        53.535 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |    10 |        93.097 ns |       1.8106 ns |       1.9373 ns |        92.761 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |    10 |        52.062 ns |       0.9310 ns |       0.8709 ns |        51.559 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |   100 |    29,723.900 ns |     449.2769 ns |     398.2723 ns |    29,499.696 ns |   4.4250 |   0.1221 |       - |   27832 B |
|                 MapNew_ofArray |   100 |     7,419.026 ns |     146.2653 ns |     190.1862 ns |     7,430.708 ns |   0.8850 |   0.0229 |       - |    5592 B |
|                     Map_ofList |   100 |    32,662.766 ns |     638.2004 ns |     829.8408 ns |    33,016.833 ns |   4.7607 |   0.1221 |       - |   30040 B |
|                  MapNew_ofList |   100 |     8,761.496 ns |     171.6155 ns |     197.6326 ns |     8,729.741 ns |   1.2054 |   0.0305 |       - |    7608 B |
|                      Map_ofSeq |   100 |    30,452.781 ns |     595.6754 ns |     873.1332 ns |    30,019.138 ns |   4.5776 |   0.1221 |       - |   28792 B |
|                   MapNew_ofSeq |   100 |     9,161.780 ns |     142.2103 ns |     152.1635 ns |     9,137.865 ns |   1.2054 |   0.0305 |       - |    7648 B |
|                    Map_toArray |   100 |     2,541.918 ns |      48.4192 ns |      47.5541 ns |     2,515.090 ns |   1.0223 |   0.0191 |       - |    6424 B |
|                 MapNew_toArray |   100 |     1,141.603 ns |      22.1971 ns |      28.0722 ns |     1,126.029 ns |   0.5169 |   0.0114 |       - |    3248 B |
|                     Map_toList |   100 |     1,940.132 ns |      29.2750 ns |      30.0632 ns |     1,932.004 ns |   0.8926 |   0.0267 |       - |    5600 B |
|                  MapNew_toList |   100 |     1,925.769 ns |      37.8980 ns |      51.8752 ns |     1,932.167 ns |   0.8926 |   0.0267 |       - |    5600 B |
|                  Map_enumerate |   100 |     4,379.305 ns |       7.2149 ns |       6.0248 ns |     4,377.381 ns |   1.1978 |        - |       - |    7560 B |
|               MapNew_enumerate |   100 |     2,356.856 ns |      46.4383 ns |      49.6885 ns |     2,329.710 ns |   0.6371 |        - |       - |    4000 B |
|                 Map_toSeq_enum |   100 |     6,523.367 ns |     111.3768 ns |     104.1820 ns |     6,562.488 ns |   1.6098 |        - |       - |   10112 B |
|              MapNew_toSeq_enum |   100 |     6,235.845 ns |      96.8196 ns |      90.5651 ns |     6,177.855 ns |   1.0452 |        - |       - |    6592 B |
|            Map_containsKey_all |   100 |     6,753.753 ns |      48.5030 ns |      45.3697 ns |     6,764.004 ns |        - |        - |       - |         - |
|         MapNew_containsKey_all |   100 |     3,367.230 ns |      63.8027 ns |      68.2682 ns |     3,380.673 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |   100 |        65.559 ns |       1.3062 ns |       1.6519 ns |        64.398 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   100 |        28.209 ns |       0.5805 ns |       0.6685 ns |        28.088 ns |        - |        - |       - |         - |
|                    Map_tryFind |   100 |        67.599 ns |       1.4086 ns |       1.7299 ns |        66.691 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   100 |        26.614 ns |       0.6184 ns |       1.0501 ns |        26.462 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |   100 |        58.589 ns |       0.8034 ns |       0.6709 ns |        58.327 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   100 |        28.718 ns |       0.6576 ns |       1.0042 ns |        28.738 ns |        - |        - |       - |         - |
|                 Map_remove_all |   100 |    24,884.142 ns |      22.0263 ns |      19.5257 ns |    24,882.411 ns |   4.4556 |        - |       - |   27952 B |
|              MapNew_remove_all |   100 |    21,172.886 ns |     420.2597 ns |     449.6732 ns |    21,045.973 ns |   3.6926 |        - |       - |   23288 B |
|                     Map_exists |   100 |       907.489 ns |       7.0369 ns |       6.2380 ns |       908.816 ns |   0.0038 |        - |       - |      24 B |
|                  MapNew_exists |   100 |       438.948 ns |       6.4889 ns |       6.0698 ns |       435.226 ns |   0.0038 |        - |       - |      24 B |
|                       Map_fold |   100 |       807.399 ns |      14.9741 ns |      14.0068 ns |       801.644 ns |   0.0038 |        - |       - |      24 B |
|                    MapNew_fold |   100 |       444.470 ns |       2.7849 ns |       2.1742 ns |       444.966 ns |   0.0038 |        - |       - |      24 B |
|                   Map_foldBack |   100 |       854.992 ns |      16.3486 ns |      16.0565 ns |       860.339 ns |   0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |   100 |       441.277 ns |       6.8166 ns |       6.3763 ns |       439.011 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray |  1000 |   550,527.513 ns |  10,763.8290 ns |  11,963.9674 ns |   553,847.656 ns |  71.2891 |  16.6016 |       - |  447720 B |
|                 MapNew_ofArray |  1000 |   127,610.808 ns |   2,512.8940 ns |   4,128.7569 ns |   129,415.234 ns |   8.3008 |   1.4648 |       - |   52344 B |
|                     Map_ofList |  1000 |   547,708.594 ns |  10,559.2376 ns |  10,843.5596 ns |   543,263.184 ns |  71.2891 |  16.6016 |       - |  453312 B |
|                  MapNew_ofList |  1000 |   135,026.445 ns |   2,580.3059 ns |   3,071.6706 ns |   134,863.647 ns |  10.7422 |   1.9531 |       - |   68768 B |
|                      Map_ofSeq |  1000 |   541,583.594 ns |  10,814.4090 ns |  12,873.7846 ns |   533,393.652 ns |  70.3125 |  15.6250 |       - |  442888 B |
|                   MapNew_ofSeq |  1000 |   143,736.600 ns |   2,803.2595 ns |   3,228.2378 ns |   143,439.111 ns |  10.7422 |   1.7090 |       - |   68808 B |
|                    Map_toArray |  1000 |    31,621.652 ns |     608.5312 ns |     832.9643 ns |    31,708.536 ns |  10.1929 |   1.0986 |       - |   64024 B |
|                 MapNew_toArray |  1000 |    11,574.309 ns |     225.0985 ns |     231.1596 ns |    11,602.573 ns |   5.0964 |   0.8698 |       - |   32048 B |
|                     Map_toList |  1000 |    22,595.986 ns |     189.2345 ns |     158.0194 ns |    22,532.532 ns |   8.9111 |   1.8921 |       - |   56000 B |
|                  MapNew_toList |  1000 |    22,280.099 ns |     432.9565 ns |     404.9878 ns |    22,457.590 ns |   8.9111 |   1.8921 |       - |   56000 B |
|                  Map_enumerate |  1000 |    45,015.934 ns |     883.8157 ns |     868.0252 ns |    44,651.727 ns |  12.1460 |        - |       - |   76320 B |
|               MapNew_enumerate |  1000 |    22,422.016 ns |     424.6452 ns |     454.3656 ns |    22,746.278 ns |   6.3477 |        - |       - |   40000 B |
|                 Map_toSeq_enum |  1000 |    65,354.321 ns |   1,305.8836 ns |   1,872.8592 ns |    65,397.009 ns |  15.9912 |        - |       - |  100712 B |
|              MapNew_toSeq_enum |  1000 |    61,004.062 ns |     668.2160 ns |     625.0496 ns |    60,621.283 ns |  10.1929 |        - |       - |   64193 B |
|            Map_containsKey_all |  1000 |   113,211.109 ns |   1,355.7356 ns |   1,268.1559 ns |   112,803.015 ns |        - |        - |       - |       2 B |
|         MapNew_containsKey_all |  1000 |    76,359.277 ns |   1,467.1146 ns |   1,801.7485 ns |    75,622.852 ns |        - |        - |       - |         - |
|    Map_containsKey_nonexisting |  1000 |       103.614 ns |       2.0208 ns |       2.0752 ns |       103.811 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  1000 |        39.686 ns |       0.7112 ns |       0.6305 ns |        39.448 ns |        - |        - |       - |         - |
|                    Map_tryFind |  1000 |       102.466 ns |       0.1069 ns |       0.0835 ns |       102.451 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  1000 |        45.768 ns |       0.9591 ns |       0.9420 ns |        45.361 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting |  1000 |       108.540 ns |       1.9362 ns |       1.8111 ns |       107.159 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  1000 |        39.426 ns |       0.8296 ns |       0.8877 ns |        39.469 ns |        - |        - |       - |         - |
|                 Map_remove_all |  1000 |   484,270.787 ns |   9,490.3447 ns |  15,856.2341 ns |   487,986.035 ns |  70.3125 |   2.9297 |       - |  442872 B |
|              MapNew_remove_all |  1000 |   353,287.463 ns |   6,986.3898 ns |   6,861.5689 ns |   353,214.990 ns |  62.5000 |   2.4414 |       - |  393248 B |
|                     Map_exists |  1000 |    10,903.803 ns |     217.8469 ns |     203.7741 ns |    10,859.595 ns |        - |        - |       - |      24 B |
|                  MapNew_exists |  1000 |     4,062.723 ns |      62.0580 ns |      55.0128 ns |     4,060.047 ns |        - |        - |       - |      24 B |
|                       Map_fold |  1000 |    10,098.989 ns |     195.4677 ns |     217.2618 ns |    10,130.963 ns |        - |        - |       - |      24 B |
|                    MapNew_fold |  1000 |     4,042.333 ns |      78.3828 ns |      90.2657 ns |     4,047.409 ns |        - |        - |       - |      24 B |
|                   Map_foldBack |  1000 |    10,153.699 ns |     196.9994 ns |     218.9643 ns |    10,168.208 ns |        - |        - |       - |      24 B |
|                MapNew_foldBack |  1000 |     3,762.548 ns |      72.9091 ns |      68.1992 ns |     3,749.877 ns |   0.0038 |        - |       - |      24 B |
|                    Map_ofArray | 10000 | 9,786,431.791 ns | 195,520.4995 ns | 267,630.6666 ns | 9,622,908.594 ns | 968.7500 | 250.0000 | 46.8750 | 6092584 B |
|                 MapNew_ofArray | 10000 | 1,878,258.001 ns |  21,147.4533 ns |  19,781.3410 ns | 1,867,261.426 ns |  85.9375 |  42.9688 |       - |  541779 B |
|                     Map_ofList | 10000 | 9,623,494.583 ns | 178,899.8156 ns | 167,342.9985 ns | 9,709,590.625 ns | 953.1250 | 250.0000 | 46.8750 | 6075121 B |
|                  MapNew_ofList | 10000 | 2,079,357.413 ns |  40,984.0873 ns |  75,966.6337 ns | 2,063,997.656 ns | 121.0938 |  78.1250 | 39.0625 |  804056 B |
|                      Map_ofSeq | 10000 | 9,693,559.263 ns | 184,873.8644 ns | 163,885.8654 ns | 9,701,959.375 ns | 968.7500 | 265.6250 | 46.8750 | 6158341 B |
|                   MapNew_ofSeq | 10000 | 2,243,933.261 ns |  42,104.5458 ns |  59,024.6928 ns | 2,246,679.688 ns | 121.0938 |  78.1250 | 39.0625 |  804096 B |
|                    Map_toArray | 10000 |   498,515.820 ns |   9,644.3634 ns |  10,319.3608 ns |   492,590.186 ns | 101.5625 |  39.0625 |       - |  640025 B |
|                 MapNew_toArray | 10000 |   161,349.051 ns |   2,942.2119 ns |   2,752.1469 ns |   161,865.576 ns |  50.7813 |  25.3906 |       - |  320048 B |
|                     Map_toList | 10000 |   376,615.105 ns |   7,416.3642 ns |  10,151.6031 ns |   376,078.076 ns |  88.8672 |  44.4336 |       - |  560000 B |
|                  MapNew_toList | 10000 |   337,039.692 ns |   6,717.8094 ns |   7,187.9808 ns |   333,634.692 ns |  88.8672 |  44.4336 |       - |  560000 B |
|                  Map_enumerate | 10000 |   463,612.703 ns |   3,100.2127 ns |   2,420.4422 ns |   462,946.484 ns | 119.6289 |        - |       - |  753361 B |
|               MapNew_enumerate | 10000 |   243,325.653 ns |   4,838.9111 ns |   5,760.3795 ns |   239,381.494 ns |  63.4766 |        - |       - |  400000 B |
|                 Map_toSeq_enum | 10000 |   677,574.719 ns |  13,272.2454 ns |  13,629.6189 ns |   682,583.301 ns | 158.2031 |        - |       - |  994112 B |
|              MapNew_toSeq_enum | 10000 |   640,235.913 ns |  12,715.3766 ns |  12,488.2000 ns |   645,060.547 ns | 101.5625 |        - |       - |  640193 B |
|            Map_containsKey_all | 10000 | 1,702,253.307 ns |  25,567.4281 ns |  23,915.7881 ns | 1,687,944.922 ns |        - |        - |       - |       3 B |
|         MapNew_containsKey_all | 10000 | 1,128,320.133 ns |   1,270.8471 ns |     992.1938 ns | 1,128,035.449 ns |        - |        - |       - |       1 B |
|    Map_containsKey_nonexisting | 10000 |       111.293 ns |       1.6754 ns |       1.5672 ns |       110.780 ns |        - |        - |       - |         - |
| MapNew_containsKey_nonexisting | 10000 |        54.620 ns |       0.4175 ns |       0.3487 ns |        54.456 ns |        - |        - |       - |         - |
|                    Map_tryFind | 10000 |       140.599 ns |       2.8804 ns |       3.3171 ns |       138.987 ns |   0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind | 10000 |        43.622 ns |       0.8549 ns |       0.7997 ns |        43.068 ns |   0.0038 |        - |       - |      24 B |
|        Map_tryFind_nonexisting | 10000 |       153.582 ns |       0.6183 ns |       0.5163 ns |       153.430 ns |        - |        - |       - |         - |
|     MapNew_tryFind_nonexisting | 10000 |        54.384 ns |       0.5109 ns |       0.4266 ns |        54.511 ns |        - |        - |       - |         - |
|                 Map_remove_all | 10000 | 7,602,156.510 ns | 104,277.0835 ns |  97,540.8486 ns | 7,585,447.656 ns | 968.7500 | 265.6250 |       - | 6091960 B |
|              MapNew_remove_all | 10000 | 5,408,995.312 ns |  91,957.7889 ns |  81,518.1847 ns | 5,424,740.625 ns | 875.0000 | 242.1875 |       - | 5511224 B |
|                     Map_exists | 10000 |   141,867.563 ns |   2,771.3651 ns |   2,592.3366 ns |   139,812.231 ns |        - |        - |       - |      24 B |
|                  MapNew_exists | 10000 |    48,138.034 ns |     660.3577 ns |     515.5639 ns |    48,345.331 ns |        - |        - |       - |      25 B |
|                       Map_fold | 10000 |   133,436.739 ns |     496.0632 ns |     414.2352 ns |   133,490.356 ns |        - |        - |       - |      24 B |
|                    MapNew_fold | 10000 |    49,762.162 ns |     880.5253 ns |     823.6439 ns |    49,088.611 ns |        - |        - |       - |      24 B |
|                   Map_foldBack | 10000 |   134,331.441 ns |     182.0826 ns |     142.1581 ns |   134,288.562 ns |        - |        - |       - |      24 B |
|                MapNew_foldBack | 10000 |    51,476.104 ns |     485.7287 ns |     405.6055 ns |    51,513.641 ns |        - |        - |       - |      24 B |