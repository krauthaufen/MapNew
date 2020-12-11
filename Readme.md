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
* All operations tested so far are about `1.2x` - `2x` faster than for the current F# Map
* The `ofArray` does not only perform better but also allocates way less garbage (see GC stats in benchmark)

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.17763.1554 (1809/October2018Update/Redstone5)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
```

|                         Method | Count |              Mean |           Error |          StdDev |            Median |     Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|------------------------------- |------ |------------------:|----------------:|----------------:|------------------:|----------:|---------:|--------:|----------:|
|                    Map_ofArray |     1 |         38.293 ns |       1.1264 ns |       3.3213 ns |         39.509 ns |    0.0153 |        - |       - |      64 B |
|                 MapNew_ofArray |     1 |         29.225 ns |       0.8783 ns |       2.5758 ns |         29.999 ns |    0.0134 |        - |       - |      56 B |
|                    Map_toArray |     1 |         48.712 ns |       1.0717 ns |       1.9324 ns |         49.271 ns |    0.0210 |        - |       - |      88 B |
|                 MapNew_toArray |     1 |         24.099 ns |       0.5919 ns |       1.6301 ns |         24.536 ns |    0.0134 |        - |       - |      56 B |
|                  Map_enumerate |     1 |         90.890 ns |       1.8633 ns |       4.3920 ns |         91.987 ns |    0.0286 |        - |       - |     120 B |
|               MapNew_enumerate |     1 |         65.055 ns |       2.1084 ns |       6.2166 ns |         67.590 ns |    0.0095 |        - |       - |      40 B |
|            Map_containsKey_all |     1 |         10.549 ns |       0.2695 ns |       0.7861 ns |         10.796 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     1 |          8.528 ns |       0.2272 ns |       0.5311 ns |          8.684 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     1 |         10.179 ns |       0.2592 ns |       0.5467 ns |         10.310 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     1 |          3.970 ns |       0.1375 ns |       0.3879 ns |          4.105 ns |         - |        - |       - |         - |
|                 Map_remove_all |     1 |         21.898 ns |       0.5396 ns |       0.7565 ns |         22.068 ns |    0.0095 |        - |       - |      40 B |
|              MapNew_remove_all |     1 |         23.271 ns |       0.5757 ns |       0.7070 ns |         23.221 ns |    0.0076 |        - |       - |      32 B |
|                    Map_ofArray |     2 |         66.856 ns |       1.3839 ns |       1.2945 ns |         66.601 ns |    0.0267 |        - |       - |     112 B |
|                 MapNew_ofArray |     2 |         70.446 ns |       1.5234 ns |       4.0132 ns |         71.261 ns |    0.0248 |        - |       - |     104 B |
|                    Map_toArray |     2 |         89.504 ns |       1.8997 ns |       3.4738 ns |         90.036 ns |    0.0362 |        - |       - |     152 B |
|                 MapNew_toArray |     2 |         37.957 ns |       0.8248 ns |       0.8470 ns |         38.280 ns |    0.0210 |        - |       - |      88 B |
|                  Map_enumerate |     2 |        181.290 ns |       2.2773 ns |       2.0188 ns |        181.603 ns |    0.0572 |        - |       - |     240 B |
|               MapNew_enumerate |     2 |         94.986 ns |       2.4841 ns |       7.3243 ns |         97.582 ns |    0.0191 |        - |       - |      80 B |
|            Map_containsKey_all |     2 |         46.619 ns |       0.9912 ns |       1.2888 ns |         46.646 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     2 |         22.216 ns |       0.4693 ns |       0.4390 ns |         22.217 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     2 |         26.613 ns |       0.6033 ns |       1.7788 ns |         27.030 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     2 |          6.355 ns |       0.1846 ns |       0.1813 ns |          6.368 ns |         - |        - |       - |         - |
|                 Map_remove_all |     2 |         99.886 ns |       2.0999 ns |       3.4501 ns |        100.774 ns |    0.0248 |        - |       - |     104 B |
|              MapNew_remove_all |     2 |         69.669 ns |       1.4746 ns |       2.4637 ns |         70.230 ns |    0.0210 |        - |       - |      88 B |
|                    Map_ofArray |     3 |        162.720 ns |       3.9811 ns |      11.7385 ns |        167.577 ns |    0.0496 |        - |       - |     208 B |
|                 MapNew_ofArray |     3 |        100.715 ns |       2.1057 ns |       3.5182 ns |        101.281 ns |    0.0305 |        - |       - |     128 B |
|                    Map_toArray |     3 |        131.218 ns |       2.7376 ns |       6.1230 ns |        132.187 ns |    0.0515 |        - |       - |     216 B |
|                 MapNew_toArray |     3 |         51.867 ns |       1.1462 ns |       2.0668 ns |         51.693 ns |    0.0286 |        - |       - |     120 B |
|                  Map_enumerate |     3 |        288.346 ns |       5.0114 ns |       4.6876 ns |        288.892 ns |    0.0861 |        - |       - |     360 B |
|               MapNew_enumerate |     3 |        125.681 ns |       2.5641 ns |       7.3568 ns |        126.903 ns |    0.0286 |        - |       - |     120 B |
|            Map_containsKey_all |     3 |         93.145 ns |       2.4025 ns |       7.0840 ns |         96.072 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     3 |         39.746 ns |       0.8436 ns |       1.6454 ns |         40.034 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     3 |         44.088 ns |       0.9129 ns |       1.2798 ns |         44.112 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     3 |          8.581 ns |       0.2427 ns |       0.7119 ns |          8.828 ns |         - |        - |       - |         - |
|                 Map_remove_all |     3 |        208.672 ns |       4.2389 ns |       7.7510 ns |        211.378 ns |    0.0458 |        - |       - |     192 B |
|              MapNew_remove_all |     3 |        137.698 ns |       3.8427 ns |      11.0870 ns |        141.335 ns |    0.0401 |        - |       - |     168 B |
|                    Map_ofArray |     4 |        246.389 ns |       5.2346 ns |      15.4345 ns |        250.708 ns |    0.0668 |        - |       - |     280 B |
|                 MapNew_ofArray |     4 |        161.875 ns |       3.3324 ns |       7.0291 ns |        164.135 ns |    0.0420 |        - |       - |     176 B |
|                    Map_toArray |     4 |        147.901 ns |       3.3356 ns |       9.7828 ns |        151.400 ns |    0.0668 |        - |       - |     280 B |
|                 MapNew_toArray |     4 |         65.098 ns |       1.4233 ns |       2.9709 ns |         65.309 ns |    0.0362 |        - |       - |     152 B |
|                  Map_enumerate |     4 |        314.906 ns |       6.2816 ns |      10.6666 ns |        317.563 ns |    0.0858 |        - |       - |     360 B |
|               MapNew_enumerate |     4 |        145.005 ns |       3.6859 ns |      10.8102 ns |        148.846 ns |    0.0381 |        - |       - |     160 B |
|            Map_containsKey_all |     4 |        139.873 ns |       2.8378 ns |       5.4674 ns |        140.258 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     4 |         52.106 ns |       1.0897 ns |       2.5037 ns |         52.868 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     4 |         42.794 ns |       0.9007 ns |       1.9389 ns |         43.184 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     4 |         15.458 ns |       0.3682 ns |       0.9239 ns |         15.715 ns |         - |        - |       - |         - |
|                 Map_remove_all |     4 |        230.826 ns |       6.7471 ns |      19.8941 ns |        238.400 ns |    0.0553 |        - |       - |     232 B |
|              MapNew_remove_all |     4 |        308.730 ns |       3.8526 ns |       3.4152 ns |        309.744 ns |    0.0648 |        - |       - |     272 B |
|                    Map_ofArray |     5 |        467.451 ns |       9.4088 ns |      26.6912 ns |        475.076 ns |    0.1011 |        - |       - |     424 B |
|                 MapNew_ofArray |     5 |        339.002 ns |       6.6825 ns |       9.5839 ns |        341.026 ns |    0.0763 |        - |       - |     320 B |
|                    Map_toArray |     5 |        198.143 ns |       4.0321 ns |       8.1451 ns |        200.234 ns |    0.0823 |        - |       - |     344 B |
|                 MapNew_toArray |     5 |        111.529 ns |       2.3200 ns |       5.6472 ns |        112.194 ns |    0.0439 |        - |       - |     184 B |
|                  Map_enumerate |     5 |        427.976 ns |       8.4810 ns |      16.1360 ns |        431.498 ns |    0.1144 |        - |       - |     480 B |
|               MapNew_enumerate |     5 |        165.185 ns |       3.7228 ns |      10.8596 ns |        168.272 ns |    0.0477 |        - |       - |     200 B |
|            Map_containsKey_all |     5 |        182.032 ns |       3.6615 ns |      10.1460 ns |        185.279 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     5 |         71.645 ns |       1.4776 ns |       3.8926 ns |         72.367 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     5 |         42.314 ns |       0.8822 ns |       1.6131 ns |         42.583 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     5 |         47.826 ns |       1.0157 ns |       2.2507 ns |         48.209 ns |         - |        - |       - |         - |
|                 Map_remove_all |     5 |        408.966 ns |       8.2876 ns |      22.6872 ns |        415.585 ns |    0.1011 |        - |       - |     424 B |
|              MapNew_remove_all |     5 |        521.923 ns |      10.4516 ns |      29.8189 ns |        530.388 ns |    0.0896 |        - |       - |     376 B |
|                    Map_ofArray |    10 |      1,750.057 ns |      34.0162 ns |      33.4085 ns |      1,755.682 ns |    0.3014 |        - |       - |    1264 B |
|                 MapNew_ofArray |    10 |      1,640.030 ns |      32.8542 ns |      88.2606 ns |      1,663.681 ns |    0.2251 |        - |       - |     944 B |
|                    Map_toArray |    10 |        366.609 ns |      10.2643 ns |      30.2646 ns |        378.634 ns |    0.1583 |        - |       - |     664 B |
|                 MapNew_toArray |    10 |        177.251 ns |       3.6581 ns |      10.4367 ns |        180.234 ns |    0.0823 |        - |       - |     344 B |
|                  Map_enumerate |    10 |        811.071 ns |      16.0409 ns |      25.9031 ns |        816.415 ns |    0.2003 |        - |       - |     840 B |
|               MapNew_enumerate |    10 |        406.119 ns |       8.1700 ns |      17.9333 ns |        413.707 ns |    0.0954 |        - |       - |     400 B |
|            Map_containsKey_all |    10 |        584.277 ns |      11.6892 ns |      29.9639 ns |        589.816 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    10 |        257.331 ns |       8.9384 ns |      26.0736 ns |        266.927 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    10 |         42.747 ns |       0.9077 ns |       2.5304 ns |         43.582 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    10 |         46.515 ns |       0.9787 ns |       2.3069 ns |         47.071 ns |         - |        - |       - |         - |
|                 Map_remove_all |    10 |      1,649.841 ns |      32.9315 ns |      56.8053 ns |      1,660.870 ns |    0.3014 |        - |       - |    1264 B |
|              MapNew_remove_all |    10 |      1,490.971 ns |      29.7824 ns |      59.4786 ns |      1,503.458 ns |    0.2365 |        - |       - |     992 B |
|                    Map_ofArray |    20 |      5,624.919 ns |     159.1141 ns |     469.1516 ns |      5,803.922 ns |    0.8698 |        - |       - |    3664 B |
|                 MapNew_ofArray |    20 |      3,914.002 ns |      78.2760 ns |     124.1540 ns |      3,941.897 ns |    0.4005 |        - |       - |    1688 B |
|                    Map_toArray |    20 |        790.713 ns |      15.0823 ns |      15.4884 ns |        789.331 ns |    0.3109 |        - |       - |    1304 B |
|                 MapNew_toArray |    20 |        397.755 ns |       8.0562 ns |      11.8087 ns |        398.718 ns |    0.1583 |        - |       - |     664 B |
|                  Map_enumerate |    20 |      1,530.780 ns |      30.1890 ns |      61.6681 ns |      1,548.476 ns |    0.3719 |        - |       - |    1560 B |
|               MapNew_enumerate |    20 |        802.326 ns |      15.9108 ns |      35.5867 ns |        811.378 ns |    0.1907 |        - |       - |     800 B |
|            Map_containsKey_all |    20 |      1,520.510 ns |      37.0385 ns |     109.2088 ns |      1,554.713 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    20 |        723.515 ns |      14.1325 ns |      19.3447 ns |        729.483 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    20 |         59.080 ns |       1.2319 ns |       3.2238 ns |         59.998 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    20 |         50.392 ns |       1.1010 ns |       3.2463 ns |         51.572 ns |         - |        - |       - |         - |
|                 Map_remove_all |    20 |      4,651.681 ns |      92.2251 ns |      81.7551 ns |      4,670.834 ns |    0.8011 |        - |       - |    3352 B |
|              MapNew_remove_all |    20 |      4,577.043 ns |      80.8544 ns |      71.6754 ns |      4,590.497 ns |    0.6638 |        - |       - |    2800 B |
|                    Map_ofArray |    30 |      9,850.576 ns |     194.4713 ns |     291.0754 ns |      9,921.317 ns |    1.4038 |        - |       - |    5920 B |
|                 MapNew_ofArray |    30 |      7,448.846 ns |     191.7848 ns |     565.4818 ns |      7,640.097 ns |    0.6866 |        - |       - |    2888 B |
|                    Map_toArray |    30 |      1,153.802 ns |      23.0920 ns |      57.0778 ns |      1,165.231 ns |    0.4635 |        - |       - |    1944 B |
|                 MapNew_toArray |    30 |        726.108 ns |      14.5992 ns |      35.8121 ns |        731.455 ns |    0.2346 |        - |       - |     984 B |
|                  Map_enumerate |    30 |      2,221.329 ns |      44.3098 ns |     124.2493 ns |      2,252.949 ns |    0.5417 |        - |       - |    2280 B |
|               MapNew_enumerate |    30 |      1,181.107 ns |      22.9428 ns |      35.7192 ns |      1,186.339 ns |    0.2861 |        - |       - |    1200 B |
|            Map_containsKey_all |    30 |      2,858.018 ns |      51.4613 ns |      55.0630 ns |      2,871.124 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    30 |      1,319.724 ns |      33.4994 ns |      98.7737 ns |      1,356.213 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    30 |         58.680 ns |       1.2169 ns |       2.8203 ns |         59.657 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    30 |         57.787 ns |       1.1299 ns |       1.5467 ns |         57.921 ns |         - |        - |       - |         - |
|                 Map_remove_all |    30 |      8,427.553 ns |     169.4508 ns |     273.6315 ns |      8,514.053 ns |    1.4191 |        - |       - |    5944 B |
|              MapNew_remove_all |    30 |      8,485.846 ns |     167.6542 ns |     378.4234 ns |      8,595.416 ns |    1.1902 |        - |       - |    5040 B |
|                    Map_ofArray |    40 |     13,636.855 ns |     267.3842 ns |     374.8353 ns |     13,650.661 ns |    1.9226 |        - |       - |    8056 B |
|                 MapNew_ofArray |    40 |      9,515.578 ns |     187.3945 ns |     347.3478 ns |      9,593.642 ns |    0.9460 |        - |       - |    3968 B |
|                    Map_toArray |    40 |      1,617.101 ns |      31.5708 ns |      38.7717 ns |      1,621.082 ns |    0.6161 |        - |       - |    2584 B |
|                 MapNew_toArray |    40 |        998.317 ns |      19.1410 ns |      17.9045 ns |        994.756 ns |    0.3109 |        - |       - |    1304 B |
|                  Map_enumerate |    40 |      2,914.346 ns |      57.8400 ns |     162.1895 ns |      2,975.913 ns |    0.7172 |        - |       - |    3000 B |
|               MapNew_enumerate |    40 |      1,491.062 ns |      32.9149 ns |      96.5337 ns |      1,529.208 ns |    0.3815 |        - |       - |    1600 B |
|            Map_containsKey_all |    40 |      3,817.906 ns |      75.7488 ns |     164.6717 ns |      3,858.068 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    40 |      1,963.001 ns |      39.2172 ns |      98.3883 ns |      1,995.706 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    40 |         95.643 ns |       2.1967 ns |       6.4771 ns |         97.936 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    40 |         56.576 ns |       1.1792 ns |       3.1270 ns |         57.388 ns |         - |        - |       - |         - |
|                 Map_remove_all |    40 |     12,289.303 ns |     245.8497 ns |     529.2175 ns |     12,391.245 ns |    1.9684 |        - |       - |    8256 B |
|              MapNew_remove_all |    40 |     12,212.431 ns |     243.7421 ns |     602.4700 ns |     12,446.034 ns |    1.7395 |        - |       - |    7304 B |
|                    Map_ofArray |    50 |     19,570.608 ns |     381.3246 ns |     356.6913 ns |     19,608.698 ns |    2.7466 |        - |       - |   11488 B |
|                 MapNew_ofArray |    50 |     14,409.607 ns |     284.1927 ns |     497.7406 ns |     14,477.185 ns |    1.3733 |        - |       - |    5792 B |
|                    Map_toArray |    50 |      2,119.862 ns |      41.9803 ns |      80.8818 ns |      2,135.234 ns |    0.7706 |        - |       - |    3224 B |
|                 MapNew_toArray |    50 |      1,293.433 ns |      25.4844 ns |      38.1439 ns |      1,295.882 ns |    0.3872 |        - |       - |    1624 B |
|                  Map_enumerate |    50 |      3,745.603 ns |      74.0809 ns |     156.2618 ns |      3,776.044 ns |    0.8850 |        - |       - |    3720 B |
|               MapNew_enumerate |    50 |      1,887.927 ns |      45.3402 ns |     132.9750 ns |      1,916.661 ns |    0.4768 |        - |       - |    2000 B |
|            Map_containsKey_all |    50 |      5,106.015 ns |     102.1602 ns |     294.7556 ns |      5,196.136 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    50 |      2,714.706 ns |      54.0714 ns |     112.8671 ns |      2,753.822 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    50 |         99.285 ns |       2.0162 ns |       5.3466 ns |        100.773 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    50 |         56.026 ns |       1.1791 ns |       2.5128 ns |         56.895 ns |         - |        - |       - |         - |
|                 Map_remove_all |    50 |     17,254.507 ns |     342.4676 ns |     707.2550 ns |     17,417.146 ns |    2.7771 |        - |       - |   11728 B |
|              MapNew_remove_all |    50 |     16,649.948 ns |     341.8628 ns |     997.2299 ns |     16,882.307 ns |    2.3193 |        - |       - |    9808 B |
|                    Map_ofArray |   100 |     47,604.240 ns |     919.6331 ns |   1,376.4636 ns |     47,886.746 ns |    6.5918 |        - |       - |   27736 B |
|                 MapNew_ofArray |   100 |     33,940.764 ns |     646.4576 ns |     718.5359 ns |     33,996.295 ns |    2.6855 |        - |       - |   11384 B |
|                    Map_toArray |   100 |      3,891.012 ns |     101.3773 ns |     297.3220 ns |      3,973.853 ns |    1.5335 |        - |       - |    6424 B |
|                 MapNew_toArray |   100 |      2,370.507 ns |      46.9774 ns |      88.2349 ns |      2,373.334 ns |    0.7706 |        - |       - |    3224 B |
|                  Map_enumerate |   100 |      6,984.717 ns |     138.3660 ns |     273.1209 ns |      7,054.285 ns |    1.7166 |        - |       - |    7200 B |
|               MapNew_enumerate |   100 |      3,588.945 ns |      71.1211 ns |     170.4016 ns |      3,645.361 ns |    0.9537 |        - |       - |    4000 B |
|            Map_containsKey_all |   100 |     12,156.518 ns |     273.6059 ns |     806.7331 ns |     12,432.977 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   100 |      6,726.174 ns |     173.1675 ns |     510.5883 ns |      6,929.941 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   100 |         97.631 ns |       3.7667 ns |      11.1063 ns |        100.763 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   100 |         61.996 ns |       1.7451 ns |       4.9505 ns |         64.133 ns |         - |        - |       - |         - |
|                 Map_remove_all |   100 |     42,221.546 ns |     860.5962 ns |   2,537.4872 ns |     43,175.348 ns |    6.5918 |        - |       - |   27704 B |
|              MapNew_remove_all |   100 |     42,949.103 ns |     855.7581 ns |   1,983.3496 ns |     43,484.360 ns |    5.8594 |        - |       - |   24512 B |
|                    Map_ofArray |   200 |    122,149.891 ns |   2,405.2864 ns |   4,634.1702 ns |    122,870.972 ns |   15.8691 |   0.2441 |       - |   67096 B |
|                 MapNew_ofArray |   200 |     81,134.833 ns |   1,596.1737 ns |   3,187.7314 ns |     81,907.556 ns |    6.7139 |        - |       - |   28424 B |
|                    Map_toArray |   200 |      8,285.086 ns |     164.8151 ns |     368.6326 ns |      8,352.173 ns |    3.0518 |   0.0153 |       - |   12824 B |
|                 MapNew_toArray |   200 |      5,058.131 ns |     101.2055 ns |     261.2436 ns |      5,123.740 ns |    1.5335 |        - |       - |    6424 B |
|                  Map_enumerate |   200 |     14,599.095 ns |     348.4890 ns |     994.2586 ns |     14,833.147 ns |    3.5400 |        - |       - |   14880 B |
|               MapNew_enumerate |   200 |      7,467.004 ns |     199.6955 ns |     579.3527 ns |      7,670.025 ns |    1.9073 |        - |       - |    8000 B |
|            Map_containsKey_all |   200 |     29,210.495 ns |     493.3195 ns |     461.4514 ns |     29,241.504 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   200 |     17,100.741 ns |     341.8675 ns |     743.1918 ns |     17,252.632 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   200 |        131.889 ns |       2.6618 ns |       5.4373 ns |        133.425 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   200 |         76.916 ns |       1.8554 ns |       5.4122 ns |         78.575 ns |         - |        - |       - |         - |
|                 Map_remove_all |   200 |    100,995.063 ns |   2,482.0089 ns |   7,318.2592 ns |    104,250.391 ns |   15.8691 |        - |       - |   66424 B |
|              MapNew_remove_all |   200 |     93,554.894 ns |   1,867.7916 ns |   4,177.5836 ns |     94,812.714 ns |   13.9160 |        - |       - |   58528 B |
|                    Map_ofArray |   300 |    200,323.985 ns |   3,981.7403 ns |   8,571.1160 ns |    202,835.669 ns |   25.8789 |        - |       - |  109072 B |
|                 MapNew_ofArray |   300 |    125,389.588 ns |   3,719.1551 ns |  10,966.0126 ns |    129,234.094 ns |   10.4980 |        - |       - |   44816 B |
|                    Map_toArray |   300 |     12,680.127 ns |     248.5967 ns |     408.4515 ns |     12,725.108 ns |    4.5929 |        - |       - |   19224 B |
|                 MapNew_toArray |   300 |      7,727.137 ns |     152.4432 ns |     304.4455 ns |      7,810.555 ns |    2.2964 |   0.0076 |       - |    9624 B |
|                  Map_enumerate |   300 |     22,133.488 ns |     440.8335 ns |   1,047.6875 ns |     22,378.531 ns |    5.2795 |        - |       - |   22200 B |
|               MapNew_enumerate |   300 |     11,628.481 ns |     231.5140 ns |     417.4676 ns |     11,777.791 ns |    2.8687 |        - |       - |   12000 B |
|            Map_containsKey_all |   300 |     46,751.999 ns |     934.7511 ns |   2,636.4821 ns |     47,456.519 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   300 |     27,494.231 ns |     545.0385 ns |   1,230.2427 ns |     27,865.140 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   300 |        127.327 ns |       2.5753 ns |       6.6015 ns |        129.970 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   300 |         70.838 ns |       1.7572 ns |       5.1259 ns |         72.209 ns |         - |        - |       - |         - |
|                 Map_remove_all |   300 |    170,044.813 ns |   3,385.2365 ns |   9,928.3126 ns |    173,339.380 ns |   25.6348 |        - |       - |  107800 B |
|              MapNew_remove_all |   300 |    150,038.691 ns |   2,994.7468 ns |   7,941.6436 ns |    151,608.240 ns |   22.4609 |        - |       - |   94104 B |
|                    Map_ofArray |   400 |    283,576.081 ns |   3,205.1151 ns |   2,998.0667 ns |    283,581.641 ns |   36.1328 |   0.4883 |       - |  152153 B |
|                 MapNew_ofArray |   400 |    171,367.523 ns |   5,159.9258 ns |  15,214.1578 ns |    177,787.585 ns |   11.9629 |   0.4883 |       - |   50624 B |
|                    Map_toArray |   400 |     16,923.789 ns |     336.0024 ns |     605.8816 ns |     17,074.547 ns |    6.1188 |        - |       - |   25624 B |
|                 MapNew_toArray |   400 |     10,584.749 ns |     209.9136 ns |     502.9396 ns |     10,741.261 ns |    3.0518 |   0.0153 |       - |   12824 B |
|                  Map_enumerate |   400 |     29,625.539 ns |     780.0179 ns |   2,299.9003 ns |     30,511.292 ns |    7.2021 |        - |       - |   30360 B |
|               MapNew_enumerate |   400 |     15,429.570 ns |     303.4601 ns |     653.2299 ns |     15,578.220 ns |    3.8147 |        - |       - |   16000 B |
|            Map_containsKey_all |   400 |     65,421.247 ns |   1,258.8144 ns |   1,498.5290 ns |     65,555.408 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   400 |     38,496.058 ns |     917.9474 ns |   2,692.1809 ns |     39,433.276 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   400 |        129.323 ns |       2.6778 ns |       7.8536 ns |        131.751 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   400 |         62.008 ns |       1.2940 ns |       2.2664 ns |         62.294 ns |         - |        - |       - |         - |
|                 Map_remove_all |   400 |    247,972.021 ns |   3,693.2233 ns |   3,084.0087 ns |    247,999.512 ns |   36.1328 |        - |       - |  151545 B |
|              MapNew_remove_all |   400 |    222,522.063 ns |   4,367.2033 ns |   8,620.4332 ns |    224,720.752 ns |   32.4707 |        - |       - |  136376 B |
|                    Map_ofArray |   500 |    382,048.100 ns |   7,639.3132 ns |  13,775.2574 ns |    385,086.182 ns |   47.8516 |        - |       - |  201496 B |
|                 MapNew_ofArray |   500 |    234,193.073 ns |   4,669.4736 ns |  12,624.1934 ns |    238,490.723 ns |   16.1133 |   0.2441 |       - |   68192 B |
|                    Map_toArray |   500 |     21,244.699 ns |     423.7230 ns |     965.0317 ns |     21,447.203 ns |    7.6294 |   0.0610 |       - |   32024 B |
|                 MapNew_toArray |   500 |     12,790.316 ns |     262.9226 ns |     771.1064 ns |     13,086.424 ns |    3.8300 |        - |       - |   16024 B |
|                  Map_enumerate |   500 |     37,382.276 ns |     774.1501 ns |   2,282.5992 ns |     38,080.673 ns |    9.1553 |        - |       - |   38520 B |
|               MapNew_enumerate |   500 |     19,145.954 ns |     486.1556 ns |   1,433.4408 ns |     19,673.340 ns |    4.7607 |        - |       - |   20000 B |
|            Map_containsKey_all |   500 |     84,790.923 ns |   1,864.4458 ns |   5,497.3604 ns |     86,580.524 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   500 |     50,777.311 ns |     999.6741 ns |   1,926.0327 ns |     51,087.540 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   500 |        181.863 ns |       3.6631 ns |       5.0141 ns |        182.464 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   500 |         72.460 ns |       1.6041 ns |       4.7046 ns |         73.901 ns |         - |        - |       - |         - |
|                 Map_remove_all |   500 |    332,005.459 ns |   4,223.7449 ns |   3,950.8936 ns |    332,778.125 ns |   47.3633 |        - |       - |  199076 B |
|              MapNew_remove_all |   500 |    268,230.359 ns |   8,436.7421 ns |  24,875.9243 ns |    279,395.752 ns |   41.9922 |        - |       - |  177448 B |
|                    Map_ofArray |  1000 |    875,097.871 ns |  17,354.2960 ns |  50,071.1120 ns |    887,585.986 ns |  105.4688 |  20.5078 |       - |  441592 B |
|                 MapNew_ofArray |  1000 |    532,314.032 ns |  10,538.4395 ns |  28,129.2285 ns |    539,526.562 ns |   33.2031 |   6.8359 |       - |  142592 B |
|                    Map_toArray |  1000 |     45,617.207 ns |     904.8904 ns |   1,967.1572 ns |     45,954.385 ns |   15.2588 |   2.5024 |       - |   64024 B |
|                 MapNew_toArray |  1000 |     27,740.516 ns |     549.2930 ns |   1,045.0869 ns |     28,109.564 ns |    7.6294 |        - |       - |   32024 B |
|                  Map_enumerate |  1000 |     73,796.929 ns |   1,647.2449 ns |   4,856.9387 ns |     75,444.238 ns |   17.9443 |        - |       - |   75480 B |
|               MapNew_enumerate |  1000 |     38,876.853 ns |     766.6702 ns |   1,866.1813 ns |     39,243.811 ns |    9.5215 |        - |       - |   40000 B |
|            Map_containsKey_all |  1000 |    191,804.698 ns |   3,821.5406 ns |   8,547.4232 ns |    194,503.601 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  1000 |    115,542.904 ns |   2,306.7021 ns |   5,995.4253 ns |    117,269.897 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  1000 |        147.464 ns |       2.9950 ns |       6.5109 ns |        149.316 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  1000 |         81.663 ns |       2.1943 ns |       6.4701 ns |         83.985 ns |         - |        - |       - |         - |
|                 Map_remove_all |  1000 |    764,320.160 ns |  15,086.6196 ns |  36,146.5771 ns |    769,421.338 ns |  106.4453 |        - |       - |  445512 B |
|              MapNew_remove_all |  1000 |    620,465.644 ns |  12,359.8917 ns |  29,613.5111 ns |    629,744.580 ns |   95.7031 |   0.9766 |       - |  403160 B |
|                    Map_ofArray |  2000 |  1,999,868.119 ns |  71,312.8189 ns | 209,148.1532 ns |  2,073,672.656 ns |  234.3750 |  11.7188 |       - |  995656 B |
|                 MapNew_ofArray |  2000 |  1,146,646.570 ns |  32,394.8439 ns |  95,516.9281 ns |  1,180,155.078 ns |   70.3125 |  23.4375 |       - |  301136 B |
|                    Map_toArray |  2000 |     95,554.260 ns |   2,436.3501 ns |   7,183.6332 ns |     98,577.527 ns |   30.5176 |        - |       - |  128024 B |
|                 MapNew_toArray |  2000 |     56,309.557 ns |   1,189.0080 ns |   3,505.8169 ns |     57,817.886 ns |   15.2588 |   0.1221 |       - |   64024 B |
|                  Map_enumerate |  2000 |    146,280.549 ns |   4,332.1652 ns |  12,637.1291 ns |    151,307.727 ns |   35.8887 |        - |       - |  150960 B |
|               MapNew_enumerate |  2000 |     78,135.755 ns |   2,151.3666 ns |   6,343.3530 ns |     80,320.789 ns |   19.0430 |        - |       - |   80000 B |
|            Map_containsKey_all |  2000 |    418,158.519 ns |   9,879.4889 ns |  29,129.8960 ns |    430,314.966 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  2000 |    255,670.868 ns |   5,273.2743 ns |  15,548.3682 ns |    261,923.340 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  2000 |        173.549 ns |       4.3360 ns |      12.7168 ns |        177.543 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  2000 |         85.997 ns |       1.9870 ns |       5.8587 ns |         87.993 ns |         - |        - |       - |         - |
|                 Map_remove_all |  2000 |  1,741,052.605 ns |  34,420.0852 ns |  76,272.5312 ns |  1,762,387.109 ns |  234.3750 |        - |       - |  984704 B |
|              MapNew_remove_all |  2000 |  1,388,488.229 ns |  31,469.1005 ns |  92,787.3528 ns |  1,425,690.723 ns |  214.8438 |   7.8125 |       - |  903568 B |
|                    Map_ofArray |  3000 |  3,442,917.178 ns |  67,491.4131 ns |  80,343.7263 ns |  3,457,205.273 ns |  371.0938 | 121.0938 |       - | 1576480 B |
|                 MapNew_ofArray |  3000 |  1,838,139.744 ns |  41,675.1311 ns | 122,880.0643 ns |  1,887,438.477 ns |   97.6563 |  31.2500 |       - |  459248 B |
|                    Map_toArray |  3000 |    157,725.864 ns |   3,144.2727 ns |   8,059.9729 ns |    159,740.479 ns |   44.4336 |  11.9629 |       - |  192024 B |
|                 MapNew_toArray |  3000 |     91,140.085 ns |   1,814.0952 ns |   4,650.2196 ns |     92,385.144 ns |   22.8271 |   7.5684 |       - |   96024 B |
|                  Map_enumerate |  3000 |    228,720.081 ns |   4,322.1100 ns |   4,438.4887 ns |    229,088.770 ns |   53.9551 |        - |       - |  225722 B |
|               MapNew_enumerate |  3000 |    117,011.233 ns |   2,446.9377 ns |   7,214.8509 ns |    119,753.125 ns |   28.6865 |        - |       - |  120000 B |
|            Map_containsKey_all |  3000 |    662,012.278 ns |  17,795.3975 ns |  52,190.8203 ns |    680,010.449 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  3000 |    411,477.955 ns |   8,271.0782 ns |  24,387.4607 ns |    418,512.476 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  3000 |        162.489 ns |       3.1800 ns |       4.5607 ns |        162.631 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  3000 |         85.511 ns |       1.7384 ns |       3.5117 ns |         86.575 ns |         - |        - |       - |         - |
|                 Map_remove_all |  3000 |  2,726,057.483 ns |  69,608.0881 ns | 204,148.4729 ns |  2,789,098.242 ns |  375.0000 |   3.9063 |       - | 1571648 B |
|              MapNew_remove_all |  3000 |  2,287,310.836 ns |  45,647.1337 ns |  63,990.9063 ns |  2,291,538.281 ns |  339.8438 |   3.9063 |       - | 1442640 B |
|                    Map_ofArray |  4000 |  4,995,734.786 ns |  99,482.8860 ns | 253,215.7084 ns |  5,067,810.547 ns |  437.5000 | 171.8750 |       - | 2184904 B |
|                 MapNew_ofArray |  4000 |  2,642,970.754 ns |  55,145.5797 ns | 161,732.4391 ns |  2,697,683.984 ns |  125.0000 |  46.8750 |       - |  639032 B |
|                    Map_toArray |  4000 |    228,677.419 ns |   6,795.2716 ns |  20,036.0118 ns |    236,141.748 ns |   51.7578 |  19.0430 |       - |  256024 B |
|                 MapNew_toArray |  4000 |    123,898.770 ns |   2,339.4345 ns |   2,297.6375 ns |    124,372.937 ns |   29.0527 |   7.8125 |       - |  128024 B |
|                  Map_enumerate |  4000 |    307,237.981 ns |   4,521.6145 ns |   4,008.2935 ns |    307,570.825 ns |   71.7773 |        - |       - |  301681 B |
|               MapNew_enumerate |  4000 |    155,776.603 ns |   4,522.3246 ns |  13,191.8330 ns |    161,145.105 ns |   38.0859 |        - |       - |  160000 B |
|            Map_containsKey_all |  4000 |    952,925.563 ns |  18,899.6225 ns |  34,559.0284 ns |    962,732.324 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  4000 |    591,196.666 ns |   8,979.3683 ns |   7,959.9761 ns |    590,210.303 ns |         - |        - |       - |       1 B |
|    Map_containsKey_nonexisting |  4000 |        195.090 ns |       3.3735 ns |       3.1556 ns |        194.575 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  4000 |         89.051 ns |       1.8292 ns |       4.5891 ns |         90.191 ns |         - |        - |       - |         - |
|                 Map_remove_all |  4000 |  3,979,149.696 ns |  77,781.8037 ns | 129,955.9207 ns |  3,993,576.562 ns |  515.6250 |   7.8125 |       - | 2174376 B |
|              MapNew_remove_all |  4000 |  3,184,212.905 ns |  62,475.2015 ns | 130,409.0107 ns |  3,224,619.922 ns |  476.5625 |  66.4063 |       - | 2014112 B |
|                    Map_ofArray |  5000 |  6,333,466.312 ns | 188,513.5478 ns | 555,836.4490 ns |  6,603,030.078 ns |  500.0000 | 218.7500 | 15.6250 | 2805088 B |
|                 MapNew_ofArray |  5000 |  3,524,014.009 ns |  70,311.6169 ns | 192,476.9165 ns |  3,588,938.281 ns |  160.1563 |  70.3125 |       - |  840752 B |
|                    Map_toArray |  5000 |    292,586.762 ns |   5,790.5059 ns |  14,527.2471 ns |    296,218.896 ns |   70.8008 |  16.6016 |       - |  320024 B |
|                 MapNew_toArray |  5000 |    169,680.348 ns |   3,343.1316 ns |   6,904.1465 ns |    170,336.218 ns |   32.2266 |  12.2070 |       - |  160024 B |
|                  Map_enumerate |  5000 |    377,725.639 ns |   7,546.2554 ns |  18,368.6286 ns |    384,372.559 ns |   89.8438 |        - |       - |  377040 B |
|               MapNew_enumerate |  5000 |    198,301.197 ns |   3,952.2381 ns |  11,082.4994 ns |    201,424.780 ns |   47.6074 |        - |       - |  200000 B |
|            Map_containsKey_all |  5000 |  1,203,191.097 ns |  23,961.6922 ns |  56,009.6831 ns |  1,221,786.133 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  5000 |    745,137.042 ns |  14,720.1712 ns |  36,384.6160 ns |    758,236.377 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  5000 |        193.658 ns |       3.8889 ns |       8.0312 ns |        195.627 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  5000 |         93.985 ns |       1.7302 ns |       1.6185 ns |         94.110 ns |         - |        - |       - |         - |
|                 Map_remove_all |  5000 |  4,951,575.741 ns | 110,533.8260 ns | 322,432.3587 ns |  5,071,184.766 ns |  664.0625 |  78.1250 |       - | 2788008 B |
|              MapNew_remove_all |  5000 |  4,146,121.267 ns |  82,431.5374 ns | 189,400.1654 ns |  4,182,662.500 ns |  609.3750 |  54.6875 |       - | 2568928 B |
|                    Map_ofArray | 10000 | 15,950,267.548 ns |  83,243.6564 ns |  69,512.2232 ns | 15,969,281.250 ns |  968.7500 | 250.0000 | 31.2500 | 6100570 B |
|                 MapNew_ofArray | 10000 |  8,488,838.161 ns | 168,438.3660 ns | 230,560.3366 ns |  8,513,071.875 ns |  281.2500 | 140.6250 |       - | 1737272 B |
|                    Map_toArray | 10000 |    685,301.009 ns |  13,666.4830 ns |  12,783.6367 ns |    684,761.719 ns |  105.4688 |  52.7344 |       - |  640026 B |
|                 MapNew_toArray | 10000 |    364,418.853 ns |   7,250.4733 ns |  20,686.0084 ns |    371,746.460 ns |   50.7813 |  25.3906 |       - |  320024 B |
|                  Map_enumerate | 10000 |    765,064.548 ns |  18,013.1202 ns |  53,112.0912 ns |    784,722.705 ns |  180.6641 |        - |       - |  755880 B |
|               MapNew_enumerate | 10000 |    402,789.579 ns |  15,587.7858 ns |  43,709.8226 ns |    420,343.311 ns |   95.2148 |        - |       - |  400000 B |
|            Map_containsKey_all | 10000 |  2,643,086.970 ns |  40,749.9944 ns |  36,123.8086 ns |  2,641,887.109 ns |         - |        - |       - |       2 B |
|         MapNew_containsKey_all | 10000 |  1,721,518.888 ns |  34,296.4534 ns |  35,219.9325 ns |  1,724,712.891 ns |         - |        - |       - |      13 B |
|    Map_containsKey_nonexisting | 10000 |        189.876 ns |       3.8347 ns |      11.0023 ns |        193.203 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting | 10000 |         94.099 ns |       1.1923 ns |       1.0569 ns |         94.293 ns |         - |        - |       - |         - |
|                 Map_remove_all | 10000 | 11,332,151.562 ns | 254,211.3978 ns | 749,548.0420 ns | 11,599,707.031 ns | 1453.1250 | 453.1250 |       - | 6082984 B |
|              MapNew_remove_all | 10000 |  9,628,755.389 ns | 188,307.5911 ns | 329,805.5557 ns |  9,672,977.344 ns | 1343.7500 | 234.3750 |       - | 5668568 B |