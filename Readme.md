### Overview 

[![CI - Windows](https://github.com/krauthaufen/MapNew/workflows/CI%20-%20Windows/badge.svg)](https://github.com/krauthaufen/MapNew/actions?query=workflow%3A%22CI+-+Windows%22)
[![CI - MacOS](https://github.com/krauthaufen/MapNew/workflows/CI%20-%20MacOS/badge.svg)](https://github.com/krauthaufen/MapNew/actions?query=workflow%3A%22CI+-+MacOS%22)
[![CI - Linux](https://github.com/krauthaufen/MapNew/workflows/CI%20-%20Linux/badge.svg)](https://github.com/krauthaufen/MapNew/actions?query=workflow%3A%22CI+-+Linux%22)

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
* `ofSeq`, `ofList` and `ofArray` are slightly worse for some small counts but I think that will be acceptable
* `toSeq` is more or less identical in speed which was more or less expected, due to its heavy use of virtual methods.
* `add` itself seems to be a little slower for very small counts but quite significantly faster for larger ones.

```

// * Summary *

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
Intel Core i7-4930K CPU 3.40GHz (Haswell), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT DEBUG
  Job-GNSQLR : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Server=False  IterationTime=1.0000 s  MaxIterationCount=100  
```


|                         Method | Count |             Mean |           Error |          StdDev |           Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|------------------------------- |------ |-----------------:|----------------:|----------------:|-----------------:|------:|--------:|----------:|---------:|--------:|----------:|
|                        Map_add |     1 |        25.671 ns |       0.5417 ns |       0.5563 ns |        25.796 ns |  1.00 |    0.00 |    0.0140 |        - |       - |      88 B |
|                     MapNew_add |     1 |        31.574 ns |       0.5057 ns |       0.6020 ns |        31.281 ns |  1.23 |    0.05 |    0.0140 |        - |       - |      88 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     1 |        18.082 ns |       0.2786 ns |       0.2606 ns |        17.931 ns |  1.00 |    0.00 |    0.0064 |        - |       - |      40 B |
|                  MapNew_remove |     1 |        16.143 ns |       0.1489 ns |       0.1392 ns |        16.099 ns |  0.89 |    0.01 |    0.0064 |        - |       - |      40 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     1 |        18.756 ns |       0.2907 ns |       0.2719 ns |        18.653 ns |  1.00 |    0.00 |    0.0102 |        - |       - |      64 B |
|                 MapNew_ofArray |     1 |        13.221 ns |       0.2305 ns |       0.2156 ns |        13.133 ns |  0.71 |    0.02 |    0.0102 |        - |       - |      64 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     1 |        30.815 ns |       0.1979 ns |       0.1653 ns |        30.843 ns |  1.00 |    0.00 |    0.0140 |        - |       - |      88 B |
|                  MapNew_ofList |     1 |        13.112 ns |       0.0967 ns |       0.0755 ns |        13.100 ns |  0.43 |    0.00 |    0.0102 |        - |       - |      64 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     1 |        35.144 ns |       0.4102 ns |       0.3837 ns |        35.065 ns |  1.00 |    0.00 |    0.0140 |        - |       - |      88 B |
|                   MapNew_ofSeq |     1 |        18.085 ns |       0.2245 ns |       0.2100 ns |        18.012 ns |  0.51 |    0.01 |    0.0102 |        - |       - |      64 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     1 |        29.610 ns |       0.5156 ns |       0.4822 ns |        29.445 ns |  1.00 |    0.00 |    0.0140 |        - |       - |      88 B |
|                 MapNew_toArray |     1 |        19.268 ns |       0.3352 ns |       0.3135 ns |        19.067 ns |  0.65 |    0.02 |    0.0128 |        - |       - |      80 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     1 |        13.582 ns |       0.1420 ns |       0.1329 ns |        13.533 ns |  1.00 |    0.00 |    0.0089 |        - |       - |      56 B |
|                  MapNew_toList |     1 |        12.137 ns |       0.1641 ns |       0.1535 ns |        12.128 ns |  0.89 |    0.01 |    0.0089 |        - |       - |      56 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     1 |        52.946 ns |       0.6783 ns |       0.6345 ns |        52.921 ns |  1.00 |    0.00 |    0.0191 |        - |       - |     120 B |
|               MapNew_enumerate |     1 |        41.704 ns |       0.4348 ns |       0.4067 ns |        41.661 ns |  0.79 |    0.01 |    0.0064 |        - |       - |      40 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     1 |       125.170 ns |       1.5292 ns |       1.4304 ns |       125.595 ns |  1.00 |    0.00 |    0.0471 |        - |       - |     296 B |
|              MapNew_toSeq_enum |     1 |       151.930 ns |       1.5077 ns |       1.4103 ns |       151.290 ns |  1.21 |    0.02 |    0.0407 |        - |       - |     256 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     1 |         8.420 ns |       0.0907 ns |       0.0849 ns |         8.391 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     1 |         7.405 ns |       0.0996 ns |       0.0931 ns |         7.449 ns |  0.88 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     1 |         6.944 ns |       0.0386 ns |       0.0322 ns |         6.947 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     1 |         4.835 ns |       0.0343 ns |       0.0287 ns |         4.831 ns |  0.70 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     1 |        11.952 ns |       0.1963 ns |       0.1836 ns |        11.965 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     1 |         7.884 ns |       0.0679 ns |       0.0530 ns |         7.873 ns |  0.66 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     1 |         8.841 ns |       0.0695 ns |       0.0543 ns |         8.846 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     1 |         6.083 ns |       0.1089 ns |       0.1018 ns |         6.073 ns |  0.69 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     1 |        13.389 ns |       0.1732 ns |       0.1535 ns |        13.319 ns |  1.00 |    0.00 |    0.0064 |        - |       - |      40 B |
|              MapNew_remove_all |     1 |        12.559 ns |       0.2396 ns |       0.2124 ns |        12.511 ns |  0.94 |    0.02 |    0.0064 |        - |       - |      40 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     1 |        12.927 ns |       0.2074 ns |       0.1940 ns |        12.839 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     1 |        11.669 ns |       0.0583 ns |       0.0487 ns |        11.665 ns |  0.90 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     1 |        13.127 ns |       0.1871 ns |       0.1750 ns |        13.043 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     1 |        12.046 ns |       0.1672 ns |       0.1564 ns |        12.028 ns |  0.92 |    0.02 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     1 |        13.117 ns |       0.2038 ns |       0.1906 ns |        13.089 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     1 |        11.837 ns |       0.0720 ns |       0.0601 ns |        11.832 ns |  0.90 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     2 |       132.748 ns |       1.4616 ns |       1.2957 ns |       132.360 ns |  1.00 |    0.00 |    0.0394 |        - |       - |     248 B |
|                     MapNew_add |     2 |       112.763 ns |       1.9346 ns |       1.8097 ns |       112.102 ns |  0.85 |    0.02 |    0.0394 |        - |       - |     248 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     2 |        67.792 ns |       1.0335 ns |       0.9668 ns |        67.341 ns |  1.00 |    0.00 |    0.0165 |        - |       - |     104 B |
|                  MapNew_remove |     2 |        46.809 ns |       0.5825 ns |       0.5164 ns |        46.694 ns |  0.69 |    0.01 |    0.0166 |        - |       - |     104 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     2 |        31.759 ns |       0.5709 ns |       0.5340 ns |        31.747 ns |  1.00 |    0.00 |    0.0178 |        - |       - |     112 B |
|                 MapNew_ofArray |     2 |        32.918 ns |       0.2293 ns |       0.1790 ns |        32.917 ns |  1.04 |    0.02 |    0.0178 |        - |       - |     112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     2 |        46.052 ns |       0.9235 ns |       0.8638 ns |        45.762 ns |  1.00 |    0.00 |    0.0217 |        - |       - |     136 B |
|                  MapNew_ofList |     2 |        33.981 ns |       0.5687 ns |       0.5320 ns |        33.675 ns |  0.74 |    0.02 |    0.0178 |        - |       - |     112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     2 |        48.945 ns |       0.4696 ns |       0.4393 ns |        48.833 ns |  1.00 |    0.00 |    0.0216 |        - |       - |     136 B |
|                   MapNew_ofSeq |     2 |        40.220 ns |       0.6728 ns |       0.6293 ns |        40.065 ns |  0.82 |    0.02 |    0.0178 |        - |       - |     112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     2 |        54.463 ns |       0.6298 ns |       0.5891 ns |        54.265 ns |  1.00 |    0.00 |    0.0242 |        - |       - |     152 B |
|                 MapNew_toArray |     2 |        31.196 ns |       0.4398 ns |       0.4114 ns |        31.059 ns |  0.57 |    0.01 |    0.0178 |        - |       - |     112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     2 |        35.531 ns |       0.4128 ns |       0.3861 ns |        35.318 ns |  1.00 |    0.00 |    0.0178 |        - |       - |     112 B |
|                  MapNew_toList |     2 |        25.764 ns |       0.2977 ns |       0.2785 ns |        25.703 ns |  0.73 |    0.01 |    0.0178 |        - |       - |     112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     2 |       104.708 ns |       0.6834 ns |       0.6058 ns |       104.451 ns |  1.00 |    0.00 |    0.0382 |        - |       - |     240 B |
|               MapNew_enumerate |     2 |        58.650 ns |       0.5078 ns |       0.4750 ns |        58.607 ns |  0.56 |    0.01 |    0.0127 |        - |       - |      80 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     2 |       202.779 ns |       3.0263 ns |       2.8308 ns |       201.658 ns |  1.00 |    0.00 |    0.0701 |        - |       - |     440 B |
|              MapNew_toSeq_enum |     2 |       199.286 ns |       2.1593 ns |       1.9141 ns |       198.343 ns |  0.98 |    0.02 |    0.0509 |        - |       - |     320 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     2 |        30.994 ns |       0.3913 ns |       0.3660 ns |        30.841 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     2 |        18.694 ns |       0.2466 ns |       0.2306 ns |        18.652 ns |  0.60 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     2 |        16.245 ns |       0.2593 ns |       0.2425 ns |        16.254 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     2 |         6.865 ns |       0.1054 ns |       0.0986 ns |         6.817 ns |  0.42 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     2 |        21.336 ns |       0.3431 ns |       0.3209 ns |        21.220 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     2 |        11.744 ns |       0.1682 ns |       0.1573 ns |        11.731 ns |  0.55 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     2 |        19.109 ns |       0.3289 ns |       0.2916 ns |        19.018 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     2 |         9.568 ns |       0.0858 ns |       0.0716 ns |         9.537 ns |  0.50 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     2 |        56.623 ns |       0.5055 ns |       0.4221 ns |        56.423 ns |  1.00 |    0.00 |    0.0165 |        - |       - |     104 B |
|              MapNew_remove_all |     2 |        40.081 ns |       0.3609 ns |       0.3014 ns |        40.138 ns |  0.71 |    0.01 |    0.0165 |        - |       - |     104 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     2 |        25.878 ns |       0.3995 ns |       0.3737 ns |        25.663 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     2 |        17.377 ns |       0.1877 ns |       0.1756 ns |        17.374 ns |  0.67 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     2 |        25.870 ns |       0.3993 ns |       0.3735 ns |        25.894 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     2 |        17.719 ns |       0.1406 ns |       0.1246 ns |        17.687 ns |  0.68 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     2 |        25.831 ns |       0.2691 ns |       0.2517 ns |        25.732 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     2 |        17.786 ns |       0.2613 ns |       0.2316 ns |        17.681 ns |  0.69 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     3 |       215.811 ns |       2.5728 ns |       2.4066 ns |       215.112 ns |  1.00 |    0.00 |    0.0649 |        - |       - |     408 B |
|                     MapNew_add |     3 |       324.962 ns |       4.6924 ns |       4.3892 ns |       324.790 ns |  1.51 |    0.03 |    0.0763 |        - |       - |     480 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     3 |       172.133 ns |       0.7805 ns |       0.6517 ns |       172.229 ns |  1.00 |    0.00 |    0.0381 |        - |       - |     240 B |
|                  MapNew_remove |     3 |       145.147 ns |       1.7995 ns |       1.6832 ns |       144.726 ns |  0.84 |    0.01 |    0.0382 |        - |       - |     240 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     3 |        93.217 ns |       0.6394 ns |       0.5981 ns |        93.109 ns |  1.00 |    0.00 |    0.0331 |        - |       - |     208 B |
|                 MapNew_ofArray |     3 |       121.849 ns |       1.9981 ns |       1.8690 ns |       120.965 ns |  1.31 |    0.01 |    0.0331 |        - |       - |     208 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     3 |       108.807 ns |       1.3872 ns |       1.2976 ns |       108.268 ns |  1.00 |    0.00 |    0.0369 |        - |       - |     232 B |
|                  MapNew_ofList |     3 |       107.317 ns |       0.6088 ns |       0.4753 ns |       107.479 ns |  0.99 |    0.01 |    0.0331 |        - |       - |     208 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     3 |       110.665 ns |       1.4994 ns |       1.4025 ns |       110.279 ns |  1.00 |    0.00 |    0.0369 |        - |       - |     232 B |
|                   MapNew_ofSeq |     3 |        62.354 ns |       0.6053 ns |       0.5366 ns |        62.101 ns |  0.56 |    0.01 |    0.0217 |        - |       - |     136 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     3 |        67.208 ns |       0.3972 ns |       0.3521 ns |        67.165 ns |  1.00 |    0.00 |    0.0344 |        - |       - |     216 B |
|                 MapNew_toArray |     3 |        41.859 ns |       0.4529 ns |       0.4237 ns |        41.872 ns |  0.62 |    0.01 |    0.0229 |        - |       - |     144 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     3 |        55.194 ns |       0.3364 ns |       0.2809 ns |        55.158 ns |  1.00 |    0.00 |    0.0268 |        - |       - |     168 B |
|                  MapNew_toList |     3 |        39.606 ns |       0.3966 ns |       0.3516 ns |        39.512 ns |  0.72 |    0.01 |    0.0268 |        - |       - |     168 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     3 |       160.953 ns |       2.2504 ns |       2.1050 ns |       160.481 ns |  1.00 |    0.00 |    0.0573 |        - |       - |     360 B |
|               MapNew_enumerate |     3 |        69.918 ns |       0.6595 ns |       0.5846 ns |        69.869 ns |  0.43 |    0.01 |    0.0191 |        - |       - |     120 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     3 |       231.651 ns |       3.6082 ns |       3.3751 ns |       231.606 ns |  1.00 |    0.00 |    0.0738 |        - |       - |     464 B |
|              MapNew_toSeq_enum |     3 |       279.621 ns |       3.6754 ns |       3.4380 ns |       277.688 ns |  1.21 |    0.02 |    0.0611 |        - |       - |     384 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     3 |        42.891 ns |       0.2091 ns |       0.1853 ns |        42.880 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     3 |        33.963 ns |       0.1643 ns |       0.1283 ns |        33.962 ns |  0.79 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     3 |        26.337 ns |       0.4173 ns |       0.3903 ns |        26.168 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     3 |         9.307 ns |       0.1293 ns |       0.1147 ns |         9.278 ns |  0.35 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     3 |        27.342 ns |       0.1755 ns |       0.1466 ns |        27.329 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     3 |        15.798 ns |       0.2576 ns |       0.2410 ns |        15.800 ns |  0.58 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     3 |        19.364 ns |       0.2968 ns |       0.2777 ns |        19.438 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     3 |         6.824 ns |       0.1487 ns |       0.1318 ns |         6.771 ns |  0.35 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     3 |       118.653 ns |       1.1976 ns |       1.1202 ns |       118.144 ns |  1.00 |    0.00 |    0.0305 |        - |       - |     192 B |
|              MapNew_remove_all |     3 |       102.924 ns |       1.0919 ns |       0.9680 ns |       102.514 ns |  0.87 |    0.01 |    0.0305 |        - |       - |     192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     3 |        40.421 ns |       0.1049 ns |       0.0876 ns |        40.419 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     3 |        23.228 ns |       0.1398 ns |       0.1239 ns |        23.179 ns |  0.57 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     3 |        37.889 ns |       0.2900 ns |       0.2570 ns |        37.903 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     3 |        18.388 ns |       0.1424 ns |       0.1332 ns |        18.354 ns |  0.49 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     3 |        25.355 ns |       0.3130 ns |       0.2928 ns |        25.351 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     3 |        23.852 ns |       0.2620 ns |       0.2322 ns |        23.805 ns |  0.94 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     4 |       398.716 ns |       5.8240 ns |       5.4478 ns |       400.746 ns |  1.00 |    0.00 |    0.0981 |        - |       - |     616 B |
|                     MapNew_add |     4 |       378.638 ns |       4.3392 ns |       3.8466 ns |       377.336 ns |  0.95 |    0.02 |    0.0943 |        - |       - |     592 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     4 |       272.961 ns |       3.4530 ns |       3.2300 ns |       271.666 ns |  1.00 |    0.00 |    0.0739 |        - |       - |     464 B |
|                  MapNew_remove |     4 |       262.086 ns |       3.2468 ns |       3.0370 ns |       261.084 ns |  0.96 |    0.02 |    0.0637 |        - |       - |     400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     4 |       239.709 ns |       3.4039 ns |       3.1840 ns |       240.380 ns |  1.00 |    0.00 |    0.0676 |        - |       - |     424 B |
|                 MapNew_ofArray |     4 |       174.403 ns |       2.2278 ns |       2.0839 ns |       173.903 ns |  0.73 |    0.01 |    0.0445 |        - |       - |     280 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     4 |       162.870 ns |       0.9640 ns |       0.8050 ns |       162.997 ns |  1.00 |    0.00 |    0.0483 |        - |       - |     304 B |
|                  MapNew_ofList |     4 |       357.591 ns |       5.0466 ns |       4.7206 ns |       358.910 ns |  2.19 |    0.03 |    0.0675 |        - |       - |     424 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     4 |       158.114 ns |       2.3335 ns |       2.1828 ns |       158.074 ns |  1.00 |    0.00 |    0.0484 |        - |       - |     304 B |
|                   MapNew_ofSeq |     4 |       118.979 ns |       2.3280 ns |       2.1776 ns |       118.602 ns |  0.75 |    0.01 |    0.0292 |        - |       - |     184 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     4 |        93.205 ns |       1.5289 ns |       1.4301 ns |        93.139 ns |  1.00 |    0.00 |    0.0445 |        - |       - |     280 B |
|                 MapNew_toArray |     4 |        49.309 ns |       0.8221 ns |       0.7690 ns |        49.051 ns |  0.53 |    0.01 |    0.0280 |        - |       - |     176 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     4 |        64.442 ns |       0.4891 ns |       0.4335 ns |        64.523 ns |  1.00 |    0.00 |    0.0357 |        - |       - |     224 B |
|                  MapNew_toList |     4 |        49.275 ns |       0.6804 ns |       0.6364 ns |        49.122 ns |  0.76 |    0.01 |    0.0357 |        - |       - |     224 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     4 |       167.405 ns |       0.5500 ns |       0.4593 ns |       167.595 ns |  1.00 |    0.00 |    0.0573 |        - |       - |     360 B |
|               MapNew_enumerate |     4 |        87.244 ns |       1.5058 ns |       1.4086 ns |        87.040 ns |  0.52 |    0.01 |    0.0255 |        - |       - |     160 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     4 |       294.490 ns |       4.4698 ns |       4.1810 ns |       292.598 ns |  1.00 |    0.00 |    0.0967 |        - |       - |     608 B |
|              MapNew_toSeq_enum |     4 |       315.468 ns |       3.3591 ns |       3.1421 ns |       316.395 ns |  1.07 |    0.02 |    0.0712 |        - |       - |     448 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     4 |        70.593 ns |       0.3377 ns |       0.2820 ns |        70.648 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     4 |        44.913 ns |       0.3628 ns |       0.3030 ns |        44.819 ns |  0.64 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     4 |        28.204 ns |       0.4248 ns |       0.3974 ns |        28.043 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     4 |        12.661 ns |       0.1023 ns |       0.0907 ns |        12.639 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     4 |        29.409 ns |       0.4759 ns |       0.4452 ns |        29.307 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     4 |        15.536 ns |       0.2146 ns |       0.2008 ns |        15.564 ns |  0.53 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     4 |        26.898 ns |       0.3869 ns |       0.3619 ns |        26.812 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     4 |         9.449 ns |       0.1818 ns |       0.1611 ns |         9.399 ns |  0.35 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     4 |       166.817 ns |       0.9372 ns |       0.7317 ns |       166.788 ns |  1.00 |    0.00 |    0.0446 |        - |       - |     280 B |
|              MapNew_remove_all |     4 |       252.810 ns |       4.1454 ns |       3.8776 ns |       252.219 ns |  1.52 |    0.02 |    0.0484 |        - |       - |     304 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     4 |        39.840 ns |       0.2878 ns |       0.2551 ns |        39.787 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     4 |        23.930 ns |       0.2948 ns |       0.2462 ns |        23.912 ns |  0.60 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     4 |        38.606 ns |       0.3398 ns |       0.3179 ns |        38.507 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     4 |        24.573 ns |       0.3089 ns |       0.2889 ns |        24.485 ns |  0.64 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     4 |        37.850 ns |       0.2121 ns |       0.1771 ns |        37.851 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     4 |        24.568 ns |       0.3576 ns |       0.3345 ns |        24.424 ns |  0.65 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     5 |       381.738 ns |       6.2296 ns |       5.8272 ns |       379.059 ns |  1.00 |    0.00 |    0.1083 |        - |       - |     680 B |
|                     MapNew_add |     5 |     1,058.915 ns |      14.9747 ns |      14.0073 ns |     1,053.098 ns |  2.77 |    0.06 |    0.1844 |        - |       - |    1160 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     5 |       462.878 ns |       6.5226 ns |       6.1012 ns |       462.192 ns |  1.00 |    0.00 |    0.1058 |        - |       - |     664 B |
|                  MapNew_remove |     5 |       461.455 ns |       6.2232 ns |       5.8211 ns |       460.593 ns |  1.00 |    0.01 |    0.0929 |        - |       - |     584 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     5 |       245.385 ns |       2.5187 ns |       2.3560 ns |       244.047 ns |  1.00 |    0.00 |    0.0674 |        - |       - |     424 B |
|                 MapNew_ofArray |     5 |       165.299 ns |       2.0609 ns |       1.8269 ns |       164.989 ns |  0.67 |    0.01 |    0.0446 |        - |       - |     280 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     5 |       218.744 ns |       2.7478 ns |       2.5703 ns |       219.792 ns |  1.00 |    0.00 |    0.0637 |        - |       - |     400 B |
|                  MapNew_ofList |     5 |       254.208 ns |       4.2844 ns |       3.7980 ns |       253.377 ns |  1.16 |    0.02 |    0.0597 |        - |       - |     376 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     5 |       220.903 ns |       1.8474 ns |       1.5427 ns |       220.516 ns |  1.00 |    0.00 |    0.0637 |        - |       - |     400 B |
|                   MapNew_ofSeq |     5 |       174.190 ns |       2.0490 ns |       1.9167 ns |       174.264 ns |  0.79 |    0.01 |    0.0446 |        - |       - |     280 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     5 |       118.686 ns |       1.0332 ns |       0.8627 ns |       118.595 ns |  1.00 |    0.00 |    0.0547 |        - |       - |     344 B |
|                 MapNew_toArray |     5 |        55.383 ns |       0.9123 ns |       0.8088 ns |        55.120 ns |  0.47 |    0.01 |    0.0331 |        - |       - |     208 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     5 |        80.941 ns |       0.6512 ns |       0.6092 ns |        80.906 ns |  1.00 |    0.00 |    0.0446 |        - |       - |     280 B |
|                  MapNew_toList |     5 |        62.698 ns |       0.9851 ns |       0.9214 ns |        62.325 ns |  0.77 |    0.01 |    0.0446 |   0.0001 |       - |     280 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     5 |       220.028 ns |       3.7183 ns |       3.4781 ns |       218.769 ns |  1.00 |    0.00 |    0.0764 |        - |       - |     480 B |
|               MapNew_enumerate |     5 |       102.489 ns |       1.2670 ns |       1.1851 ns |       102.958 ns |  0.47 |    0.01 |    0.0319 |        - |       - |     200 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     5 |       364.113 ns |       3.5568 ns |       3.1530 ns |       363.228 ns |  1.00 |    0.00 |    0.1197 |        - |       - |     752 B |
|              MapNew_toSeq_enum |     5 |       359.439 ns |       1.8352 ns |       1.4328 ns |       359.523 ns |  0.99 |    0.01 |    0.0814 |        - |       - |     512 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     5 |        97.595 ns |       1.4243 ns |       1.3323 ns |        97.912 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     5 |        61.418 ns |       0.7120 ns |       0.6312 ns |        61.646 ns |  0.63 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     5 |        36.240 ns |       0.3776 ns |       0.3347 ns |        36.194 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     5 |         9.228 ns |       0.1831 ns |       0.1623 ns |         9.146 ns |  0.25 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     5 |        28.936 ns |       0.1805 ns |       0.1507 ns |        28.912 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     5 |        16.015 ns |       0.2613 ns |       0.2444 ns |        15.944 ns |  0.55 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     5 |        26.405 ns |       0.2851 ns |       0.2381 ns |        26.315 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     5 |         9.494 ns |       0.1566 ns |       0.1388 ns |         9.440 ns |  0.36 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     5 |       201.192 ns |       1.2826 ns |       1.1370 ns |       200.688 ns |  1.00 |    0.00 |    0.0598 |        - |       - |     376 B |
|              MapNew_remove_all |     5 |       260.664 ns |       5.0830 ns |       5.6498 ns |       261.433 ns |  1.29 |    0.03 |    0.0586 |        - |       - |     368 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     5 |        53.512 ns |       0.4150 ns |       0.3466 ns |        53.444 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     5 |        32.128 ns |       0.0977 ns |       0.0763 ns |        32.122 ns |  0.60 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     5 |        37.347 ns |       0.4449 ns |       0.4161 ns |        37.193 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     5 |        25.659 ns |       0.3392 ns |       0.3173 ns |        25.539 ns |  0.69 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     5 |        38.379 ns |       0.7237 ns |       0.6770 ns |        38.044 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     5 |        30.558 ns |       0.4058 ns |       0.3795 ns |        30.447 ns |  0.80 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     6 |       724.624 ns |       6.7591 ns |       6.3225 ns |       723.425 ns |  1.00 |    0.00 |    0.1753 |        - |       - |    1104 B |
|                     MapNew_add |     6 |       710.257 ns |       9.7579 ns |       9.1275 ns |       709.268 ns |  0.98 |    0.01 |    0.1759 |        - |       - |    1104 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     6 |       603.675 ns |       4.5155 ns |       3.7707 ns |       602.641 ns |  1.00 |    0.00 |    0.1337 |        - |       - |     840 B |
|                  MapNew_remove |     6 |       543.511 ns |       6.4022 ns |       5.9886 ns |       545.310 ns |  0.90 |    0.01 |    0.1185 |        - |       - |     744 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     6 |       402.183 ns |       5.2088 ns |       4.8723 ns |       401.106 ns |  1.00 |    0.00 |    0.0944 |        - |       - |     592 B |
|                 MapNew_ofArray |     6 |       242.507 ns |       1.9622 ns |       1.7394 ns |       242.346 ns |  0.60 |    0.01 |    0.0636 |        - |       - |     400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     6 |       332.691 ns |       3.7904 ns |       3.5455 ns |       333.459 ns |  1.00 |    0.00 |    0.0866 |        - |       - |     544 B |
|                  MapNew_ofList |     6 |       296.374 ns |       3.0157 ns |       2.8209 ns |       296.023 ns |  0.89 |    0.01 |    0.0764 |        - |       - |     480 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     6 |       474.995 ns |       5.0178 ns |       4.4482 ns |       473.699 ns |  1.00 |    0.00 |    0.1132 |        - |       - |     712 B |
|                   MapNew_ofSeq |     6 |       301.133 ns |       3.7388 ns |       3.4973 ns |       302.278 ns |  0.63 |    0.01 |    0.0764 |        - |       - |     480 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     6 |       142.908 ns |       1.1769 ns |       1.1009 ns |       142.999 ns |  1.00 |    0.00 |    0.0649 |        - |       - |     408 B |
|                 MapNew_toArray |     6 |        68.110 ns |       0.9899 ns |       0.9259 ns |        67.874 ns |  0.48 |    0.01 |    0.0382 |        - |       - |     240 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     6 |        95.636 ns |       1.9892 ns |       1.9536 ns |        95.249 ns |  1.00 |    0.00 |    0.0535 |   0.0001 |       - |     336 B |
|                  MapNew_toList |     6 |        73.133 ns |       0.4729 ns |       0.3949 ns |        73.069 ns |  0.76 |    0.02 |    0.0535 |   0.0001 |       - |     336 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     6 |       225.119 ns |       2.5770 ns |       2.4105 ns |       225.316 ns |  1.00 |    0.00 |    0.0765 |        - |       - |     480 B |
|               MapNew_enumerate |     6 |       117.402 ns |       1.3825 ns |       1.2932 ns |       117.032 ns |  0.52 |    0.01 |    0.0382 |        - |       - |     240 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     6 |       436.882 ns |       3.7262 ns |       3.4855 ns |       435.490 ns |  1.00 |    0.00 |    0.1427 |        - |       - |     896 B |
|              MapNew_toSeq_enum |     6 |       449.669 ns |       3.3768 ns |       2.8198 ns |       448.971 ns |  1.03 |    0.01 |    0.0914 |        - |       - |     576 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     6 |       134.092 ns |       1.9490 ns |       1.8231 ns |       134.021 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     6 |        75.249 ns |       0.9073 ns |       0.8487 ns |        75.043 ns |  0.56 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     6 |        24.814 ns |       0.3481 ns |       0.3256 ns |        24.763 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     6 |        12.940 ns |       0.1471 ns |       0.1304 ns |        12.937 ns |  0.52 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     6 |        29.312 ns |       0.5079 ns |       0.4503 ns |        29.128 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     6 |        16.292 ns |       0.1889 ns |       0.1767 ns |        16.212 ns |  0.56 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     6 |        26.836 ns |       0.2488 ns |       0.2077 ns |        26.763 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     6 |        12.908 ns |       0.1292 ns |       0.1145 ns |        12.898 ns |  0.48 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     6 |       373.278 ns |       3.9885 ns |       3.7309 ns |       374.626 ns |  1.00 |    0.00 |    0.0892 |        - |       - |     560 B |
|              MapNew_remove_all |     6 |       391.579 ns |       4.8186 ns |       4.0237 ns |       391.171 ns |  1.05 |    0.02 |    0.0840 |        - |       - |     528 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     6 |        53.808 ns |       0.7300 ns |       0.6828 ns |        54.014 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     6 |        33.640 ns |       0.4238 ns |       0.3964 ns |        33.446 ns |  0.63 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     6 |        49.547 ns |       0.1626 ns |       0.1521 ns |        49.553 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     6 |        31.927 ns |       0.3237 ns |       0.3028 ns |        31.841 ns |  0.64 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     6 |        50.315 ns |       0.5683 ns |       0.5316 ns |        50.239 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     6 |        31.146 ns |       0.2671 ns |       0.2230 ns |        31.105 ns |  0.62 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     7 |     1,003.116 ns |      11.0531 ns |      10.3390 ns |     1,004.297 ns |  1.00 |    0.00 |    0.2279 |        - |       - |    1432 B |
|                     MapNew_add |     7 |       861.235 ns |       8.6582 ns |       8.0988 ns |       859.807 ns |  0.86 |    0.01 |    0.2053 |        - |       - |    1288 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     7 |       742.567 ns |       7.5677 ns |       7.0788 ns |       742.511 ns |  1.00 |    0.00 |    0.1679 |        - |       - |    1056 B |
|                  MapNew_remove |     7 |       607.980 ns |       7.5718 ns |       7.0827 ns |       609.156 ns |  0.82 |    0.01 |    0.1515 |        - |       - |     952 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     7 |       433.470 ns |       6.0508 ns |       5.6600 ns |       433.156 ns |  1.00 |    0.00 |    0.1055 |        - |       - |     664 B |
|                 MapNew_ofArray |     7 |       284.733 ns |       3.3675 ns |       3.1500 ns |       284.681 ns |  0.66 |    0.01 |    0.0700 |        - |       - |     440 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     7 |       441.972 ns |       6.0492 ns |       5.6585 ns |       445.380 ns |  1.00 |    0.00 |    0.1057 |        - |       - |     664 B |
|                  MapNew_ofList |     7 |       308.966 ns |       3.4191 ns |       3.0310 ns |       308.963 ns |  0.70 |    0.01 |    0.0814 |        - |       - |     512 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     7 |       496.505 ns |       3.5927 ns |       3.1849 ns |       495.510 ns |  1.00 |    0.00 |    0.1135 |        - |       - |     712 B |
|                   MapNew_ofSeq |     7 |       316.872 ns |       4.2114 ns |       3.9393 ns |       315.207 ns |  0.64 |    0.01 |    0.0815 |        - |       - |     512 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     7 |       154.405 ns |       0.5960 ns |       0.4977 ns |       154.401 ns |  1.00 |    0.00 |    0.0752 |        - |       - |     472 B |
|                 MapNew_toArray |     7 |        74.172 ns |       1.0403 ns |       0.9731 ns |        74.582 ns |  0.48 |    0.01 |    0.0434 |   0.0001 |       - |     272 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     7 |       113.610 ns |       1.1125 ns |       0.9290 ns |       113.630 ns |  1.00 |    0.00 |    0.0624 |   0.0001 |       - |     392 B |
|                  MapNew_toList |     7 |        79.533 ns |       1.0728 ns |       1.0035 ns |        79.132 ns |  0.70 |    0.01 |    0.0624 |   0.0001 |       - |     392 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     7 |       275.158 ns |       3.8233 ns |       3.5763 ns |       273.753 ns |  1.00 |    0.00 |    0.0956 |        - |       - |     600 B |
|               MapNew_enumerate |     7 |       132.552 ns |       1.4122 ns |       1.3210 ns |       131.953 ns |  0.48 |    0.01 |    0.0446 |        - |       - |     280 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     7 |       475.662 ns |       6.8887 ns |       6.4437 ns |       473.094 ns |  1.00 |    0.00 |    0.1463 |        - |       - |     920 B |
|              MapNew_toSeq_enum |     7 |       515.716 ns |       4.9078 ns |       4.5908 ns |       517.605 ns |  1.08 |    0.02 |    0.1016 |        - |       - |     640 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     7 |       163.176 ns |       2.0153 ns |       1.8851 ns |       162.679 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     7 |        89.935 ns |       0.9712 ns |       0.9084 ns |        89.851 ns |  0.55 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     7 |        24.433 ns |       0.2863 ns |       0.2678 ns |        24.353 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     7 |        12.889 ns |       0.1762 ns |       0.1648 ns |        12.795 ns |  0.53 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     7 |        29.385 ns |       0.2276 ns |       0.1901 ns |        29.371 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     7 |         8.461 ns |       0.0914 ns |       0.0763 ns |         8.450 ns |  0.29 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     7 |        27.096 ns |       0.4209 ns |       0.3937 ns |        26.915 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     7 |        13.012 ns |       0.2350 ns |       0.2198 ns |        13.037 ns |  0.48 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     7 |       514.633 ns |       3.5915 ns |       3.1838 ns |       513.573 ns |  1.00 |    0.00 |    0.1184 |        - |       - |     744 B |
|              MapNew_remove_all |     7 |       513.927 ns |       7.5200 ns |       7.0342 ns |       511.677 ns |  1.00 |    0.01 |    0.1056 |        - |       - |     664 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     7 |        65.720 ns |       0.4266 ns |       0.3990 ns |        65.566 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     7 |        34.617 ns |       0.4962 ns |       0.4642 ns |        34.570 ns |  0.53 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     7 |        61.936 ns |       0.2451 ns |       0.2047 ns |        61.907 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     7 |        32.529 ns |       0.1356 ns |       0.1202 ns |        32.526 ns |  0.53 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     7 |        62.924 ns |       0.7514 ns |       0.7029 ns |        62.554 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     7 |        31.639 ns |       0.2714 ns |       0.2406 ns |        31.611 ns |  0.50 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     8 |     1,182.335 ns |       6.3627 ns |       4.9676 ns |     1,182.011 ns |  1.00 |    0.00 |    0.2608 |        - |       - |    1640 B |
|                     MapNew_add |     8 |     1,249.718 ns |      17.4200 ns |      16.2947 ns |     1,244.469 ns |  1.06 |    0.01 |    0.2652 |        - |       - |    1664 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     8 |       912.842 ns |       5.4776 ns |       4.5740 ns |       913.254 ns |  1.00 |    0.00 |    0.2063 |        - |       - |    1296 B |
|                  MapNew_remove |     8 |       958.894 ns |      12.8274 ns |      11.9988 ns |       957.501 ns |  1.05 |    0.01 |    0.1771 |        - |       - |    1112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     8 |       661.631 ns |       6.6212 ns |       6.1935 ns |       660.485 ns |  1.00 |    0.00 |    0.1555 |        - |       - |     976 B |
|                 MapNew_ofArray |     8 |       364.779 ns |       5.1532 ns |       4.8203 ns |       363.531 ns |  0.55 |    0.01 |    0.0803 |        - |       - |     504 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     8 |       607.536 ns |       7.7311 ns |       7.2317 ns |       606.569 ns |  1.00 |    0.00 |    0.1398 |        - |       - |     880 B |
|                  MapNew_ofList |     8 |       434.919 ns |       5.0863 ns |       4.7577 ns |       434.455 ns |  0.72 |    0.01 |    0.0902 |        - |       - |     568 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     8 |       758.961 ns |       8.3311 ns |       7.7929 ns |       757.023 ns |  1.00 |    0.00 |    0.1669 |        - |       - |    1048 B |
|                   MapNew_ofSeq |     8 |       437.869 ns |       4.7248 ns |       4.4196 ns |       438.836 ns |  0.58 |    0.00 |    0.0905 |        - |       - |     568 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     8 |       180.499 ns |       1.4369 ns |       1.3440 ns |       180.249 ns |  1.00 |    0.00 |    0.0853 |        - |       - |     536 B |
|                 MapNew_toArray |     8 |        86.075 ns |       1.1160 ns |       1.0439 ns |        85.956 ns |  0.48 |    0.01 |    0.0484 |   0.0001 |       - |     304 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     8 |       135.301 ns |       0.9210 ns |       0.7691 ns |       134.961 ns |  1.00 |    0.00 |    0.0713 |   0.0001 |       - |     448 B |
|                  MapNew_toList |     8 |        96.702 ns |       1.1425 ns |       1.0128 ns |        96.440 ns |  0.71 |    0.01 |    0.0714 |   0.0001 |       - |     448 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     8 |       331.459 ns |       3.4320 ns |       3.2103 ns |       332.188 ns |  1.00 |    0.00 |    0.1146 |        - |       - |     720 B |
|               MapNew_enumerate |     8 |       166.246 ns |       2.0574 ns |       1.9245 ns |       165.942 ns |  0.50 |    0.01 |    0.0510 |        - |       - |     320 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     8 |       496.245 ns |       3.2306 ns |       2.8639 ns |       495.276 ns |  1.00 |    0.00 |    0.1503 |        - |       - |     944 B |
|              MapNew_toSeq_enum |     8 |       558.973 ns |       8.5204 ns |       7.9700 ns |       560.174 ns |  1.12 |    0.02 |    0.1119 |        - |       - |     704 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     8 |       193.843 ns |       2.3304 ns |       2.1799 ns |       193.151 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     8 |       108.776 ns |       0.8924 ns |       0.7452 ns |       108.603 ns |  0.56 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     8 |        25.643 ns |       0.2763 ns |       0.2307 ns |        25.625 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     8 |        16.669 ns |       0.2170 ns |       0.2030 ns |        16.652 ns |  0.65 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     8 |        37.652 ns |       0.6733 ns |       0.6298 ns |        37.482 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     8 |        16.192 ns |       0.1705 ns |       0.1595 ns |        16.171 ns |  0.43 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     8 |        36.863 ns |       0.5869 ns |       0.5490 ns |        36.608 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     8 |        16.523 ns |       0.1901 ns |       0.1685 ns |        16.486 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     8 |       542.312 ns |       3.7721 ns |       3.3439 ns |       541.074 ns |  1.00 |    0.00 |    0.1414 |        - |       - |     888 B |
|              MapNew_remove_all |     8 |       669.381 ns |      10.7031 ns |      10.0117 ns |       670.768 ns |  1.23 |    0.02 |    0.1193 |        - |       - |     752 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     8 |        79.476 ns |       0.3218 ns |       0.2513 ns |        79.464 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     8 |        40.960 ns |       0.4194 ns |       0.3923 ns |        40.809 ns |  0.52 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     8 |        74.809 ns |       0.6545 ns |       0.5802 ns |        74.653 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     8 |        38.553 ns |       0.4362 ns |       0.4081 ns |        38.353 ns |  0.52 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     8 |        62.538 ns |       0.8999 ns |       0.8418 ns |        62.429 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     8 |        37.556 ns |       0.1581 ns |       0.1320 ns |        37.504 ns |  0.60 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |     9 |     1,610.804 ns |      19.7253 ns |      18.4510 ns |     1,604.378 ns |  1.00 |    0.00 |    0.3508 |        - |       - |    2208 B |
|                     MapNew_add |     9 |     1,822.485 ns |      23.8048 ns |      22.2671 ns |     1,814.038 ns |  1.13 |    0.02 |    0.3161 |        - |       - |    1992 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |     9 |     1,178.102 ns |       5.1547 ns |       4.3044 ns |     1,179.570 ns |  1.00 |    0.00 |    0.2332 |        - |       - |    1464 B |
|                  MapNew_remove |     9 |       974.621 ns |      15.9936 ns |      14.9604 ns |       976.552 ns |  0.83 |    0.01 |    0.2020 |        - |       - |    1272 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |     9 |       832.302 ns |       9.1656 ns |       8.1251 ns |       830.008 ns |  1.00 |    0.00 |    0.1896 |        - |       - |    1192 B |
|                 MapNew_ofArray |     9 |       447.027 ns |       4.1024 ns |       3.6367 ns |       445.455 ns |  0.54 |    0.01 |    0.0902 |        - |       - |     568 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |     9 |       713.508 ns |       6.7081 ns |       5.6016 ns |       711.795 ns |  1.00 |    0.00 |    0.1555 |        - |       - |     976 B |
|                  MapNew_ofList |     9 |       490.493 ns |       6.6586 ns |       6.2284 ns |       489.798 ns |  0.69 |    0.01 |    0.0991 |        - |       - |     624 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |     9 |       838.374 ns |       8.3494 ns |       7.8100 ns |       836.965 ns |  1.00 |    0.00 |    0.1861 |        - |       - |    1168 B |
|                   MapNew_ofSeq |     9 |       484.124 ns |       3.2085 ns |       2.6792 ns |       483.520 ns |  0.58 |    0.01 |    0.0993 |        - |       - |     624 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |     9 |       191.922 ns |       2.7574 ns |       2.5793 ns |       191.015 ns |  1.00 |    0.00 |    0.0956 |   0.0002 |       - |     600 B |
|                 MapNew_toArray |     9 |        98.504 ns |       1.2185 ns |       1.1398 ns |        98.241 ns |  0.51 |    0.01 |    0.0536 |   0.0001 |       - |     336 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |     9 |       156.715 ns |       2.3545 ns |       2.2024 ns |       155.692 ns |  1.00 |    0.00 |    0.0803 |   0.0002 |       - |     504 B |
|                  MapNew_toList |     9 |       110.114 ns |       1.2891 ns |       1.2058 ns |       109.829 ns |  0.70 |    0.01 |    0.0802 |   0.0002 |       - |     504 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |     9 |       341.814 ns |       3.8627 ns |       3.4242 ns |       342.097 ns |  1.00 |    0.00 |    0.1146 |        - |       - |     720 B |
|               MapNew_enumerate |     9 |       184.896 ns |       1.9434 ns |       1.8179 ns |       184.912 ns |  0.54 |    0.01 |    0.0573 |        - |       - |     360 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |     9 |       573.114 ns |       8.4991 ns |       7.9501 ns |       570.087 ns |  1.00 |    0.00 |    0.1730 |        - |       - |    1088 B |
|              MapNew_toSeq_enum |     9 |       598.995 ns |       2.8472 ns |       2.3775 ns |       598.117 ns |  1.04 |    0.01 |    0.1221 |        - |       - |     768 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |     9 |       249.666 ns |       3.6816 ns |       3.4438 ns |       247.845 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |     9 |       128.205 ns |       1.4449 ns |       1.3516 ns |       127.880 ns |  0.51 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |     9 |        24.812 ns |       0.3627 ns |       0.3393 ns |        24.768 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     9 |        16.481 ns |       0.0481 ns |       0.0426 ns |        16.479 ns |  0.66 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |     9 |        35.661 ns |       0.3642 ns |       0.3407 ns |        35.561 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |     9 |         8.415 ns |       0.0383 ns |       0.0320 ns |         8.419 ns |  0.24 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |     9 |        29.371 ns |       0.3899 ns |       0.3647 ns |        29.343 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |     9 |        16.580 ns |       0.2210 ns |       0.1960 ns |        16.524 ns |  0.56 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |     9 |       600.926 ns |       7.4990 ns |       7.0146 ns |       599.509 ns |  1.00 |    0.00 |    0.1577 |        - |       - |     992 B |
|              MapNew_remove_all |     9 |       882.005 ns |      12.4434 ns |      11.6396 ns |       874.894 ns |  1.47 |    0.03 |    0.1450 |        - |       - |     912 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |     9 |        65.436 ns |       0.8443 ns |       0.7898 ns |        65.272 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |     9 |        47.635 ns |       0.6470 ns |       0.6052 ns |        47.392 ns |  0.73 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |     9 |        87.652 ns |       1.0596 ns |       0.9912 ns |        87.270 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |     9 |        44.330 ns |       0.4181 ns |       0.3706 ns |        44.213 ns |  0.51 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |     9 |        87.924 ns |       0.8091 ns |       0.7172 ns |        87.638 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |     9 |        43.861 ns |       0.5669 ns |       0.5302 ns |        43.603 ns |  0.50 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    10 |     1,604.721 ns |      20.8396 ns |      19.4933 ns |     1,593.880 ns |  1.00 |    0.00 |    0.3349 |        - |       - |    2104 B |
|                     MapNew_add |    10 |     1,588.930 ns |      22.5172 ns |      21.0626 ns |     1,581.020 ns |  0.99 |    0.02 |    0.3545 |        - |       - |    2224 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    10 |     1,291.548 ns |      20.1422 ns |      18.8410 ns |     1,291.737 ns |  1.00 |    0.00 |    0.2373 |        - |       - |    1496 B |
|                  MapNew_remove |    10 |     1,338.480 ns |      14.9947 ns |      14.0260 ns |     1,342.263 ns |  1.04 |    0.02 |    0.2283 |        - |       - |    1432 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    10 |       892.948 ns |       7.2499 ns |       6.0540 ns |       890.857 ns |  1.00 |    0.00 |    0.1938 |        - |       - |    1216 B |
|                 MapNew_ofArray |    10 |       504.622 ns |       4.2989 ns |       3.8108 ns |       503.594 ns |  0.56 |    0.01 |    0.1004 |        - |       - |     632 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    10 |       923.976 ns |       3.8913 ns |       3.0381 ns |       923.794 ns |  1.00 |    0.00 |    0.1971 |        - |       - |    1240 B |
|                  MapNew_ofList |    10 |       539.772 ns |       4.7231 ns |       4.1869 ns |       539.942 ns |  0.58 |    0.01 |    0.1083 |        - |       - |     680 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    10 |       818.376 ns |       3.3836 ns |       2.8255 ns |       818.496 ns |  1.00 |    0.00 |    0.1743 |        - |       - |    1096 B |
|                   MapNew_ofSeq |    10 |       553.994 ns |       7.7575 ns |       7.2564 ns |       551.793 ns |  0.68 |    0.01 |    0.1079 |        - |       - |     680 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    10 |       218.581 ns |       2.7250 ns |       2.5490 ns |       217.425 ns |  1.00 |    0.00 |    0.1057 |   0.0002 |       - |     664 B |
|                 MapNew_toArray |    10 |       110.336 ns |       1.4270 ns |       1.3348 ns |       109.831 ns |  0.50 |    0.01 |    0.0587 |   0.0001 |       - |     368 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    10 |       169.450 ns |       2.0372 ns |       1.9056 ns |       169.978 ns |  1.00 |    0.00 |    0.0892 |   0.0002 |       - |     560 B |
|                  MapNew_toList |    10 |       124.093 ns |       1.5089 ns |       1.4115 ns |       123.567 ns |  0.73 |    0.01 |    0.0892 |   0.0003 |       - |     560 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    10 |       386.367 ns |       5.9269 ns |       5.5440 ns |       388.951 ns |  1.00 |    0.00 |    0.1335 |        - |       - |     840 B |
|               MapNew_enumerate |    10 |       224.573 ns |       2.1656 ns |       2.0257 ns |       224.642 ns |  0.58 |    0.01 |    0.0637 |        - |       - |     400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    10 |       638.577 ns |       9.6056 ns |       8.9851 ns |       633.672 ns |  1.00 |    0.00 |    0.1963 |        - |       - |    1232 B |
|              MapNew_toSeq_enum |    10 |       678.253 ns |       8.6641 ns |       8.1044 ns |       677.223 ns |  1.06 |    0.02 |    0.1326 |        - |       - |     832 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    10 |       262.360 ns |       2.7567 ns |       2.5786 ns |       262.829 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    10 |       148.927 ns |       1.8939 ns |       1.7715 ns |       149.527 ns |  0.57 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    10 |        25.871 ns |       0.3195 ns |       0.2988 ns |        25.831 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    10 |        16.565 ns |       0.2222 ns |       0.2079 ns |        16.475 ns |  0.64 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    10 |        39.040 ns |       0.6016 ns |       0.5627 ns |        38.702 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    10 |        16.756 ns |       0.2812 ns |       0.2630 ns |        16.620 ns |  0.43 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    10 |        36.919 ns |       0.4377 ns |       0.4094 ns |        36.900 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    10 |        16.496 ns |       0.1049 ns |       0.0819 ns |        16.481 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    10 |       797.745 ns |       5.3335 ns |       4.4537 ns |       797.072 ns |  1.00 |    0.00 |    0.1862 |        - |       - |    1168 B |
|              MapNew_remove_all |    10 |     1,131.470 ns |      16.0750 ns |      15.0366 ns |     1,132.982 ns |  1.42 |    0.02 |    0.1741 |        - |       - |    1096 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    10 |        93.302 ns |       1.3189 ns |       1.2337 ns |        93.142 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                  MapNew_exists |    10 |        53.929 ns |       0.5489 ns |       0.5134 ns |        53.777 ns |  0.58 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    10 |        73.803 ns |       0.8027 ns |       0.6703 ns |        73.736 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |    10 |        50.455 ns |       0.2079 ns |       0.1843 ns |        50.510 ns |  0.68 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    10 |        74.486 ns |       0.5893 ns |       0.5224 ns |        74.458 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |    10 |        49.762 ns |       0.4654 ns |       0.4353 ns |        49.644 ns |  0.67 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    20 |     4,421.771 ns |      36.4372 ns |      32.3007 ns |     4,416.306 ns |  1.00 |    0.00 |    0.8746 |        - |       - |    5504 B |
|                     MapNew_add |    20 |     3,755.751 ns |      42.7362 ns |      39.9755 ns |     3,762.254 ns |  0.85 |    0.01 |    0.8649 |        - |       - |    5432 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    20 |     3,851.363 ns |      74.9730 ns |     112.2161 ns |     3,870.566 ns |  1.00 |    0.00 |    0.7211 |        - |       - |    4536 B |
|                  MapNew_remove |    20 |     2,774.731 ns |      43.9451 ns |      41.1062 ns |     2,779.836 ns |  0.73 |    0.02 |    0.6042 |        - |       - |    3800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    20 |     3,011.107 ns |      40.0894 ns |      37.4996 ns |     3,013.647 ns |  1.00 |    0.00 |    0.5633 |   0.0030 |       - |    3544 B |
|                 MapNew_ofArray |    20 |     1,100.352 ns |      18.5862 ns |      17.3855 ns |     1,093.074 ns |  0.37 |    0.01 |    0.1875 |   0.0011 |       - |    1176 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    20 |     2,984.266 ns |      51.0348 ns |      47.7380 ns |     2,979.149 ns |  1.00 |    0.00 |    0.5676 |   0.0030 |       - |    3568 B |
|                  MapNew_ofList |    20 |     1,214.431 ns |      15.4348 ns |      14.4377 ns |     1,209.903 ns |  0.41 |    0.01 |    0.2268 |   0.0012 |       - |    1424 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    20 |     3,142.821 ns |      50.4156 ns |      47.1588 ns |     3,121.843 ns |  1.00 |    0.00 |    0.5937 |   0.0031 |       - |    3736 B |
|                   MapNew_ofSeq |    20 |     1,229.937 ns |      17.6977 ns |      16.5544 ns |     1,224.719 ns |  0.39 |    0.01 |    0.2269 |   0.0012 |       - |    1424 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    20 |       425.398 ns |       5.7863 ns |       5.4125 ns |       423.428 ns |  1.00 |    0.00 |    0.2075 |   0.0009 |       - |    1304 B |
|                 MapNew_toArray |    20 |       208.293 ns |       2.7162 ns |       2.5408 ns |       208.325 ns |  0.49 |    0.01 |    0.1096 |   0.0004 |       - |     688 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    20 |       325.575 ns |       3.5492 ns |       3.1463 ns |       324.085 ns |  1.00 |    0.00 |    0.1783 |   0.0010 |       - |    1120 B |
|                  MapNew_toList |    20 |       246.424 ns |       2.9710 ns |       2.7791 ns |       245.323 ns |  0.76 |    0.01 |    0.1784 |   0.0010 |       - |    1120 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    20 |       777.279 ns |      10.7119 ns |      10.0199 ns |       773.081 ns |  1.00 |    0.00 |    0.2483 |        - |       - |    1560 B |
|               MapNew_enumerate |    20 |       426.774 ns |       4.9000 ns |       4.5834 ns |       427.435 ns |  0.55 |    0.01 |    0.1273 |        - |       - |     800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    20 |     1,180.498 ns |       6.7527 ns |       5.2720 ns |     1,181.468 ns |  1.00 |    0.00 |    0.3493 |        - |       - |    2192 B |
|              MapNew_toSeq_enum |    20 |     1,204.156 ns |       6.5926 ns |       6.1667 ns |     1,205.143 ns |  1.02 |    0.01 |    0.2337 |        - |       - |    1472 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    20 |       659.633 ns |      10.2679 ns |       9.6046 ns |       657.553 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    20 |       357.689 ns |       4.7296 ns |       4.4241 ns |       356.728 ns |  0.54 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    20 |        35.186 ns |       0.4900 ns |       0.4583 ns |        35.369 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    20 |        20.547 ns |       0.4410 ns |       0.4125 ns |        20.450 ns |  0.58 |    0.02 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    20 |        45.029 ns |       0.1465 ns |       0.1224 ns |        45.039 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    20 |        16.177 ns |       0.2286 ns |       0.2026 ns |        16.086 ns |  0.36 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    20 |        37.116 ns |       0.5217 ns |       0.4880 ns |        36.841 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    20 |        20.135 ns |       0.1529 ns |       0.1277 ns |        20.076 ns |  0.54 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    20 |     2,225.933 ns |      32.6270 ns |      30.5193 ns |     2,223.952 ns |  1.00 |    0.00 |    0.5418 |        - |       - |    3400 B |
|              MapNew_remove_all |    20 |     2,692.097 ns |      31.7112 ns |      29.6627 ns |     2,695.271 ns |  1.21 |    0.02 |    0.4524 |        - |       - |    2840 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    20 |       173.897 ns |       2.0961 ns |       1.9607 ns |       173.204 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                  MapNew_exists |    20 |        96.698 ns |       1.3458 ns |       1.2588 ns |        96.130 ns |  0.56 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    20 |       161.428 ns |       2.2126 ns |       2.0697 ns |       160.301 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                    MapNew_fold |    20 |        91.002 ns |       1.1797 ns |       1.1035 ns |        90.822 ns |  0.56 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    20 |       147.073 ns |       0.7139 ns |       0.6329 ns |       147.052 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |    20 |        88.391 ns |       0.9430 ns |       0.8821 ns |        88.218 ns |  0.60 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    30 |     7,855.881 ns |     108.7945 ns |     101.7665 ns |     7,854.245 ns |  1.00 |    0.00 |    1.4590 |        - |       - |    9192 B |
|                     MapNew_add |    30 |     6,353.539 ns |      84.3965 ns |      78.9445 ns |     6,337.524 ns |  0.81 |    0.02 |    1.3389 |        - |       - |    8400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    30 |     6,690.231 ns |      96.7350 ns |      90.4860 ns |     6,642.947 ns |  1.00 |    0.00 |    1.2344 |        - |       - |    7776 B |
|                  MapNew_remove |    30 |     4,758.884 ns |      62.8952 ns |      58.8322 ns |     4,763.430 ns |  0.71 |    0.01 |    1.0979 |        - |       - |    6888 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    30 |     5,339.286 ns |      78.4956 ns |      73.4249 ns |     5,343.730 ns |  1.00 |    0.00 |    0.9799 |   0.0054 |       - |    6160 B |
|                 MapNew_ofArray |    30 |     1,537.128 ns |      20.8081 ns |      18.4458 ns |     1,526.856 ns |  0.29 |    0.00 |    0.2622 |   0.0015 |       - |    1648 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    30 |     5,709.955 ns |      73.5214 ns |      68.7719 ns |     5,696.778 ns |  1.00 |    0.00 |    1.0160 |   0.0057 |       - |    6400 B |
|                  MapNew_ofList |    30 |     1,722.317 ns |      15.6969 ns |      14.6829 ns |     1,719.145 ns |  0.30 |    0.00 |    0.2893 |   0.0017 |       - |    1816 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    30 |     6,102.986 ns |      88.9006 ns |      83.1577 ns |     6,096.590 ns |  1.00 |    0.00 |    1.0629 |   0.0061 |       - |    6688 B |
|                   MapNew_ofSeq |    30 |     1,748.908 ns |      22.5441 ns |      21.0878 ns |     1,746.226 ns |  0.29 |    0.00 |    0.2895 |   0.0017 |       - |    1816 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    30 |       654.999 ns |       8.4824 ns |       7.9345 ns |       652.941 ns |  1.00 |    0.00 |    0.3098 |   0.0013 |       - |    1944 B |
|                 MapNew_toArray |    30 |       290.582 ns |       3.2372 ns |       3.0281 ns |       289.434 ns |  0.44 |    0.01 |    0.1607 |   0.0012 |       - |    1008 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    30 |       522.840 ns |       7.9362 ns |       7.4235 ns |       520.010 ns |  1.00 |    0.00 |    0.2674 |   0.0021 |       - |    1680 B |
|                  MapNew_toList |    30 |       368.078 ns |       4.7487 ns |       4.4420 ns |       368.154 ns |  0.70 |    0.01 |    0.2675 |   0.0022 |       - |    1680 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    30 |     1,273.114 ns |      24.4094 ns |      29.9769 ns |     1,276.741 ns |  1.00 |    0.00 |    0.3819 |        - |       - |    2400 B |
|               MapNew_enumerate |    30 |       629.219 ns |       7.6666 ns |       7.1714 ns |       627.011 ns |  0.50 |    0.01 |    0.1912 |        - |       - |    1200 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    30 |     1,733.087 ns |      19.6235 ns |      17.3957 ns |     1,726.397 ns |  1.00 |    0.00 |    0.5020 |        - |       - |    3152 B |
|              MapNew_toSeq_enum |    30 |     1,834.480 ns |      22.0120 ns |      20.5900 ns |     1,827.458 ns |  1.06 |    0.01 |    0.3361 |        - |       - |    2112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    30 |     1,196.978 ns |      19.0896 ns |      17.8565 ns |     1,196.669 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    30 |       582.125 ns |       7.9516 ns |       7.4379 ns |       583.776 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    30 |        53.606 ns |       0.7414 ns |       0.6935 ns |        53.472 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    30 |        20.427 ns |       0.2812 ns |       0.2630 ns |        20.382 ns |  0.38 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    30 |        50.650 ns |       0.7639 ns |       0.7145 ns |        50.409 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    30 |        12.527 ns |       0.2098 ns |       0.1962 ns |        12.442 ns |  0.25 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    30 |        48.251 ns |       0.3238 ns |       0.2870 ns |        48.266 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    30 |        20.225 ns |       0.3711 ns |       0.3471 ns |        20.046 ns |  0.42 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    30 |     4,237.062 ns |      35.9068 ns |      31.8305 ns |     4,232.504 ns |  1.00 |    0.00 |    0.8946 |        - |       - |    5616 B |
|              MapNew_remove_all |    30 |     4,304.067 ns |      51.3940 ns |      48.0740 ns |     4,314.780 ns |  1.02 |    0.01 |    0.7924 |        - |       - |    4992 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    30 |       267.515 ns |       3.1231 ns |       2.9214 ns |       266.841 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                  MapNew_exists |    30 |       122.156 ns |       1.5195 ns |       1.4213 ns |       121.510 ns |  0.46 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    30 |       208.925 ns |       3.1983 ns |       2.9917 ns |       207.059 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |    30 |       113.738 ns |       1.1992 ns |       1.1218 ns |       113.145 ns |  0.54 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    30 |       262.089 ns |       3.6558 ns |       3.2407 ns |       261.628 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                MapNew_foldBack |    30 |       109.609 ns |       0.6847 ns |       0.6405 ns |       109.408 ns |  0.42 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    40 |    10,294.645 ns |     138.8361 ns |     129.8674 ns |    10,252.576 ns |  1.00 |    0.00 |    1.9385 |        - |       - |   12184 B |
|                     MapNew_add |    40 |     9,457.669 ns |      56.8536 ns |      47.4753 ns |     9,440.421 ns |  0.92 |    0.01 |    2.0473 |        - |       - |   12880 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    40 |     9,985.139 ns |     144.3229 ns |     134.9997 ns |     9,913.221 ns |  1.00 |    0.00 |    1.7863 |        - |       - |   11240 B |
|                  MapNew_remove |    40 |     7,248.373 ns |     102.4928 ns |      95.8719 ns |     7,214.275 ns |  0.73 |    0.02 |    1.5104 |        - |       - |    9496 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    40 |     8,019.331 ns |      90.3914 ns |      84.5521 ns |     7,997.463 ns |  1.00 |    0.00 |    1.3960 |   0.0160 |       - |    8800 B |
|                 MapNew_ofArray |    40 |     2,524.525 ns |      13.1162 ns |      10.9526 ns |     2,531.517 ns |  0.31 |    0.00 |    0.3606 |   0.0026 |       - |    2264 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    40 |     7,933.952 ns |      47.0271 ns |      39.2697 ns |     7,916.313 ns |  1.00 |    0.00 |    1.3845 |   0.0158 |       - |    8704 B |
|                  MapNew_ofList |    40 |     2,747.626 ns |      29.8316 ns |      27.9045 ns |     2,742.849 ns |  0.35 |    0.00 |    0.4587 |   0.0027 |       - |    2888 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    40 |     8,133.915 ns |     110.5354 ns |     103.3949 ns |     8,156.907 ns |  1.00 |    0.00 |    1.3797 |   0.0159 |       - |    8656 B |
|                   MapNew_ofSeq |    40 |     2,696.886 ns |      16.1806 ns |      12.6327 ns |     2,697.683 ns |  0.33 |    0.01 |    0.4587 |   0.0055 |       - |    2888 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    40 |       870.310 ns |      12.6105 ns |      11.7958 ns |       866.942 ns |  1.00 |    0.00 |    0.4116 |   0.0035 |       - |    2584 B |
|                 MapNew_toArray |    40 |       412.991 ns |       4.2332 ns |       3.7526 ns |       412.341 ns |  0.47 |    0.01 |    0.2115 |   0.0017 |       - |    1328 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    40 |       661.340 ns |       7.5681 ns |       7.0792 ns |       660.936 ns |  1.00 |    0.00 |    0.3567 |   0.0039 |       - |    2240 B |
|                  MapNew_toList |    40 |       508.321 ns |      10.1089 ns |       9.9283 ns |       507.664 ns |  0.77 |    0.02 |    0.3569 |   0.0040 |       - |    2240 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    40 |     1,622.565 ns |      21.4391 ns |      20.0541 ns |     1,624.155 ns |  1.00 |    0.00 |    0.4959 |        - |       - |    3120 B |
|               MapNew_enumerate |    40 |       826.716 ns |       8.3611 ns |       7.4119 ns |       826.148 ns |  0.51 |    0.01 |    0.2550 |        - |       - |    1600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    40 |     2,294.815 ns |      33.0876 ns |      30.9501 ns |     2,309.184 ns |  1.00 |    0.00 |    0.6539 |        - |       - |    4112 B |
|              MapNew_toSeq_enum |    40 |     2,383.537 ns |      26.2079 ns |      24.5149 ns |     2,386.569 ns |  1.04 |    0.02 |    0.4373 |        - |       - |    2752 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    40 |     1,830.712 ns |      26.6112 ns |      24.8921 ns |     1,830.122 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    40 |       858.361 ns |      12.6951 ns |      11.8750 ns |       860.906 ns |  0.47 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    40 |        53.162 ns |       0.3924 ns |       0.3479 ns |        53.033 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    40 |        24.065 ns |       0.3037 ns |       0.2841 ns |        23.997 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    40 |        63.495 ns |       0.8621 ns |       0.8064 ns |        63.325 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    40 |        26.584 ns |       0.3621 ns |       0.3210 ns |        26.531 ns |  0.42 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    40 |        48.801 ns |       0.5960 ns |       0.5284 ns |        48.592 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    40 |        23.947 ns |       0.3838 ns |       0.3590 ns |        23.802 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    40 |     6,505.682 ns |      92.4872 ns |      86.5126 ns |     6,472.562 ns |  1.00 |    0.00 |    1.3414 |        - |       - |    8424 B |
|              MapNew_remove_all |    40 |     6,199.681 ns |      73.3056 ns |      68.5701 ns |     6,170.542 ns |  0.95 |    0.02 |    1.1602 |        - |       - |    7312 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    40 |       345.228 ns |       3.9141 ns |       3.6613 ns |       343.886 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                  MapNew_exists |    40 |       188.318 ns |       1.1387 ns |       1.0095 ns |       188.190 ns |  0.55 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    40 |       338.589 ns |       4.2722 ns |       3.9962 ns |       339.775 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |    40 |       169.102 ns |       1.7105 ns |       1.4284 ns |       168.828 ns |  0.50 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    40 |       336.039 ns |       4.4759 ns |       4.1867 ns |       334.414 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |    40 |       164.109 ns |       0.8300 ns |       0.7357 ns |       163.797 ns |  0.49 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    50 |    14,178.639 ns |     138.5410 ns |     129.5913 ns |    14,194.407 ns |  1.00 |    0.00 |    2.6563 |        - |       - |   16736 B |
|                     MapNew_add |    50 |    11,366.490 ns |     120.6422 ns |     106.9461 ns |    11,346.016 ns |  0.80 |    0.01 |    2.5900 |        - |       - |   16256 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    50 |    12,934.287 ns |     196.7572 ns |     184.0468 ns |    12,881.439 ns |  1.00 |    0.00 |    2.3214 |        - |       - |   14584 B |
|                  MapNew_remove |    50 |     9,325.533 ns |      73.9063 ns |      61.7151 ns |     9,338.775 ns |  0.72 |    0.01 |    1.9826 |        - |       - |   12488 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    50 |    10,940.882 ns |     134.6140 ns |     125.9180 ns |    10,977.603 ns |  1.00 |    0.00 |    1.8506 |   0.0215 |       - |   11632 B |
|                 MapNew_ofArray |    50 |     3,118.839 ns |      45.2629 ns |      42.3390 ns |     3,117.154 ns |  0.29 |    0.01 |    0.4510 |   0.0063 |       - |    2832 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    50 |    11,262.454 ns |      48.3551 ns |      37.7525 ns |    11,262.535 ns |  1.00 |    0.00 |    1.9402 |   0.0224 |       - |   12184 B |
|                  MapNew_ofList |    50 |     3,393.317 ns |      44.9626 ns |      42.0581 ns |     3,405.401 ns |  0.30 |    0.00 |    0.5373 |   0.0067 |       - |    3376 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    50 |    10,770.125 ns |      54.6813 ns |      48.4736 ns |    10,760.949 ns |  1.00 |    0.00 |    1.8445 |   0.0214 |       - |   11608 B |
|                   MapNew_ofSeq |    50 |     3,355.060 ns |      45.7440 ns |      42.7890 ns |     3,344.786 ns |  0.31 |    0.00 |    0.5351 |   0.0067 |       - |    3376 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    50 |     1,130.873 ns |      14.2970 ns |      13.3734 ns |     1,126.742 ns |  1.00 |    0.00 |    0.5132 |   0.0046 |       - |    3224 B |
|                 MapNew_toArray |    50 |       520.977 ns |       6.4491 ns |       6.0325 ns |       520.068 ns |  0.46 |    0.01 |    0.2623 |   0.0031 |       - |    1648 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    50 |       820.882 ns |       3.1256 ns |       2.6100 ns |       820.215 ns |  1.00 |    0.00 |    0.4463 |   0.0067 |       - |    2800 B |
|                  MapNew_toList |    50 |       635.565 ns |       4.1454 ns |       3.4616 ns |       636.411 ns |  0.77 |    0.00 |    0.4457 |   0.0065 |       - |    2800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    50 |     2,103.463 ns |      13.5425 ns |      11.3086 ns |     2,101.805 ns |  1.00 |    0.00 |    0.6683 |        - |       - |    4200 B |
|               MapNew_enumerate |    50 |     1,067.824 ns |      19.4492 ns |      16.2410 ns |     1,069.519 ns |  0.51 |    0.01 |    0.3184 |        - |       - |    2000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    50 |     2,830.942 ns |      38.2211 ns |      35.7520 ns |     2,816.471 ns |  1.00 |    0.00 |    0.7878 |        - |       - |    4952 B |
|              MapNew_toSeq_enum |    50 |     2,938.557 ns |      37.8665 ns |      35.4203 ns |     2,944.997 ns |  1.04 |    0.02 |    0.5387 |        - |       - |    3392 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    50 |     2,280.105 ns |      12.2524 ns |       9.5659 ns |     2,277.620 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    50 |     1,121.770 ns |      13.3015 ns |      12.4422 ns |     1,119.868 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    50 |        52.642 ns |       0.7156 ns |       0.6693 ns |        52.452 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    50 |        23.813 ns |       0.2268 ns |       0.1894 ns |        23.759 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    50 |        54.112 ns |       0.7161 ns |       0.6698 ns |        53.886 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    50 |        21.072 ns |       0.3741 ns |       0.3500 ns |        20.906 ns |  0.39 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    50 |        57.424 ns |       0.5782 ns |       0.5408 ns |        57.570 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    50 |        23.739 ns |       0.3491 ns |       0.3265 ns |        23.851 ns |  0.41 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    50 |     9,390.093 ns |      81.7949 ns |      76.5110 ns |     9,401.750 ns |  1.00 |    0.00 |    1.7912 |        - |       - |   11288 B |
|              MapNew_remove_all |    50 |     9,496.636 ns |     186.5627 ns |     174.5109 ns |     9,526.417 ns |  1.01 |    0.02 |    1.5845 |        - |       - |    9968 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    50 |       435.363 ns |       5.0290 ns |       4.7041 ns |       435.152 ns |  1.00 |    0.00 |    0.0035 |        - |       - |      24 B |
|                  MapNew_exists |    50 |       228.801 ns |       0.9610 ns |       0.8025 ns |       228.635 ns |  0.53 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    50 |       432.335 ns |       6.8577 ns |       6.4147 ns |       431.200 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                    MapNew_fold |    50 |       215.781 ns |       2.4316 ns |       2.2745 ns |       214.937 ns |  0.50 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    50 |       381.504 ns |       4.7510 ns |       4.4441 ns |       379.937 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                MapNew_foldBack |    50 |       216.523 ns |       2.8828 ns |       2.6966 ns |       215.256 ns |  0.57 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    60 |    18,055.859 ns |     227.7933 ns |     213.0780 ns |    17,967.294 ns |  1.00 |    0.00 |    3.1661 |        - |       - |   19872 B |
|                     MapNew_add |    60 |    14,404.440 ns |     195.7621 ns |     183.1160 ns |    14,331.214 ns |  0.80 |    0.01 |    3.1243 |        - |       - |   19680 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    60 |    16,820.767 ns |      56.9683 ns |      44.4771 ns |    16,809.929 ns |  1.00 |    0.00 |    2.9286 |        - |       - |   18376 B |
|                  MapNew_remove |    60 |    12,280.388 ns |     181.2693 ns |     169.5594 ns |    12,202.126 ns |  0.73 |    0.01 |    2.6230 |        - |       - |   16488 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    60 |    13,473.471 ns |     108.4267 ns |      90.5412 ns |    13,445.986 ns |  1.00 |    0.00 |    2.2642 |   0.0402 |       - |   14248 B |
|                 MapNew_ofArray |    60 |     3,609.579 ns |      59.1409 ns |      55.3204 ns |     3,576.669 ns |  0.27 |    0.00 |    0.5125 |   0.0072 |       - |    3232 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    60 |    14,262.804 ns |      79.5623 ns |      66.4381 ns |    14,283.397 ns |  1.00 |    0.00 |    2.3322 |   0.0427 |       - |   14704 B |
|                  MapNew_ofList |    60 |     4,076.377 ns |      50.6335 ns |      47.3626 ns |     4,085.607 ns |  0.29 |    0.00 |    0.5866 |   0.0081 |       - |    3696 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    60 |    14,423.285 ns |      68.2691 ns |      53.3001 ns |    14,432.267 ns |  1.00 |    0.00 |    2.3425 |   0.0431 |       - |   14776 B |
|                   MapNew_ofSeq |    60 |     4,015.605 ns |      55.5359 ns |      51.9483 ns |     4,022.523 ns |  0.28 |    0.00 |    0.5891 |   0.0079 |       - |    3696 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    60 |     1,317.186 ns |      17.4858 ns |      16.3562 ns |     1,306.943 ns |  1.00 |    0.00 |    0.6153 |   0.0065 |       - |    3864 B |
|                 MapNew_toArray |    60 |       582.891 ns |       6.3747 ns |       5.6510 ns |       584.785 ns |  0.44 |    0.01 |    0.3134 |   0.0041 |       - |    1968 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    60 |     1,020.803 ns |      14.1853 ns |      13.2689 ns |     1,015.058 ns |  1.00 |    0.00 |    0.5354 |   0.0093 |       - |    3360 B |
|                  MapNew_toList |    60 |       733.525 ns |       9.6480 ns |       9.0248 ns |       728.419 ns |  0.72 |    0.01 |    0.5350 |   0.0096 |       - |    3360 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    60 |     2,384.698 ns |      28.2560 ns |      26.4307 ns |     2,370.941 ns |  1.00 |    0.00 |    0.7444 |        - |       - |    4680 B |
|               MapNew_enumerate |    60 |     1,290.129 ns |      14.4061 ns |      13.4755 ns |     1,287.472 ns |  0.54 |    0.01 |    0.3814 |        - |       - |    2400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    60 |     3,518.185 ns |      43.5918 ns |      40.7758 ns |     3,522.730 ns |  1.00 |    0.00 |    0.9802 |        - |       - |    6152 B |
|              MapNew_toSeq_enum |    60 |     3,452.118 ns |      41.6504 ns |      38.9598 ns |     3,439.466 ns |  0.98 |    0.01 |    0.6416 |        - |       - |    4032 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    60 |     3,000.055 ns |      52.4978 ns |      49.1065 ns |     2,999.169 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    60 |     1,381.784 ns |      23.2488 ns |      21.7469 ns |     1,369.714 ns |  0.46 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    60 |        52.588 ns |       0.8182 ns |       0.7654 ns |        52.176 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    60 |        23.706 ns |       0.1316 ns |       0.1099 ns |        23.664 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    60 |        47.232 ns |       0.6795 ns |       0.6356 ns |        46.904 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    60 |        25.342 ns |       0.4165 ns |       0.3896 ns |        25.219 ns |  0.54 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    60 |        79.182 ns |       1.1181 ns |       1.0459 ns |        79.005 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    60 |        23.965 ns |       0.3980 ns |       0.3723 ns |        23.884 ns |  0.30 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    60 |    11,960.820 ns |     141.5809 ns |     132.4349 ns |    11,902.365 ns |  1.00 |    0.00 |    2.3144 |        - |       - |   14592 B |
|              MapNew_remove_all |    60 |    11,510.929 ns |     143.9724 ns |     134.6719 ns |    11,481.715 ns |  0.96 |    0.02 |    2.0370 |        - |       - |   12840 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    60 |       475.944 ns |       3.2541 ns |       2.7173 ns |       474.315 ns |  1.00 |    0.00 |    0.0034 |        - |       - |      24 B |
|                  MapNew_exists |    60 |       250.076 ns |       2.3368 ns |       2.0715 ns |       249.481 ns |  0.53 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    60 |       473.063 ns |       7.0302 ns |       6.5760 ns |       476.870 ns |  1.00 |    0.00 |    0.0034 |        - |       - |      24 B |
|                    MapNew_fold |    60 |       220.393 ns |       1.0246 ns |       0.8000 ns |       220.426 ns |  0.47 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    60 |       457.164 ns |       5.7777 ns |       5.4044 ns |       456.240 ns |  1.00 |    0.00 |    0.0036 |        - |       - |      24 B |
|                MapNew_foldBack |    60 |       213.884 ns |       2.4241 ns |       2.2675 ns |       213.091 ns |  0.47 |    0.01 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    70 |    20,437.643 ns |     283.5761 ns |     265.2573 ns |    20,533.310 ns |  1.00 |    0.00 |    3.5567 |        - |       - |   22336 B |
|                     MapNew_add |    70 |    19,938.170 ns |     246.4048 ns |     230.4872 ns |    19,853.231 ns |  0.98 |    0.01 |    3.9773 |        - |       - |   24952 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    70 |    20,336.123 ns |     286.1163 ns |     267.6334 ns |    20,208.741 ns |  1.00 |    0.00 |    3.4756 |        - |       - |   21912 B |
|                  MapNew_remove |    70 |    15,247.131 ns |     227.6066 ns |     212.9034 ns |    15,245.963 ns |  0.75 |    0.01 |    3.1371 |        - |       - |   19768 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    70 |    17,115.273 ns |     114.3983 ns |      95.5278 ns |    17,101.884 ns |  1.00 |    0.00 |    2.8542 |   0.0516 |       - |   17968 B |
|                 MapNew_ofArray |    70 |     5,371.369 ns |      68.4023 ns |      63.9836 ns |     5,371.416 ns |  0.31 |    0.00 |    0.6055 |   0.0105 |       - |    3800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    70 |    16,757.719 ns |     155.4711 ns |     137.8211 ns |    16,728.815 ns |  1.00 |    0.00 |    2.7622 |   0.0499 |       - |   17344 B |
|                  MapNew_ofList |    70 |     5,499.500 ns |      33.5821 ns |      28.0426 ns |     5,492.031 ns |  0.33 |    0.00 |    0.8331 |   0.0169 |       - |    5232 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    70 |    17,759.259 ns |     124.9417 ns |     104.3320 ns |    17,723.354 ns |  1.00 |    0.00 |    2.8522 |   0.0531 |       - |   17896 B |
|                   MapNew_ofSeq |    70 |     5,744.541 ns |      66.2257 ns |      61.9475 ns |     5,761.395 ns |  0.32 |    0.00 |    0.8325 |   0.0169 |       - |    5232 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    70 |     1,520.788 ns |      17.5521 ns |      16.4183 ns |     1,516.783 ns |  1.00 |    0.00 |    0.7177 |   0.0106 |       - |    4504 B |
|                 MapNew_toArray |    70 |       678.259 ns |       3.6194 ns |       3.2085 ns |       679.697 ns |  0.45 |    0.00 |    0.3646 |   0.0054 |       - |    2288 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    70 |     1,157.729 ns |      15.0879 ns |      14.1133 ns |     1,154.429 ns |  1.00 |    0.00 |    0.6246 |   0.0127 |       - |    3920 B |
|                  MapNew_toList |    70 |       878.861 ns |       9.2667 ns |       8.6680 ns |       876.105 ns |  0.76 |    0.01 |    0.6241 |   0.0131 |       - |    3920 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    70 |     2,746.439 ns |      22.9367 ns |      19.1532 ns |     2,741.848 ns |  1.00 |    0.00 |    0.8408 |        - |       - |    5280 B |
|               MapNew_enumerate |    70 |     1,425.193 ns |      17.6749 ns |      16.5331 ns |     1,432.790 ns |  0.52 |    0.01 |    0.4455 |        - |       - |    2800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    70 |     4,041.882 ns |      52.4903 ns |      49.0994 ns |     4,021.330 ns |  1.00 |    0.00 |    1.1515 |        - |       - |    7232 B |
|              MapNew_toSeq_enum |    70 |     4,078.640 ns |      46.1792 ns |      43.1961 ns |     4,067.631 ns |  1.01 |    0.02 |    0.7442 |        - |       - |    4672 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    70 |     3,728.236 ns |      53.8310 ns |      50.3536 ns |     3,754.517 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    70 |     1,703.630 ns |      26.9595 ns |      25.2179 ns |     1,701.167 ns |  0.46 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    70 |        60.964 ns |       0.3692 ns |       0.2883 ns |        60.912 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    70 |        28.646 ns |       0.6054 ns |       0.9777 ns |        28.467 ns |  0.46 |    0.02 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    70 |        65.238 ns |       1.4003 ns |       1.4984 ns |        64.919 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    70 |        16.655 ns |       0.4032 ns |       0.3960 ns |        16.681 ns |  0.26 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    70 |        69.524 ns |       0.4358 ns |       0.3639 ns |        69.467 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    70 |        27.953 ns |       0.6162 ns |       0.5764 ns |        28.092 ns |  0.40 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    70 |    14,770.145 ns |     167.2239 ns |     156.4213 ns |    14,759.730 ns |  1.00 |    0.00 |    2.8078 |        - |       - |   17688 B |
|              MapNew_remove_all |    70 |    13,009.851 ns |     139.0450 ns |     130.0628 ns |    13,000.989 ns |  0.88 |    0.01 |    2.4733 |        - |       - |   15592 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    70 |       574.439 ns |       7.3363 ns |       6.8624 ns |       571.482 ns |  1.00 |    0.00 |    0.0034 |        - |       - |      24 B |
|                  MapNew_exists |    70 |       294.394 ns |       3.5186 ns |       3.2913 ns |       293.594 ns |  0.51 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    70 |       559.931 ns |       5.4910 ns |       5.1363 ns |       559.000 ns |  1.00 |    0.00 |    0.0033 |        - |       - |      24 B |
|                    MapNew_fold |    70 |       269.947 ns |       3.0410 ns |       2.8445 ns |       268.808 ns |  0.48 |    0.01 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    70 |       564.198 ns |       6.3975 ns |       5.9842 ns |       563.067 ns |  1.00 |    0.00 |    0.0033 |        - |       - |      24 B |
|                MapNew_foldBack |    70 |       272.501 ns |       3.2171 ns |       3.0093 ns |       272.850 ns |  0.48 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    80 |    25,212.770 ns |     298.6708 ns |     279.3768 ns |    25,111.511 ns |  1.00 |    0.00 |    4.5214 |        - |       - |   28400 B |
|                     MapNew_add |    80 |    20,747.011 ns |     285.5908 ns |     267.1418 ns |    20,653.049 ns |  0.82 |    0.01 |    4.6342 |        - |       - |   29168 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    80 |    25,006.706 ns |     285.3255 ns |     266.8936 ns |    24,878.333 ns |  1.00 |    0.00 |    4.2545 |        - |       - |   26776 B |
|                  MapNew_remove |    80 |    15,736.856 ns |     198.0947 ns |     185.2979 ns |    15,826.644 ns |  0.63 |    0.01 |    3.6339 |        - |       - |   22808 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    80 |    21,042.539 ns |     272.7588 ns |     255.1388 ns |    20,963.410 ns |  1.00 |    0.00 |    3.3688 |   0.0837 |       - |   21232 B |
|                 MapNew_ofArray |    80 |     5,654.989 ns |      53.2490 ns |      49.8091 ns |     5,648.930 ns |  0.27 |    0.00 |    0.7075 |   0.0112 |       - |    4440 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    80 |    20,684.479 ns |     315.0143 ns |     294.6646 ns |    20,549.434 ns |  1.00 |    0.00 |    3.4102 |   0.0827 |       - |   21400 B |
|                  MapNew_ofList |    80 |     6,122.622 ns |      81.2287 ns |      75.9814 ns |     6,113.512 ns |  0.30 |    0.01 |    0.9176 |   0.0184 |       - |    5792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    80 |    21,862.459 ns |     225.3527 ns |     210.7951 ns |    21,829.476 ns |  1.00 |    0.00 |    3.4481 |   0.0867 |       - |   21736 B |
|                   MapNew_ofSeq |    80 |     6,135.241 ns |      53.4670 ns |      47.3971 ns |     6,132.163 ns |  0.28 |    0.00 |    0.9215 |   0.0184 |       - |    5792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    80 |     1,794.351 ns |      15.6187 ns |      14.6097 ns |     1,793.570 ns |  1.00 |    0.00 |    0.8200 |   0.0125 |       - |    5144 B |
|                 MapNew_toArray |    80 |       830.071 ns |      12.6324 ns |      11.8163 ns |       827.910 ns |  0.46 |    0.01 |    0.4154 |   0.0074 |       - |    2608 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    80 |     1,350.795 ns |       5.6662 ns |       5.0229 ns |     1,348.486 ns |  1.00 |    0.00 |    0.7134 |   0.0165 |       - |    4480 B |
|                  MapNew_toList |    80 |     1,028.562 ns |      12.3505 ns |      11.5526 ns |     1,032.610 ns |  0.76 |    0.01 |    0.7132 |   0.0164 |       - |    4480 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    80 |     3,253.983 ns |      21.1908 ns |      18.7851 ns |     3,255.134 ns |  1.00 |    0.00 |    0.9917 |        - |       - |    6240 B |
|               MapNew_enumerate |    80 |     1,640.655 ns |      14.3789 ns |      13.4500 ns |     1,639.181 ns |  0.50 |    0.00 |    0.5098 |        - |       - |    3200 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    80 |     4,599.918 ns |      32.8774 ns |      29.1450 ns |     4,595.422 ns |  1.00 |    0.00 |    1.2867 |        - |       - |    8072 B |
|              MapNew_toSeq_enum |    80 |     4,654.598 ns |      56.5056 ns |      52.8553 ns |     4,654.224 ns |  1.01 |    0.01 |    0.8441 |        - |       - |    5312 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    80 |     4,626.565 ns |      64.1682 ns |      60.0230 ns |     4,638.339 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    80 |     2,036.375 ns |      30.2562 ns |      28.3016 ns |     2,030.568 ns |  0.44 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    80 |        61.548 ns |       0.9012 ns |       0.8429 ns |        61.161 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    80 |        27.528 ns |       0.2717 ns |       0.2408 ns |        27.503 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    80 |        63.459 ns |       0.9840 ns |       0.9204 ns |        63.123 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    80 |        16.228 ns |       0.2452 ns |       0.2294 ns |        16.164 ns |  0.26 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    80 |        58.705 ns |       0.3435 ns |       0.3045 ns |        58.569 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    80 |        27.735 ns |       0.4295 ns |       0.4017 ns |        27.654 ns |  0.47 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    80 |    18,005.097 ns |     191.4311 ns |     179.0648 ns |    17,911.560 ns |  1.00 |    0.00 |    3.4177 |        - |       - |   21520 B |
|              MapNew_remove_all |    80 |    15,085.971 ns |     179.2406 ns |     167.6618 ns |    14,998.601 ns |  0.84 |    0.01 |    2.8878 |        - |       - |   18128 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    80 |       711.889 ns |       9.5146 ns |       8.8999 ns |       710.239 ns |  1.00 |    0.00 |    0.0036 |        - |       - |      24 B |
|                  MapNew_exists |    80 |       363.282 ns |       3.1027 ns |       2.9023 ns |       362.587 ns |  0.51 |    0.01 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    80 |       645.034 ns |       8.1879 ns |       7.6590 ns |       646.318 ns |  1.00 |    0.00 |    0.0032 |        - |       - |      24 B |
|                    MapNew_fold |    80 |       338.198 ns |       1.3814 ns |       1.1535 ns |       337.671 ns |  0.52 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    80 |       649.023 ns |       9.2678 ns |       8.6691 ns |       644.714 ns |  1.00 |    0.00 |    0.0032 |        - |       - |      24 B |
|                MapNew_foldBack |    80 |       326.059 ns |       5.1309 ns |       4.7994 ns |       324.533 ns |  0.50 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |    90 |    31,489.808 ns |     427.8299 ns |     400.1924 ns |    31,725.418 ns |  1.00 |    0.00 |    5.3429 |        - |       - |   33600 B |
|                     MapNew_add |    90 |    24,184.447 ns |     324.4791 ns |     303.5180 ns |    24,338.891 ns |  0.77 |    0.01 |    5.3333 |        - |       - |   33456 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |    90 |    27,216.932 ns |     126.3911 ns |     105.5423 ns |    27,210.348 ns |  1.00 |    0.00 |    4.6497 |        - |       - |   29176 B |
|                  MapNew_remove |    90 |    19,585.726 ns |     120.3164 ns |     100.4696 ns |    19,564.512 ns |  0.72 |    0.00 |    4.1028 |        - |       - |   25848 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |    90 |    24,263.833 ns |     230.6723 ns |     215.7710 ns |    24,234.189 ns |  1.00 |    0.00 |    3.9092 |   0.0959 |       - |   24616 B |
|                 MapNew_ofArray |    90 |     6,569.700 ns |      77.5413 ns |      68.7383 ns |     6,540.953 ns |  0.27 |    0.00 |    0.8056 |   0.0198 |       - |    5080 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |    90 |    24,879.624 ns |     309.8259 ns |     289.8113 ns |    25,072.312 ns |  1.00 |    0.00 |    3.8849 |   0.0977 |       - |   24496 B |
|                  MapNew_ofList |    90 |     7,141.459 ns |      96.0079 ns |      89.8058 ns |     7,112.477 ns |  0.29 |    0.00 |    1.0090 |   0.0216 |       - |    6352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |    90 |    24,108.921 ns |     150.5303 ns |     133.4412 ns |    24,078.553 ns |  1.00 |    0.00 |    3.9648 |   0.0961 |       - |   25000 B |
|                   MapNew_ofSeq |    90 |     7,049.201 ns |      91.8860 ns |      85.9502 ns |     7,056.713 ns |  0.29 |    0.00 |    1.0056 |   0.0211 |       - |    6352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |    90 |     1,956.206 ns |      16.4060 ns |      14.5435 ns |     1,950.702 ns |  1.00 |    0.00 |    0.9213 |   0.0177 |       - |    5784 B |
|                 MapNew_toArray |    90 |       943.111 ns |      13.6875 ns |      12.8033 ns |       944.332 ns |  0.48 |    0.01 |    0.4661 |   0.0095 |       - |    2928 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |    90 |     1,530.280 ns |      14.1542 ns |      12.5474 ns |     1,529.450 ns |  1.00 |    0.00 |    0.8034 |   0.0214 |       - |    5040 B |
|                  MapNew_toList |    90 |     1,286.753 ns |      16.6813 ns |      15.6037 ns |     1,281.322 ns |  0.84 |    0.02 |    0.8027 |   0.0218 |       - |    5040 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |    90 |     3,615.305 ns |      43.2605 ns |      40.4659 ns |     3,609.972 ns |  1.00 |    0.00 |    1.0879 |        - |       - |    6840 B |
|               MapNew_enumerate |    90 |     1,917.910 ns |      24.9809 ns |      23.3672 ns |     1,917.216 ns |  0.53 |    0.01 |    0.5729 |        - |       - |    3600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |    90 |     5,095.089 ns |      40.8036 ns |      38.1677 ns |     5,081.552 ns |  1.00 |    0.00 |    1.4202 |        - |       - |    8912 B |
|              MapNew_toSeq_enum |    90 |     5,180.264 ns |      29.9078 ns |      23.3500 ns |     5,186.285 ns |  1.02 |    0.01 |    0.9480 |        - |       - |    5952 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |    90 |     5,342.143 ns |      71.7746 ns |      67.1380 ns |     5,313.877 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |    90 |     2,330.665 ns |       7.2496 ns |       5.6600 ns |     2,330.770 ns |  0.44 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |    90 |        53.598 ns |       0.7423 ns |       0.6943 ns |        53.503 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    90 |        27.563 ns |       0.3065 ns |       0.2867 ns |        27.432 ns |  0.51 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |    90 |        61.816 ns |       0.6254 ns |       0.5850 ns |        61.985 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |    90 |        21.053 ns |       0.3231 ns |       0.3022 ns |        21.044 ns |  0.34 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |    90 |        56.950 ns |       0.9070 ns |       0.8484 ns |        56.506 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |    90 |        27.709 ns |       0.3956 ns |       0.3701 ns |        27.612 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |    90 |    20,377.454 ns |     216.2371 ns |     202.2683 ns |    20,318.558 ns |  1.00 |    0.00 |    3.8105 |        - |       - |   24008 B |
|              MapNew_remove_all |    90 |    18,841.739 ns |     256.7980 ns |     240.2090 ns |    18,773.710 ns |  0.92 |    0.01 |    3.3819 |        - |       - |   21312 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |    90 |       714.708 ns |       9.8303 ns |       9.1952 ns |       711.376 ns |  1.00 |    0.00 |    0.0036 |        - |       - |      24 B |
|                  MapNew_exists |    90 |       424.962 ns |       4.7338 ns |       4.1964 ns |       424.054 ns |  0.59 |    0.01 |    0.0034 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |    90 |       703.520 ns |       9.9211 ns |       9.2802 ns |       703.623 ns |  1.00 |    0.00 |    0.0035 |        - |       - |      24 B |
|                    MapNew_fold |    90 |       395.978 ns |       4.6235 ns |       4.3248 ns |       395.576 ns |  0.56 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |    90 |       710.895 ns |       4.7585 ns |       3.9736 ns |       711.160 ns |  1.00 |    0.00 |    0.0036 |        - |       - |      24 B |
|                MapNew_foldBack |    90 |       398.511 ns |       4.4519 ns |       4.1643 ns |       397.736 ns |  0.56 |    0.01 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   100 |    34,995.882 ns |     412.7919 ns |     386.1258 ns |    35,103.321 ns |  1.00 |    0.00 |    5.9211 |        - |       - |   37240 B |
|                     MapNew_add |   100 |    26,047.793 ns |     301.5056 ns |     282.0285 ns |    25,882.897 ns |  0.74 |    0.01 |    5.9386 |        - |       - |   37384 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   100 |    32,315.160 ns |     113.0658 ns |     105.7618 ns |    32,345.171 ns |  1.00 |    0.00 |    5.4180 |        - |       - |   34120 B |
|                  MapNew_remove |   100 |    20,931.857 ns |     267.2518 ns |     249.9875 ns |    20,967.424 ns |  0.65 |    0.01 |    4.7115 |        - |       - |   29608 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   100 |    28,561.141 ns |     329.9785 ns |     308.6621 ns |    28,458.597 ns |  1.00 |    0.00 |    4.5245 |   0.1405 |       - |   28408 B |
|                 MapNew_ofArray |   100 |     7,623.359 ns |     145.3197 ns |     135.9321 ns |     7,578.434 ns |  0.27 |    0.01 |    0.8921 |   0.0225 |       - |    5600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   100 |    28,003.189 ns |     358.0881 ns |     334.9558 ns |    27,925.372 ns |  1.00 |    0.00 |    4.4703 |   0.1397 |       - |   28096 B |
|                  MapNew_ofList |   100 |     7,917.078 ns |     107.2229 ns |     100.2964 ns |     7,945.671 ns |  0.28 |    0.00 |    1.0800 |   0.0233 |       - |    6792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   100 |    27,637.532 ns |     121.1530 ns |     101.1682 ns |    27,683.386 ns |  1.00 |    0.00 |    4.3701 |   0.1374 |       - |   27472 B |
|                   MapNew_ofSeq |   100 |     7,851.952 ns |     116.1575 ns |     108.6538 ns |     7,801.728 ns |  0.28 |    0.00 |    1.0764 |   0.0232 |       - |    6792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   100 |     2,265.239 ns |      31.5603 ns |      29.5216 ns |     2,262.564 ns |  1.00 |    0.00 |    1.0221 |   0.0223 |       - |    6424 B |
|                 MapNew_toArray |   100 |     1,040.292 ns |      10.3918 ns |       9.7205 ns |     1,040.556 ns |  0.46 |    0.01 |    0.5174 |   0.0113 |       - |    3248 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   100 |     1,753.667 ns |      20.6147 ns |      19.2830 ns |     1,756.442 ns |  1.00 |    0.00 |    0.8920 |   0.0265 |       - |    5600 B |
|                  MapNew_toList |   100 |     1,292.696 ns |      13.7860 ns |      12.8955 ns |     1,286.051 ns |  0.74 |    0.01 |    0.8923 |   0.0259 |       - |    5600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   100 |     4,064.294 ns |      49.6002 ns |      46.3961 ns |     4,041.338 ns |  1.00 |    0.00 |    1.2618 |        - |       - |    7920 B |
|               MapNew_enumerate |   100 |     2,099.248 ns |      22.3327 ns |      20.8900 ns |     2,095.565 ns |  0.52 |    0.01 |    0.6370 |        - |       - |    4000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   100 |     5,704.677 ns |      55.3234 ns |      51.7495 ns |     5,685.725 ns |  1.00 |    0.00 |    1.5703 |        - |       - |    9872 B |
|              MapNew_toSeq_enum |   100 |     5,798.098 ns |      79.1485 ns |      74.0356 ns |     5,753.863 ns |  1.02 |    0.01 |    1.0462 |        - |       - |    6592 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   100 |     6,063.463 ns |      63.2452 ns |      59.1596 ns |     6,051.633 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |   100 |     2,763.175 ns |      53.5780 ns |      52.6207 ns |     2,759.434 ns |  0.46 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   100 |        53.463 ns |       0.6173 ns |       0.5775 ns |        53.458 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   100 |        27.725 ns |       0.4117 ns |       0.3851 ns |        27.545 ns |  0.52 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   100 |        64.347 ns |       0.2700 ns |       0.2108 ns |        64.313 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   100 |        29.576 ns |       0.6226 ns |       0.5824 ns |        29.363 ns |  0.46 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   100 |        58.672 ns |       0.4366 ns |       0.3646 ns |        58.682 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   100 |        27.561 ns |       0.4512 ns |       0.4221 ns |        27.442 ns |  0.47 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   100 |    24,221.938 ns |     235.3124 ns |     220.1113 ns |    24,291.030 ns |  1.00 |    0.00 |    4.5290 |   0.0238 |       - |   28440 B |
|              MapNew_remove_all |   100 |    19,752.302 ns |     249.1455 ns |     233.0508 ns |    19,658.302 ns |  0.82 |    0.01 |    3.8827 |   0.0198 |       - |   24376 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   100 |       898.102 ns |      12.3008 ns |      11.5062 ns |       895.928 ns |  1.00 |    0.00 |    0.0036 |        - |       - |      24 B |
|                  MapNew_exists |   100 |       468.345 ns |       4.9972 ns |       4.6744 ns |       468.176 ns |  0.52 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   100 |       793.549 ns |      10.9133 ns |      10.2083 ns |       792.542 ns |  1.00 |    0.00 |    0.0031 |        - |       - |      24 B |
|                    MapNew_fold |   100 |       438.216 ns |       2.0491 ns |       1.5998 ns |       437.954 ns |  0.55 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   100 |       809.669 ns |       9.9188 ns |       9.2781 ns |       812.317 ns |  1.00 |    0.00 |    0.0032 |        - |       - |      24 B |
|                MapNew_foldBack |   100 |       428.712 ns |       5.9574 ns |       5.5726 ns |       430.753 ns |  0.53 |    0.01 |    0.0034 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   200 |    81,315.054 ns |     926.8078 ns |     866.9366 ns |    81,046.544 ns |  1.00 |    0.00 |   13.7274 |        - |       - |   86192 B |
|                     MapNew_add |   200 |    58,115.792 ns |     164.3515 ns |     128.3149 ns |    58,089.009 ns |  0.71 |    0.01 |   13.4013 |        - |       - |   84416 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   200 |    77,894.299 ns |     916.4366 ns |     857.2353 ns |    77,587.963 ns |  1.00 |    0.00 |   12.6543 |        - |       - |   79440 B |
|                  MapNew_remove |   200 |    49,189.954 ns |     167.1034 ns |     130.4633 ns |    49,206.108 ns |  0.63 |    0.01 |   10.9228 |        - |       - |   68648 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   200 |    67,395.745 ns |     336.2221 ns |     262.5001 ns |    67,370.738 ns |  1.00 |    0.00 |   10.4614 |   0.6035 |       - |   65968 B |
|                 MapNew_ofArray |   200 |    18,737.450 ns |     223.3577 ns |     208.9289 ns |    18,654.376 ns |  0.28 |    0.00 |    1.7629 |   0.0928 |       - |   11136 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   200 |    71,357.894 ns |     808.7941 ns |     756.5465 ns |    71,284.142 ns |  1.00 |    0.00 |   10.5956 |   0.6443 |       - |   66736 B |
|                  MapNew_ofList |   200 |    19,088.906 ns |     161.0982 ns |     134.5243 ns |    19,046.403 ns |  0.27 |    0.00 |    2.1604 |   0.1137 |       - |   13600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   200 |    70,384.179 ns |     379.6078 ns |     296.3728 ns |    70,309.862 ns |  1.00 |    0.00 |   10.6502 |   0.6306 |       - |   66904 B |
|                   MapNew_ofSeq |   200 |    18,726.418 ns |     234.2363 ns |     219.1048 ns |    18,682.403 ns |  0.27 |    0.00 |    2.1615 |   0.1108 |       - |   13600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   200 |     4,603.235 ns |      60.9530 ns |      57.0155 ns |     4,575.565 ns |  1.00 |    0.00 |    2.0411 |   0.0829 |       - |   12824 B |
|                 MapNew_toArray |   200 |     2,113.012 ns |      30.2766 ns |      28.3208 ns |     2,114.631 ns |  0.46 |    0.01 |    1.0276 |   0.0447 |       - |    6448 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   200 |     3,548.446 ns |      52.4688 ns |      49.0793 ns |     3,529.248 ns |  1.00 |    0.00 |    1.7820 |   0.1025 |       - |   11200 B |
|                  MapNew_toList |   200 |     2,612.609 ns |      14.0805 ns |      10.9932 ns |     2,616.223 ns |  0.74 |    0.01 |    1.7843 |   0.1017 |       - |   11200 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   200 |     8,027.929 ns |      41.3773 ns |      36.6799 ns |     8,027.066 ns |  1.00 |    0.00 |    2.4485 |        - |       - |   15360 B |
|               MapNew_enumerate |   200 |     4,148.775 ns |      51.1330 ns |      47.8298 ns |     4,124.321 ns |  0.52 |    0.01 |    1.2742 |        - |       - |    8000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   200 |    11,584.416 ns |     109.8827 ns |     102.7843 ns |    11,538.862 ns |  1.00 |    0.00 |    3.2128 |        - |       - |   20192 B |
|              MapNew_toSeq_enum |   200 |    11,571.311 ns |     130.3077 ns |     121.8899 ns |    11,596.685 ns |  1.00 |    0.01 |    2.0639 |        - |       - |   12992 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   200 |    14,962.274 ns |     110.6405 ns |      92.3898 ns |    14,967.970 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |   200 |     7,381.033 ns |     102.3477 ns |      95.7361 ns |     7,346.222 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   200 |        86.856 ns |       0.9566 ns |       0.8948 ns |        86.705 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   200 |        31.094 ns |       0.0882 ns |       0.0736 ns |        31.105 ns |  0.36 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   200 |        75.903 ns |       0.6969 ns |       0.6177 ns |        75.789 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   200 |        34.909 ns |       0.5667 ns |       0.5301 ns |        34.652 ns |  0.46 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   200 |        94.839 ns |       0.9023 ns |       0.7998 ns |        94.662 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   200 |        31.345 ns |       0.3407 ns |       0.3187 ns |        31.268 ns |  0.33 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   200 |    59,879.269 ns |     622.7964 ns |     582.5642 ns |    60,037.813 ns |  1.00 |    0.00 |   10.5151 |   0.0591 |       - |   66048 B |
|              MapNew_remove_all |   200 |    48,144.486 ns |     342.3331 ns |     285.8636 ns |    48,080.974 ns |  0.80 |    0.01 |    9.2173 |   0.0493 |       - |   58040 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   200 |     1,748.115 ns |      14.8886 ns |      13.1984 ns |     1,752.391 ns |  1.00 |    0.00 |    0.0035 |        - |       - |      24 B |
|                  MapNew_exists |   200 |       937.705 ns |      12.6679 ns |      11.8496 ns |       944.258 ns |  0.54 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   200 |     1,598.506 ns |      22.3582 ns |      20.9139 ns |     1,591.230 ns |  1.00 |    0.00 |    0.0032 |        - |       - |      24 B |
|                    MapNew_fold |   200 |       893.235 ns |      13.7836 ns |      12.8932 ns |       894.562 ns |  0.56 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   200 |     1,537.677 ns |      15.8371 ns |      14.0392 ns |     1,535.488 ns |  1.00 |    0.00 |    0.0031 |        - |       - |      24 B |
|                MapNew_foldBack |   200 |       851.139 ns |      10.5218 ns |       9.8421 ns |       844.634 ns |  0.55 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   300 |   131,212.172 ns |   1,367.0842 ns |   1,211.8846 ns |   130,817.809 ns |  1.00 |    0.00 |   21.8358 |        - |       - |  137256 B |
|                     MapNew_add |   300 |   102,921.828 ns |   1,243.2790 ns |   1,162.9639 ns |   102,323.282 ns |  0.78 |    0.01 |   21.7881 |        - |       - |  136872 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   300 |   130,021.101 ns |   1,667.3166 ns |   1,478.0328 ns |   129,707.445 ns |  1.00 |    0.00 |   20.1403 |        - |       - |  126770 B |
|                  MapNew_remove |   300 |    83,777.892 ns |   1,138.3567 ns |   1,064.8196 ns |    84,279.441 ns |  0.64 |    0.01 |   18.0943 |        - |       - |  113880 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   300 |   117,227.340 ns |   1,427.2553 ns |   1,335.0555 ns |   116,706.493 ns |  1.00 |    0.00 |   17.2454 |   1.5046 |       - |  108858 B |
|                 MapNew_ofArray |   300 |    31,505.301 ns |     232.7732 ns |     206.3474 ns |    31,431.896 ns |  0.27 |    0.00 |    2.5728 |   0.1883 |       - |   16216 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   300 |   113,490.212 ns |   1,660.4112 ns |   1,553.1497 ns |   113,160.761 ns |  1.00 |    0.00 |   17.0557 |   1.4587 |       - |  107320 B |
|                  MapNew_ofList |   300 |    32,066.757 ns |     363.3191 ns |     339.8489 ns |    32,173.152 ns |  0.28 |    0.01 |    3.4774 |   0.2820 |       - |   22000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   300 |   116,575.770 ns |   1,603.5983 ns |   1,500.0068 ns |   116,733.419 ns |  1.00 |    0.00 |   17.2373 |   1.5244 |       - |  108448 B |
|                   MapNew_ofSeq |   300 |    33,660.416 ns |     383.9544 ns |     340.3656 ns |    33,572.687 ns |  0.29 |    0.01 |    3.4951 |   0.2741 |       - |   22000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   300 |     7,333.403 ns |      72.3714 ns |      64.1554 ns |     7,322.739 ns |  1.00 |    0.00 |    3.0604 |   0.1753 |       - |   19224 B |
|                 MapNew_toArray |   300 |     3,064.759 ns |      39.3934 ns |      36.8486 ns |     3,070.179 ns |  0.42 |    0.01 |    1.5370 |   0.1023 |       - |    9648 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   300 |     5,418.791 ns |      72.6735 ns |      67.9788 ns |     5,388.997 ns |  1.00 |    0.00 |    2.6744 |   0.2229 |       - |   16800 B |
|                  MapNew_toList |   300 |     3,936.680 ns |      54.0975 ns |      50.6028 ns |     3,915.536 ns |  0.73 |    0.01 |    2.6765 |   0.2247 |       - |   16800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   300 |    11,918.958 ns |     121.9266 ns |     101.8142 ns |    11,903.667 ns |  1.00 |    0.00 |    3.5742 |        - |       - |   22440 B |
|               MapNew_enumerate |   300 |     6,302.400 ns |      57.0416 ns |      50.5659 ns |     6,280.000 ns |  0.53 |    0.01 |    1.9129 |        - |       - |   12000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   300 |    17,418.213 ns |     205.2021 ns |     191.9462 ns |    17,457.011 ns |  1.00 |    0.00 |    4.8304 |        - |       - |   30392 B |
|              MapNew_toSeq_enum |   300 |    17,652.478 ns |     264.4629 ns |     247.3787 ns |    17,651.292 ns |  1.01 |    0.01 |    3.0866 |        - |       - |   19392 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   300 |    25,685.252 ns |     264.4977 ns |     247.4113 ns |    25,651.822 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |   300 |    13,041.051 ns |     198.5717 ns |     185.7441 ns |    13,045.029 ns |  0.51 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   300 |        86.483 ns |       0.7659 ns |       0.7164 ns |        86.234 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   300 |        34.800 ns |       0.2069 ns |       0.1728 ns |        34.801 ns |  0.40 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   300 |        57.982 ns |       0.2897 ns |       0.2419 ns |        58.006 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   300 |        36.243 ns |       0.4555 ns |       0.4261 ns |        36.004 ns |  0.62 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   300 |        78.682 ns |       0.6994 ns |       0.6200 ns |        78.471 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   300 |        35.082 ns |       0.4386 ns |       0.4103 ns |        34.980 ns |  0.45 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   300 |    99,698.220 ns |   1,005.1833 ns |     940.2491 ns |    99,441.260 ns |  1.00 |    0.00 |   16.9291 |   0.1969 |       - |  106744 B |
|              MapNew_remove_all |   300 |    78,265.388 ns |     512.1269 ns |     453.9872 ns |    78,199.454 ns |  0.79 |    0.01 |   15.1373 |   0.1561 |       - |   95064 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   300 |     2,621.743 ns |      30.4416 ns |      28.4751 ns |     2,627.337 ns |  1.00 |    0.00 |    0.0026 |        - |       - |      24 B |
|                  MapNew_exists |   300 |     1,291.898 ns |       3.6860 ns |       3.0780 ns |     1,291.390 ns |  0.49 |    0.01 |    0.0026 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   300 |     2,449.165 ns |      19.0568 ns |      14.8783 ns |     2,452.505 ns |  1.00 |    0.00 |    0.0025 |        - |       - |      24 B |
|                    MapNew_fold |   300 |     1,203.416 ns |       9.2628 ns |       8.6644 ns |     1,201.174 ns |  0.49 |    0.00 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   300 |     2,456.312 ns |      11.0966 ns |      10.3797 ns |     2,457.095 ns |  1.00 |    0.00 |    0.0025 |        - |       - |      24 B |
|                MapNew_foldBack |   300 |     1,175.624 ns |      11.4543 ns |      10.7143 ns |     1,172.679 ns |  0.48 |    0.00 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   400 |   203,527.956 ns |   2,375.1731 ns |   2,221.7384 ns |   202,253.488 ns |  1.00 |    0.00 |   33.0645 |        - |       - |  208552 B |
|                     MapNew_add |   400 |   133,996.755 ns |   1,625.8711 ns |   1,520.8409 ns |   134,120.197 ns |  0.66 |    0.01 |   29.8684 |        - |       - |  187696 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   400 |   180,439.639 ns |     691.9041 ns |     540.1932 ns |   180,460.964 ns |  1.00 |    0.00 |   28.3764 |        - |       - |  178056 B |
|                  MapNew_remove |   400 |   108,161.944 ns |     687.6313 ns |     574.2032 ns |   107,952.491 ns |  0.60 |    0.00 |   24.9141 |        - |       - |  156328 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   400 |   161,525.713 ns |   1,527.2551 ns |   1,353.8718 ns |   161,337.258 ns |  1.00 |    0.00 |   24.0633 |   2.5840 |       - |  151434 B |
|                 MapNew_ofArray |   400 |    42,933.142 ns |     528.4383 ns |     494.3015 ns |    42,966.222 ns |  0.27 |    0.00 |    3.5199 |   0.3863 |       - |   22208 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   400 |   161,349.270 ns |   2,029.7334 ns |   1,898.6139 ns |   161,995.649 ns |  1.00 |    0.00 |   23.5759 |   2.5316 |       - |  148554 B |
|                  MapNew_ofList |   400 |    44,030.867 ns |     505.8900 ns |     473.2098 ns |    44,022.439 ns |  0.27 |    0.00 |    4.2969 |   0.4774 |       - |   27192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   400 |   169,214.816 ns |   2,597.6657 ns |   2,429.8581 ns |   168,532.817 ns |  1.00 |    0.00 |   24.3333 |   2.6667 |       - |  153088 B |
|                   MapNew_ofSeq |   400 |    43,456.480 ns |     228.6045 ns |     190.8951 ns |    43,470.268 ns |  0.26 |    0.00 |    4.3300 |   0.4811 |       - |   27192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   400 |    10,203.625 ns |     115.3483 ns |     107.8969 ns |    10,163.056 ns |  1.00 |    0.00 |    4.0821 |   0.3172 |       - |   25624 B |
|                 MapNew_toArray |   400 |     4,295.770 ns |      41.6289 ns |      38.9397 ns |     4,281.315 ns |  0.42 |    0.01 |    2.0458 |   0.1669 |       - |   12848 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   400 |     7,577.524 ns |      86.3683 ns |      80.7890 ns |     7,550.168 ns |  1.00 |    0.00 |    3.5669 |   0.3830 |       - |   22400 B |
|                  MapNew_toList |   400 |     5,437.344 ns |      45.4980 ns |      42.5589 ns |     5,424.798 ns |  0.72 |    0.01 |    3.5682 |   0.3810 |       - |   22400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   400 |    16,123.836 ns |     191.3629 ns |     179.0010 ns |    16,134.980 ns |  1.00 |    0.00 |    4.7734 |        - |       - |   30000 B |
|               MapNew_enumerate |   400 |     8,229.156 ns |      89.7990 ns |      83.9981 ns |     8,217.652 ns |  0.51 |    0.01 |    2.5452 |        - |       - |   16000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   400 |    23,163.899 ns |     254.7291 ns |     238.2738 ns |    23,127.940 ns |  1.00 |    0.00 |    6.3766 |        - |       - |   40112 B |
|              MapNew_toSeq_enum |   400 |    22,935.919 ns |     132.3033 ns |     117.2835 ns |    22,907.859 ns |  0.99 |    0.01 |    4.0918 |        - |       - |   25792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   400 |    36,177.267 ns |     477.8576 ns |     446.9883 ns |    36,219.415 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |   400 |    18,949.880 ns |     135.1338 ns |     119.7926 ns |    18,920.620 ns |  0.52 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   400 |        71.476 ns |       0.8894 ns |       0.7884 ns |        71.258 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   400 |        34.965 ns |       0.4233 ns |       0.3960 ns |        34.802 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   400 |        77.334 ns |       0.4726 ns |       0.3947 ns |        77.250 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   400 |        33.563 ns |       0.4811 ns |       0.4500 ns |        33.435 ns |  0.43 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   400 |        95.234 ns |       1.2285 ns |       1.1491 ns |        94.961 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   400 |        35.182 ns |       0.4415 ns |       0.4130 ns |        35.246 ns |  0.37 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   400 |   144,839.913 ns |     986.7494 ns |     823.9804 ns |   144,761.955 ns |  1.00 |    0.00 |   23.9104 |   0.3027 |       - |  150632 B |
|              MapNew_remove_all |   400 |   108,514.796 ns |   1,314.6964 ns |   1,229.7678 ns |   107,934.923 ns |  0.75 |    0.01 |   21.3703 |   0.3222 |       - |  134610 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   400 |     3,668.079 ns |      12.7119 ns |      11.2687 ns |     3,670.672 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                  MapNew_exists |   400 |     1,857.683 ns |      14.0176 ns |      11.7053 ns |     1,855.156 ns |  0.51 |    0.00 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   400 |     3,358.456 ns |      45.3254 ns |      42.3974 ns |     3,340.082 ns |  1.00 |    0.00 |    0.0034 |        - |       - |      24 B |
|                    MapNew_fold |   400 |     1,751.482 ns |      26.8374 ns |      25.1037 ns |     1,757.517 ns |  0.52 |    0.01 |    0.0036 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   400 |     3,132.580 ns |      11.4278 ns |       9.5427 ns |     3,130.984 ns |  1.00 |    0.00 |    0.0032 |        - |       - |      24 B |
|                MapNew_foldBack |   400 |     1,713.367 ns |      19.9243 ns |      18.6372 ns |     1,708.298 ns |  0.55 |    0.01 |    0.0034 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   500 |   231,451.783 ns |     858.4897 ns |     670.2523 ns |   231,489.154 ns |  1.00 |    0.00 |   37.4540 |        - |       - |  234968 B |
|                     MapNew_add |   500 |   158,648.678 ns |     977.1958 ns |     762.9302 ns |   158,710.590 ns |  0.69 |    0.00 |   37.5631 |        - |       - |  235784 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   500 |   238,630.929 ns |   2,900.7157 ns |   2,713.3313 ns |   239,248.549 ns |  1.00 |    0.00 |   36.2828 |        - |       - |  228440 B |
|                  MapNew_remove |   500 |   146,491.561 ns |   1,945.1680 ns |   1,819.5114 ns |   145,564.376 ns |  0.61 |    0.01 |   33.4873 |        - |       - |  210728 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   500 |   220,566.261 ns |   1,354.3002 ns |   1,130.9020 ns |   220,348.454 ns |  1.00 |    0.00 |   32.0230 |   4.1961 |       - |  201688 B |
|                 MapNew_ofArray |   500 |    53,193.343 ns |     635.4783 ns |     594.4268 ns |    53,447.030 ns |  0.24 |    0.00 |    4.1596 |   0.4212 |       - |   26208 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   500 |   215,792.554 ns |   2,743.8547 ns |   2,566.6034 ns |   214,672.191 ns |  1.00 |    0.00 |   31.5767 |   4.1376 |       - |  199408 B |
|                  MapNew_ofList |   500 |    55,710.216 ns |     750.6922 ns |     702.1979 ns |    55,847.597 ns |  0.26 |    0.00 |    4.8161 |   0.6020 |       - |   30392 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   500 |   211,891.267 ns |   1,294.0316 ns |   1,080.5750 ns |   211,680.617 ns |  1.00 |    0.00 |   31.4611 |   4.2230 |       - |  198064 B |
|                   MapNew_ofSeq |   500 |    55,722.860 ns |     794.5215 ns |     743.1959 ns |    55,652.475 ns |  0.26 |    0.00 |    4.8288 |   0.6036 |       - |   30393 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   500 |    13,075.321 ns |      71.3821 ns |      55.7305 ns |    13,062.017 ns |  1.00 |    0.00 |    5.0926 |   0.3175 |       - |   32024 B |
|                 MapNew_toArray |   500 |     5,139.977 ns |      66.4655 ns |      62.1719 ns |     5,148.254 ns |  0.39 |    0.00 |    2.5561 |   0.2521 |       - |   16048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   500 |     9,613.954 ns |      60.9512 ns |      54.0317 ns |     9,598.989 ns |  1.00 |    0.00 |    4.4563 |   0.5605 |       - |   28000 B |
|                  MapNew_toList |   500 |     6,695.884 ns |     141.3873 ns |     416.8837 ns |     6,469.575 ns |  0.76 |    0.03 |    4.4593 |   0.5694 |       - |   28000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   500 |    20,398.450 ns |     295.6814 ns |     276.5805 ns |    20,332.928 ns |  1.00 |    0.00 |    6.0065 |        - |       - |   37680 B |
|               MapNew_enumerate |   500 |    11,269.511 ns |     158.1687 ns |     147.9511 ns |    11,287.309 ns |  0.55 |    0.01 |    3.1774 |        - |       - |   20000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   500 |    29,033.705 ns |     372.5998 ns |     348.5301 ns |    28,939.794 ns |  1.00 |    0.00 |    7.9982 |        - |       - |   50312 B |
|              MapNew_toSeq_enum |   500 |    28,235.862 ns |     347.0428 ns |     324.6241 ns |    28,183.758 ns |  0.97 |    0.01 |    5.1275 |        - |       - |   32192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   500 |    47,952.902 ns |     602.7114 ns |     563.7767 ns |    47,839.752 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |   500 |    25,620.198 ns |     310.7789 ns |     290.7027 ns |    25,683.638 ns |  0.53 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   500 |        71.154 ns |       0.1708 ns |       0.1514 ns |        71.141 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   500 |        35.078 ns |       0.4687 ns |       0.4384 ns |        34.913 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   500 |        74.919 ns |       0.7778 ns |       0.7276 ns |        74.911 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   500 |        39.716 ns |       0.1884 ns |       0.1762 ns |        39.769 ns |  0.53 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   500 |        76.752 ns |       0.8191 ns |       0.7662 ns |        76.515 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   500 |        34.723 ns |       0.1046 ns |       0.0873 ns |        34.704 ns |  0.45 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   500 |   193,460.870 ns |   2,787.9666 ns |   2,607.8657 ns |   193,745.095 ns |  1.00 |    0.00 |   31.2500 |   0.5682 |       - |  197096 B |
|              MapNew_remove_all |   500 |   145,599.087 ns |   1,700.0254 ns |   1,590.2048 ns |   145,303.784 ns |  0.75 |    0.01 |   28.0963 |   0.5734 |       - |  177008 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   500 |     4,779.844 ns |      78.8045 ns |      73.7138 ns |     4,748.672 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |   500 |     2,011.499 ns |       9.5574 ns |       7.9808 ns |     2,010.808 ns |  0.42 |    0.01 |    0.0020 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   500 |     4,304.459 ns |      50.3716 ns |      47.1176 ns |     4,296.637 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |   500 |     1,846.320 ns |       8.3274 ns |       6.5015 ns |     1,845.362 ns |  0.43 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   500 |     4,186.579 ns |      21.7753 ns |      19.3032 ns |     4,191.270 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |   500 |     1,771.250 ns |       9.2689 ns |       7.2365 ns |     1,773.125 ns |  0.42 |    0.00 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   600 |   325,404.566 ns |   3,933.7821 ns |   3,679.6622 ns |   324,177.320 ns |  1.00 |    0.00 |   49.6134 |        - |       - |  312096 B |
|                     MapNew_add |   600 |   228,354.556 ns |   1,469.9387 ns |   1,303.0624 ns |   228,170.995 ns |  0.70 |    0.01 |   48.0596 |        - |       - |  302712 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   600 |   287,975.979 ns |   3,911.1655 ns |   3,658.5067 ns |   286,373.360 ns |  1.00 |    0.00 |   44.9661 |        - |       - |  282448 B |
|                  MapNew_remove |   600 |   187,285.699 ns |   1,847.2470 ns |   1,727.9160 ns |   186,605.623 ns |  0.65 |    0.01 |   40.7448 |        - |       - |  256536 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   600 |   263,390.742 ns |   3,791.1867 ns |   3,546.2784 ns |   261,546.291 ns |  1.00 |    0.00 |   38.1224 |   5.7054 |       - |  240136 B |
|                 MapNew_ofArray |   600 |    72,449.298 ns |     676.3334 ns |     632.6427 ns |    72,325.272 ns |  0.28 |    0.00 |    5.0889 |   0.7167 |       - |   32344 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   600 |   268,013.746 ns |   1,450.8399 ns |   1,357.1165 ns |   268,286.271 ns |  1.00 |    0.00 |   38.9957 |   6.4103 |       - |  246040 B |
|                  MapNew_ofList |   600 |    73,101.058 ns |     778.1405 ns |     727.8731 ns |    72,944.333 ns |  0.27 |    0.00 |    7.0012 |   0.8751 |       - |   43944 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   600 |   271,491.020 ns |   2,938.7666 ns |   2,748.9241 ns |   270,473.788 ns |  1.00 |    0.00 |   39.3319 |   6.1961 |       - |  247436 B |
|                   MapNew_ofSeq |   600 |    73,790.926 ns |     656.3426 ns |     548.0758 ns |    73,730.753 ns |  0.27 |    0.00 |    6.9774 |   0.8907 |       - |   43944 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   600 |    16,111.464 ns |     118.2999 ns |     104.8698 ns |    16,076.765 ns |  1.00 |    0.00 |    6.1205 |   0.6962 |       - |   38424 B |
|                 MapNew_toArray |   600 |     6,300.578 ns |      78.1189 ns |      73.0724 ns |     6,290.095 ns |  0.39 |    0.01 |    3.0642 |   0.3783 |       - |   19248 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   600 |    11,752.488 ns |     140.2921 ns |     131.2293 ns |    11,831.187 ns |  1.00 |    0.00 |    5.3473 |   0.8050 |       - |   33600 B |
|                  MapNew_toList |   600 |     8,350.580 ns |     126.7420 ns |     118.5545 ns |     8,361.958 ns |  0.71 |    0.01 |    5.3507 |   0.8014 |       - |   33600 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   600 |    24,196.892 ns |     147.4409 ns |     130.7025 ns |    24,174.612 ns |  1.00 |    0.00 |    7.2514 |        - |       - |   45600 B |
|               MapNew_enumerate |   600 |    12,691.152 ns |     147.6087 ns |     138.0733 ns |    12,646.888 ns |  0.52 |    0.01 |    3.8242 |        - |       - |   24000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   600 |    34,663.460 ns |     355.2181 ns |     332.2712 ns |    34,702.106 ns |  1.00 |    0.00 |    9.4243 |        - |       - |   59312 B |
|              MapNew_toSeq_enum |   600 |    34,880.542 ns |     443.9780 ns |     415.2973 ns |    34,748.753 ns |  1.01 |    0.01 |    6.1504 |        - |       - |   38592 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   600 |    59,102.427 ns |     553.2713 ns |     517.5304 ns |    59,093.797 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |   600 |    32,046.958 ns |     462.1378 ns |     432.2840 ns |    32,010.957 ns |  0.54 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   600 |       101.637 ns |       1.4963 ns |       1.3997 ns |       101.233 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   600 |        39.039 ns |       0.4875 ns |       0.4560 ns |        38.922 ns |  0.38 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   600 |        87.917 ns |       1.0316 ns |       0.9650 ns |        87.950 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   600 |        37.358 ns |       0.5119 ns |       0.4789 ns |        37.310 ns |  0.42 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   600 |        94.742 ns |       1.0489 ns |       0.9812 ns |        94.618 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   600 |        39.094 ns |       0.5309 ns |       0.4966 ns |        38.875 ns |  0.41 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   600 |   238,360.914 ns |   1,207.1284 ns |     942.4465 ns |   238,739.776 ns |  1.00 |    0.00 |   39.1509 |   0.9434 |       - |  245912 B |
|              MapNew_remove_all |   600 |   188,446.950 ns |   2,424.4280 ns |   2,267.8114 ns |   188,605.381 ns |  0.79 |    0.01 |   34.9482 |   0.9246 |       - |  219744 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   600 |     5,797.829 ns |      60.4830 ns |      56.5759 ns |     5,780.043 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |   600 |     2,590.229 ns |      10.3136 ns |       8.6124 ns |     2,590.079 ns |  0.45 |    0.01 |    0.0026 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   600 |     5,446.827 ns |      60.1127 ns |      56.2295 ns |     5,451.857 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |   600 |     2,387.071 ns |       8.6752 ns |       8.1148 ns |     2,387.159 ns |  0.44 |    0.00 |    0.0024 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   600 |     5,286.900 ns |      27.0156 ns |      22.5593 ns |     5,295.386 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |   600 |     2,378.926 ns |      28.8592 ns |      26.9949 ns |     2,386.234 ns |  0.45 |    0.01 |    0.0023 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   700 |   339,865.341 ns |   3,761.2227 ns |   3,518.2501 ns |   338,628.864 ns |  1.00 |    0.00 |   55.4435 |        - |       - |  349672 B |
|                     MapNew_add |   700 |   253,059.362 ns |   3,395.3980 ns |   3,176.0574 ns |   252,659.226 ns |  0.74 |    0.01 |   57.0437 |        - |       - |  358960 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   700 |   356,390.019 ns |   3,691.9992 ns |   3,453.4984 ns |   354,708.333 ns |  1.00 |    0.00 |   53.6723 |        - |       - |  338624 B |
|                  MapNew_remove |   700 |   213,501.532 ns |   2,495.8109 ns |   2,334.5830 ns |   213,060.880 ns |  0.60 |    0.01 |   47.9798 |        - |       - |  301336 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   700 |   333,858.212 ns |   3,093.6812 ns |   2,742.4678 ns |   334,239.638 ns |  1.00 |    0.00 |   47.0395 |   8.5526 |       - |  296128 B |
|                 MapNew_ofArray |   700 |    83,427.387 ns |   1,125.1473 ns |   1,052.4635 ns |    83,088.639 ns |  0.25 |    0.00 |    6.1096 |   0.7431 |       - |   38744 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   700 |   320,240.225 ns |   2,286.8781 ns |   2,139.1472 ns |   319,557.207 ns |  1.00 |    0.00 |   46.2372 |   7.6531 |       - |  291616 B |
|                  MapNew_ofList |   700 |    88,724.866 ns |   1,259.7500 ns |   1,178.3710 ns |    88,197.107 ns |  0.28 |    0.00 |    7.8892 |   1.1396 |       - |   49544 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   700 |   338,176.574 ns |   4,798.5114 ns |   4,488.5305 ns |   338,088.996 ns |  1.00 |    0.00 |   47.2074 |   8.3112 |       - |  297088 B |
|                   MapNew_ofSeq |   700 |    87,906.476 ns |     436.5871 ns |     387.0231 ns |    87,804.180 ns |  0.26 |    0.00 |    7.8566 |   1.2359 |       - |   49544 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   700 |    18,921.159 ns |     212.5112 ns |     198.7832 ns |    18,871.238 ns |  1.00 |    0.00 |    7.1310 |   0.6037 |       - |   44824 B |
|                 MapNew_toArray |   700 |     7,730.600 ns |      55.2152 ns |      51.6484 ns |     7,709.466 ns |  0.41 |    0.01 |    3.5772 |   0.5088 |       - |   22448 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   700 |    13,769.175 ns |     167.7687 ns |     156.9309 ns |    13,702.477 ns |  1.00 |    0.00 |    6.2473 |   1.0344 |       - |   39200 B |
|                  MapNew_toList |   700 |     9,877.418 ns |      60.7929 ns |      53.8913 ns |     9,870.088 ns |  0.72 |    0.01 |    6.2413 |   1.0257 |       - |   39200 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   700 |    27,911.660 ns |     116.7382 ns |      97.4817 ns |    27,920.027 ns |  1.00 |    0.00 |    8.3240 |        - |       - |   52320 B |
|               MapNew_enumerate |   700 |    14,921.743 ns |      94.5261 ns |      78.9336 ns |    14,931.268 ns |  0.53 |    0.00 |    4.4536 |        - |       - |   28000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   700 |    40,557.768 ns |     519.9803 ns |     486.3899 ns |    40,613.679 ns |  1.00 |    0.00 |   11.2283 |        - |       - |   70473 B |
|              MapNew_toSeq_enum |   700 |    40,587.762 ns |     249.4836 ns |     194.7804 ns |    40,532.207 ns |  1.00 |    0.01 |    7.1602 |        - |       - |   44992 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   700 |    70,718.203 ns |     800.0474 ns |     748.3649 ns |    70,406.580 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |   700 |    38,934.677 ns |     459.7156 ns |     430.0183 ns |    38,852.344 ns |  0.55 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   700 |        72.009 ns |       1.0656 ns |       0.9967 ns |        71.742 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   700 |        38.847 ns |       0.5675 ns |       0.5309 ns |        38.700 ns |  0.54 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   700 |        90.736 ns |       1.2164 ns |       1.1379 ns |        90.371 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |   700 |        36.548 ns |       0.1733 ns |       0.1537 ns |        36.572 ns |  0.40 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   700 |        95.669 ns |       1.0924 ns |       1.0219 ns |        96.038 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   700 |        38.612 ns |       0.5247 ns |       0.4908 ns |        38.387 ns |  0.40 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   700 |   290,132.786 ns |   3,485.4698 ns |   3,260.3106 ns |   289,082.784 ns |  1.00 |    0.00 |   46.3068 |   1.4205 |       - |  292052 B |
|              MapNew_remove_all |   700 |   222,214.504 ns |     890.7609 ns |     743.8257 ns |   222,133.371 ns |  0.77 |    0.01 |   41.7411 |   1.1161 |       - |  262672 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   700 |     6,991.105 ns |     107.0034 ns |     100.0910 ns |     7,003.023 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |   700 |     3,275.297 ns |      19.3148 ns |      16.1288 ns |     3,276.796 ns |  0.47 |    0.01 |    0.0033 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   700 |     6,288.560 ns |      26.0001 ns |      23.0484 ns |     6,295.243 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |   700 |     3,077.432 ns |      42.4650 ns |      39.7218 ns |     3,059.603 ns |  0.49 |    0.01 |    0.0030 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   700 |     6,288.427 ns |      49.7952 ns |      46.5785 ns |     6,283.945 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |   700 |     2,980.431 ns |      39.4694 ns |      36.9197 ns |     2,958.912 ns |  0.47 |    0.01 |    0.0030 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   800 |   411,991.524 ns |   1,530.4744 ns |   1,356.7257 ns |   411,679.801 ns |  1.00 |    0.00 |   67.8808 |        - |       - |  426536 B |
|                     MapNew_add |   800 |   289,539.498 ns |   3,655.0691 ns |   3,418.9539 ns |   289,029.481 ns |  0.70 |    0.01 |   65.9247 |        - |       - |  413580 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   800 |   413,375.351 ns |   4,676.4809 ns |   4,374.3831 ns |   413,025.658 ns |  1.00 |    0.00 |   62.0888 |        - |       - |  391489 B |
|                  MapNew_remove |   800 |   245,767.515 ns |   2,725.7920 ns |   2,549.7076 ns |   246,195.662 ns |  0.59 |    0.01 |   55.8824 |        - |       - |  350888 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   800 |   401,346.946 ns |   5,624.8339 ns |   5,261.4731 ns |   399,731.487 ns |  1.00 |    0.00 |   55.7753 |  11.0759 |       - |  351089 B |
|                 MapNew_ofArray |   800 |    98,044.276 ns |   1,132.0923 ns |   1,058.9599 ns |    97,793.127 ns |  0.24 |    0.00 |    7.0214 |   1.1867 |       - |   44352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   800 |   396,603.578 ns |   4,701.0949 ns |   4,397.4071 ns |   394,974.180 ns |  1.00 |    0.00 |   55.0781 |  10.5469 |       - |  347056 B |
|                  MapNew_ofList |   800 |   101,986.068 ns |     539.5729 ns |     478.3173 ns |   101,870.330 ns |  0.26 |    0.00 |    8.6102 |   1.5194 |       - |   54352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   800 |   393,869.878 ns |   4,167.4926 ns |   3,898.2752 ns |   392,188.936 ns |  1.00 |    0.00 |   54.7360 |  10.8696 |       - |  345550 B |
|                   MapNew_ofSeq |   800 |   103,798.427 ns |   1,023.1590 ns |     957.0636 ns |   103,706.511 ns |  0.26 |    0.00 |    8.6603 |   1.4608 |       - |   54352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   800 |    22,508.707 ns |     254.7721 ns |     238.3140 ns |    22,521.943 ns |  1.00 |    0.00 |    8.1531 |   1.0109 |       - |   51224 B |
|                 MapNew_toArray |   800 |     8,863.511 ns |      90.0826 ns |      84.2633 ns |     8,863.105 ns |  0.39 |    0.01 |    4.0839 |   0.6601 |       - |   25648 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   800 |    15,589.482 ns |      69.4353 ns |      61.5526 ns |    15,565.287 ns |  1.00 |    0.00 |    7.1415 |   1.2914 |       - |   44800 B |
|                  MapNew_toList |   800 |    11,412.320 ns |     135.5826 ns |     126.8240 ns |    11,356.108 ns |  0.73 |    0.01 |    7.1339 |   1.2899 |       - |   44800 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   800 |    32,564.477 ns |     413.1717 ns |     386.4810 ns |    32,562.723 ns |  1.00 |    0.00 |    9.5831 |        - |       - |   60240 B |
|               MapNew_enumerate |   800 |    16,315.536 ns |      45.6854 ns |      38.1494 ns |    16,315.763 ns |  0.50 |    0.00 |    5.0990 |        - |       - |   32000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   800 |    45,943.610 ns |     418.7609 ns |     391.7092 ns |    45,836.804 ns |  1.00 |    0.00 |   12.6548 |        - |       - |   79472 B |
|              MapNew_toSeq_enum |   800 |    44,923.683 ns |     157.0040 ns |     139.1799 ns |    44,898.086 ns |  0.98 |    0.01 |    8.1483 |        - |       - |   51392 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   800 |    83,106.817 ns |     969.7273 ns |     907.0835 ns |    83,552.535 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |   800 |    45,590.963 ns |     399.4806 ns |     373.6744 ns |    45,475.389 ns |  0.55 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   800 |        87.323 ns |       1.1099 ns |       1.0382 ns |        87.166 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   800 |        38.361 ns |       0.1279 ns |       0.1068 ns |        38.361 ns |  0.44 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   800 |        88.353 ns |       1.0798 ns |       1.0100 ns |        88.372 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |   800 |        29.043 ns |       0.1662 ns |       0.1388 ns |        29.074 ns |  0.33 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   800 |       110.630 ns |       1.7051 ns |       1.5950 ns |       110.850 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   800 |        38.719 ns |       0.2029 ns |       0.1799 ns |        38.655 ns |  0.35 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   800 |   340,185.025 ns |   3,562.2353 ns |   3,332.1171 ns |   340,464.606 ns |  1.00 |    0.00 |   54.4786 |   2.0053 |       - |  343072 B |
|              MapNew_remove_all |   800 |   255,514.983 ns |   3,253.1560 ns |   3,043.0041 ns |   254,999.573 ns |  0.75 |    0.01 |   48.6948 |   1.5060 |       - |  306248 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   800 |     8,025.375 ns |      61.9640 ns |      54.9295 ns |     8,019.303 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |   800 |     3,797.102 ns |      48.4078 ns |      45.2807 ns |     3,789.169 ns |  0.47 |    0.01 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   800 |     7,412.483 ns |     127.9889 ns |     119.7209 ns |     7,388.653 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |   800 |     3,552.365 ns |      30.9759 ns |      25.8663 ns |     3,548.752 ns |  0.48 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   800 |     7,130.155 ns |      30.6839 ns |      27.2005 ns |     7,125.930 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |   800 |     3,439.825 ns |      17.7007 ns |      15.6912 ns |     3,435.681 ns |  0.48 |    0.00 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |   900 |   445,271.831 ns |   4,683.2180 ns |   4,380.6850 ns |   444,702.685 ns |  1.00 |    0.00 |   71.7430 |        - |       - |  451032 B |
|                     MapNew_add |   900 |   323,557.491 ns |   4,590.9874 ns |   4,294.4125 ns |   321,862.724 ns |  0.73 |    0.01 |   74.0385 |        - |       - |  466272 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |   900 |   469,055.945 ns |   2,921.1828 ns |   2,439.3199 ns |   468,111.403 ns |  1.00 |    0.00 |   71.5649 |        - |       - |  450168 B |
|                  MapNew_remove |   900 |   292,274.701 ns |   3,277.0482 ns |   3,065.3530 ns |   293,200.145 ns |  0.62 |    0.01 |   65.1042 |        - |       - |  410088 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |   900 |   442,970.306 ns |   1,567.6614 ns |   1,389.6910 ns |   442,513.380 ns |  1.00 |    0.00 |   62.0599 |  11.8838 |       - |  391816 B |
|                 MapNew_ofArray |   900 |   109,786.553 ns |   1,378.6476 ns |   1,289.5878 ns |   109,819.193 ns |  0.25 |    0.00 |    7.6773 |   1.5138 |       - |   48352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |   900 |   443,048.145 ns |   2,539.6483 ns |   2,120.7213 ns |   442,947.227 ns |  1.00 |    0.00 |   62.5000 |  12.3239 |       - |  392248 B |
|                  MapNew_ofList |   900 |   116,833.346 ns |   1,192.9070 ns |   1,057.4810 ns |   116,713.796 ns |  0.26 |    0.00 |    9.1435 |   1.7361 |       - |   57552 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |   900 |   464,101.011 ns |   6,073.0333 ns |   5,680.7191 ns |   462,300.906 ns |  1.00 |    0.00 |   62.9529 |  12.2283 |       - |  397072 B |
|                   MapNew_ofSeq |   900 |   114,302.527 ns |     747.5070 ns |     624.2022 ns |   114,146.716 ns |  0.25 |    0.00 |    9.0909 |   1.8182 |       - |   57554 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |   900 |    24,838.596 ns |     109.3519 ns |     102.2878 ns |    24,846.418 ns |  1.00 |    0.00 |    9.1591 |   1.3155 |       - |   57624 B |
|                 MapNew_toArray |   900 |     9,498.758 ns |      78.0169 ns |      69.1600 ns |     9,485.337 ns |  0.38 |    0.00 |    4.5912 |   0.7588 |       - |   28848 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |   900 |    17,923.339 ns |      97.9528 ns |      86.8326 ns |    17,908.097 ns |  1.00 |    0.00 |    8.0245 |   1.5867 |       - |   50400 B |
|                  MapNew_toList |   900 |    12,546.750 ns |     154.7077 ns |     144.7137 ns |    12,494.589 ns |  0.70 |    0.01 |    8.0268 |   1.5780 |       - |   50400 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |   900 |    37,057.262 ns |     478.6362 ns |     447.7166 ns |    36,984.237 ns |  1.00 |    0.00 |   10.9384 |        - |       - |   68640 B |
|               MapNew_enumerate |   900 |    19,235.805 ns |      83.3579 ns |      77.9730 ns |    19,258.668 ns |  0.52 |    0.01 |    5.7288 |        - |       - |   36000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |   900 |    52,119.366 ns |     523.4857 ns |     464.0564 ns |    51,955.491 ns |  1.00 |    0.00 |   14.5108 |        - |       - |   91232 B |
|              MapNew_toSeq_enum |   900 |    51,215.984 ns |     496.6437 ns |     464.5608 ns |    51,099.002 ns |  0.98 |    0.01 |    9.1794 |        - |       - |   57792 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |   900 |    95,576.273 ns |     891.0913 ns |     833.5273 ns |    95,875.009 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |   900 |    53,141.278 ns |     729.3460 ns |     682.2307 ns |    52,877.082 ns |  0.56 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |   900 |        86.901 ns |       0.9389 ns |       0.8783 ns |        86.668 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   900 |        38.576 ns |       0.4748 ns |       0.4441 ns |        38.446 ns |  0.44 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |   900 |        92.540 ns |       0.8817 ns |       0.8248 ns |        92.413 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |   900 |        44.728 ns |       0.6465 ns |       0.6047 ns |        44.764 ns |  0.48 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |   900 |        79.333 ns |       1.1113 ns |       1.0395 ns |        78.954 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |   900 |        38.972 ns |       0.4439 ns |       0.3935 ns |        38.841 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |   900 |   395,907.398 ns |   4,767.8116 ns |   4,459.8140 ns |   395,677.344 ns |  1.00 |    0.00 |   62.1094 |   2.3438 |       - |  391472 B |
|              MapNew_remove_all |   900 |   290,324.779 ns |   1,902.3227 ns |   1,588.5256 ns |   289,697.649 ns |  0.73 |    0.01 |   56.0748 |   2.0444 |       - |  352848 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |   900 |     8,913.647 ns |      93.1793 ns |      87.1600 ns |     8,878.255 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |   900 |     3,860.843 ns |      55.0257 ns |      51.4711 ns |     3,861.371 ns |  0.43 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |   900 |     8,426.377 ns |     131.5969 ns |     123.0958 ns |     8,374.217 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |   900 |     3,541.929 ns |      32.9103 ns |      29.1741 ns |     3,536.310 ns |  0.42 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |   900 |     8,237.329 ns |     111.1739 ns |     103.9922 ns |     8,231.101 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |   900 |     3,507.229 ns |      35.2688 ns |      32.9905 ns |     3,497.391 ns |  0.43 |    0.01 |    0.0035 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  1000 |   521,190.444 ns |   4,933.0677 ns |   4,373.0360 ns |   519,724.897 ns |  1.00 |    0.00 |   81.6116 |        - |       - |  515009 B |
|                     MapNew_add |  1000 |   357,000.817 ns |   3,796.9835 ns |   3,551.7007 ns |   357,890.113 ns |  0.69 |    0.01 |   82.6271 |        - |       - |  519808 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  1000 |   530,709.809 ns |   6,814.7264 ns |   6,374.4993 ns |   530,987.083 ns |  1.00 |    0.00 |   80.2083 |        - |       - |  506216 B |
|                  MapNew_remove |  1000 |   326,712.109 ns |   4,059.2691 ns |   3,797.0429 ns |   324,683.344 ns |  0.62 |    0.01 |   74.7423 |        - |       - |  469288 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  1000 |   509,182.560 ns |   2,264.3956 ns |   2,007.3277 ns |   508,720.910 ns |  1.00 |    0.00 |   70.6301 |  14.7358 |       - |  445872 B |
|                 MapNew_ofArray |  1000 |   123,414.220 ns |   1,591.3780 ns |   1,488.5760 ns |   123,438.280 ns |  0.24 |    0.00 |    8.2846 |   1.5838 |       - |   52352 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  1000 |   506,552.722 ns |   5,339.9155 ns |   4,994.9603 ns |   504,671.925 ns |  1.00 |    0.00 |   71.0685 |  14.1129 |       - |  448144 B |
|                  MapNew_ofList |  1000 |   132,696.799 ns |   1,757.8185 ns |   1,644.2645 ns |   132,851.382 ns |  0.26 |    0.00 |    9.6053 |   1.7105 |       - |   60752 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  1000 |   524,737.817 ns |   3,628.8286 ns |   3,030.2363 ns |   524,273.021 ns |  1.00 |    0.00 |   71.8750 |  16.6667 |       - |  452801 B |
|                   MapNew_ofSeq |  1000 |   128,548.212 ns |   1,632.2453 ns |   1,526.8032 ns |   128,634.764 ns |  0.24 |    0.00 |    9.6637 |   1.7570 |       - |   60752 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  1000 |    27,957.427 ns |      97.4148 ns |      81.3458 ns |    27,958.703 ns |  1.00 |    0.00 |   10.1854 |   1.1934 |       - |   64024 B |
|                 MapNew_toArray |  1000 |    10,466.562 ns |      53.1650 ns |      44.3952 ns |    10,468.656 ns |  0.37 |    0.00 |    5.1014 |   0.8763 |       - |   32048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  1000 |    20,023.827 ns |      94.6941 ns |      83.9438 ns |    20,004.112 ns |  1.00 |    0.00 |    8.9143 |   1.9145 |       - |   56000 B |
|                  MapNew_toList |  1000 |    13,487.816 ns |     153.3073 ns |     143.4037 ns |    13,431.748 ns |  0.67 |    0.01 |    8.9247 |   1.9057 |       - |   56000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  1000 |    41,154.621 ns |     600.7350 ns |     561.9279 ns |    41,003.250 ns |  1.00 |    0.00 |   12.0876 |        - |       - |   75960 B |
|               MapNew_enumerate |  1000 |    22,327.571 ns |     119.2704 ns |      93.1185 ns |    22,309.839 ns |  0.54 |    0.01 |    6.3699 |        - |       - |   40000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  1000 |    57,401.045 ns |     760.9153 ns |     711.7606 ns |    57,720.172 ns |  1.00 |    0.00 |   15.6812 |        - |       - |   98552 B |
|              MapNew_toSeq_enum |  1000 |    56,077.968 ns |     487.2957 ns |     431.9750 ns |    56,127.715 ns |  0.98 |    0.02 |   10.1860 |        - |       - |   64192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  1000 |   109,043.208 ns |   1,088.9186 ns |   1,018.5751 ns |   109,376.603 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |  1000 |    60,862.805 ns |     652.9921 ns |     610.8093 ns |    60,956.839 ns |  0.56 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  1000 |        95.189 ns |       0.7642 ns |       0.6774 ns |        95.094 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  1000 |        38.373 ns |       0.2005 ns |       0.1674 ns |        38.327 ns |  0.40 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  1000 |        88.144 ns |       1.0583 ns |       0.9899 ns |        87.926 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  1000 |        43.648 ns |       0.4838 ns |       0.4526 ns |        43.445 ns |  0.50 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  1000 |       104.509 ns |       1.3680 ns |       1.2796 ns |       104.115 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  1000 |        38.912 ns |       0.3942 ns |       0.3495 ns |        38.855 ns |  0.37 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  1000 |   449,749.374 ns |   2,963.4903 ns |   2,627.0570 ns |   448,838.939 ns |  1.00 |    0.00 |   71.4928 |   2.6978 |       - |  448560 B |
|              MapNew_remove_all |  1000 |   324,619.940 ns |   2,036.7342 ns |   1,805.5118 ns |   324,344.631 ns |  0.72 |    0.00 |   63.7821 |   2.5641 |       - |  402064 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  1000 |    10,118.540 ns |     113.4780 ns |     100.5953 ns |    10,073.487 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  1000 |     4,350.048 ns |      73.8935 ns |      69.1200 ns |     4,341.712 ns |  0.43 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  1000 |     9,464.591 ns |      66.4589 ns |      55.4962 ns |     9,453.142 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  1000 |     3,882.386 ns |      54.6927 ns |      51.1596 ns |     3,877.322 ns |  0.41 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  1000 |     9,396.327 ns |      43.0269 ns |      35.9294 ns |     9,394.703 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  1000 |     3,717.186 ns |      16.1408 ns |      12.6017 ns |     3,715.812 ns |  0.40 |    0.00 |    0.0037 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  2000 | 1,170,325.409 ns |  16,821.9875 ns |  15,735.2976 ns | 1,167,121.705 ns |  1.00 |    0.00 |  182.9545 |        - |       - | 1154530 B |
|                     MapNew_add |  2000 |   778,537.274 ns |   4,512.1545 ns |   3,999.9075 ns |   778,564.082 ns |  0.67 |    0.01 |  180.5556 |        - |       - | 1135304 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  2000 | 1,165,196.924 ns |  14,913.5383 ns |  13,950.1330 ns | 1,162,283.182 ns |  1.00 |    0.00 |  176.1364 |        - |       - | 1106408 B |
|                  MapNew_remove |  2000 |   727,330.179 ns |  12,053.8352 ns |  16,499.4256 ns |   721,324.290 ns |  0.63 |    0.01 |  164.7727 |        - |       - | 1034408 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  2000 | 1,198,309.807 ns |  13,092.4957 ns |  12,246.7286 ns | 1,200,869.444 ns |  1.00 |    0.00 |  156.2500 |  52.0833 |       - |  985314 B |
|                 MapNew_ofArray |  2000 |   269,782.260 ns |   1,243.7343 ns |   1,163.3898 ns |   269,657.612 ns |  0.23 |    0.00 |   16.5598 |   4.5406 |       - |  104640 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  2000 | 1,205,857.123 ns |  13,908.7648 ns |  13,010.2673 ns | 1,206,138.942 ns |  1.00 |    0.00 |  157.4519 |  51.6827 |       - |  992442 B |
|                  MapNew_ofList |  2000 |   290,265.697 ns |   3,716.1605 ns |   3,476.0988 ns |   288,830.994 ns |  0.24 |    0.00 |   19.3182 |   6.2500 |       - |  121452 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  2000 | 1,198,909.953 ns |  16,005.4172 ns |  14,971.4772 ns | 1,190,289.741 ns |  1.00 |    0.00 |  156.8396 |  51.8868 |       - |  985194 B |
|                   MapNew_ofSeq |  2000 |   279,542.581 ns |   3,712.3334 ns |   3,472.5190 ns |   278,253.374 ns |  0.23 |    0.00 |   19.0819 |   6.3606 |       - |  121448 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  2000 |    61,244.884 ns |     679.6173 ns |     635.7144 ns |    61,344.445 ns |  1.00 |    0.00 |   20.3699 |   5.4400 |       - |  128024 B |
|                 MapNew_toArray |  2000 |    21,665.474 ns |     212.9221 ns |     199.1675 ns |    21,593.838 ns |  0.35 |    0.00 |   10.1844 |   2.5300 |       - |   64048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  2000 |    43,152.743 ns |     199.6837 ns |     186.7842 ns |    43,141.805 ns |  1.00 |    0.00 |   17.8204 |   5.6546 |       - |  112000 B |
|                  MapNew_toList |  2000 |    29,531.501 ns |     394.9382 ns |     369.4254 ns |    29,578.275 ns |  0.68 |    0.01 |   17.8405 |   5.6950 |       - |  112000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  2000 |    80,977.388 ns |     929.9731 ns |     869.8974 ns |    80,678.181 ns |  1.00 |    0.00 |   24.0199 |        - |       - |  150840 B |
|               MapNew_enumerate |  2000 |    44,597.061 ns |     160.7938 ns |     125.5372 ns |    44,599.029 ns |  0.55 |    0.01 |   12.7523 |        - |       - |   80000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  2000 |   114,501.642 ns |     836.8731 ns |     698.8269 ns |   114,457.881 ns |  1.00 |    0.00 |   31.7096 |        - |       - |  199232 B |
|              MapNew_toSeq_enum |  2000 |   114,875.474 ns |   1,516.6816 ns |   1,418.7049 ns |   115,454.518 ns |  1.00 |    0.01 |   20.3804 |        - |       - |  128192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  2000 |   242,216.319 ns |   2,482.4720 ns |   2,322.1059 ns |   241,396.899 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |  2000 |   138,808.357 ns |     892.6888 ns |     791.3453 ns |   138,589.468 ns |  0.57 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  2000 |       105.129 ns |       1.3949 ns |       1.3048 ns |       105.024 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  2000 |        42.161 ns |       0.3818 ns |       0.3571 ns |        42.028 ns |  0.40 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  2000 |        95.269 ns |       1.0207 ns |       0.9049 ns |        95.053 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |  2000 |        40.902 ns |       0.6709 ns |       0.6275 ns |        40.612 ns |  0.43 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  2000 |       115.023 ns |       1.1089 ns |       1.0373 ns |       114.871 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  2000 |        41.900 ns |       0.1612 ns |       0.1259 ns |        41.888 ns |  0.36 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  2000 | 1,023,067.058 ns |   6,330.4866 ns |   5,611.8115 ns | 1,022,127.571 ns |  1.00 |    0.00 |  157.2581 |  13.1048 |       - |  986585 B |
|              MapNew_remove_all |  2000 |   736,311.031 ns |   5,970.0509 ns |   5,292.2946 ns |   735,709.193 ns |  0.72 |    0.01 |  143.1686 |  10.9012 |       - |  898233 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  2000 |    22,089.844 ns |     292.9153 ns |     273.9932 ns |    21,950.166 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  2000 |     8,688.923 ns |      42.8033 ns |      35.7427 ns |     8,686.938 ns |  0.39 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  2000 |    20,708.571 ns |     282.8171 ns |     264.5473 ns |    20,608.658 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  2000 |     7,848.660 ns |      72.6302 ns |      60.6495 ns |     7,841.897 ns |  0.38 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  2000 |    20,692.940 ns |     259.6537 ns |     242.8802 ns |    20,691.088 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  2000 |     7,518.604 ns |      43.1441 ns |      36.0273 ns |     7,507.714 ns |  0.36 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  3000 | 1,848,877.047 ns |  22,811.4858 ns |  21,337.8780 ns | 1,840,647.059 ns |  1.00 |    0.00 |  286.7647 |        - |       - | 1801994 B |
|                     MapNew_add |  3000 | 1,252,895.449 ns |  14,747.3861 ns |  13,794.7141 ns | 1,251,801.716 ns |  0.68 |    0.01 |  291.6667 |        - |       - | 1833434 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  3000 | 1,975,153.607 ns |  22,076.5804 ns |  20,650.4470 ns | 1,966,360.742 ns |  1.00 |    0.00 |  281.2500 |        - |       - | 1771880 B |
|                  MapNew_remove |  3000 | 1,085,280.015 ns |   4,441.7435 ns |   3,709.0570 ns | 1,084,274.784 ns |  0.55 |    0.01 |  252.1552 |        - |       - | 1582873 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  3000 | 1,991,935.228 ns |  23,228.9884 ns |  21,728.4102 ns | 1,991,694.556 ns |  1.00 |    0.00 |  252.0161 |  82.6613 |       - | 1585891 B |
|                 MapNew_ofArray |  3000 |   443,230.962 ns |   5,591.3249 ns |   5,230.1288 ns |   441,818.706 ns |  0.22 |    0.00 |   26.4085 |   8.8028 |       - |  167512 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  3000 | 1,947,027.639 ns |  25,182.3670 ns |  23,555.6017 ns | 1,945,798.106 ns |  1.00 |    0.00 |  250.0000 |  83.3333 |       - | 1575907 B |
|                  MapNew_ofList |  3000 |   456,288.803 ns |   2,290.1510 ns |   1,912.3798 ns |   456,184.239 ns |  0.23 |    0.00 |   33.0616 |  10.8696 |       - |  209112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  3000 | 1,891,843.725 ns |  26,607.1365 ns |  24,888.3320 ns | 1,886,156.985 ns |  1.00 |    0.00 |  248.1618 |  82.7206 |       - | 1565704 B |
|                   MapNew_ofSeq |  3000 |   457,674.798 ns |   1,450.0499 ns |   1,285.4315 ns |   457,689.236 ns |  0.24 |    0.00 |   32.8704 |  10.6481 |       - |  209112 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  3000 |    96,892.873 ns |   1,545.6539 ns |   1,445.8056 ns |    96,444.038 ns |  1.00 |    0.00 |   30.5790 |   9.7776 |       - |  192024 B |
|                 MapNew_toArray |  3000 |    35,551.939 ns |     346.1836 ns |     323.8204 ns |    35,418.902 ns |  0.37 |    0.01 |   15.2629 |   5.0876 |       - |   96048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  3000 |    70,785.334 ns |     488.3955 ns |     407.8324 ns |    70,782.716 ns |  1.00 |    0.00 |   26.7561 |   9.0570 |       - |  168001 B |
|                  MapNew_toList |  3000 |    49,609.727 ns |     208.6188 ns |     174.2061 ns |    49,553.502 ns |  0.70 |    0.00 |   26.7364 |   9.0272 |       - |  168000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  3000 |   123,027.831 ns |   1,523.5439 ns |   1,425.1239 ns |   122,791.727 ns |  1.00 |    0.00 |   35.9738 |        - |       - |  226320 B |
|               MapNew_enumerate |  3000 |    61,877.393 ns |     745.7391 ns |     697.5648 ns |    61,488.340 ns |  0.50 |    0.01 |   19.0751 |        - |       - |  120000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  3000 |   175,277.791 ns |   2,174.9201 ns |   2,034.4216 ns |   175,917.094 ns |  1.00 |    0.00 |   47.6434 |        - |       - |  299075 B |
|              MapNew_toSeq_enum |  3000 |   174,372.559 ns |   2,255.7592 ns |   2,110.0386 ns |   174,105.125 ns |  1.00 |    0.02 |   30.4709 |        - |       - |  192195 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  3000 |   386,300.051 ns |   4,685.2103 ns |   4,382.5486 ns |   387,453.430 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |  3000 |   221,977.955 ns |   2,155.0807 ns |   2,015.8638 ns |   221,305.392 ns |  0.57 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  3000 |       110.331 ns |       1.0522 ns |       0.9327 ns |       110.079 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  3000 |        45.736 ns |       0.1891 ns |       0.1769 ns |        45.736 ns |  0.41 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  3000 |       111.686 ns |       1.1419 ns |       1.0681 ns |       111.887 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  3000 |        51.003 ns |       0.5520 ns |       0.5163 ns |        50.798 ns |  0.46 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  3000 |       124.503 ns |       1.6188 ns |       1.5142 ns |       123.960 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  3000 |        46.176 ns |       0.4156 ns |       0.3684 ns |        46.099 ns |  0.37 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  3000 | 1,673,556.371 ns |  24,139.1469 ns |  22,579.7730 ns | 1,665,830.592 ns |  1.00 |    0.00 |  250.0000 |  14.8026 |       - | 1568954 B |
|              MapNew_remove_all |  3000 | 1,194,962.846 ns |  12,690.4493 ns |  11,870.6541 ns | 1,192,874.646 ns |  0.71 |    0.01 |  226.4151 |  23.5849 |       - | 1422386 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  3000 |    33,896.401 ns |     151.9148 ns |     118.6051 ns |    33,889.169 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  3000 |    14,862.441 ns |      46.9291 ns |      39.1879 ns |    14,859.878 ns |  0.44 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  3000 |    31,970.285 ns |     379.6211 ns |     355.0978 ns |    31,772.513 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  3000 |    13,558.666 ns |      38.8856 ns |      34.4710 ns |    13,553.974 ns |  0.42 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  3000 |    32,111.106 ns |     150.0662 ns |     125.3121 ns |    32,034.325 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  3000 |    13,889.682 ns |     120.0483 ns |     112.2932 ns |    13,921.316 ns |  0.43 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  4000 | 2,498,700.343 ns |  20,319.7162 ns |  18,012.8991 ns | 2,494,774.159 ns |  1.00 |    0.00 |  389.4231 |        - |       - | 2445640 B |
|                     MapNew_add |  4000 | 1,695,003.784 ns |  22,439.7540 ns |  20,990.1598 ns | 1,695,850.169 ns |  0.68 |    0.01 |  391.8919 |        - |       - | 2462850 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  4000 | 2,647,000.186 ns |  32,650.7040 ns |  28,943.9986 ns | 2,639,607.943 ns |  1.00 |    0.00 |  388.0208 |        - |       - | 2438019 B |
|                  MapNew_remove |  4000 | 1,549,874.045 ns |  21,769.7672 ns |  20,363.4537 ns | 1,537,959.451 ns |  0.58 |    0.01 |  359.7561 |        - |       - | 2260650 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  4000 | 2,804,377.319 ns |  29,947.9651 ns |  28,013.3452 ns | 2,801,344.293 ns |  1.00 |    0.00 |  347.8261 | 133.1522 |       - | 2194866 B |
|                 MapNew_ofArray |  4000 |   593,380.197 ns |   7,850.3225 ns |   7,343.1965 ns |   596,981.076 ns |  0.21 |    0.00 |   32.9861 |  10.9954 |       - |  209225 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  4000 | 2,855,033.859 ns |  35,877.7342 ns |  33,560.0548 ns | 2,865,813.859 ns |  1.00 |    0.00 |  347.8261 | 130.4348 |       - | 2182401 B |
|                  MapNew_ofList |  4000 |   624,473.664 ns |   3,513.6752 ns |   2,743.2465 ns |   624,249.381 ns |  0.22 |    0.00 |   38.3663 |  13.6139 |       - |  242817 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  4000 | 2,846,492.536 ns |  26,698.1211 ns |  24,973.4390 ns | 2,841,723.370 ns |  1.00 |    0.00 |  345.1087 | 133.1522 |       - | 2172164 B |
|                   MapNew_ofSeq |  4000 |   627,146.234 ns |   6,995.7243 ns |   6,201.5274 ns |   628,232.550 ns |  0.22 |    0.00 |   38.3663 |  13.6139 |       - |  242817 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  4000 |   136,016.577 ns |     519.2757 ns |     433.6187 ns |   135,932.717 ns |  1.00 |    0.00 |   40.7609 |  19.8370 |       - |  256024 B |
|                 MapNew_toArray |  4000 |    45,934.773 ns |     349.0195 ns |     291.4471 ns |    45,892.982 ns |  0.34 |    0.00 |   20.3553 |   6.7543 |       - |  128048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  4000 |    96,404.622 ns |     616.3009 ns |     546.3347 ns |    96,396.091 ns |  1.00 |    0.00 |   35.6583 |  13.1270 |       - |  224001 B |
|                  MapNew_toList |  4000 |    67,650.652 ns |     481.3256 ns |     450.2323 ns |    67,605.098 ns |  0.70 |    0.01 |   35.6675 |  12.3037 |       - |  224001 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  4000 |   163,049.468 ns |     987.0526 ns |     874.9964 ns |   162,728.878 ns |  1.00 |    0.00 |   48.2513 |        - |       - |  303240 B |
|               MapNew_enumerate |  4000 |    90,752.794 ns |   1,192.3096 ns |   1,115.2871 ns |    91,114.078 ns |  0.56 |    0.01 |   25.4993 |        - |       - |  160000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  4000 |   233,048.500 ns |   2,839.7946 ns |   2,656.3457 ns |   231,899.627 ns |  1.00 |    0.00 |   63.1996 |        - |       - |  397836 B |
|              MapNew_toSeq_enum |  4000 |   228,281.587 ns |   2,803.1123 ns |   2,622.0330 ns |   228,266.007 ns |  0.98 |    0.02 |   40.6924 |        - |       - |  256192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  4000 |   531,202.143 ns |   6,379.6004 ns |   5,967.4822 ns |   530,019.650 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |  4000 |   313,738.440 ns |   3,521.3893 ns |   3,293.9098 ns |   312,385.108 ns |  0.59 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  4000 |       104.664 ns |       1.1377 ns |       1.0642 ns |       104.316 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  4000 |        46.600 ns |       0.6290 ns |       0.5884 ns |        46.571 ns |  0.45 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  4000 |       118.041 ns |       1.7392 ns |       1.4523 ns |       117.524 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |  4000 |        50.537 ns |       0.3037 ns |       0.2841 ns |        50.437 ns |  0.43 |    0.00 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  4000 |       130.377 ns |       1.6198 ns |       1.5151 ns |       129.751 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  4000 |        46.269 ns |       0.4300 ns |       0.3812 ns |        46.180 ns |  0.35 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  4000 | 2,350,839.722 ns |   8,327.4553 ns |   7,789.5068 ns | 2,350,091.204 ns |  1.00 |    0.00 |  347.2222 |  43.9815 |       - | 2179841 B |
|              MapNew_remove_all |  4000 | 1,721,602.984 ns |  27,222.4432 ns |  25,463.8903 ns | 1,722,543.412 ns |  0.73 |    0.01 |  315.8784 |  48.9865 |       - | 1987746 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  4000 |    46,027.882 ns |     412.8102 ns |     386.1429 ns |    45,863.677 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  4000 |    17,610.652 ns |     302.8000 ns |     283.2393 ns |    17,547.277 ns |  0.38 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  4000 |    43,302.304 ns |     437.9833 ns |     409.6898 ns |    43,095.047 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  4000 |    15,904.077 ns |     192.5333 ns |     180.0958 ns |    15,837.727 ns |  0.37 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  4000 |    43,397.492 ns |     289.9698 ns |     242.1379 ns |    43,364.591 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  4000 |    15,034.931 ns |      48.9592 ns |      40.8831 ns |    15,028.965 ns |  0.35 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  5000 | 3,280,653.333 ns |  42,235.4401 ns |  39,507.0569 ns | 3,267,333.125 ns |  1.00 |    0.00 |  512.5000 |        - |       - | 3219775 B |
|                     MapNew_add |  5000 | 2,391,462.778 ns |  29,952.5302 ns |  28,017.6154 ns | 2,391,462.963 ns |  0.73 |    0.01 |  518.5185 |        - |       - | 3253091 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  5000 | 3,403,037.873 ns |  37,113.7038 ns |  34,716.1815 ns | 3,407,071.053 ns |  1.00 |    0.00 |  493.4211 |        - |       - | 3114986 B |
|                  MapNew_remove |  5000 | 2,105,958.347 ns |  26,840.1921 ns |  25,106.3323 ns | 2,106,429.167 ns |  0.62 |    0.01 |  454.1667 |        - |       - | 2861721 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  5000 | 3,633,895.579 ns |  43,826.7773 ns |  40,995.5946 ns | 3,622,868.056 ns |  1.00 |    0.00 |  444.4444 | 211.8056 |       - | 2800461 B |
|                 MapNew_ofArray |  5000 |   802,861.135 ns |  11,027.3895 ns |  10,315.0270 ns |   802,024.922 ns |  0.22 |    0.00 |   42.9688 |  11.7188 |       - |  270936 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  5000 | 3,659,918.934 ns |  16,530.5098 ns |  12,905.9347 ns | 3,663,296.875 ns |  1.00 |    0.00 |  448.5294 | 198.5294 |       - | 2819130 B |
|                  MapNew_ofList |  5000 |   830,645.773 ns |   8,847.6924 ns |   8,276.1370 ns |   829,982.155 ns |  0.23 |    0.00 |   56.7434 |  21.3816 |       - |  362097 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  5000 | 3,755,710.441 ns |  45,791.0445 ns |  42,832.9714 ns | 3,741,339.706 ns |  1.00 |    0.00 |  444.8529 | 205.8824 |       - | 2810613 B |
|                   MapNew_ofSeq |  5000 |   832,093.224 ns |   7,681.3501 ns |   7,185.1396 ns |   829,823.109 ns |  0.22 |    0.00 |   56.7434 |  20.5592 |       - |  362097 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  5000 |   182,089.946 ns |   2,474.4059 ns |   2,314.5608 ns |   180,983.536 ns |  1.00 |    0.00 |   50.8929 |  19.4643 |       - |  320024 B |
|                 MapNew_toArray |  5000 |    62,835.732 ns |     643.8371 ns |     602.2456 ns |    62,706.797 ns |  0.35 |    0.01 |   25.4353 |   9.5149 |       - |  160048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  5000 |   129,725.498 ns |     455.0581 ns |     379.9942 ns |   129,753.228 ns |  1.00 |    0.00 |   44.5506 |  17.9494 |       - |  280000 B |
|                  MapNew_toList |  5000 |    94,303.044 ns |     506.5160 ns |     473.7953 ns |    94,269.053 ns |  0.73 |    0.00 |   44.5751 |  17.9249 |       - |  280000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  5000 |   204,998.285 ns |   2,407.9192 ns |   2,252.3691 ns |   204,125.243 ns |  1.00 |    0.00 |   60.0728 |        - |       - |  377163 B |
|               MapNew_enumerate |  5000 |   105,279.312 ns |   1,203.2558 ns |   1,125.5262 ns |   105,160.885 ns |  0.51 |    0.01 |   31.8750 |        - |       - |  200000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  5000 |   292,060.461 ns |   2,155.4550 ns |   1,799.9026 ns |   291,898.032 ns |  1.00 |    0.00 |   79.2824 |        - |       - |  498272 B |
|              MapNew_toSeq_enum |  5000 |   293,022.695 ns |   3,624.6786 ns |   3,390.5266 ns |   293,698.202 ns |  1.00 |    0.01 |   51.0024 |        - |       - |  320192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  5000 |   685,140.959 ns |   8,141.4468 ns |   7,615.5143 ns |   685,025.336 ns |  1.00 |    0.00 |         - |        - |       - |      10 B |
|         MapNew_containsKey_all |  5000 |   413,204.840 ns |   7,898.8150 ns |   7,388.5565 ns |   408,988.121 ns |  0.60 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  5000 |       102.099 ns |       0.8210 ns |       0.6856 ns |       101.976 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  5000 |        49.949 ns |       0.5948 ns |       0.5564 ns |        49.793 ns |  0.49 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  5000 |       117.920 ns |       1.0712 ns |       1.0020 ns |       117.644 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  5000 |        56.816 ns |       0.7367 ns |       0.6891 ns |        56.573 ns |  0.48 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  5000 |       123.781 ns |       1.2066 ns |       1.0696 ns |       123.538 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  5000 |        49.791 ns |       0.4428 ns |       0.3697 ns |        49.737 ns |  0.40 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  5000 | 3,073,335.506 ns |  37,127.5131 ns |  34,729.0988 ns | 3,064,827.530 ns |  1.00 |    0.00 |  443.4524 |  71.4286 |       - | 2793732 B |
|              MapNew_remove_all |  5000 | 2,158,173.764 ns |  32,744.1307 ns |  30,628.8802 ns | 2,145,428.750 ns |  0.70 |    0.01 |  406.2500 |  72.9167 |       - | 2555651 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  5000 |    57,114.772 ns |     394.5591 ns |     329.4747 ns |    57,097.422 ns |  1.00 |    0.00 |         - |        - |       - |      25 B |
|                  MapNew_exists |  5000 |    23,630.982 ns |     236.0310 ns |     220.7835 ns |    23,553.502 ns |  0.41 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  5000 |    54,755.357 ns |     541.4285 ns |     506.4526 ns |    54,714.582 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  5000 |    21,734.721 ns |      80.0337 ns |      66.8318 ns |    21,728.318 ns |  0.40 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  5000 |    54,670.301 ns |     628.2648 ns |     587.6793 ns |    54,551.016 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  5000 |    20,814.577 ns |     195.3058 ns |     173.1335 ns |    20,777.118 ns |  0.38 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  6000 | 4,433,264.250 ns |  57,702.5269 ns |  53,974.9795 ns | 4,412,614.167 ns |  1.00 |    0.00 |  666.6667 |        - |       - | 4200677 B |
|                     MapNew_add |  6000 | 2,716,382.068 ns |  13,263.6707 ns |  11,757.8986 ns | 2,711,954.036 ns |  0.61 |    0.01 |  630.2083 |        - |       - | 3953931 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  6000 | 4,155,607.708 ns |  50,512.3817 ns |  47,249.3132 ns | 4,150,568.750 ns |  1.00 |    0.00 |  605.4688 |        - |       - | 3807117 B |
|                  MapNew_remove |  6000 | 2,458,631.923 ns |  32,755.8540 ns |  30,639.8462 ns | 2,443,586.538 ns |  0.59 |    0.01 |  550.4808 |        - |       - | 3453756 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  6000 | 4,589,790.625 ns |  29,232.6366 ns |  25,913.9709 ns | 4,583,961.161 ns |  1.00 |    0.00 |  544.6429 | 267.8571 |       - | 3425662 B |
|                 MapNew_ofArray |  6000 |   966,138.417 ns |   8,997.8653 ns |   8,416.6088 ns |   961,981.346 ns |  0.21 |    0.00 |   52.8846 |  25.9615 |       - |  334936 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  6000 | 4,787,577.589 ns |  63,292.8995 ns |  59,204.2175 ns | 4,767,250.893 ns |  1.00 |    0.00 |  544.6429 | 258.9286 |  4.4643 | 3435994 B |
|                  MapNew_ofList |  6000 | 1,013,064.026 ns |  10,700.2432 ns |  10,009.0142 ns | 1,009,790.726 ns |  0.21 |    0.00 |   66.5323 |  28.2258 |       - |  418111 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  6000 | 4,729,535.298 ns |  60,028.2890 ns |  56,150.4988 ns | 4,707,678.125 ns |  1.00 |    0.00 |  544.6429 | 267.8571 |       - | 3426406 B |
|                   MapNew_ofSeq |  6000 | 1,013,057.582 ns |  11,697.4238 ns |  10,369.4615 ns | 1,009,508.284 ns |  0.21 |    0.00 |   66.4683 |  28.7698 |       - |  418097 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  6000 |   229,194.121 ns |     989.7929 ns |     877.4256 ns |   229,017.880 ns |  1.00 |    0.00 |   61.1264 |  20.8333 |       - |  384024 B |
|                 MapNew_toArray |  6000 |    80,560.244 ns |   1,237.6933 ns |   1,157.7390 ns |    80,108.447 ns |  0.35 |    0.00 |   30.5362 |  11.2627 |       - |  192048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  6000 |   166,391.880 ns |   1,156.6800 ns |     965.8801 ns |   166,243.165 ns |  1.00 |    0.00 |   53.4759 |  23.3957 |       - |  336000 B |
|                  MapNew_toList |  6000 |   123,024.825 ns |   1,631.7581 ns |   1,526.3475 ns |   122,469.923 ns |  0.74 |    0.01 |   53.4832 |  23.4684 |       - |  336000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  6000 |   244,941.063 ns |   1,373.8768 ns |   1,147.2494 ns |   244,663.745 ns |  1.00 |    0.00 |   72.0215 |        - |       - |  452760 B |
|               MapNew_enumerate |  6000 |   125,387.186 ns |   1,936.3467 ns |   1,811.2599 ns |   124,786.081 ns |  0.51 |    0.01 |   38.2221 |        - |       - |  240002 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  6000 |   351,749.512 ns |   3,903.8229 ns |   3,651.6384 ns |   351,614.826 ns |  1.00 |    0.00 |   95.4861 |        - |       - |  599192 B |
|              MapNew_toSeq_enum |  6000 |   343,038.830 ns |   2,530.7525 ns |   2,113.2930 ns |   342,865.014 ns |  0.98 |    0.01 |   61.1413 |        - |       - |  384192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  6000 |   855,069.767 ns |   8,192.2142 ns |   7,663.0023 ns |   856,005.500 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |  6000 |   503,793.260 ns |   3,923.4348 ns |   3,669.9833 ns |   502,284.500 ns |  0.59 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  6000 |       128.874 ns |       1.6262 ns |       1.5212 ns |       128.341 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  6000 |        49.483 ns |       0.3390 ns |       0.3005 ns |        49.425 ns |  0.38 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  6000 |       117.229 ns |       0.4249 ns |       0.3767 ns |       117.358 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |  6000 |        45.819 ns |       0.6457 ns |       0.5724 ns |        45.556 ns |  0.39 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  6000 |       123.692 ns |       1.0091 ns |       0.8427 ns |       123.581 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  6000 |        49.820 ns |       0.2726 ns |       0.2417 ns |        49.827 ns |  0.40 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  6000 | 3,727,465.288 ns |  15,744.8339 ns |  12,292.5306 ns | 3,727,776.838 ns |  1.00 |    0.00 |  544.1176 |  88.2353 |       - | 3418029 B |
|              MapNew_remove_all |  6000 | 2,672,002.431 ns |  10,268.8513 ns |   8,017.2436 ns | 2,671,753.125 ns |  0.72 |    0.00 |  497.3958 |  93.7500 |       - | 3128811 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  6000 |    69,478.490 ns |     862.8368 ns |     807.0980 ns |    69,631.547 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  6000 |    30,303.738 ns |     439.1219 ns |     389.2701 ns |    30,128.982 ns |  0.44 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  6000 |    66,652.463 ns |     709.2510 ns |     663.4338 ns |    66,376.080 ns |  1.00 |    0.00 |         - |        - |       - |      25 B |
|                    MapNew_fold |  6000 |    27,414.834 ns |     196.6308 ns |     164.1956 ns |    27,428.857 ns |  0.41 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  6000 |    65,957.528 ns |     163.7548 ns |     145.1644 ns |    65,996.068 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  6000 |    28,076.438 ns |     174.1205 ns |     154.3533 ns |    28,025.961 ns |  0.43 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  7000 | 4,880,197.628 ns |  36,993.9806 ns |  34,604.1924 ns | 4,874,726.442 ns |  1.00 |    0.00 |  725.9615 |        - |       - | 4581982 B |
|                     MapNew_add |  7000 | 3,246,932.750 ns |  36,053.8477 ns |  33,724.7915 ns | 3,235,150.625 ns |  0.67 |    0.01 |  737.5000 |        - |       - | 4633460 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  7000 | 4,975,826.408 ns |  27,917.5709 ns |  24,748.1994 ns | 4,969,414.904 ns |  1.00 |    0.00 |  716.3462 |        - |       - | 4520478 B |
|                  MapNew_remove |  7000 | 3,135,204.812 ns |  37,851.0044 ns |  35,405.8530 ns | 3,121,385.312 ns |  0.63 |    0.01 |  662.5000 |        - |       - | 4169132 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  7000 | 5,956,467.938 ns |  72,298.3926 ns |  64,090.6419 ns | 5,951,377.557 ns |  1.00 |    0.00 |  647.7273 | 210.2273 | 28.4091 | 4072432 B |
|                 MapNew_ofArray |  7000 | 1,171,982.913 ns |  13,846.0740 ns |  12,951.6262 ns | 1,170,339.210 ns |  0.20 |    0.00 |   60.1415 |  29.4811 |       - |  378369 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  7000 | 5,707,052.386 ns |  34,180.8732 ns |  31,972.8100 ns | 5,698,970.455 ns |  1.00 |    0.00 |  647.7273 | 284.0909 | 17.0455 | 4085764 B |
|                  MapNew_ofList |  7000 | 1,192,156.148 ns |   7,157.2374 ns |   5,976.6174 ns | 1,190,388.221 ns |  0.21 |    0.00 |   72.1154 |  36.0577 |       - |  453530 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  7000 | 5,775,273.514 ns |  25,635.0400 ns |  21,406.4194 ns | 5,774,690.341 ns |  1.00 |    0.00 |  647.7273 | 272.7273 | 22.7273 | 4067927 B |
|                   MapNew_ofSeq |  7000 | 1,218,340.449 ns |  12,160.0575 ns |  11,374.5253 ns | 1,215,470.553 ns |  0.21 |    0.00 |   72.1154 |  36.0577 |       - |  453530 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  7000 |   277,545.372 ns |     938.2381 ns |     877.6285 ns |   277,499.889 ns |  1.00 |    0.00 |   71.3496 |  26.8252 |       - |  448024 B |
|                 MapNew_toArray |  7000 |    90,104.297 ns |     445.3585 ns |     347.7066 ns |    90,017.383 ns |  0.32 |    0.00 |   35.5990 |  10.8501 |       - |  224048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  7000 |   202,482.015 ns |     880.6434 ns |     735.3771 ns |   202,673.584 ns |  1.00 |    0.00 |   62.2977 |  31.1489 |       - |  392000 B |
|                  MapNew_toList |  7000 |   150,139.941 ns |   1,882.6052 ns |   1,760.9901 ns |   149,753.563 ns |  0.74 |    0.01 |   62.3515 |  31.1758 |       - |  392000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  7000 |   288,380.820 ns |   1,125.8038 ns |     997.9958 ns |   287,906.308 ns |  1.00 |    0.00 |   84.1014 |        - |       - |  529200 B |
|               MapNew_enumerate |  7000 |   149,619.983 ns |   1,402.2789 ns |   1,311.6925 ns |   149,180.137 ns |  0.52 |    0.00 |   44.5368 |        - |       - |  280000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  7000 |   413,994.959 ns |   4,828.6458 ns |   4,516.7183 ns |   414,749.794 ns |  1.00 |    0.00 |  110.6086 |        - |       - |  694712 B |
|              MapNew_toSeq_enum |  7000 |   406,848.280 ns |   4,630.5882 ns |   4,331.4550 ns |   407,071.755 ns |  0.98 |    0.01 |   71.3141 |        - |       - |  448193 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  7000 | 1,009,225.880 ns |  12,477.5095 ns |  11,671.4702 ns | 1,005,250.504 ns |  1.00 |    0.00 |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |  7000 |   610,004.940 ns |   5,677.2194 ns |   5,310.4746 ns |   609,932.632 ns |  0.60 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  7000 |       114.135 ns |       1.1674 ns |       1.0920 ns |       113.737 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  7000 |        49.555 ns |       0.4950 ns |       0.4630 ns |        49.330 ns |  0.43 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  7000 |       106.856 ns |       1.2216 ns |       1.1427 ns |       106.378 ns |  1.00 |    0.00 |    0.0037 |        - |       - |      24 B |
|                 MapNew_tryFind |  7000 |        55.578 ns |       0.3726 ns |       0.3303 ns |        55.499 ns |  0.52 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  7000 |       153.854 ns |       1.3193 ns |       1.1696 ns |       153.569 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  7000 |        49.741 ns |       0.2020 ns |       0.1791 ns |        49.739 ns |  0.32 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  7000 | 4,716,187.054 ns |  60,024.0888 ns |  56,146.5699 ns | 4,704,467.857 ns |  1.00 |    0.00 |  651.7857 | 151.7857 |       - | 4116614 B |
|              MapNew_remove_all |  7000 | 3,244,568.729 ns |  36,951.2751 ns |  34,564.2456 ns | 3,237,775.312 ns |  0.69 |    0.01 |  593.7500 | 118.7500 |       - | 3740756 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  7000 |    83,969.213 ns |     875.6885 ns |     819.1196 ns |    83,768.792 ns |  1.00 |    0.00 |         - |        - |       - |      25 B |
|                  MapNew_exists |  7000 |    32,798.928 ns |     376.5258 ns |     352.2024 ns |    32,698.924 ns |  0.39 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  7000 |    79,107.164 ns |     327.5508 ns |     290.3653 ns |    79,189.143 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  7000 |    30,755.418 ns |     306.5912 ns |     286.7856 ns |    30,648.773 ns |  0.39 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  7000 |    80,428.287 ns |     829.7756 ns |     776.1726 ns |    80,191.301 ns |  1.00 |    0.00 |         - |        - |       - |      25 B |
|                MapNew_foldBack |  7000 |    30,317.783 ns |     137.5844 ns |     121.9649 ns |    30,336.333 ns |  0.38 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  8000 | 5,881,054.677 ns |  37,560.1266 ns |  31,364.4068 ns | 5,881,259.659 ns |  1.00 |    0.00 |  869.3182 |        - |       - | 5462920 B |
|                     MapNew_add |  8000 | 3,689,182.410 ns |  18,983.9043 ns |  15,852.4199 ns | 3,687,145.956 ns |  0.63 |    0.00 |  845.5882 |        - |       - | 5309821 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  8000 | 5,709,418.125 ns |  51,612.5917 ns |  48,278.4503 ns | 5,703,608.854 ns |  1.00 |    0.00 |  833.3333 |        - |       - | 5246455 B |
|                  MapNew_remove |  8000 | 3,584,465.625 ns |  33,750.6674 ns |  31,570.3953 ns | 3,573,862.847 ns |  0.63 |    0.01 |  781.2500 |        - |       - | 4905133 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  8000 | 7,082,405.278 ns |  70,865.4445 ns |  66,287.5808 ns | 7,061,156.250 ns |  1.00 |    0.00 |  750.0000 | 256.9444 | 34.7222 | 4735546 B |
|                 MapNew_ofArray |  8000 | 1,307,812.378 ns |  14,554.1073 ns |  13,613.9210 ns | 1,299,696.484 ns |  0.18 |    0.00 |   66.4063 |  32.5521 |       - |  418388 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  8000 | 6,869,024.083 ns |  54,588.7155 ns |  51,062.3183 ns | 6,868,165.625 ns |  1.00 |    0.00 |  750.0000 | 275.0000 | 37.5000 | 4739928 B |
|                  MapNew_ofList |  8000 | 1,379,068.090 ns |  12,199.6501 ns |  10,814.6720 ns | 1,375,591.033 ns |  0.20 |    0.00 |   76.0870 |  38.0435 |       - |  485530 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  8000 | 7,161,456.968 ns |  67,839.5986 ns |  63,457.2026 ns | 7,175,765.625 ns |  1.00 |    0.00 |  750.0000 | 277.7778 | 34.7222 | 4747744 B |
|                   MapNew_ofSeq |  8000 | 1,377,991.504 ns |  18,056.0525 ns |  16,889.6427 ns | 1,367,749.457 ns |  0.19 |    0.00 |   76.0870 |  38.0435 |       - |  485530 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  8000 |   334,359.692 ns |   1,131.2102 ns |   1,058.1347 ns |   333,952.360 ns |  1.00 |    0.00 |   81.4495 |  39.2287 |       - |  512024 B |
|                 MapNew_toArray |  8000 |   105,447.365 ns |   1,118.8182 ns |   1,046.5433 ns |   104,976.463 ns |  0.32 |    0.00 |   40.7609 |  20.3804 |       - |  256048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  8000 |   243,056.181 ns |   3,349.2542 ns |   3,132.8945 ns |   241,465.948 ns |  1.00 |    0.00 |   71.3602 |  35.6801 |       - |  448000 B |
|                  MapNew_toList |  8000 |   183,413.697 ns |   3,021.6975 ns |   2,826.4977 ns |   183,007.199 ns |  0.75 |    0.01 |   71.2751 |  35.6375 |       - |  448000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  8000 |   332,258.562 ns |   2,074.3350 ns |   1,940.3342 ns |   331,686.413 ns |  1.00 |    0.00 |   96.1277 |        - |       - |  603360 B |
|               MapNew_enumerate |  8000 |   182,466.410 ns |   1,963.2535 ns |   1,836.4285 ns |   182,321.008 ns |  0.55 |    0.01 |   50.9393 |        - |       - |  320000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  8000 |   475,480.099 ns |   6,562.2859 ns |   6,138.3663 ns |   475,378.053 ns |  1.00 |    0.00 |  126.4313 |        - |       - |  794553 B |
|              MapNew_toSeq_enum |  8000 |   464,134.136 ns |   6,519.0761 ns |   6,097.9479 ns |   463,035.681 ns |  0.98 |    0.02 |   81.6231 |        - |       - |  512195 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  8000 | 1,177,490.527 ns |  12,606.9324 ns |  11,792.5324 ns | 1,175,081.132 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|         MapNew_containsKey_all |  8000 |   734,676.037 ns |   6,286.5243 ns |   5,572.8401 ns |   733,528.520 ns |  0.62 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  8000 |       119.580 ns |       1.2729 ns |       1.1907 ns |       119.148 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  8000 |        49.702 ns |       0.5082 ns |       0.4753 ns |        49.579 ns |  0.42 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  8000 |       118.643 ns |       0.4570 ns |       0.3816 ns |       118.716 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  8000 |        54.914 ns |       0.6570 ns |       0.6145 ns |        54.741 ns |  0.46 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  8000 |       140.193 ns |       1.4373 ns |       1.3444 ns |       139.682 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  8000 |        50.403 ns |       0.6291 ns |       0.5884 ns |        50.518 ns |  0.36 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  8000 | 5,342,304.848 ns |  33,102.8128 ns |  27,642.3480 ns | 5,337,451.562 ns |  1.00 |    0.00 |  755.2083 | 182.2917 |       - | 4739883 B |
|              MapNew_remove_all |  8000 | 3,798,931.275 ns |  40,891.3002 ns |  38,249.7475 ns | 3,793,493.382 ns |  0.71 |    0.01 |  691.1765 | 150.7353 |       - | 4358289 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  8000 |   101,734.527 ns |   1,119.1860 ns |   1,046.8873 ns |   101,286.046 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  8000 |    38,832.919 ns |     332.5796 ns |     294.8232 ns |    38,859.149 ns |  0.38 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  8000 |    97,195.309 ns |     966.6925 ns |     904.2448 ns |    97,004.795 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  8000 |    34,509.436 ns |     209.1814 ns |     174.6760 ns |    34,469.150 ns |  0.36 |    0.00 |         - |        - |       - |      25 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  8000 |    96,212.701 ns |   1,129.5843 ns |   1,056.6138 ns |    95,922.866 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  8000 |    35,621.994 ns |     337.9293 ns |     299.5655 ns |    35,691.481 ns |  0.37 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add |  9000 | 6,640,205.083 ns |  61,779.8632 ns |  57,788.9223 ns | 6,618,485.625 ns |  1.00 |    0.00 |  981.2500 |        - |       - | 6181926 B |
|                     MapNew_add |  9000 | 4,584,280.744 ns |  67,801.9938 ns |  63,422.0271 ns | 4,595,528.125 ns |  0.69 |    0.01 |  991.0714 |        - |       - | 6241470 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove |  9000 | 6,638,267.292 ns |  63,294.3044 ns |  59,205.5316 ns | 6,645,425.625 ns |  1.00 |    0.00 |  950.0000 |        - |       - | 5988808 B |
|                  MapNew_remove |  9000 | 4,121,582.031 ns |  38,469.7562 ns |  35,984.6338 ns | 4,114,313.281 ns |  0.62 |    0.01 |  886.7188 |        - |       - | 5563421 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray |  9000 | 7,935,989.167 ns |  85,828.6167 ns |  80,284.1413 ns | 7,900,265.625 ns |  1.00 |    0.00 |  859.3750 | 257.8125 | 39.0625 | 5419202 B |
|                 MapNew_ofArray |  9000 | 1,610,999.530 ns |  16,287.9466 ns |  15,235.7554 ns | 1,605,298.718 ns |  0.20 |    0.00 |   75.3205 |  36.8590 |       - |  477786 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList |  9000 | 8,034,943.854 ns |  89,617.0618 ns |  83,827.8552 ns | 8,027,882.812 ns |  1.00 |    0.00 |  867.1875 | 242.1875 | 39.0625 | 5443024 B |
|                  MapNew_ofList |  9000 | 1,779,731.232 ns |  22,297.0470 ns |  20,856.6716 ns | 1,777,581.710 ns |  0.22 |    0.00 |  102.9412 |  60.6618 | 40.4412 |  668042 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq |  9000 | 8,073,763.385 ns |  85,763.5285 ns |  80,223.2578 ns | 8,035,069.531 ns |  1.00 |    0.00 |  859.3750 | 234.3750 | 46.8750 | 5439050 B |
|                   MapNew_ofSeq |  9000 | 1,775,144.482 ns |  19,332.0500 ns |  18,083.2116 ns | 1,767,539.583 ns |  0.22 |    0.00 |  102.2727 |  60.6061 | 39.7727 |  668044 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray |  9000 |   348,199.280 ns |   1,415.1011 ns |   1,181.6735 ns |   348,352.130 ns |  1.00 |    0.00 |   90.7821 |  45.3911 |       - |  576024 B |
|                 MapNew_toArray |  9000 |   115,137.667 ns |     692.9990 ns |     614.3256 ns |   115,226.433 ns |  0.33 |    0.00 |   45.3499 |  22.6750 |       - |  288048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList |  9000 |   296,938.196 ns |   3,200.8518 ns |   2,994.0787 ns |   295,811.209 ns |  1.00 |    0.00 |   80.1056 |  39.9061 |       - |  504000 B |
|                  MapNew_toList |  9000 |   224,379.629 ns |   2,740.3264 ns |   2,563.3030 ns |   223,976.272 ns |  0.76 |    0.01 |   80.2632 |  40.1316 |       - |  504000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate |  9000 |   374,519.479 ns |   2,098.1686 ns |   1,859.9718 ns |   374,684.036 ns |  1.00 |    0.00 |  108.0572 |        - |       - |  678600 B |
|               MapNew_enumerate |  9000 |   195,070.467 ns |   2,375.7272 ns |   2,222.2567 ns |   193,709.241 ns |  0.52 |    0.01 |   57.3236 |        - |       - |  360000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum |  9000 |   533,670.611 ns |   2,081.1664 ns |   1,844.8998 ns |   534,028.972 ns |  1.00 |    0.00 |  142.4788 |        - |       - |  894513 B |
|              MapNew_toSeq_enum |  9000 |   515,969.666 ns |   4,421.4418 ns |   4,135.8194 ns |   513,819.215 ns |  0.97 |    0.01 |   91.4256 |        - |       - |  576192 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all |  9000 | 1,365,533.697 ns |  17,841.0045 ns |  16,688.4867 ns | 1,365,912.367 ns |  1.00 |    0.00 |         - |        - |       - |       2 B |
|         MapNew_containsKey_all |  9000 |   851,254.789 ns |   8,185.9490 ns |   7,657.1417 ns |   850,000.257 ns |  0.62 |    0.01 |         - |        - |       - |       1 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting |  9000 |       127.974 ns |       0.6603 ns |       0.5514 ns |       127.797 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  9000 |        53.423 ns |       0.3016 ns |       0.2518 ns |        53.317 ns |  0.42 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind |  9000 |       137.090 ns |       1.3610 ns |       1.2065 ns |       136.875 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind |  9000 |        56.070 ns |       0.6127 ns |       0.5731 ns |        55.898 ns |  0.41 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting |  9000 |       159.547 ns |       1.3786 ns |       1.2895 ns |       159.369 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting |  9000 |        53.623 ns |       0.5193 ns |       0.4858 ns |        53.487 ns |  0.34 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all |  9000 | 6,197,765.000 ns |  66,671.9957 ns |  62,365.0261 ns | 6,184,443.750 ns |  1.00 |    0.00 |  857.9545 | 215.9091 |       - | 5411192 B |
|              MapNew_remove_all |  9000 | 4,309,228.667 ns |  45,093.0055 ns |  42,180.0253 ns | 4,286,247.500 ns |  0.70 |    0.01 |  791.6667 | 208.3333 |       - | 4969134 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists |  9000 |   117,344.400 ns |     483.9517 ns |     429.0105 ns |   117,209.795 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists |  9000 |    42,393.823 ns |     139.6496 ns |     130.6284 ns |    42,454.660 ns |  0.36 |    0.00 |         - |        - |       - |      25 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold |  9000 |   112,108.861 ns |     763.8871 ns |     637.8803 ns |   112,037.354 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                    MapNew_fold |  9000 |    40,326.905 ns |     226.5483 ns |     189.1781 ns |    40,209.224 ns |  0.36 |    0.00 |         - |        - |       - |      25 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack |  9000 |   113,709.372 ns |   1,297.6494 ns |   1,213.8221 ns |   113,783.266 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack |  9000 |    43,281.562 ns |     212.1560 ns |     177.1599 ns |    43,299.505 ns |  0.38 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                        Map_add | 10000 | 7,230,028.380 ns | 104,172.2853 ns |  97,442.8203 ns | 7,189,109.028 ns |  1.00 |    0.00 | 1076.3889 |        - |       - | 6794761 B |
|                     MapNew_add | 10000 | 5,203,566.907 ns | 103,010.7404 ns | 154,181.6259 ns | 5,144,345.433 ns |  0.73 |    0.02 | 1110.5769 |        - |       - | 6990161 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_remove | 10000 | 7,408,072.315 ns |  55,373.1212 ns |  51,796.0519 ns | 7,399,147.917 ns |  1.00 |    0.00 | 1062.5000 |        - |       - | 6688265 B |
|                  MapNew_remove | 10000 | 4,496,783.323 ns |  44,466.3003 ns |  39,418.2170 ns | 4,490,120.089 ns |  0.61 |    0.01 |  986.6071 |        - |       - | 6203422 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_ofArray | 10000 | 9,473,671.301 ns | 133,439.4051 ns | 118,290.5569 ns | 9,489,411.607 ns |  1.00 |    0.00 |  973.2143 | 276.7857 | 53.5714 | 6095116 B |
|                 MapNew_ofArray | 10000 | 1,794,501.560 ns |  16,916.8042 ns |  15,823.9892 ns | 1,790,164.643 ns |  0.19 |    0.00 |   85.7143 |  42.8571 |       - |  541786 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_ofList | 10000 | 9,189,759.877 ns | 177,252.0204 ns | 174,085.1847 ns | 9,141,654.464 ns |  1.00 |    0.00 |  973.2143 | 267.8571 | 44.6429 | 6059717 B |
|                  MapNew_ofList | 10000 | 2,012,498.799 ns |  13,330.9559 ns |  11,817.5452 ns | 2,009,267.996 ns |  0.22 |    0.00 |  122.8448 |  81.8966 | 40.9483 |  724044 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                      Map_ofSeq | 10000 | 9,357,274.405 ns | 172,762.3636 ns | 161,602.0221 ns | 9,363,566.071 ns |  1.00 |    0.00 |  973.2143 | 267.8571 | 53.5714 | 6094012 B |
|                   MapNew_ofSeq | 10000 | 1,982,447.030 ns |  16,436.0144 ns |  13,724.8164 ns | 1,976,358.929 ns |  0.21 |    0.00 |  122.7679 |  80.3571 | 40.1786 |  724040 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_toArray | 10000 |   454,392.637 ns |   4,398.9794 ns |   4,114.8081 ns |   453,240.602 ns |  1.00 |    0.00 |  101.7336 |  39.6898 |       - |  640024 B |
|                 MapNew_toArray | 10000 |   145,812.427 ns |   1,592.3149 ns |   1,489.4523 ns |   145,413.903 ns |  0.32 |    0.00 |   50.7813 |  25.3183 |       - |  320048 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_toList | 10000 |   343,808.381 ns |   2,442.4153 ns |   2,039.5274 ns |   343,711.979 ns |  1.00 |    0.00 |   89.2361 |  44.4444 |       - |  560000 B |
|                  MapNew_toList | 10000 |   259,565.782 ns |   1,036.3900 ns |     865.4326 ns |   259,572.417 ns |  0.76 |    0.01 |   89.1012 |  44.4215 |       - |  560000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                  Map_enumerate | 10000 |   420,323.386 ns |   2,401.9179 ns |   2,246.7555 ns |   420,103.833 ns |  1.00 |    0.00 |  119.5833 |        - |       - |  752520 B |
|               MapNew_enumerate | 10000 |   213,685.515 ns |   2,570.3706 ns |   2,404.3262 ns |   214,443.159 ns |  0.51 |    0.01 |   63.5557 |        - |       - |  400000 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_toSeq_enum | 10000 |   599,914.885 ns |   6,481.7363 ns |   6,063.0201 ns |   596,473.988 ns |  1.00 |    0.00 |  158.3333 |        - |       - |  996752 B |
|              MapNew_toSeq_enum | 10000 |   583,721.975 ns |   4,936.4644 ns |   4,617.5719 ns |   582,869.039 ns |  0.97 |    0.01 |  101.8519 |        - |       - |  640193 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|            Map_containsKey_all | 10000 | 1,530,278.442 ns |  19,427.3944 ns |  18,172.3968 ns | 1,530,307.292 ns |  1.00 |    0.00 |         - |        - |       - |      22 B |
|         MapNew_containsKey_all | 10000 |   969,232.042 ns |  10,195.6091 ns |   9,536.9791 ns |   966,805.066 ns |  0.63 |    0.01 |         - |        - |       - |      14 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|    Map_containsKey_nonexisting | 10000 |       124.020 ns |       1.7360 ns |       1.6238 ns |       123.541 ns |  1.00 |    0.00 |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting | 10000 |        53.771 ns |       0.5391 ns |       0.5043 ns |        53.542 ns |  0.43 |    0.01 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                    Map_tryFind | 10000 |       118.166 ns |       1.4235 ns |       1.3316 ns |       118.015 ns |  1.00 |    0.00 |    0.0038 |        - |       - |      24 B |
|                 MapNew_tryFind | 10000 |        57.830 ns |       0.5061 ns |       0.4734 ns |        57.856 ns |  0.49 |    0.01 |    0.0038 |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|        Map_tryFind_nonexisting | 10000 |       134.740 ns |       1.7132 ns |       1.6025 ns |       134.179 ns |  1.00 |    0.00 |         - |        - |       - |         - |
|     MapNew_tryFind_nonexisting | 10000 |        52.984 ns |       0.3050 ns |       0.2382 ns |        53.032 ns |  0.39 |    0.00 |         - |        - |       - |         - |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                 Map_remove_all | 10000 | 6,902,369.152 ns |  50,939.0551 ns |  45,156.1455 ns | 6,902,996.562 ns |  1.00 |    0.00 |  968.7500 | 256.2500 |       - | 6086036 B |
|              MapNew_remove_all | 10000 | 5,103,790.256 ns |  51,918.2049 ns |  48,564.3211 ns | 5,084,536.538 ns |  0.74 |    0.01 |  889.4231 | 225.9615 |       - | 5591662 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                     Map_exists | 10000 |   134,247.499 ns |     374.9780 ns |     350.7547 ns |   134,304.794 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                  MapNew_exists | 10000 |    50,980.419 ns |     529.9120 ns |     495.6800 ns |    50,844.578 ns |  0.38 |    0.00 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                       Map_fold | 10000 |   129,198.330 ns |   1,866.9200 ns |   1,746.3182 ns |   128,697.819 ns |  1.00 |    0.00 |         - |        - |       - |      26 B |
|                    MapNew_fold | 10000 |    47,543.402 ns |     487.6922 ns |     432.3265 ns |    47,517.137 ns |  0.37 |    0.01 |         - |        - |       - |      24 B |
|                                |       |                  |                 |                 |                  |       |         |           |          |         |           |
|                   Map_foldBack | 10000 |   129,027.913 ns |   1,197.8775 ns |   1,061.8872 ns |   128,844.607 ns |  1.00 |    0.00 |         - |        - |       - |      24 B |
|                MapNew_foldBack | 10000 |    50,119.295 ns |     285.4796 ns |     253.0702 ns |    50,167.005 ns |  0.39 |    0.00 |         - |        - |       - |      24 B |
