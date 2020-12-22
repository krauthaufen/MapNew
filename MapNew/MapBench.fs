namespace Benchmark

open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open MapNew
open BenchmarkDotNet.Configs
open System
open Microsoft.FSharp.NativeInterop
open System.Runtime.CompilerServices

type ReferenceNumber(value : decimal) =
    member x.Value = value

    static member Zero = ReferenceNumber(0.0m)
    static member One = ReferenceNumber(1.0m)

    static member (+) (l : ReferenceNumber, r : ReferenceNumber) =
        ReferenceNumber(l.Value + r.Value)

    member x.CompareTo(o : ReferenceNumber) =
        compare value o.Value
        
    override x.GetHashCode() =
        Unchecked.hash value

    override x.Equals o =
        match o with
        | :? ReferenceNumber as o -> value = o.Value
        | _ -> false

    interface System.IComparable<ReferenceNumber> with
        member x.CompareTo o =
            x.CompareTo o

    interface System.IComparable with
        member x.CompareTo o =
            x.CompareTo (o :?> ReferenceNumber)

[<PlainExporter; MemoryDiagnoser; IterationTime(100.0); MaxIterationCount(20)>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
type MapBenchmark() =

    [<DefaultValue; Params(100)>]
    val mutable public Count : int

    let mutable data : (_ * _)[] = [||]
    let mutable list : list<_ * _> = []
    let mutable seq : seq<_ * _> = Seq.empty
    let mutable map = Map.ofArray data
    let mutable mapNew = MapNew.ofArray data
    let mutable existing = Unchecked.defaultof<_> //LanguagePrimitives.GenericZero
    let mutable toolarge = Unchecked.defaultof<_> //LanguagePrimitives.GenericZero
    let mutable randomElements  : (_ * _)[] = [||]

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

    static let randomDecimalArray (n : int) =
        let s = System.Collections.Generic.HashSet<decimal>()
        let res = Array.zeroCreate n
        let mutable i = 0
        while i < n do
            let v = rand.NextDouble() |> decimal
            if s.Add v then
                res.[i] <- v
                i <- i + 1
        res

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static let keep (v : 'a) =
        ()

    [<GlobalSetup>]
    member x.Setup() =
        // int -> int
        let arr = randomArray (1 <<< 30) x.Count |> Array.mapi (fun i v -> v, i)

        // decimal -> int
        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> v, i)

        // ref -> int
        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> ReferenceNumber v, i)

        data <- arr
        list <- Array.toList data
        seq <- Seq.init data.Length (fun i -> arr.[i])
        map <- Map.ofArray data
        mapNew <- MapNew.ofArray data
        existing <- data.[rand.Next(data.Length)] |> fst

        let all = System.Collections.Generic.HashSet (data |> Array.map fst)
        let arr = Array.zeroCreate x.Count
        let mutable cnt = 0
        while cnt < arr.Length do
            let v = rand.Next()
            if all.Add v then
                arr.[cnt] <- (v, cnt)
                cnt <- cnt + 1
        randomElements <- arr

        let largest = arr |> Seq.map fst |> Seq.max
        toolarge <- LanguagePrimitives.GenericOne + largest //System.Decimal.MaxValue //System.Int32.MaxValue

        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("add")>]
    member x.``Map_add``() =
        let r = map
        for (k, v) in randomElements do
            Map.add k v r |> keep

    [<Benchmark>]
    [<BenchmarkCategory("add")>]
    member x.``MapNew_add``() =
        let r = mapNew
        for (k, v) in randomElements do
            MapNew.add k v r |> keep
        

    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("remove")>]
    member x.``Map_remove``() =
        let r = map
        for i in 0 .. min randomElements.Length data.Length - 1 do
            let (k,_) = data.[i]
            Map.remove k r |> keep

    [<Benchmark>]
    [<BenchmarkCategory("remove")>]
    member x.``MapNew_remove``() =
        let r = mapNew
        for i in 0 .. min randomElements.Length data.Length - 1 do
            let (k,_) = data.[i]
            MapNew.remove k r |> keep


    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("ofArray")>]
    member x.``Map_ofArray``() =
        Map.ofArray data

    [<Benchmark>]
    [<BenchmarkCategory("ofArray")>]
    member x.``MapNew_ofArray``() =
        MapNew.ofArray data
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("ofList")>]
    member x.``Map_ofList``() =
        Map.ofList list
        
    [<Benchmark>]
    [<BenchmarkCategory("ofList")>]
    member x.``MapNew_ofList``() =
        MapNew.ofList list
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("ofSeq")>]
    member x.``Map_ofSeq``() =
        Map.ofSeq list
        
    [<Benchmark>]
    [<BenchmarkCategory("ofSeq")>]
    member x.``MapNew_ofSeq``() =
        MapNew.ofSeq list

    [<Benchmark(Baseline = true)>]
    [<BenchmarkCategory("toArray")>]
    member x.``Map_toArray``() =
        Map.toArray map
        
    [<Benchmark>]
    [<BenchmarkCategory("toArray")>]
    member x.``MapNew_toArray``() =
        MapNew.toArray mapNew
        
    [<Benchmark(Baseline = true)>]
    [<BenchmarkCategory("toList")>]
    member x.``Map_toList``() =
        Map.toList map
        
    [<Benchmark>]
    [<BenchmarkCategory("toList")>]
    member x.``MapNew_toList``() =
        MapNew.toList mapNew
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("enumerate")>]
    member x.``Map_enumerate``() =
        let mutable sum = LanguagePrimitives.GenericZero
        for KeyValue(_,v) in map do
            sum <- sum + v
        sum
        
    [<Benchmark>]
    [<BenchmarkCategory("enumerate")>]
    member x.``MapNew_enumerate``() =
        let mutable sum = LanguagePrimitives.GenericZero
        for KeyValue(_,v) in mapNew do
            sum <- sum + v
        sum
         
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("toSeq_enum")>]
    member x.``Map_toSeq_enum``() =
        let mutable sum = LanguagePrimitives.GenericZero
        for (_,v) in Map.toSeq map do
            sum <- sum + v
        sum
        
    [<Benchmark>]
    [<BenchmarkCategory("toSeq_enum")>]
    member x.``MapNew_toSeq_enum``() =
        let mutable sum = LanguagePrimitives.GenericZero
        for (_,v) in MapNew.toSeq mapNew do
            sum <- sum + v
        sum
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("containsKey_all")>]
    member x.``Map_containsKey_all``() =
        let mutable res = true
        for (k, _) in data do
            res <- Map.containsKey k map && res
        res

    [<Benchmark>]
    [<BenchmarkCategory("containsKey_all")>]
    member x.``MapNew_containsKey_all``() =
        let mutable res = true
        for (k, _) in data do
            res <- mapNew.ContainsKey k && res
        res
       
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("containsKey_nonexisting")>]
    member x.``Map_containsKey_nonexisting``() =
        Map.containsKey toolarge map
        
    [<Benchmark>]
    [<BenchmarkCategory("containsKey_nonexisting")>]
    member x.``MapNew_containsKey_nonexisting``() =
        mapNew.ContainsKey toolarge
         
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("tryFind")>]
    member x.``Map_tryFind``() =
        Map.tryFind (fst data.[0]) map
        
    [<Benchmark>]
    [<BenchmarkCategory("tryFind")>]
    member x.``MapNew_tryFind``() =
        mapNew.TryFind(fst data.[0])
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("tryFind_nonexisting")>]
    member x.``Map_tryFind_nonexisting``() =
        Map.tryFind toolarge map
        
    [<Benchmark>]
    [<BenchmarkCategory("tryFind_nonexisting")>]
    member x.``MapNew_tryFind_nonexisting``() =
        MapNew.tryFind toolarge mapNew
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("remove_all")>]
    member x.``Map_remove_all``() =
        
        let mutable res = map
        for (k, _) in data do
            res <- Map.remove k res
        res

    [<Benchmark>]
    [<BenchmarkCategory("remove_all")>]
    member x.``MapNew_remove_all``() =
        let mutable res = mapNew
        for (k, _) in data do
            res <- MapNew.remove k res
        res
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("exists")>]
    member x.``Map_exists``() =
        map |> Map.exists (fun _ _ -> false)
        
    [<Benchmark>]
    [<BenchmarkCategory("exists")>]
    member x.``MapNew_exists``() =
        mapNew |> MapNew.exists (fun _ _ -> false)
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("fold")>]
    member x.``Map_fold``() =
        (LanguagePrimitives.GenericZero, map) ||> Map.fold (fun s _ v -> s + v)

    [<Benchmark>]
    [<BenchmarkCategory("fold")>]
    member x.``MapNew_fold``() =
        (LanguagePrimitives.GenericZero, mapNew) ||> MapNew.fold (fun s _ v -> s + v)
        
    [<Benchmark(Baseline=true)>]
    [<BenchmarkCategory("foldBack")>]
    member x.``Map_foldBack``() =
        (map, LanguagePrimitives.GenericZero) ||> Map.foldBack (fun _ v s -> s + v)

    [<Benchmark>]
    [<BenchmarkCategory("foldBack")>]
    member x.``MapNew_foldBack``() =
        (mapNew, LanguagePrimitives.GenericZero) ||> MapNew.foldBack (fun _ v s -> s + v)


