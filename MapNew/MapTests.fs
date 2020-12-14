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
    
    let rec checkHeight (n : MapNewImplementation.Node<'K, 'V>) =
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
        #if TWO
        | :? MapNewImplementation.MapTwo<'K, 'V> as n ->
            1
        #endif
        | _ ->
            failwith "unexpected node"
            
    let rec checkCount (n : MapNewImplementation.Node<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> 0
        | :? MapNewImplementation.MapLeaf<'K, 'V> -> 1
        | :? MapNewImplementation.MapInner<'K, 'V> as n ->
            let lh = checkCount n.Left
            let rh = checkCount n.Right
            let h = 1 + lh + rh
            if n._Count <> h then failwithf "node has bad count: %d (should be %d)" n._Count h
            h
        #if TWO
        | :? MapNewImplementation.MapTwo<'K, 'V> as n ->
            2
        #endif
        | _ ->
            failwith "unexpected node"

    checkHeight mn.Root |> ignore
    checkCount mn.Root |> ignore

let identical (mn : MapNew<'K, 'V>) (m : Map<'K, 'V>) =
    checkConsistency mn

    let rec getAll (n : MapNewImplementation.Node<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> []
        | :? MapNewImplementation.MapLeaf<'K, 'V> as l -> [l.Key, l.Value]
        | :? MapNewImplementation.MapInner<'K, 'V> as n ->
            let l = getAll n.Left
            let r = getAll n.Right
            l @ [n.Key, n.Value] @ r
        #if TWO
        | :? MapNewImplementation.MapTwo<'K, 'V> as n ->
            [n.K0, n.V0; n.K1, n.V1]
        #endif
        | _ ->
            failwith "unexpected node"

    let a = getAll mn.Root
    let b = Map.toList m
    a |> should equal b

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
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = mn |> MapNew.add k k
            let b = m |> Map.add k k
            identical a b
        )
    
        testProperty "remove" (fun (m : Map<int, int>) (k : int) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m
            
            let a = mn |> MapNew.remove k
            let b = m |> Map.remove k
            identical a b
        )
        
        testProperty "fold" (fun (m : Map<int, int>)  ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m

            let a = (0, mn) ||> MapNew.fold (fun s k _ -> s <<< 1 + k)
            let b = (0, m) ||> Map.fold (fun s k _ -> s <<< 1 + k)
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

    ]