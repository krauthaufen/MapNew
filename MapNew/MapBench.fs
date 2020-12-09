namespace Benchmark

open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open Aardvark.Base


[<PlainExporter; MemoryDiagnoser>]
type MapBenchmark() =

    [<DefaultValue; Params(1, 100)>]
    val mutable public Count : int

    let mutable data : (int * int)[] = [||]
    let mutable map = Map.ofArray data
    let mutable mapNew = MapNew.ofArray data
    let mutable existing = 0
    let mutable toolarge = 123

    static let rand = System.Random()

    static let randomArray (maxValue : int) (n : int) =
        let s = System.Collections.Generic.HashSet<int>()
        let res = Array.zeroCreate n
        let mutable i = 0
        while i < n do
            let v = rand.Next(maxValue)
            if s.Add v then
                res.[i] <- v
                i <- i + 1
        res




    [<GlobalSetup>]
    member x.Setup() =
        data <- randomArray (1 <<< 30) x.Count |> Array.map (fun i -> i,i)
        map <- Map.ofArray data
        mapNew <- MapNew.ofArray data
        existing <- data.[rand.Next(data.Length)] |> fst
        toolarge <- System.Int32.MaxValue

    [<Benchmark>]
    member x.``Map_ofArray``() =
        Map.ofArray data
        
    [<Benchmark>]
    member x.``MapNew_ofArray``() =
        MapNew.ofArray data

    [<Benchmark>]
    member x.``Map_toArray``() =
        Map.toArray map
        
    [<Benchmark>]
    member x.``MapNew_toArray``() =
        MapNew.toArray mapNew
        
    [<Benchmark>]
    member x.``Map_containsKey``() =
        let mutable res = true
        for (k, _) in data do
            res <- Map.containsKey k map && res
        res

    [<Benchmark>]
    member x.``MapNew_containsKey``() =
        let mutable res = true
        for (k, _) in data do
            res <- mapNew.ContainsKey k && res
        res

    [<Benchmark>]
    member x.``Map_containsKey_toolarge``() =
        Map.containsKey toolarge map
        
    [<Benchmark>]
    member x.``MapNew_containsKey_toolarge``() =
        mapNew.ContainsKey toolarge
        
        

