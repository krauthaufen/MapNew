[<EntryPoint>]
let main _argv =

    let fs = typeof<list<int>>.Assembly
    printfn "FSharp.Core: %A" fs.FullName
    BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>()
    |> ignore
    0
