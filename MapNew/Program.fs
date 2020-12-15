open System.Collections.Generic
open MapNew
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs

let profiling() =
    let rand = System.Random()
    let arr = Array.init ((1 <<< 20) - 1) (fun i -> (rand.Next 1000, i))

    while true do
        let m = MapNew.ofArray arr
        System.Console.WriteLine("{0}", m.Count)


[<EntryPoint>]
let main _argv =
    printfn "FSharp.Core: %A" typeof<list<int>>.Assembly.FullName
    
    let a = MapNew.ofArray [|0,0;1,0|]
    let b = MapNew.ofArray [|0,0;2,0|]
    printfn "%A" (MapNew.union a b)
    exit 0

    //profiling()


    //BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.SortBenchmark>()
    //|> ignore

    ManualConfig
        .Create(DefaultConfig.Instance)
        .AddJob(Job.Default.WithGcServer(false))
        // .AddJob(Job.Default.WithGcServer(false))

    |> BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>
    |> ignore
    0
