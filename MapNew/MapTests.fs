﻿module MapTests

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
        | :? MapNewImplementation.MapNode<'K, 'V> as n ->
            let lh = checkHeight n.Left
            let rh = checkHeight n.Right
            let b = abs (rh - lh)
            if b > 2 then failwithf "node has bad balance: %d" (rh - lh)
            let h = 1 + max lh rh
            if n._Height <> h then failwithf "node has bad height: %d (should be %d)" n._Height h
            h
        | _ ->
            failwith "unexpected node"
            
    let rec checkCount (n : MapNewImplementation.Node<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> 0
        | :? MapNewImplementation.MapLeaf<'K, 'V> -> 1
        | :? MapNewImplementation.MapNode<'K, 'V> as n ->
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

    let rec getAll (n : MapNewImplementation.Node<'K, 'V>) =
        match n with
        | :? MapNewImplementation.MapEmpty<'K, 'V> -> []
        | :? MapNewImplementation.MapLeaf<'K, 'V> as l -> [l.Key, l.Value]
        | :? MapNewImplementation.MapNode<'K, 'V> as n ->
            let l = getAll n.Left
            let r = getAll n.Right
            l @ [n.Key, n.Value] @ r
        | _ ->
            failwith "unexpected node"

    let a = getAll mn.Root
    let b = Map.toList m
    a |> should equal b

[<Tests>]
let tests =
    testList "MapNew" [
        testProperty "ofSeq" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofSeq (Map.toSeq m)
            identical mn m
        )
        
        testProperty "ofList" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofList (Map.toList m)
            identical mn m
        )
        
        testProperty "ofArray" (fun (m : Map<int, int>) ->
            let mn = MapNew.ofArray (Map.toArray m)
            identical mn m
        )

        testProperty "enumerate" (fun (m : Map<int, int>) ->
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

    ]