open System.Collections.Generic
open MapNew

[<EntryPoint>]
let main _argv =
    printfn "FSharp.Core: %A" typeof<list<int>>.Assembly.FullName
    
    //BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.SortBenchmark>()
    //|> ignore

    BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>()
    |> ignore
    0
