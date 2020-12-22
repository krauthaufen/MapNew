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
            let mutable mn = MapNew.empty
            for KeyValue(k,v) in m do mn <- MapNew.add k v mn
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
