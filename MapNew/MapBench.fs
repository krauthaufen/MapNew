namespace Benchmark

open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open MapNew


[<PlainExporter; MemoryDiagnoser>]
type MapBenchmark() =

    [<DefaultValue; Params(100)>]
    val mutable public Count : int

    let mutable data : (int * int)[] = [||]
    let mutable list : list<int*int> = []
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
        list <- Array.toList data
        map <- Map.ofArray data
        mapNew <- MapNew.ofArray data
        existing <- data.[rand.Next(data.Length)] |> fst
        toolarge <- System.Int32.MaxValue

    [<Benchmark>]
    member x.``Map_ofArray``() =
        Map.ofArray data
        
    //[<Benchmark>]
    //member x.``MapNew_ofArray_add``() =
    //    MapNew.FromArrayAddInPlace data

    [<Benchmark>]
    member x.``MapNew_ofArray``() =
        MapNew.ofArray data
        
        
    //[<Benchmark>]
    //member x.``Map_ofList``() =
    //    Map.ofList list
        
    //[<Benchmark>]
    //member x.``MapNew_ofList``() =
    //    MapNew.ofList list
        
    //[<Benchmark>]
    //member x.``Map_ofSeq``() =
    //    Map.ofSeq list
        
    //[<Benchmark>]
    //member x.``MapNew_ofSeq``() =
    //    MapNew.ofSeq list

    //[<Benchmark>]
    //member x.``Map_toArray``() =
    //    Map.toArray map
        
    //[<Benchmark>]
    //member x.``MapNew_toArray``() =
    //    MapNew.toArray mapNew
        
    //[<Benchmark>]
    //member x.``Map_toList``() =
    //    Map.toList map
        
    //[<Benchmark>]
    //member x.``MapNew_toList``() =
    //    MapNew.toList mapNew
        
    //[<Benchmark>]
    //member x.``Map_enumerate``() =
    //    let mutable sum = 0
    //    for KeyValue(k,_) in map do
    //        sum <- sum + k
    //    sum
        
    //[<Benchmark>]
    //member x.``MapNew_enumerate``() =
    //    let mutable sum = 0
    //    for KeyValue(k,_) in mapNew do
    //        sum <- sum + k
    //    sum
         
    //[<Benchmark>]
    //member x.``Map_toSeq_enum``() =
    //    let mutable sum = 0
    //    for (k,_) in Map.toSeq map do
    //        sum <- sum + k
    //    sum
        
    //[<Benchmark>]
    //member x.``MapNew_toSeq_enum``() =
    //    let mutable sum = 0
    //    for (k,_) in MapNew.toSeq mapNew do
    //        sum <- sum + k
    //    sum
        
    //[<Benchmark>]
    //member x.``Map_containsKey_all``() =
    //    let mutable res = true
    //    for (k, _) in data do
    //        res <- Map.containsKey k map && res
    //    res

    //[<Benchmark>]
    //member x.``MapNew_containsKey_all``() =
    //    let mutable res = true
    //    for (k, _) in data do
    //        res <- mapNew.ContainsKey k && res
    //    res
       
    //[<Benchmark>]
    //member x.``Map_containsKey_nonexisting``() =
    //    Map.containsKey toolarge map
        
    //[<Benchmark>]
    //member x.``MapNew_containsKey_nonexisting``() =
    //    mapNew.ContainsKey toolarge
         
    //[<Benchmark>]
    //member x.``Map_tryFind``() =
    //    Map.tryFind (fst data.[0]) map
        
    //[<Benchmark>]
    //member x.``MapNew_tryFind``() =
    //    MapNew.tryFind (fst data.[0]) mapNew
        
    //[<Benchmark>]
    //member x.``Map_tryFind_nonexisting``() =
    //    Map.tryFind toolarge map
        
    //[<Benchmark>]
    //member x.``MapNew_tryFind_nonexisting``() =
    //    MapNew.tryFind toolarge mapNew
        
    //[<Benchmark>]
    //member x.``Map_remove_all``() =
        
    //    let mutable res = map
    //    for (k, _) in data do
    //        res <- Map.remove k res
    //    res

    //[<Benchmark>]
    //member x.``MapNew_remove_all``() =
    //    let mutable res = mapNew
    //    for (k, _) in data do
    //        res <- MapNew.remove k res
    //    res
        
    //[<Benchmark>]
    //member x.``Map_exists``() =
    //    map |> Map.exists (fun _ _ -> false)
        
    //[<Benchmark>]
    //member x.``MapNew_exists``() =
    //    mapNew |> MapNew.exists (fun _ _ -> false)
        
    //[<Benchmark>]
    //member x.``Map_fold``() =
    //    (0, map) ||> Map.fold (fun s k _ -> s + k)

    //[<Benchmark>]
    //member x.``MapNew_fold``() =
    //    (0, mapNew) ||> MapNew.fold (fun s k _ -> s + k)
        
    [<Benchmark>]
    member x.``Map_foldBack``() =
        (map, 0) ||> Map.foldBack (fun s k _ -> s + k)

    [<Benchmark>]
    member x.``MapNew_foldBack``() =
        (mapNew, 0) ||> MapNew.foldBack (fun s k _ -> s + k)


        

