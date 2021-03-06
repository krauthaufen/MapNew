﻿namespace Benchmark

open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open MapNew
open BenchmarkDotNet.Configs
open System
open Microsoft.FSharp.NativeInterop
open System.Runtime.CompilerServices
open Temp.FSharp.Collections
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

[<PlainExporter; MemoryDiagnoser>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
type MapBenchmark() =

    [<DefaultValue; Params(100)>]
    val mutable public Count : int

    let mutable data : (_ * _)[] = [||]
    let mutable list : list<_ * _> = []
    let mutable seq : seq<_ * _> = Seq.empty
    let mutable map = Map.ofArray data
    let mutable mapNew = MapNew.ofArray data
    let mutable yam = Yam.ofArray data
    let mutable yam2 = Yam.ofArray data
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
        
    static let computeDeltaNaive 
        (add : 'K -> 'V -> 'OP)
        (update : 'K -> 'V -> 'V -> voption<'OP>)
        (remove : 'K -> 'V -> 'OP)
        (l : Yam<'K, 'V>)
        (r : Yam<'K, 'V>) =

        let mutable res = Yam.empty
        let mutable l = l
        let mutable lv = Unchecked.defaultof<_>
        for KeyValue(k, rv) in r do
            if l.TryRemove(k, &l, &lv) then
                match update k lv rv with
                | ValueSome op ->
                    res <- Yam.add k op res
                | ValueNone ->
                    ()
            else
                res <- Yam.add k (add k rv) res
        for KeyValue(k, lv) in l do
            res <- Yam.add k (remove k lv) res

        res


    static let applyDeltaNaive
        (apply : 'K -> voption<'V> -> 'OP -> voption<'V>)
        (state : Yam<'K, 'V>)
        (delta : Yam<'K, 'OP>) =
            
        let mutable s = state
        for KeyValue(k, d) in delta do
            match Yam.tryFind k s with
            | Some v ->
                match apply k (ValueSome v) d with
                | ValueSome r -> s <- Yam.add k r s
                | ValueNone -> s <- Yam.remove k s
            | None ->
                match apply k ValueNone d with
                | ValueSome r -> s <- Yam.add k r s
                | ValueNone -> ()
        s



    [<GlobalSetup>]
    member x.Setup() =
        let valueRange = x.Count * 4 //1 <<< 30
        // int -> int
        let arr = randomArray valueRange x.Count |> Array.mapi (fun i v -> v, 1+i)

        // decimal -> int
        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> v, i)

        // ref -> int
        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> ReferenceNumber v, i)

        data <- arr
        list <- Array.toList data
        seq <- Seq.init data.Length (fun i -> arr.[i])
        map <- Map.ofArray data
        mapNew <- MapNew.ofArray data
        yam <- Yam.ofArray data
        existing <- data.[rand.Next(data.Length)] |> fst



        //let all = System.Collections.Generic.HashSet (data |> Array.map fst)
        //let arr = Array.zeroCreate x.Count
        //let mutable cnt = 0
        //while cnt < arr.Length do
        //    let v = rand.Next()
        //    if all.Add v then
        //        arr.[cnt] <- (v, cnt)
        //        cnt <- cnt + 1
        //randomElements <- arr

        let largest = arr |> Seq.map fst |> Seq.max
        toolarge <- LanguagePrimitives.GenericOne + largest //System.Decimal.MaxValue //System.Int32.MaxValue
        
        let arr2 = randomArray valueRange x.Count |> Array.mapi (fun i v -> v, 1+i)
        yam2 <- Yam.ofArray arr2
        randomElements <- arr2

        let test = Yam.union yam yam2
        printfn "overlap: %A" (2*x.Count - test.Count)



    //[<Benchmark>]
    //[<BenchmarkCategory("computeDelta")>]
    //member x.``Yam_computeDelta``() =
    //    (yam, yam2) ||> Yam.computeDelta 
    //        (fun k v -> v) 
    //        (fun k l r -> let v = r - l in if v <> 0 then ValueSome v else ValueNone) 
    //        (fun k v -> -v)
            
    //[<Benchmark>]
    //[<BenchmarkCategory("computeDelta")>]
    //member x.``Yam_computeDeltaNaive``() =
    //    (yam, yam2) ||> computeDeltaNaive
    //        (fun k v -> v)
    //        (fun k l r -> let v = r - l in if v <> 0 then ValueSome v else ValueNone)
    //        (fun k v -> -v)
    
            
    //[<Benchmark(Baseline = true)>]
    //[<BenchmarkCategory("applyDelta")>]
    //member x.``Yam_applyDeltaNaive``() =
    //    (yam, yam2) ||> applyDeltaNaive (fun k v o ->
    //        let cnt = 
    //            match v with
    //            | ValueSome v -> v + o
    //            | ValueNone -> o
    //        if cnt <> 0 then ValueSome cnt  
    //        else ValueNone
    //    )

    [<Benchmark>]
    member x.``Yam_applyDelta``() =
        (yam, yam2) ||> Yam.applyDelta (fun k v o ->
            let cnt = 
                match v with
                | ValueSome v -> v + o
                | ValueNone -> o
            if cnt <> 0 then ValueSome cnt  
            else ValueNone
        )

        
    [<Benchmark>]
    member x.``Yam_applyDeltaAndGetEffective``() =
        (yam, yam2) ||> Yam.applyDeltaAndGetEffective (fun k v o ->
            let cnt = 
                match v with
                | ValueSome v -> v + o
                | ValueNone -> o
            if cnt <> 0 then ValueSome cnt  
            else ValueNone
        )
        
    //[<Benchmark>]
    //member x.``Yam_computeDelta``() =
    //    (yam, yam2) ||> Yam.computeDelta 
    //        (fun _ v -> v)
    //        (fun _ a b -> let v = b - a in if v <> 0 then ValueSome v else ValueNone)
    //        (fun _ v -> -v)
        
    //[<Benchmark>]
    //member x.``Yam_unionWith``() =
    //    (yam, yam2) ||> Yam.unionWith (fun _ a b -> a + b)
            
            
    //[<Benchmark>]
    //member x.``Yam_union``() =
    //    (yam, yam2) ||> Yam.union
            

    //[<Benchmark>]
    //member x.``Yam_add``() =
    //    let mutable r = yam
    //    for (k, v) in randomElements do
    //        r <- r.Add(k, v)
    //    r
    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("add")>]
    //member x.``Map_add``() =
    //    let r = map
    //    for (k, v) in randomElements do
    //        Map.add k v r |> keep

    //[<Benchmark>]
    //[<BenchmarkCategory("remove")>]
    //member x.``Yam_remove``() =
    //    let r = yam
    //    for i in 0 .. min randomElements.Length data.Length - 1 do
    //        let (k,_) = data.[i]
    //        Yam.remove k r |> keep
        
    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("remove")>]
    //member x.``Map_remove``() =
    //    let r = map
    //    for i in 0 .. min randomElements.Length data.Length - 1 do
    //        let (k,_) = data.[i]
    //        Map.remove k r |> keep

    //[<Benchmark>]
    //[<BenchmarkCategory("ofArray")>]
    //member x.``Yam_ofArray``() =
    //    Yam.ofArray data

    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("ofArray")>]
    //member x.``Map_ofArray``() =
    //    Map.ofArray data
        

    //[<Benchmark>]
    //[<BenchmarkCategory("toArray")>]
    //member x.``Yam_toArray``() =
    //    Yam.toArray yam

    //[<Benchmark(Baseline = true)>]
    //[<BenchmarkCategory("toArray")>]
    //member x.``Map_toArray``() =
    //    Map.toArray map
        

//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("enumerate")>]
//    member x.``Map_enumerate``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for KeyValue(_,v) in map do
//            sum <- sum + v
//        sum
        
//    [<Benchmark>]
//    [<BenchmarkCategory("enumerate")>]
//    member x.``MapNew_enumerate``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for KeyValue(_,v) in mapNew do
//            sum <- sum + v
//        sum
         
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("toSeq_enum")>]
//    member x.``Map_toSeq_enum``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for (_,v) in Map.toSeq map do
//            sum <- sum + v
//        sum
        
//    [<Benchmark>]
//    [<BenchmarkCategory("toSeq_enum")>]
//    member x.``MapNew_toSeq_enum``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for (_,v) in MapNew.toSeq mapNew do
//            sum <- sum + v
//        sum
        
    //[<Benchmark>]
    //[<BenchmarkCategory("containsKey_all")>]
    //member x.``Yam_containsKey_all``() =
    //    let mutable res = true
    //    for (k, _) in data do
    //        res <- yam.ContainsKey k && res
    //    res

    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("containsKey_all")>]
    //member x.``Map_containsKey_all``() =
    //    let mutable res = true
    //    for (k, _) in data do
    //        res <- Map.containsKey k map && res
    //    res

    //[<Benchmark>]
    //[<BenchmarkCategory("containsKey_nonexisting")>]
    //member x.``Yam_containsKey_nonexisting``() =
    //    yam.ContainsKey toolarge
       
    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("containsKey_nonexisting")>]
    //member x.``Map_containsKey_nonexisting``() =
    //    Map.containsKey toolarge map
        
         
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("tryFind")>]
//    member x.``Map_tryFind``() =
//        Map.tryFind (fst data.[0]) map
        
//    [<Benchmark>]
//    [<BenchmarkCategory("tryFind")>]
//    member x.``MapNew_tryFind``() =
//        mapNew.TryFind(fst data.[0])
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("tryFind_nonexisting")>]
//    member x.``Map_tryFind_nonexisting``() =
//        Map.tryFind toolarge map
        
//    [<Benchmark>]
//    [<BenchmarkCategory("tryFind_nonexisting")>]
//    member x.``MapNew_tryFind_nonexisting``() =
//        MapNew.tryFind toolarge mapNew
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("remove_all")>]
//    member x.``Map_remove_all``() =
        
//        let mutable res = map
//        for (k, _) in data do
//            res <- Map.remove k res
//        res

//    [<Benchmark>]
//    [<BenchmarkCategory("remove_all")>]
//    member x.``MapNew_remove_all``() =
//        let mutable res = mapNew
//        for (k, _) in data do
//            res <- MapNew.remove k res
//        res
    //[<Benchmark>]
    //[<BenchmarkCategory("exists")>]
    //member x.``Yam_exists``() =
    //    yam |> Yam.exists (fun _ _ -> false)
        
    //[<Benchmark(Baseline=true)>]
    //[<BenchmarkCategory("exists")>]
    //member x.``Map_exists``() =
    //    map |> Map.exists (fun _ _ -> false)
        
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("fold")>]
//    member x.``Map_fold``() =
//        (LanguagePrimitives.GenericZero, map) ||> Map.fold (fun s _ v -> s + v)

//    [<Benchmark>]
//    [<BenchmarkCategory("fold")>]
//    member x.``MapNew_fold``() =
//        (LanguagePrimitives.GenericZero, mapNew) ||> MapNew.fold (fun s _ v -> s + v)
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("foldBack")>]
//    member x.``Map_foldBack``() =
//        (map, LanguagePrimitives.GenericZero) ||> Map.foldBack (fun _ v s -> s + v)

//    [<Benchmark>]
//    [<BenchmarkCategory("foldBack")>]
//    member x.``MapNew_foldBack``() =
//        (mapNew, LanguagePrimitives.GenericZero) ||> MapNew.foldBack (fun _ v s -> s + v)


        
//[<PlainExporter; MemoryDiagnoser; IterationTime(100.0); MaxIterationCount(20)>]
//[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
//type SetBenchmark() =

//    [<DefaultValue; Params(100)>]
//    val mutable public Count : int

//    let mutable data : _[] = [||]
//    let mutable list : list<_> = []
//    let mutable seq : seq<_> = Seq.empty
//    let mutable set = Set.ofArray data
//    let mutable setNew = SetNew.ofArray data
//    let mutable existing = Unchecked.defaultof<_> //LanguagePrimitives.GenericZero
//    let mutable toolarge = Unchecked.defaultof<_> //LanguagePrimitives.GenericZero
//    let mutable randomElements  : _[] = [||]

//    static let rand = System.Random()

//    static let randomArray (maxValue : int) (n : int) =
//        let s = System.Collections.Generic.HashSet<int>()
//        let res = Array.zeroCreate n
//        let mutable i = 0
//        while i < n do
//            let v = rand.Next(maxValue)
//            if s.Add v then
//                res.[i] <- v
//                i <- i + 1
//        res

//    static let randomDecimalArray (n : int) =
//        let s = System.Collections.Generic.HashSet<decimal>()
//        let res = Array.zeroCreate n
//        let mutable i = 0
//        while i < n do
//            let v = rand.NextDouble() |> decimal
//            if s.Add v then
//                res.[i] <- v
//                i <- i + 1
//        res

//    [<MethodImpl(MethodImplOptions.NoInlining)>]
//    static let keep (v : 'a) =
//        ()

//    [<GlobalSetup>]
//    member x.Setup() =
//        // int -> int
//        let arr = randomArray (1 <<< 30) x.Count

//        // decimal -> int
//        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> v, i)

//        // ref -> int
//        //let arr = randomDecimalArray x.Count |> Array.mapi (fun i v -> ReferenceNumber v, i)

//        data <- arr
//        list <- Array.toList data
//        seq <- Seq.init data.Length (fun i -> arr.[i])
//        set <- Set.ofArray data
//        setNew <- SetNew.ofArray data
//        existing <- data.[rand.Next(data.Length)] 

//        let all = System.Collections.Generic.HashSet data 
//        let arr = Array.zeroCreate x.Count
//        let mutable cnt = 0
//        while cnt < arr.Length do
//            let v = rand.Next()
//            if all.Add v then
//                arr.[cnt] <- v
//                cnt <- cnt + 1
//        randomElements <- arr

//        let largest = arr |> Seq.max
//        toolarge <- LanguagePrimitives.GenericOne + largest //System.Decimal.MaxValue //System.Int32.MaxValue

        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("add")>]
//    member x.``Set_add``() =
//        let r = set
//        for v in randomElements do
//            Set.add v r |> keep

//    [<Benchmark>]
//    [<BenchmarkCategory("add")>]
//    member x.``SetNew_add``() =
//        let r = setNew
//        for v in randomElements do
//            SetNew.add v r |> keep
       
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("remove")>]
//    member x.``Set_remove``() =
//        let r = set
//        for i in 0 .. min randomElements.Length data.Length - 1 do
//            let k = data.[i]
//            Set.remove k r |> keep

//    [<Benchmark>]
//    [<BenchmarkCategory("remove")>]
//    member x.``SetNew_remove``() =
//        let r = setNew
//        for i in 0 .. min randomElements.Length data.Length - 1 do
//            let k = data.[i]
//            SetNew.remove k r |> keep
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("ofArray")>]
//    member x.``Set_ofArray``() =
//        Set.ofArray data

//    [<Benchmark>]
//    [<BenchmarkCategory("ofArray")>]
//    member x.``SetNew_ofArray``() =
//        SetNew.ofArray data
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("ofList")>]
//    member x.``Set_ofList``() =
//        Set.ofList list
        
//    [<Benchmark>]
//    [<BenchmarkCategory("ofList")>]
//    member x.``SetNew_ofList``() =
//        SetNew.ofList list
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("ofSeq")>]
//    member x.``Set_ofSeq``() =
//        Set.ofSeq list
        
//    [<Benchmark>]
//    [<BenchmarkCategory("ofSeq")>]
//    member x.``SetNew_ofSeq``() =
//        SetNew.ofSeq list

//    [<Benchmark(Baseline = true)>]
//    [<BenchmarkCategory("toArray")>]
//    member x.``Set_toArray``() =
//        Set.toArray set
        
//    [<Benchmark>]
//    [<BenchmarkCategory("toArray")>]
//    member x.``SetNew_toArray``() =
//        SetNew.toArray setNew
        
//    [<Benchmark(Baseline = true)>]
//    [<BenchmarkCategory("toList")>]
//    member x.``Set_toList``() =
//        Set.toList set
        
//    [<Benchmark>]
//    [<BenchmarkCategory("toList")>]
//    member x.``SetNew_toList``() =
//        SetNew.toList setNew
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("enumerate")>]
//    member x.``Set_enumerate``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for v in set do
//            sum <- sum + v
//        sum
        
//    [<Benchmark>]
//    [<BenchmarkCategory("enumerate")>]
//    member x.``SetNew_enumerate``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for v in setNew do
//            sum <- sum + v
//        sum
         
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("toSeq_enum")>]
//    member x.``Set_toSeq_enum``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for v in Set.toSeq set do
//            sum <- sum + v
//        sum
        
//    [<Benchmark>]
//    [<BenchmarkCategory("toSeq_enum")>]
//    member x.``SetNew_toSeq_enum``() =
//        let mutable sum = LanguagePrimitives.GenericZero
//        for v in SetNew.toSeq setNew do
//            sum <- sum + v
//        sum
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("containsKey_all")>]
//    member x.``Set_contains_all``() =
//        let mutable res = true
//        for k in data do
//            res <- Set.contains k set && res
//        res

//    [<Benchmark>]
//    [<BenchmarkCategory("containsKey_all")>]
//    member x.``SetNew_containsKey_all``() =
//        let mutable res = true
//        for k in data do
//            res <- setNew.Contains k && res
//        res
       
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("containsKey_nonexisting")>]
//    member x.``Set_containsKey_nonexisting``() =
//        Set.contains toolarge set
        
//    [<Benchmark>]
//    [<BenchmarkCategory("containsKey_nonexisting")>]
//    member x.``SetNew_containsKey_nonexisting``() =
//        setNew.Contains toolarge
   
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("remove_all")>]
//    member x.``Set_remove_all``() =
        
//        let mutable res = set
//        for k in data do
//            res <- Set.remove k res
//        res

//    [<Benchmark>]
//    [<BenchmarkCategory("remove_all")>]
//    member x.``SetNew_remove_all``() =
//        let mutable res = setNew
//        for k in data do
//            res <- SetNew.remove k res
//        res
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("exists")>]
//    member x.``Set_exists``() =
//        set |> Set.exists (fun _ -> false)
        
//    [<Benchmark>]
//    [<BenchmarkCategory("exists")>]
//    member x.``SetNew_exists``() =
//        setNew |> SetNew.exists (fun _ -> false)
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("fold")>]
//    member x.``Set_fold``() =
//        (LanguagePrimitives.GenericZero, set) ||> Set.fold (fun s v -> s + v)

//    [<Benchmark>]
//    [<BenchmarkCategory("fold")>]
//    member x.``SetNew_fold``() =
//        (LanguagePrimitives.GenericZero, setNew) ||> SetNew.fold (fun s v -> s + v)
        
//    [<Benchmark(Baseline=true)>]
//    [<BenchmarkCategory("foldBack")>]
//    member x.``Set_foldBack``() =
//        (set, LanguagePrimitives.GenericZero) ||> Set.foldBack (fun v s -> s + v)

//    [<Benchmark>]
//    [<BenchmarkCategory("foldBack")>]
//    member x.``SetNew_foldBack``() =
//        (setNew, LanguagePrimitives.GenericZero) ||> SetNew.foldBack (fun v s -> s + v)