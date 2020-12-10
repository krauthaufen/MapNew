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

#### Bottom Line
* All operations tested so far are about `~1.8x` faster than for the current F# Map
* The `ofArray` does not only perform better but also allocates way less garbage (see GC stats in benchmark)

```
// * Summary *
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.17763.1554 (1809/October2018Update/Redstone5)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
```

|                         Method | Count |              Mean |           Error |          StdDev |            Median |     Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|------------------------------- |------ |------------------:|----------------:|----------------:|------------------:|----------:|---------:|--------:|----------:|
|                    Map_ofArray |     1 |         39.875 ns |       0.9068 ns |       2.6017 ns |         40.831 ns |    0.0153 |        - |       - |      64 B |
|                 MapNew_ofArray |     1 |         28.759 ns |       0.6728 ns |       1.1240 ns |         28.818 ns |    0.0134 |        - |       - |      56 B |
|                    Map_toArray |     1 |         53.378 ns |       1.1664 ns |       2.5108 ns |         53.799 ns |    0.0210 |        - |       - |      88 B |
|                 MapNew_toArray |     1 |         24.887 ns |       0.6137 ns |       1.6592 ns |         25.104 ns |    0.0134 |        - |       - |      56 B |
|                  Map_enumerate |     1 |         98.520 ns |       2.0298 ns |       4.0538 ns |         99.320 ns |    0.0286 |        - |       - |     120 B |
|               MapNew_enumerate |     1 |         68.481 ns |       1.9655 ns |       5.7953 ns |         70.608 ns |    0.0095 |        - |       - |      40 B |
|            Map_containsKey_all |     1 |         10.732 ns |       0.2715 ns |       0.5032 ns |         10.914 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     1 |          8.872 ns |       0.2334 ns |       0.4497 ns |          8.933 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     1 |         12.475 ns |       0.2081 ns |       0.1737 ns |         12.478 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     1 |          3.583 ns |       0.1308 ns |       0.3183 ns |          3.666 ns |         - |        - |       - |         - |
|                 Map_remove_all |     1 |         24.148 ns |       0.5856 ns |       0.9288 ns |         24.267 ns |    0.0095 |        - |       - |      40 B |
|              MapNew_remove_all |     1 |         21.068 ns |       0.4847 ns |       0.4533 ns |         20.908 ns |    0.0076 |        - |       - |      32 B |
|                    Map_ofArray |     2 |         66.404 ns |       1.1412 ns |       1.0675 ns |         66.428 ns |    0.0267 |        - |       - |     112 B |
|                 MapNew_ofArray |     2 |         75.163 ns |       1.1044 ns |       1.0331 ns |         74.769 ns |    0.0248 |        - |       - |     104 B |
|                    Map_toArray |     2 |        100.100 ns |       0.9912 ns |       0.8786 ns |        100.236 ns |    0.0362 |        - |       - |     152 B |
|                 MapNew_toArray |     2 |         41.775 ns |       0.9523 ns |       1.9666 ns |         41.968 ns |    0.0210 |        - |       - |      88 B |
|                  Map_enumerate |     2 |        188.628 ns |       3.3722 ns |       2.9894 ns |        188.520 ns |    0.0572 |        - |       - |     240 B |
|               MapNew_enumerate |     2 |        111.105 ns |       2.4070 ns |       7.0972 ns |        113.129 ns |    0.0191 |        - |       - |      80 B |
|            Map_containsKey_all |     2 |         44.238 ns |       0.9422 ns |       0.8813 ns |         44.466 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     2 |         22.527 ns |       0.4992 ns |       0.6313 ns |         22.614 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     2 |         26.077 ns |       0.5787 ns |       1.2944 ns |         26.453 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     2 |          8.820 ns |       0.2548 ns |       0.7391 ns |          9.045 ns |         - |        - |       - |         - |
|                 Map_remove_all |     2 |        107.471 ns |       1.4199 ns |       1.2587 ns |        107.341 ns |    0.0248 |        - |       - |     104 B |
|              MapNew_remove_all |     2 |         68.232 ns |       1.4771 ns |       2.9157 ns |         68.731 ns |    0.0210 |        - |       - |      88 B |
|                    Map_ofArray |     3 |        180.830 ns |       3.6765 ns |       8.5208 ns |        182.908 ns |    0.0496 |        - |       - |     208 B |
|                 MapNew_ofArray |     3 |        164.546 ns |       3.3759 ns |       8.2175 ns |        166.544 ns |    0.0477 |        - |       - |     200 B |
|                    Map_toArray |     3 |        142.635 ns |       2.1805 ns |       1.9330 ns |        142.750 ns |    0.0515 |        - |       - |     216 B |
|                 MapNew_toArray |     3 |         52.340 ns |       1.4133 ns |       4.1225 ns |         53.950 ns |    0.0287 |        - |       - |     120 B |
|                  Map_enumerate |     3 |        312.209 ns |       6.1713 ns |      12.1816 ns |        313.754 ns |    0.0858 |        - |       - |     360 B |
|               MapNew_enumerate |     3 |        192.009 ns |       3.8697 ns |       6.5711 ns |        193.449 ns |    0.0286 |        - |       - |     120 B |
|            Map_containsKey_all |     3 |         94.528 ns |       1.9344 ns |       5.3278 ns |         95.785 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     3 |         41.512 ns |       0.8862 ns |       1.8692 ns |         41.954 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     3 |         44.878 ns |       0.9494 ns |       2.6934 ns |         45.657 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     3 |          8.882 ns |       0.1805 ns |       0.1688 ns |          8.939 ns |         - |        - |       - |         - |
|                 Map_remove_all |     3 |        207.586 ns |       4.2065 ns |       6.5491 ns |        208.771 ns |    0.0458 |        - |       - |     192 B |
|              MapNew_remove_all |     3 |        197.125 ns |       3.1987 ns |       2.9920 ns |        197.820 ns |    0.0401 |        - |       - |     168 B |
|                    Map_ofArray |     4 |        392.760 ns |       7.9301 ns |      12.5780 ns |        394.764 ns |    0.0896 |        - |       - |     376 B |
|                 MapNew_ofArray |     4 |        210.210 ns |       2.8221 ns |       2.6398 ns |        210.286 ns |    0.0534 |        - |       - |     224 B |
|                    Map_toArray |     4 |        163.941 ns |       4.2291 ns |      12.0657 ns |        168.142 ns |    0.0668 |        - |       - |     280 B |
|                 MapNew_toArray |     4 |        103.912 ns |       2.1456 ns |       3.1450 ns |        104.327 ns |    0.0362 |        - |       - |     152 B |
|                  Map_enumerate |     4 |        313.426 ns |       6.1237 ns |       8.9760 ns |        315.999 ns |    0.0858 |        - |       - |     360 B |
|               MapNew_enumerate |     4 |        185.740 ns |       3.7644 ns |       8.6493 ns |        187.966 ns |    0.0381 |        - |       - |     160 B |
|            Map_containsKey_all |     4 |        124.814 ns |       2.1694 ns |       2.0292 ns |        125.110 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     4 |         54.183 ns |       1.1409 ns |       2.9654 ns |         54.782 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     4 |         27.351 ns |       0.9782 ns |       2.8843 ns |         28.356 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     4 |         17.107 ns |       0.9946 ns |       2.9326 ns |         17.788 ns |         - |        - |       - |         - |
|                 Map_remove_all |     4 |        303.609 ns |      19.5543 ns |      57.6561 ns |        320.768 ns |    0.0801 |        - |       - |     336 B |
|              MapNew_remove_all |     4 |        246.903 ns |       5.0154 ns |      13.2127 ns |        249.999 ns |    0.0591 |        - |       - |     248 B |
|                    Map_ofArray |     5 |        502.711 ns |      12.6058 ns |      36.9706 ns |        515.492 ns |    0.1011 |        - |       - |     424 B |
|                 MapNew_ofArray |     5 |        226.602 ns |       4.6261 ns |      12.5069 ns |        229.647 ns |    0.0534 |        - |       - |     224 B |
|                    Map_toArray |     5 |        208.503 ns |       4.2781 ns |       6.2708 ns |        210.730 ns |    0.0823 |        - |       - |     344 B |
|                 MapNew_toArray |     5 |        118.929 ns |       2.4276 ns |       3.4032 ns |        119.207 ns |    0.0439 |        - |       - |     184 B |
|                  Map_enumerate |     5 |        430.648 ns |       8.5227 ns |      14.2395 ns |        433.739 ns |    0.1144 |        - |       - |     480 B |
|               MapNew_enumerate |     5 |        267.735 ns |       4.3679 ns |       3.8720 ns |        267.604 ns |    0.0477 |        - |       - |     200 B |
|            Map_containsKey_all |     5 |        219.140 ns |       4.3700 ns |       6.9313 ns |        221.321 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |     5 |         75.261 ns |       1.7592 ns |       5.1594 ns |         76.870 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |     5 |         26.986 ns |       0.6121 ns |       1.8046 ns |         27.549 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |     5 |         15.850 ns |       0.2813 ns |       0.2493 ns |         15.886 ns |         - |        - |       - |         - |
|                 Map_remove_all |     5 |        551.988 ns |      11.0458 ns |      26.6770 ns |        558.262 ns |    0.1259 |        - |       - |     528 B |
|              MapNew_remove_all |     5 |        433.951 ns |       8.7288 ns |      17.4324 ns |        439.302 ns |    0.0782 |        - |       - |     328 B |
|                    Map_ofArray |    10 |      1,974.599 ns |      37.9468 ns |      37.2688 ns |      1,974.667 ns |    0.3281 |        - |       - |    1384 B |
|                 MapNew_ofArray |    10 |      1,343.256 ns |      31.2315 ns |      91.1037 ns |      1,369.402 ns |    0.2079 |        - |       - |     872 B |
|                    Map_toArray |    10 |        455.375 ns |       9.1086 ns |      21.4699 ns |        459.471 ns |    0.1583 |        - |       - |     664 B |
|                 MapNew_toArray |    10 |        189.473 ns |       3.7889 ns |       5.8989 ns |        190.082 ns |    0.0823 |        - |       - |     344 B |
|                  Map_enumerate |    10 |        804.553 ns |      15.6745 ns |      21.9734 ns |        805.791 ns |    0.2003 |        - |       - |     840 B |
|               MapNew_enumerate |    10 |        529.778 ns |      10.7550 ns |      31.7114 ns |        541.077 ns |    0.0954 |        - |       - |     400 B |
|            Map_containsKey_all |    10 |        548.926 ns |      10.9359 ns |      25.9904 ns |        553.819 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    10 |        272.163 ns |       7.4719 ns |      22.0310 ns |        280.242 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    10 |         55.563 ns |       1.1377 ns |       1.2173 ns |         55.621 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    10 |         16.223 ns |       0.3851 ns |       0.7691 ns |         16.452 ns |         - |        - |       - |         - |
|                 Map_remove_all |    10 |      1,719.584 ns |      33.6134 ns |      55.2278 ns |      1,728.299 ns |    0.3014 |        - |       - |    1264 B |
|              MapNew_remove_all |    10 |      1,478.123 ns |      25.2957 ns |      23.6616 ns |      1,486.304 ns |    0.2594 |        - |       - |    1088 B |
|                    Map_ofArray |    20 |      5,168.385 ns |     102.8112 ns |     265.3885 ns |      5,248.594 ns |    0.7782 |        - |       - |    3256 B |
|                 MapNew_ofArray |    20 |      3,803.504 ns |      76.0711 ns |     120.6568 ns |      3,823.568 ns |    0.4272 |        - |       - |    1808 B |
|                    Map_toArray |    20 |        865.586 ns |      10.6263 ns |       9.9399 ns |        865.103 ns |    0.3109 |        - |       - |    1304 B |
|                 MapNew_toArray |    20 |        483.270 ns |      15.2460 ns |      43.9883 ns |        497.945 ns |    0.1583 |        - |       - |     664 B |
|                  Map_enumerate |    20 |      1,602.810 ns |      31.7455 ns |      67.6522 ns |      1,615.107 ns |    0.3719 |        - |       - |    1560 B |
|               MapNew_enumerate |    20 |      1,081.265 ns |      21.7549 ns |      49.1045 ns |      1,090.356 ns |    0.1907 |        - |       - |     800 B |
|            Map_containsKey_all |    20 |      1,438.603 ns |      28.7141 ns |      56.0047 ns |      1,455.486 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    20 |        745.968 ns |      14.7518 ns |      26.6006 ns |        749.249 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    20 |         85.057 ns |       1.7157 ns |       2.1070 ns |         85.284 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    20 |         48.180 ns |       1.0143 ns |       1.6947 ns |         48.377 ns |         - |        - |       - |         - |
|                 Map_remove_all |    20 |      4,510.432 ns |     109.9944 ns |     322.5944 ns |      4,628.942 ns |    0.7401 |        - |       - |    3120 B |
|              MapNew_remove_all |    20 |      4,389.275 ns |      86.8897 ns |     154.4463 ns |      4,421.950 ns |    0.6714 |        - |       - |    2824 B |
|                    Map_ofArray |    30 |      9,311.881 ns |     185.3091 ns |     447.5424 ns |      9,395.531 ns |    1.3275 |        - |       - |    5584 B |
|                 MapNew_ofArray |    30 |      8,548.549 ns |     170.5080 ns |     320.2553 ns |      8,638.651 ns |    0.9918 |        - |       - |    4184 B |
|                    Map_toArray |    30 |      1,252.569 ns |      25.0874 ns |      64.7586 ns |      1,273.497 ns |    0.4635 |        - |       - |    1944 B |
|                 MapNew_toArray |    30 |        675.578 ns |      13.7254 ns |      39.8198 ns |        684.446 ns |    0.2346 |        - |       - |     984 B |
|                  Map_enumerate |    30 |      2,433.116 ns |      48.1608 ns |      64.2933 ns |      2,443.798 ns |    0.5417 |        - |       - |    2280 B |
|               MapNew_enumerate |    30 |      1,513.738 ns |      30.2627 ns |      76.4777 ns |      1,531.865 ns |    0.2861 |        - |       - |    1200 B |
|            Map_containsKey_all |    30 |      2,645.672 ns |      52.6629 ns |     134.0438 ns |      2,677.132 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    30 |      1,436.141 ns |      28.7826 ns |      49.6486 ns |      1,449.133 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    30 |         88.958 ns |       1.8303 ns |       5.2515 ns |         90.747 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    30 |         63.113 ns |       1.2938 ns |       2.2317 ns |         63.491 ns |         - |        - |       - |         - |
|                 Map_remove_all |    30 |      8,303.210 ns |     164.3733 ns |     339.4593 ns |      8,385.858 ns |    1.3123 |        - |       - |    5520 B |
|              MapNew_remove_all |    30 |      8,017.514 ns |     103.9241 ns |      97.2107 ns |      8,030.251 ns |    1.1902 |        - |       - |    5040 B |
|                    Map_ofArray |    40 |     14,423.461 ns |     287.4266 ns |     455.8891 ns |     14,565.338 ns |    2.0905 |        - |       - |    8776 B |
|                 MapNew_ofArray |    40 |      9,221.741 ns |     294.9521 ns |     869.6729 ns |      9,647.771 ns |    1.1139 |        - |       - |    4712 B |
|                    Map_toArray |    40 |      1,636.657 ns |      32.6865 ns |      74.4438 ns |      1,659.249 ns |    0.6161 |        - |       - |    2584 B |
|                 MapNew_toArray |    40 |      1,077.530 ns |      19.6847 ns |      24.1746 ns |      1,076.596 ns |    0.3109 |        - |       - |    1304 B |
|                  Map_enumerate |    40 |      3,072.014 ns |      61.1200 ns |     161.0145 ns |      3,122.762 ns |    0.7172 |        - |       - |    3000 B |
|               MapNew_enumerate |    40 |      2,059.989 ns |      40.8635 ns |     106.2096 ns |      2,078.953 ns |    0.3815 |        - |       - |    1600 B |
|            Map_containsKey_all |    40 |      3,872.205 ns |      77.3904 ns |     203.8772 ns |      3,908.317 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    40 |      1,948.039 ns |      52.7713 ns |     155.5974 ns |      2,001.930 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    40 |         94.271 ns |       1.9301 ns |       4.8775 ns |         95.810 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    40 |         56.881 ns |       1.1923 ns |       2.7396 ns |         57.618 ns |         - |        - |       - |         - |
|                 Map_remove_all |    40 |     12,909.579 ns |     256.5460 ns |     494.2770 ns |     13,062.760 ns |    2.0447 |        - |       - |    8560 B |
|              MapNew_remove_all |    40 |     11,655.668 ns |     153.7256 ns |     143.7950 ns |     11,678.931 ns |    1.7853 |        - |       - |    7496 B |
|                    Map_ofArray |    50 |     19,624.984 ns |     221.7361 ns |     185.1597 ns |     19,568.787 ns |    2.6855 |        - |       - |   11272 B |
|                 MapNew_ofArray |    50 |     14,030.017 ns |     279.9903 ns |     717.7221 ns |     14,291.621 ns |    1.4343 |        - |       - |    6008 B |
|                    Map_toArray |    50 |      2,194.445 ns |      44.3894 ns |      89.6688 ns |      2,214.221 ns |    0.7706 |        - |       - |    3224 B |
|                 MapNew_toArray |    50 |      1,236.967 ns |      24.5641 ns |      57.4177 ns |      1,246.461 ns |    0.3872 |        - |       - |    1624 B |
|                  Map_enumerate |    50 |      3,876.322 ns |      76.8110 ns |     219.1460 ns |      3,926.818 ns |    0.8850 |        - |       - |    3720 B |
|               MapNew_enumerate |    50 |      2,459.595 ns |      49.2635 ns |     133.1867 ns |      2,502.888 ns |    0.4768 |        - |       - |    2000 B |
|            Map_containsKey_all |    50 |      5,039.178 ns |     113.2811 ns |     334.0118 ns |      5,144.973 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |    50 |      2,858.456 ns |      56.4803 ns |     101.8456 ns |      2,875.109 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |    50 |         95.981 ns |       1.8464 ns |       1.7271 ns |         96.321 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |    50 |         61.747 ns |       1.3987 ns |       4.1241 ns |         63.132 ns |         - |        - |       - |         - |
|                 Map_remove_all |    50 |     17,496.713 ns |     312.0691 ns |     276.6412 ns |     17,475.502 ns |    2.7466 |        - |       - |   11600 B |
|              MapNew_remove_all |    50 |     15,100.078 ns |     298.9529 ns |     617.3896 ns |     15,205.649 ns |    2.3193 |        - |       - |    9712 B |
|                    Map_ofArray |   100 |     48,329.146 ns |     967.0594 ns |   2,790.1874 ns |     49,206.784 ns |    6.6528 |        - |       - |   27952 B |
|                 MapNew_ofArray |   100 |     32,769.761 ns |     649.0510 ns |   1,465.0164 ns |     33,134.491 ns |    2.6245 |        - |       - |   11096 B |
|                    Map_toArray |   100 |      4,392.816 ns |      87.0709 ns |     215.2176 ns |      4,443.429 ns |    1.5335 |        - |       - |    6424 B |
|                 MapNew_toArray |   100 |      2,616.493 ns |      45.6635 ns |      40.4795 ns |      2,617.833 ns |    0.7706 |        - |       - |    3224 B |
|                  Map_enumerate |   100 |      7,986.671 ns |     158.6327 ns |     305.6313 ns |      8,055.053 ns |    1.7700 |        - |       - |    7440 B |
|               MapNew_enumerate |   100 |      4,849.283 ns |      96.2754 ns |     271.5465 ns |      4,909.573 ns |    0.9537 |        - |       - |    4000 B |
|            Map_containsKey_all |   100 |     11,676.492 ns |     280.6194 ns |     827.4127 ns |     11,927.202 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   100 |      7,226.473 ns |     143.9555 ns |     353.1254 ns |      7,332.134 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   100 |        109.761 ns |       1.2894 ns |       1.2061 ns |        109.673 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   100 |         61.687 ns |       1.2921 ns |       3.2652 ns |         62.438 ns |         - |        - |       - |         - |
|                 Map_remove_all |   100 |     46,269.289 ns |     924.6025 ns |   1,265.6064 ns |     46,542.560 ns |    6.6528 |        - |       - |   27992 B |
|              MapNew_remove_all |   100 |     37,337.835 ns |     738.6448 ns |   1,984.3212 ns |     38,205.089 ns |    5.9814 |        - |       - |   25040 B |
|                    Map_ofArray |   200 |    117,145.890 ns |   2,989.6596 ns |   8,673.5446 ns |    119,754.077 ns |   15.6250 |   0.2441 |       - |   66328 B |
|                 MapNew_ofArray |   200 |     75,200.939 ns |   1,577.1323 ns |   4,650.2101 ns |     76,712.469 ns |    6.1035 |   0.1221 |       - |   25712 B |
|                    Map_toArray |   200 |      9,085.546 ns |     162.1998 ns |     151.7218 ns |      9,062.028 ns |    3.0518 |   0.0153 |       - |   12824 B |
|                 MapNew_toArray |   200 |      5,276.513 ns |     126.2397 ns |     362.2054 ns |      5,395.320 ns |    1.5335 |        - |       - |    6424 B |
|                  Map_enumerate |   200 |     15,894.948 ns |     314.3546 ns |     839.0760 ns |     16,064.438 ns |    3.6011 |        - |       - |   15120 B |
|               MapNew_enumerate |   200 |      9,807.496 ns |     190.0940 ns |     260.2028 ns |      9,852.873 ns |    1.9073 |        - |       - |    8000 B |
|            Map_containsKey_all |   200 |     27,628.249 ns |     549.8732 ns |     675.2937 ns |     27,729.721 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   200 |     16,999.129 ns |     337.6529 ns |     871.5898 ns |     17,302.582 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   200 |        114.217 ns |       2.2895 ns |       4.8791 ns |        115.147 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   200 |         70.630 ns |       2.2864 ns |       6.7415 ns |         73.633 ns |         - |        - |       - |         - |
|                 Map_remove_all |   200 |    110,178.057 ns |   2,119.3699 ns |   2,355.6740 ns |    110,030.090 ns |   15.6250 |        - |       - |   65384 B |
|              MapNew_remove_all |   200 |     87,358.896 ns |   1,731.5847 ns |   3,336.1758 ns |     88,355.273 ns |   13.7939 |        - |       - |   57712 B |
|                    Map_ofArray |   300 |    194,509.995 ns |   3,851.1204 ns |   9,661.7081 ns |    197,861.316 ns |   25.6348 |   0.2441 |       - |  107320 B |
|                 MapNew_ofArray |   300 |    128,985.392 ns |   2,514.7033 ns |   3,269.8246 ns |    129,187.073 ns |   10.4980 |   0.2441 |       - |   44600 B |
|                    Map_toArray |   300 |     13,478.120 ns |     268.4855 ns |     711.9854 ns |     13,638.101 ns |    4.5929 |        - |       - |   19224 B |
|                 MapNew_toArray |   300 |      7,812.176 ns |     195.7710 ns |     552.1756 ns |      7,927.794 ns |    2.2888 |   0.0153 |       - |    9624 B |
|                  Map_enumerate |   300 |     23,294.010 ns |     456.3909 ns |     696.9572 ns |     23,396.988 ns |    5.3406 |        - |       - |   22440 B |
|               MapNew_enumerate |   300 |     14,326.499 ns |     285.6457 ns |     678.8672 ns |     14,491.943 ns |    2.8687 |        - |       - |   12000 B |
|            Map_containsKey_all |   300 |     45,002.956 ns |     884.3369 ns |   1,724.8312 ns |     45,493.179 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   300 |     27,804.775 ns |     563.6585 ns |   1,661.9599 ns |     28,402.182 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   300 |        123.201 ns |       2.5134 ns |       6.2592 ns |        124.703 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   300 |         68.101 ns |       1.4187 ns |       2.7672 ns |         68.200 ns |         - |        - |       - |         - |
|                 Map_remove_all |   300 |    186,155.645 ns |   3,641.5757 ns |   6,377.9261 ns |    187,193.262 ns |   25.1465 |        - |       - |  106088 B |
|              MapNew_remove_all |   300 |    142,406.731 ns |   2,833.6726 ns |   6,279.2228 ns |    143,580.615 ns |   22.9492 |        - |       - |   96312 B |
|                    Map_ofArray |   400 |    281,664.758 ns |   5,450.8985 ns |   8,645.7026 ns |    282,194.141 ns |   35.6445 |   0.4883 |       - |  150688 B |
|                 MapNew_ofArray |   400 |    163,183.071 ns |   1,881.5525 ns |   1,760.0054 ns |    163,348.950 ns |   11.2305 |   0.2441 |       - |   47002 B |
|                    Map_toArray |   400 |     18,525.102 ns |     301.5837 ns |     267.3461 ns |     18,456.419 ns |    6.1035 |        - |       - |   25624 B |
|                 MapNew_toArray |   400 |     11,152.169 ns |     219.6330 ns |     378.8561 ns |     11,215.857 ns |    3.0518 |        - |       - |   12824 B |
|                  Map_enumerate |   400 |     31,395.986 ns |     621.4879 ns |   1,464.9202 ns |     31,714.230 ns |    7.2021 |        - |       - |   30240 B |
|               MapNew_enumerate |   400 |     19,201.009 ns |     383.0549 ns |   1,042.1291 ns |     19,472.124 ns |    3.8147 |        - |       - |   16000 B |
|            Map_containsKey_all |   400 |     61,530.898 ns |   1,647.2647 ns |   4,831.1422 ns |     63,170.490 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   400 |     40,269.591 ns |     553.2190 ns |     517.4814 ns |     40,209.915 ns |         - |        - |       - |       1 B |
|    Map_containsKey_nonexisting |   400 |        140.377 ns |       3.3611 ns |       9.8576 ns |        143.467 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   400 |         72.566 ns |       1.5001 ns |       2.7430 ns |         73.306 ns |         - |        - |       - |         - |
|                 Map_remove_all |   400 |    253,831.853 ns |   5,536.9522 ns |  16,238.9223 ns |    258,158.887 ns |   35.6445 |        - |       - |  151320 B |
|              MapNew_remove_all |   400 |    197,345.555 ns |   3,926.6830 ns |   8,782.5893 ns |    200,114.807 ns |   32.2266 |        - |       - |  135728 B |
|                    Map_ofArray |   500 |    376,749.579 ns |   7,366.2793 ns |   9,578.2439 ns |    378,048.291 ns |   46.8750 |   0.4883 |       - |  197248 B |
|                 MapNew_ofArray |   500 |    233,045.111 ns |   4,563.6580 ns |   7,238.4451 ns |    234,870.349 ns |   16.3574 |        - |       - |   68816 B |
|                    Map_toArray |   500 |     22,965.594 ns |     457.0072 ns |     943.7990 ns |     23,264.874 ns |    7.6294 |   0.0305 |       - |   32024 B |
|                 MapNew_toArray |   500 |     13,610.434 ns |     269.1726 ns |     394.5498 ns |     13,651.010 ns |    3.8300 |        - |       - |   16024 B |
|                  Map_enumerate |   500 |     39,658.391 ns |     785.9821 ns |   2,191.0040 ns |     40,311.694 ns |    9.1553 |        - |       - |   38400 B |
|               MapNew_enumerate |   500 |     24,856.649 ns |     496.3658 ns |   1,057.7959 ns |     25,032.385 ns |    4.7607 |        - |       - |   20000 B |
|            Map_containsKey_all |   500 |     82,107.051 ns |   1,630.6673 ns |   3,906.9748 ns |     83,122.974 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |   500 |     50,981.437 ns |   1,012.7807 ns |   2,954.3290 ns |     51,783.371 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |   500 |        143.421 ns |       2.8797 ns |       2.8283 ns |        143.454 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |   500 |         71.079 ns |       1.8017 ns |       5.3123 ns |         72.741 ns |         - |        - |       - |         - |
|                 Map_remove_all |   500 |    345,860.083 ns |   6,816.1821 ns |  15,661.3119 ns |    349,553.320 ns |   46.8750 |        - |       - |  196824 B |
|              MapNew_remove_all |   500 |    266,889.186 ns |   5,341.4062 ns |  11,611.7774 ns |    267,857.275 ns |   42.4805 |        - |       - |  179680 B |
|                    Map_ofArray |  1000 |    865,078.566 ns |  17,067.9474 ns |  36,002.0941 ns |    878,020.215 ns |  105.4688 |   5.8594 |       - |  443776 B |
|                 MapNew_ofArray |  1000 |    497,589.868 ns |   9,264.5581 ns |   9,099.0348 ns |    498,388.672 ns |   33.2031 |   5.8594 |       - |  139401 B |
|                    Map_toArray |  1000 |     47,911.257 ns |   1,099.4768 ns |   3,207.2253 ns |     49,098.322 ns |   15.2588 |   2.5024 |       - |   64024 B |
|                 MapNew_toArray |  1000 |     28,964.860 ns |     579.2589 ns |   1,209.1291 ns |     29,150.491 ns |    7.6294 |        - |       - |   32024 B |
|                  Map_enumerate |  1000 |     78,592.011 ns |   1,540.4286 ns |   2,930.8256 ns |     79,193.225 ns |   17.8223 |        - |       - |   74880 B |
|               MapNew_enumerate |  1000 |     48,469.914 ns |     960.6981 ns |   2,128.8406 ns |     48,993.164 ns |    9.5215 |        - |       - |   40000 B |
|            Map_containsKey_all |  1000 |    182,700.744 ns |   3,605.1522 ns |   7,760.4705 ns |    184,580.078 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  1000 |    115,438.382 ns |   2,329.9810 ns |   6,833.4310 ns |    117,867.029 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  1000 |        142.627 ns |       1.6745 ns |       1.4844 ns |        143.130 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  1000 |         76.945 ns |       1.5849 ns |       3.4454 ns |         77.886 ns |         - |        - |       - |         - |
|                 Map_remove_all |  1000 |    780,239.314 ns |  15,437.2078 ns |  36,986.5638 ns |    790,610.791 ns |  105.4688 |        - |       - |  444976 B |
|              MapNew_remove_all |  1000 |    572,525.158 ns |  11,438.3978 ns |  29,729.9167 ns |    581,230.762 ns |   95.7031 |   3.9063 |       - |  401720 B |
|                    Map_ofArray |  2000 |  2,057,274.723 ns |  59,433.5959 ns | 171,479.5133 ns |  2,116,486.914 ns |  238.2813 |  39.0625 |       - |  996904 B |
|                 MapNew_ofArray |  2000 |  1,125,316.450 ns |  27,127.4541 ns |  77,396.1531 ns |  1,145,905.371 ns |   70.3125 |  21.4844 |       - |  295904 B |
|                    Map_toArray |  2000 |    103,691.537 ns |   2,067.1307 ns |   4,449.7170 ns |    104,929.742 ns |   30.5176 |        - |       - |  128024 B |
|                 MapNew_toArray |  2000 |     59,336.049 ns |   1,179.8461 ns |   3,366.1673 ns |     60,195.239 ns |   15.2588 |   0.2441 |       - |   64024 B |
|                  Map_enumerate |  2000 |    158,166.644 ns |   3,136.8565 ns |   8,263.7347 ns |    159,691.370 ns |   35.6445 |        - |       - |  150000 B |
|               MapNew_enumerate |  2000 |     96,034.749 ns |   2,250.9133 ns |   6,530.3076 ns |     98,363.788 ns |   19.0430 |        - |       - |   80000 B |
|            Map_containsKey_all |  2000 |    410,932.366 ns |   7,470.3824 ns |   6,622.2994 ns |    409,724.316 ns |         - |        - |       - |       1 B |
|         MapNew_containsKey_all |  2000 |    264,178.714 ns |   4,178.0758 ns |   3,703.7553 ns |    264,591.089 ns |         - |        - |       - |       1 B |
|    Map_containsKey_nonexisting |  2000 |        166.007 ns |       3.3411 ns |       6.9741 ns |        168.079 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  2000 |         81.515 ns |       1.6606 ns |       3.4295 ns |         82.312 ns |         - |        - |       - |         - |
|                 Map_remove_all |  2000 |  1,789,253.265 ns |  35,668.2671 ns |  76,779.7078 ns |  1,812,564.355 ns |  232.4219 |        - |       - |  981952 B |
|              MapNew_remove_all |  2000 |  1,281,253.705 ns |  33,832.8898 ns |  98,692.1275 ns |  1,315,069.531 ns |  212.8906 |  17.5781 |       - |  901984 B |
|                    Map_ofArray |  3000 |  3,340,121.401 ns |  65,730.8963 ns | 120,192.6607 ns |  3,379,651.562 ns |  371.0938 | 121.0938 |       - | 1571656 B |
|                 MapNew_ofArray |  3000 |  1,989,728.277 ns |  38,919.3705 ns |  41,643.2904 ns |  1,991,151.367 ns |  121.0938 |  39.0625 |       - |  539176 B |
|                    Map_toArray |  3000 |    166,565.240 ns |   3,299.3922 ns |   8,806.7458 ns |    168,716.821 ns |   44.4336 |  11.2305 |       - |  192024 B |
|                 MapNew_toArray |  3000 |     92,621.827 ns |   1,927.4122 ns |   5,622.3518 ns |     94,556.555 ns |   22.8271 |   7.5684 |       - |   96024 B |
|                  Map_enumerate |  3000 |    238,499.373 ns |   4,717.9825 ns |  11,661.6838 ns |    240,653.735 ns |   53.7109 |        - |       - |  225720 B |
|               MapNew_enumerate |  3000 |    152,765.744 ns |   2,678.0874 ns |   2,505.0846 ns |    152,736.816 ns |   28.5645 |        - |       - |  120000 B |
|            Map_containsKey_all |  3000 |    643,462.786 ns |  12,635.4576 ns |  23,104.6488 ns |    647,893.359 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  3000 |    419,810.747 ns |   8,239.0968 ns |  14,644.9862 ns |    422,957.617 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  3000 |        160.494 ns |       3.2602 ns |       7.4910 ns |        162.861 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  3000 |         86.709 ns |       1.7781 ns |       3.3830 ns |         87.296 ns |         - |        - |       - |         - |
|                 Map_remove_all |  3000 |  2,905,140.914 ns |  57,879.6049 ns | 166,995.8939 ns |  2,947,135.352 ns |  371.0938 |        - |       - | 1572792 B |
|              MapNew_remove_all |  3000 |  2,126,885.911 ns |  30,853.9380 ns |  28,860.7928 ns |  2,129,493.750 ns |  339.8438 |   3.9063 |       - | 1435946 B |
|                    Map_ofArray |  4000 |  4,920,784.353 ns |  95,604.2617 ns | 157,080.5436 ns |  4,942,620.312 ns |  437.5000 | 171.8750 |       - | 2169040 B |
|                 MapNew_ofArray |  4000 |  2,538,827.412 ns |  50,529.4078 ns |  97,353.0144 ns |  2,558,279.688 ns |  121.0938 |  35.1563 |       - |  586064 B |
|                    Map_toArray |  4000 |    247,765.632 ns |   3,944.6701 ns |   3,689.8468 ns |    248,139.648 ns |   51.2695 |  20.5078 |       - |  256024 B |
|                 MapNew_toArray |  4000 |    128,733.185 ns |   2,546.8913 ns |   6,295.2843 ns |    130,293.262 ns |   29.7852 |   9.2773 |       - |  128024 B |
|                  Map_enumerate |  4000 |    317,642.249 ns |   6,237.8088 ns |  12,882.1568 ns |    319,461.426 ns |   71.2891 |        - |       - |  299760 B |
|               MapNew_enumerate |  4000 |    199,440.548 ns |   5,313.1125 ns |  15,665.8322 ns |    204,665.295 ns |   38.0859 |        - |       - |  160000 B |
|            Map_containsKey_all |  4000 |    885,755.046 ns |  17,501.3930 ns |  33,298.2191 ns |    892,810.449 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  4000 |    589,503.909 ns |  11,707.1457 ns |  18,904.8580 ns |    590,877.637 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  4000 |        177.529 ns |       3.5035 ns |       3.2772 ns |        177.281 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  4000 |         85.616 ns |       1.8496 ns |       5.3953 ns |         87.330 ns |         - |        - |       - |         - |
|                 Map_remove_all |  4000 |  4,091,685.491 ns |  42,032.0625 ns |  37,260.3286 ns |  4,088,500.781 ns |  507.8125 |  15.6250 |       - | 2159626 B |
|              MapNew_remove_all |  4000 |  2,939,354.106 ns |  58,252.1770 ns | 158,479.3436 ns |  2,983,346.289 ns |  476.5625 |        - |       - | 1997216 B |
|                    Map_ofArray |  5000 |  6,528,245.141 ns | 129,747.4170 ns | 233,961.3565 ns |  6,581,739.062 ns |  492.1875 | 210.9375 | 15.6250 | 2785624 B |
|                 MapNew_ofArray |  5000 |  3,504,518.107 ns |  65,840.3236 ns |  67,613.1648 ns |  3,514,761.719 ns |  156.2500 |  66.4063 |       - |  828973 B |
|                    Map_toArray |  5000 |    304,285.063 ns |   6,868.8732 ns |  20,253.0277 ns |    311,159.912 ns |   68.3594 |  21.4844 |       - |  320024 B |
|                 MapNew_toArray |  5000 |    179,616.538 ns |   3,577.1708 ns |   7,926.7633 ns |    181,073.169 ns |   31.9824 |  12.9395 |       - |  160024 B |
|                  Map_enumerate |  5000 |    409,467.026 ns |   8,159.7258 ns |  14,075.1260 ns |    410,704.053 ns |   89.8438 |        - |       - |  377280 B |
|               MapNew_enumerate |  5000 |    250,225.483 ns |   4,978.0130 ns |  14,040.5739 ns |    253,073.657 ns |   47.3633 |        - |       - |  200000 B |
|            Map_containsKey_all |  5000 |  1,150,988.810 ns |  22,861.8493 ns |  29,726.8617 ns |  1,159,682.812 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all |  5000 |    757,747.671 ns |  14,945.6815 ns |  23,705.4346 ns |    759,252.051 ns |         - |        - |       - |         - |
|    Map_containsKey_nonexisting |  5000 |        188.301 ns |       3.8142 ns |       8.3723 ns |        191.336 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting |  5000 |         91.019 ns |       1.8731 ns |       3.9917 ns |         92.038 ns |         - |        - |       - |         - |
|                 Map_remove_all |  5000 |  5,481,234.229 ns | 102,458.4796 ns | 100,627.9269 ns |  5,489,273.047 ns |  664.0625 |  31.2500 |       - | 2800044 B |
|              MapNew_remove_all |  5000 |  3,760,899.418 ns |  75,379.5496 ns | 219,885.6842 ns |  3,847,565.625 ns |  613.2813 |   7.8125 |       - | 2574568 B |
|                    Map_ofArray | 10000 | 15,679,296.420 ns | 312,766.9155 ns | 666,531.7972 ns | 15,818,596.875 ns |  968.7500 | 312.5000 | 62.5000 | 6086224 B |
|                 MapNew_ofArray | 10000 |  8,037,487.292 ns | 130,703.8452 ns | 122,260.4579 ns |  8,023,884.375 ns |  250.0000 | 125.0000 |       - | 1585589 B |
|                    Map_toArray | 10000 |    710,321.786 ns |  14,177.4444 ns |  33,968.2513 ns |    721,232.373 ns |  105.4688 |  52.7344 |       - |  640024 B |
|                 MapNew_toArray | 10000 |    371,077.707 ns |   9,303.0126 ns |  27,430.1426 ns |    382,409.644 ns |   50.7813 |  25.3906 |       - |  320024 B |
|                  Map_enumerate | 10000 |    839,409.171 ns |  16,663.7809 ns |  27,379.0699 ns |    845,207.617 ns |  179.6875 |        - |       - |  751560 B |
|               MapNew_enumerate | 10000 |    515,885.399 ns |  10,216.3933 ns |  26,553.7645 ns |    523,462.646 ns |   94.7266 |        - |       - |  400000 B |
|            Map_containsKey_all | 10000 |  2,511,058.093 ns |  49,596.5712 ns | 107,818.8641 ns |  2,532,810.938 ns |         - |        - |       - |         - |
|         MapNew_containsKey_all | 10000 |  1,747,589.689 ns |  32,906.3848 ns |  33,792.4345 ns |  1,755,443.262 ns |         - |        - |       - |       1 B |
|    Map_containsKey_nonexisting | 10000 |        194.774 ns |       3.9467 ns |       6.9124 ns |        195.948 ns |         - |        - |       - |         - |
| MapNew_containsKey_nonexisting | 10000 |         95.083 ns |       1.9356 ns |       5.3957 ns |         96.367 ns |         - |        - |       - |         - |
|                 Map_remove_all | 10000 | 12,311,654.554 ns | 242,384.8216 ns | 398,245.2128 ns | 12,364,978.125 ns | 1453.1250 |  31.2500 |       - | 6114216 B |
|              MapNew_remove_all | 10000 |  9,252,097.872 ns | 183,591.5278 ns | 358,081.1751 ns |  9,340,557.812 ns | 1343.7500 | 156.2500 |       - | 5666432 B |



   