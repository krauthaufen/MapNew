module MapTests

open FsUnit
open Expecto
open MapNew

let cfg = 
    { FsCheckConfig.defaultConfig with
        maxTest = 400
        startSize = 1
        endSize = 400
    }

let testProperty name action =
    testPropertyWithConfig cfg name action


let checkConsistency (mn : MapNew<'K, 'V>) =
    
    let rec checkHeight (n : MapNewImplementation.MapNode<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> 0
        | :? MapNewImplementation.MapLeaf<'K, 'V> -> 1
        | :? MapNewImplementation.MapInner<'K, 'V> as n ->
            let lh = checkHeight n.Left
            let rh = checkHeight n.Right
            let b = abs (rh - lh)
            if b > 2 then failwithf "node has bad balance: %d" (rh - lh)
            let h = 1 + max lh rh
            if n._Height <> h then failwithf "node has bad height: %d (should be %d)" n._Height h
            h
        | _ ->
            failwith "unexpected node"
            
    let rec checkCount (n : MapNewImplementation.MapNode<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> 0
        | :? MapNewImplementation.MapLeaf<'K, 'V> -> 1
        | :? MapNewImplementation.MapInner<'K, 'V> as n ->
            let lh = checkCount n.Left
            let rh = checkCount n.Right
            let h = 1 + lh + rh
            if n._Count <> h then failwithf "node has bad count: %d (should be %d)" n._Count h
            h
        | _ ->
            failwith "unexpected node"

    checkHeight mn.Root |> ignore
    checkCount mn.Root |> ignore

let identical (mn : MapNew<'K, 'V>) (m : Map<'K, 'V>) =
    checkConsistency mn

    let rec getAll (n : MapNewImplementation.MapNode<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> []
        | :? MapNewImplementation.MapLeaf<'K, 'V> as l -> [l.Key, l.Value]
        | :? MapNewImplementation.MapInner<'K, 'V> as n ->
            let l = getAll n.Left
            let r = getAll n.Right
            l @ [n.Key, n.Value] @ r
        | _ ->
            failwith "unexpected node"


    let a = getAll mn.Root
    let b = Map.toList m
    a |> should equal b

    MapNew.count mn |> should equal (Map.count m)

let rand = System.Random()

module Map =
    let union (l : Map<'K, 'V>) (r : Map<'K, 'V>) =
        let mutable l = l
        for KeyValue(k,v) in r do
            l <- Map.add k v l
        l

    let unionWith (resolve : 'K -> 'V -> 'V -> 'V) (l : Map<'K, 'V>) (r : Map<'K, 'V>) =
        let mutable l = l
        for KeyValue(k,v) in r do   
            l <-
                l |> Map.change k (function
                    | Some lv -> resolve k lv v |> Some
                    | None -> v |> Some
                )
        l

        
    let ofListRandomOrder (rand : System.Random) (l : list<'K * 'V>) =
        let l = Map.ofList l |> Map.toList
        let rec run (rand : System.Random) (acc : Map<'K, 'V>) (l : list<'K * 'V>) =
            match l with
            | [] ->
                acc
            | (k,v) :: t ->
                if rand.NextDouble() >= 0.5 then
                    run rand (Map.add k v acc) t
                else
                    run rand acc t |> Map.add k v
        run rand Map.empty l

    let neighbours (key : 'K) (map : Map<'K, 'V>) =
        let l = Map.toList map
        let rec run (key : 'K) (last : option<'K * 'V>) (l : list<'K * 'V>) =
            match l with
            | [] -> last, None, None
            | [k,v] -> 
                if key < k then last, None, Some(k,v)
                elif k < key then Some(k,v), None, None
                else last, Some(v), None
            | (k,v) :: rest ->
                if key < k then last, None, Some(k,v)
                elif k < key then run key (Some(k,v)) rest
                else last, Some v, List.tryHead rest
        run key None l
        
    let neighboursAt (index : int) (map : Map<'K, 'V>) =
        let arr = Map.toArray map
        if index < 0 then
            None, None, Array.tryHead arr
        elif index >= arr.Length then
            Array.tryLast arr, None, None
        else
            let l = if index > 0 then Some arr.[index-1] else None
            let r = if index < arr.Length-1 then Some arr.[index+1] else None
            let s = arr.[index] |> snd
            l, Some s, r

    let replaceRange (minKey : 'K) (maxKey : 'K) replace (map : Map<'K, 'V>) =
        if minKey > maxKey then 
            map
        else
            let l = map |> Map.filter (fun k _ -> k < minKey)
            let r = map |> Map.filter (fun k _ -> k > maxKey)

            let vv o = match o with | Some o -> ValueSome o | None -> ValueNone

            let ln = l |> Seq.map (fun (KeyValue(k,v)) -> struct(k, v)) |> Seq.tryLast |> vv
            let rn = r |> Seq.map (fun (KeyValue(k,v)) -> struct(k, v)) |> Seq.tryHead |> vv
            let struct(lv, rv) = replace ln rn

            let mutable res = union l r 
            match lv with
            | ValueSome v -> res <- Map.add minKey v res
            | ValueNone -> ()
            match rv with
            | ValueSome v -> res <- Map.add maxKey v res
            | ValueNone -> ()
            res

    let computeDelta 
        (add : 'K -> 'V -> 'OP)
        (update : 'K -> 'V -> 'V -> voption<'OP>)
        (remove : 'K -> 'V -> 'OP)
        (l : Map<'K, 'V>)
        (r : Map<'K, 'V>) =

        let mutable res = Map.empty
        let mutable l = l
        for KeyValue(k, rv) in r do
            match Map.tryFind k l with
            | Some lv ->
                l <- Map.remove k l
                match update k lv rv with
                | ValueSome op ->
                    res <- Map.add k op res
                | ValueNone ->
                    ()
            | None ->
                res <- Map.add k (add k rv) res
        for KeyValue(k, lv) in l do
            res <- Map.add k (remove k lv) res

        res

    let applyDelta 
        (apply : 'K -> voption<'V> -> 'OP -> voption<'V>)
        (state : Map<'K, 'V>)
        (delta : Map<'K, 'OP>) =
            
        let mutable s = state
        for KeyValue(k, d) in delta do
            match Map.tryFind k s with
            | Some v ->
                match apply k (ValueSome v) d with
                | ValueSome r -> s <- Map.add k r s
                | ValueNone -> s <- Map.remove k s
            | None ->
                match apply k ValueNone d with
                | ValueSome r -> s <- Map.add k r s
                | ValueNone -> ()
        s




module MapNew =

    let ofListRandomOrder (rand : System.Random) (l : list<'K * 'V>) =
        let l = Map.ofList l |> Map.toList
        let rec run (rand : System.Random) (acc : MapNew<'K, 'V>) (l : list<'K * 'V>) =
            match l with
            | [] ->
                acc
            | (k,v) :: t ->
                if rand.NextDouble() >= 0.5 then
                    run rand (MapNew.add k v acc) t
                else
                    run rand acc t |> MapNew.add k v
        run rand MapNew.empty l

[<Tests>]
let tests =
    testList "MapNew" [

        testProperty "ofSeq" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofSeq l
            identical mn m
        )
        
        testProperty "ofList" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofList l
            identical mn m
        )
        
        testProperty "ofArray" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofArray (List.toArray l)
            identical mn m
        )

        
        testProperty "ofSeqV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofSeqV (l |> List.map (fun (a,b) -> struct(a,b)))
            identical mn m
        )
        
        testProperty "ofListV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofListV (l |> List.map (fun (a,b) -> struct(a,b)))
            identical mn m
        )
        
        testProperty "ofArrayV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofArrayV (l |> List.map (fun (a,b) -> struct(a,b)) |> List.toArray)
            identical mn m
        )




        testProperty "enumerate" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m

            for (KeyValue(lk, lv)), (KeyValue(rk, rv)) in Seq.zip m mn do
                rk |> should equal lk
                rv |> should equal lv
        )

        testProperty "toSeq" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            for (lk, lv), (rk, rv) in Seq.zip (Map.toSeq m) (MapNew.toSeq mn) do
                rk |> should equal lk
                rv |> should equal lv
        )

        testProperty "toList" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            (MapNew.toList mn) |> should equal (Map.toList m)
        )

        testProperty "toArray" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            (MapNew.toArray mn) |> should equal (Map.toArray m)
        )
        
        testProperty "toArrayV" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            MapNew.toArrayV mn |> should equal (Map.toArray m |> Array.map (fun (a,b) -> struct(a,b)))
        )
        testProperty "toListV" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            MapNew.toListV mn |> should equal (Map.toList m |> List.map (fun (a,b) -> struct(a,b)))
        )
        testProperty "toSeqV" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
            MapNew.toSeqV mn |> Seq.toList |> should equal (Map.toList m |> List.map (fun (a,b) -> struct(a,b)))
        )

        testProperty "containsKey" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> MapNew.containsKey k
            let b = m |> Map.containsKey k
            a |> should equal b
        )
        
        testProperty "tryFind" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> MapNew.tryFind k
            let b = m |> Map.tryFind k
            a |> should equal b
        )

        testProperty "add" (fun (m : Map<int, int>) (k : int) ->
            let mutable mn = MapNew.empty
            for KeyValue(k,v) in m do mn <- MapNew.add k v mn
            identical mn m

            let a = mn |> MapNew.add k k
            let b = m |> Map.add k k
            identical a b
        )
    
    
        testProperty "neighbours" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let all = k :: List.map fst (Map.toList m)

            for k in all do
                let (ml,ms,mr) = Map.neighbours k m
                let (nl,ns,nr) = MapNew.neighbours k mn
                nl |> should equal ml
                ns |> should equal ms
                nr |> should equal mr

        )
        
    
        testProperty "replaceRange" (fun (m : Map<int, int>) (l : int) (h : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let replacement (l : voption<struct(int * int)>) (r : voption<struct(int * int)>) =
                struct(ValueSome (Unchecked.hash l), ValueSome (Unchecked.hash r))

            let l, h = if l < h then l, h else h, l
            let a = m |> Map.replaceRange l h replacement
            let b = mn |> MapNew.replaceRange l h replacement

            identical b a

        )
        testProperty "neighboursAt" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            for k in -1 .. mn.Count do
                let (ml,ms,mr) = Map.neighboursAt k m
                let (nl,ns,nr) = MapNew.neighboursAt k mn
                nl |> should equal ml
                ns |> should equal ms
                nr |> should equal mr

        )

        testProperty "remove" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m
            
            let a = mn |> MapNew.remove k
            let b = m |> Map.remove k
            identical a b
        
            let mutable a = mn
            let mutable b = m
            for (KeyValue(k,_)) in m do
                a <- MapNew.remove k a
                b <- Map.remove k b
                identical a b

        )
        
        testProperty "partition" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m

            let ma, mb = m |> Map.partition (fun k v -> k >= v)
            let na, nb = mn |> MapNew.partition (fun k v -> k >= v)

            identical na ma
            identical nb mb


        )
        
        
        testProperty "fold" (fun (m : Map<int, int>)  ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = (0, mn) ||> MapNew.fold (fun s k _ -> s * 27 + k)
            let b = (0, m) ||> Map.fold (fun s k _ -> s * 27 + k)
            a |> should equal b
        )
        
        testProperty "foldBack" (fun (m : Map<int, int>)  ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = (mn, 0) ||> MapNew.foldBack (fun _ k s -> s * 27 + k)
            let b = (m, 0) ||> Map.foldBack (fun _ k s -> s * 27 + k)
            a |> should equal b
        )

        
        testProperty "exists" (fun (m : Map<int, int>)  ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> MapNew.exists (fun k v -> k % 3 = 2)
            let b = m |> Map.exists (fun k v -> k % 3 = 2)
            a |> should equal b
        )
        
        
        testProperty "forall" (fun (m : Map<int, int>)  ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> MapNew.forall (fun k v -> k % 3 < 2)
            let b = m |> Map.forall (fun k v -> k % 3 < 2)
            a |> should equal b
        )
        
        testProperty "count" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m
            MapNew.count mn |> should equal (Map.count m)
        )

        testProperty "tryMin/tryMax" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let mMin = m |> Seq.tryHead |> Option.map(function KeyValue(k,v) -> k,v)
            let nMin = MapNew.tryMin mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast |> Option.map(function KeyValue(k,v) -> k,v)
            let nMax = MapNew.tryMax mn
            nMax |> should equal mMax

            let inline toVOption o =
                match o with
                | Some(KeyValue(k,v)) -> ValueSome(struct(k,v))
                | None -> ValueNone

            let mMin = m |> Seq.tryHead |> toVOption
            let nMin = MapNew.tryMinV mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast |> toVOption
            let nMax = MapNew.tryMaxV mn
            nMax |> should equal mMax
        )
        
        testProperty "change" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let m1 = m |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> MapNew.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1

            
            let m1 = m |> Map.add k k |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> MapNew.add k k |> MapNew.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1

            
            let m1 = m |> Map.remove k |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> MapNew.remove k |> MapNew.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1
        )

        testProperty "withRange" (fun (m : Map<int, int>) ->
            let m = m |> Map.add 1 2 |> Map.add 3 4 |> Map.add 5 6
            let arr = Map.toArray m
            let mn = MapNew.ofArray arr
            identical mn m

            let l,_ = arr.[arr.Length / 3]
            let h,_ = arr.[(2 * arr.Length) / 3]

            let a = MapNew.withRange l h mn
            let b = m |> Map.filter (fun k _ -> k >= l && k <= h)
            identical a b
        )

        testProperty "union" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = MapNew.ofArray (Map.toArray a)
            let nb = MapNew.ofArray (Map.toArray b)
            
            identical (MapNew.union na nb) (Map.union a b)
        )
        
        testProperty "unionWith" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = MapNew.ofArray (Map.toArray a)
            let nb = MapNew.ofArray (Map.toArray b)
            
            let resolve k l r =
                27*k+13*l+r

            identical (MapNew.unionWith resolve na nb) (Map.unionWith resolve a b)
        )

        


        testProperty "tryAt" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let arr = m |> Map.toArray

            let inline tup v =
                match v with
                | ValueSome struct(a,b) -> Some(a,b)
                | ValueNone -> None

            for i in -1 .. arr.Length do
                mn 
                |> MapNew.tryAt i 
                |> should equal (Array.tryItem i arr)

                mn 
                |> MapNew.tryAtV i 
                |> tup
                |> should equal (Array.tryItem i arr)
        )

        
        testProperty "compare" (fun (la : list<int * int>) (lb : list<int * int>) ->
            let na = MapNew.ofList la
            let nb = MapNew.ofList lb
            let a = Map.ofList la
            let b = Map.ofList lb

            compare na nb |> should equal (compare a b)

            let somea = List.init 8 (fun _ -> MapNew.ofListRandomOrder rand la)
            let someb = List.init 8 (fun _ -> MapNew.ofListRandomOrder rand lb)

            for (va, vb) in List.allPairs somea someb do
                compare va na |> should equal 0
                compare vb nb |> should equal 0
                compare va vb |> should equal (compare a b)
        )

        
        testProperty "equals" (fun (la : list<int * int>) (lb : list<int * int>) ->
            let na = MapNew.ofList la
            let nb = MapNew.ofList lb
            let a = Map.ofList la
            let b = Map.ofList lb

            let e = Unchecked.equals a b
            let ne = Unchecked.equals na nb

            ne |> should equal e
            if ne then
                let ha = Unchecked.hash na
                let hb = Unchecked.hash nb
                ha |> should equal hb

            
            let somea = List.init 8 (fun _ -> MapNew.ofListRandomOrder rand la)
            let someb = List.init 8 (fun _ -> MapNew.ofListRandomOrder rand lb)

            for (a, b) in List.allPairs somea somea do
                a |> should equal b

            for (a, b) in List.allPairs someb someb do
                a |> should equal b


        )


    ]

[<AutoOpen>]
module Yam = 
    let checkConsistency (mn : Yam<'K, 'V>) =
    
        let rec checkHeight (n : YamImplementation.Node<'K, 'V>) =
            if isNull n then 0uy
            elif n.Height = 1uy then 1uy
            else
                let n = n :?> YamImplementation.Inner<'K, 'V>
                let lh = checkHeight n.Left
                let rh = checkHeight n.Right
                let b = abs (int rh - int lh)
                if b > 2 then failwithf "node has bad balance: %d" (rh - lh)
                let h = 1uy + max lh rh
                if n.Height <> h then failwithf "node has bad height: %d (should be %d)" n.Height h
                h
             
        let rec checkCount (n : YamImplementation.Node<'K, 'V>) =
            if isNull n then 0
            elif n.Height = 1uy then 1
            else
                let n = n :?> YamImplementation.Inner<'K, 'V>
                let lh = checkCount n.Left
                let rh = checkCount n.Right
                let h = 1 + lh + rh
                //if n.Count <> h then failwithf "node has bad count: %d (should be %d)" n.Count h
                h

        checkHeight mn.Root |> ignore
        checkCount mn.Root |> ignore

    let identical (mn : Yam<'K, 'V>) (m : Map<'K, 'V>) =
        checkConsistency mn

        let rec getAll (n : YamImplementation.Node<'K, 'V>) =
            if isNull n then []
            elif n.Height = 1uy then [n.Key, n.Value]
            else 
                let n = n :?> YamImplementation.Inner<'K, 'V>
                let l = getAll n.Left
                let r = getAll n.Right
                l @ [n.Key, n.Value] @ r


        let a = getAll mn.Root
        let b = Map.toList m
        a |> should equal b

        Yam.count mn |> should equal (Map.count m)


[<Tests>]
let yamTests =

    let memory (creator : unit -> 'a) =
        creator() |> ignore
        System.GC.Collect(3, System.GCCollectionMode.Forced, true, true)
        System.GC.WaitForFullGCComplete() |> ignore
        let before = System.GC.GetTotalMemory(true)
        let a = creator()
        System.GC.Collect(3, System.GCCollectionMode.Forced, true, true)
        System.GC.WaitForFullGCComplete() |> ignore
        let size = System.GC.GetTotalMemory(true) - before
        size + int64 (float (Unchecked.hash a % 2) - 0.5)



    testList "Yam" [
        //testProperty "memorySize" (fun (size : uint32) ->
        //    let l = List.init (int size) (fun i -> i,i)
        //    let mm = memory (fun () -> Map.ofList l)
        //    let my = memory (fun () -> Yam.ofList l)

        //    mm |> should be (greaterThanOrEqualTo (my * 3L / 2L))


        //)

        testProperty "ofSeq" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofSeq l
            identical mn m
        )
        
        testProperty "ofList" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofList l
            identical mn m
        )
        
        testProperty "ofArray" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofArray (List.toArray l)
            identical mn m
        )

        
        testProperty "ofSeqV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofSeqV (l |> List.map (fun (a,b) -> struct(a,b)))
            identical mn m
        )
        
        testProperty "ofListV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofListV (l |> List.map (fun (a,b) -> struct(a,b)))
            identical mn m
        )
        
        testProperty "ofArrayV" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofArrayV (l |> List.map (fun (a,b) -> struct(a,b)) |> List.toArray)
            identical mn m
        )

        testProperty "enumerate" (fun (l : list<int * int>) ->
            let m = Map.ofList l
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m

            for (KeyValue(lk, lv)), (KeyValue(rk, rv)) in Seq.zip m mn do
                rk |> should equal lk
                rv |> should equal lv
        )

        testProperty "toSeq" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            for (lk, lv), (rk, rv) in Seq.zip (Map.toSeq m) (Yam.toSeq mn) do
                rk |> should equal lk
                rv |> should equal lv
        )

        testProperty "toList" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            (Yam.toList mn) |> should equal (Map.toList m)
        )

        testProperty "toArray" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            (Yam.toArray mn) |> should equal (Map.toArray m)
        )
        
        testProperty "toArrayV" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            Yam.toArrayV mn |> should equal (Map.toArray m |> Array.map (fun (a,b) -> struct(a,b)))
        )
        testProperty "toListV" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            Yam.toListV mn |> should equal (Map.toList m |> List.map (fun (a,b) -> struct(a,b)))
        )
        testProperty "toSeqV" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m
            let a = Yam.toSeqV mn |> Seq.toList
            let b = Map.toList m |> List.map (fun (a,b) -> struct(a,b))
            a |> should equal b
        )

        testProperty "containsKey" (fun (m : Map<int, int>) (k : int) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> Yam.containsKey k
            let b = m |> Map.containsKey k
            a |> should equal b
        )
        
        testProperty "tryFind" (fun (m : Map<int, int>) (k : int) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> Yam.tryFind k
            let b = m |> Map.tryFind k
            a |> should equal b
        )

        testProperty "add" (fun (m : Map<int, int>) (k : int) ->
            let mutable mn = Yam.empty
            for KeyValue(k,v) in m do mn <- Yam.add k v mn
            identical mn m

            let a = mn |> Yam.add k k
            let b = m |> Map.add k k
            identical a b
        )
    
    
        //testProperty "neighbours" (fun (m : Map<int, int>) (k : int) ->
        //    let mn = Yam.ofSeq (Map.toSeq m)
        //    identical mn m

        //    let all = k :: List.map fst (Map.toList m)

        //    for k in all do
        //        let (ml,ms,mr) = Map.neighbours k m
        //        let (nl,ns,nr) = Yam.neighbours k mn
        //        nl |> should equal ml
        //        ns |> should equal ms
        //        nr |> should equal mr

        //)
        
    
        testProperty "replaceRange" (fun (m : Map<int, int>) (l : int) (h : int) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let replacement (l : voption<struct(int * int)>) (r : voption<struct(int * int)>) =
                struct(ValueSome (Unchecked.hash l), ValueSome (Unchecked.hash r))

            let l, h = if l < h then l, h else h, l
            let a = m |> Map.replaceRange l h replacement
            let b = mn |> Yam.replaceRangeV l h replacement

            identical b a

        )
        //testProperty "neighboursAt" (fun (m : Map<int, int>) ->
        //    let mn = Yam.ofSeq (Map.toSeq m)
        //    identical mn m

        //    for k in -1 .. mn.Count do
        //        let (ml,ms,mr) = Map.neighboursAt k m
        //        let (nl,ns,nr) = Yam.neighboursAt k mn
        //        nl |> should equal ml
        //        ns |> should equal ms
        //        nr |> should equal mr

        //)

        testProperty "remove" (fun (m : Map<int, int>) (k : int) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m
            
            let a = mn |> Yam.remove k
            let b = m |> Map.remove k
            identical a b
        
            let mutable a = mn
            let mutable b = m
            for (KeyValue(k,_)) in m do
                a <- Yam.remove k a
                b <- Map.remove k b
                identical a b

        )
        
        testProperty "partition" (fun (m : Map<int, int>) ->
            let mn = Yam.ofArray (Map.toArray m)
            identical mn m

            let ma, mb = m |> Map.partition (fun k v -> k >= v)
            let na, nb = mn |> Yam.partition (fun k v -> k >= v)

            identical na ma
            identical nb mb


        )
        
        testProperty "fold" (fun (m : Map<int, int>)  ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = (0, mn) ||> Yam.fold (fun s k _ -> s * 27 + k)
            let b = (0, m) ||> Map.fold (fun s k _ -> s * 27 + k)
            a |> should equal b
        )
        
        testProperty "foldBack" (fun (m : Map<int, int>)  ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = (mn, 0) ||> Yam.foldBack (fun _ k s -> s * 27 + k)
            let b = (m, 0) ||> Map.foldBack (fun _ k s -> s * 27 + k)
            a |> should equal b
        )

        
        testProperty "exists" (fun (m : Map<int, int>)  ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> Yam.exists (fun k v -> k % 3 = 2)
            let b = m |> Map.exists (fun k v -> k % 3 = 2)
            a |> should equal b
        )
        
        
        testProperty "forall" (fun (m : Map<int, int>)  ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> Yam.forall (fun k v -> k % 3 < 2)
            let b = m |> Map.forall (fun k v -> k % 3 < 2)
            a |> should equal b
        )
        
        testProperty "count" (fun (m : Map<int, int>) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m
            Yam.count mn |> should equal (Map.count m)
        )

        testProperty "tryMin/tryMax" (fun (m : Map<int, int>) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let mMin = m |> Seq.tryHead |> Option.map(function KeyValue(k,v) -> k,v)
            let nMin = Yam.tryMin mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast |> Option.map(function KeyValue(k,v) -> k,v)
            let nMax = Yam.tryMax mn
            nMax |> should equal mMax

            let inline toVOption o =
                match o with
                | Some(KeyValue(k,v)) -> ValueSome(struct(k,v))
                | None -> ValueNone

            let mMin = m |> Seq.tryHead |> toVOption
            let nMin = Yam.tryMinV mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast |> toVOption
            let nMax = Yam.tryMaxV mn
            nMax |> should equal mMax
        )
        
        testProperty "change" (fun (m : Map<int, int>) (k : int) ->
            let mn = Yam.ofSeq (Map.toSeq m)
            identical mn m

            let m1 = m |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> Yam.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1

            
            let m1 = m |> Map.add k k |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> Yam.add k k |> Yam.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1

            
            let m1 = m |> Map.remove k |> Map.change k  (function None -> Some 123 | Some o -> None)
            let n1 = mn |> Yam.remove k |> Yam.change k  (function None -> Some 123 | Some o -> None)
            identical n1 m1
        )

        testProperty "withRange" (fun (m : Map<int, int>) ->
            let m = m |> Map.add 1 2 |> Map.add 3 4 |> Map.add 5 6
            let arr = Map.toArray m
            let mn = Yam.ofArray arr
            identical mn m

            let l,_ = arr.[arr.Length / 3]
            let h,_ = arr.[(2 * arr.Length) / 3]

            let a = Yam.slice l h mn
            let b = m |> Map.filter (fun k _ -> k >= l && k <= h)
            identical a b
        )

        testProperty "union" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = Yam.ofArray (Map.toArray a)
            let nb = Yam.ofArray (Map.toArray b)
            
            identical (Yam.union na nb) (Map.union a b)
        )
        
        testProperty "unionWith" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = Yam.ofArray (Map.toArray a)
            let nb = Yam.ofArray (Map.toArray b)
            
            let resolve k l r =
                27*k+13*l+r

            identical (Yam.unionWith resolve na nb) (Map.unionWith resolve a b)
        )

        testProperty "computeDelta" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = Yam.ofArray (Map.toArray a)
            let nb = Yam.ofArray (Map.toArray b)
            
            let add k v = (k, v, true)
            let rem k v = (k, v, false)
            let update k v1 v2 =
                if v1 = v2 then ValueNone
                else ValueSome (k, v2, true)

            let r = Map.computeDelta add update rem a b
            let t = Yam.computeDelta add update rem na nb

            identical t r
        )

        
        testProperty "applyDelta" (fun (a : Map<int, int>) (b : Map<int, int>) ->
            let na = Yam.ofArray (Map.toArray a)
            let nb = Yam.ofArray (Map.toArray b)
            
            let apply k v op =
                let v = match v with | ValueNone -> 0 | ValueSome v -> v
                let r = v + op
                if r % 2 = 0 then ValueSome r
                else ValueNone

            let r = Map.applyDelta apply a b
            let t = Yam.applyDelta apply na nb

            identical t r
        )

        //testProperty "tryAt" (fun (m : Map<int, int>) ->
        //    let mn = Yam.ofSeq (Map.toSeq m)
        //    identical mn m

        //    let arr = m |> Map.toArray

        //    let inline tup v =
        //        match v with
        //        | ValueSome struct(a,b) -> Some(a,b)
        //        | ValueNone -> None

        //    for i in -1 .. arr.Length do
        //        mn 
        //        |> Yam.tryAt i 
        //        |> should equal (Array.tryItem i arr)

        //        mn 
        //        |> Yam.tryAtV i 
        //        |> tup
        //        |> should equal (Array.tryItem i arr)
        //)

        
        //testProperty "compare" (fun (la : list<int * int>) (lb : list<int * int>) ->
        //    let na = Yam.ofList la
        //    let nb = Yam.ofList lb
        //    let a = Map.ofList la
        //    let b = Map.ofList lb

        //    compare na nb |> should equal (compare a b)

        //    let somea = List.init 8 (fun _ -> Yam.ofListRandomOrder rand la)
        //    let someb = List.init 8 (fun _ -> Yam.ofListRandomOrder rand lb)

        //    for (va, vb) in List.allPairs somea someb do
        //        compare va na |> should equal 0
        //        compare vb nb |> should equal 0
        //        compare va vb |> should equal (compare a b)
        //)

        
        //testProperty "equals" (fun (la : list<int * int>) (lb : list<int * int>) ->
        //    let na = Yam.ofList la
        //    let nb = Yam.ofList lb
        //    let a = Map.ofList la
        //    let b = Map.ofList lb

        //    let e = Unchecked.equals a b
        //    let ne = Unchecked.equals na nb

        //    ne |> should equal e
        //    if ne then
        //        let ha = Unchecked.hash na
        //        let hb = Unchecked.hash nb
        //        ha |> should equal hb

            
        //    let somea = List.init 8 (fun _ -> Yam.ofListRandomOrder rand la)
        //    let someb = List.init 8 (fun _ -> Yam.ofListRandomOrder rand lb)

        //    for (a, b) in List.allPairs somea somea do
        //        a |> should equal b

        //    for (a, b) in List.allPairs someb someb do
        //        a |> should equal b


        //)


    ]

