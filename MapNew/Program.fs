open System.Collections.Generic
open MapNew

let profiling() =
    
    let rand = System.Random()
    let arr = Array.init ((1 <<< 20) - 1) (fun i -> (rand.Next 1000, i))

    while true do
        let m = MapNew.ofArray arr
        System.Console.WriteLine("{0}", m.Count)


[<EntryPoint>]
let main _argv =
    printfn "FSharp.Core: %A" typeof<list<int>>.Assembly.FullName
    
    //profiling()


    //BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.SortBenchmark>()
    //|> ignore

    BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>()
    |> ignore
    0
