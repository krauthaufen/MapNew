module SetTests

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


let checkConsistency (mn : SetNew<'T>) =
    
    let rec checkHeight (n : SetNewImplementation.SetNode<'T>) =
        match n with
        | :? SetNewImplementation.SetEmpty<'T> -> 0
        | :? SetNewImplementation.SetLeaf<'T> -> 1
        | :? SetNewImplementation.SetInner<'T> as n ->
            let lh = checkHeight n.Left
            let rh = checkHeight n.Right
            let b = abs (rh - lh)
            if b > 2 then failwithf "node has bad balance: %d" (rh - lh)
            let h = 1 + max lh rh
            if n._Height <> h then failwithf "node has bad height: %d (should be %d)" n._Height h
            h
        | _ ->
            failwith "unexpected node"
            
    let rec checkCount (n : SetNewImplementation.SetNode<'T>) =
        match n with
        | :? SetNewImplementation.SetEmpty<'T> -> 0
        | :? SetNewImplementation.SetLeaf<'T> -> 1
        | :? SetNewImplementation.SetInner<'T> as n ->
            let lc = checkCount n.Left
            let rc = checkCount n.Right
            let c = 1 + lc + rc
            if n._Count <> c then failwithf "node has bad count: %d (should be %d)" n._Count c
            c
        | _ ->
            failwith "unexpected node"

    checkHeight mn.Root |> ignore
    checkCount mn.Root |> ignore

let identical (mn : SetNew<'T>) (m : Set<'T>) =
    checkConsistency mn

    let rec getAll (n : SetNewImplementation.SetNode<'T>) =
        match n with
        | :? SetNewImplementation.SetEmpty<'T> -> []
        | :? SetNewImplementation.SetLeaf<'T> as l -> [l.Value]
        | :? SetNewImplementation.SetInner<'T> as n ->
            let l = getAll n.Left
            let r = getAll n.Right
            l @ [n.Value] @ r
        | _ ->
            failwith "unexpected node"


    let a = getAll mn.Root
    let b = Set.toList m
    a |> should equal b

    SetNew.count mn |> should equal (Set.count m)

let rand = System.Random()

module Set =
    let ofListRandomOrder (rand : System.Random) (l : list<'T>) =
        let l = Set.ofList l |> Set.toList
        let rec run (rand : System.Random) (acc : Set<'T>) (l : list<'T>) =
            match l with
            | [] ->
                acc
            | v :: t ->
                if rand.NextDouble() >= 0.5 then
                    run rand (Set.add v acc) t
                else
                    run rand acc t |> Set.add v
        run rand Set.empty l

module SetNew =
    let ofListRandomOrder (rand : System.Random) (l : list<'T>) =
        let l = Set.ofList l |> Set.toList
        let rec run (rand : System.Random) (acc : SetNew<'T>) (l : list<'T>) =
            match l with
            | [] ->
                acc
            | v :: t ->
                if rand.NextDouble() >= 0.5 then
                    run rand (SetNew.add v acc) t
                else
                    run rand acc t |> SetNew.add v
        run rand SetNew.empty l

[<Tests>]
let tests =
    testList "SetNew" [

        testProperty "ofSeq" (fun (l : list<int>) ->
            let m = Set.ofList l
            let mn = SetNew.ofSeq l
            identical mn m
        )
        
        testProperty "ofList" (fun (l : list<int>) ->
            let m = Set.ofList l
            let mn = SetNew.ofList l
            identical mn m
        )
        
        testProperty "ofArray" (fun (l : list<int>) ->
            let m = Set.ofList l
            let mn = SetNew.ofArray (List.toArray l)
            identical mn m
        )

        
        testProperty "enumerate" (fun (l : list<int>) ->
            let m = Set.ofList l
            let mn = SetNew.ofArray (Set.toArray m)
            identical mn m

            for lv, rv in Seq.zip m mn do
                rv |> should equal lv
        )

        testProperty "toSeq" (fun (m : Set<int>) ->
            let mn = SetNew.ofArray (Set.toArray m)
            identical mn m
            for lv, rv in Seq.zip (Set.toSeq m) (SetNew.toSeq mn) do
                rv |> should equal lv
        )

        testProperty "toList" (fun (m : Set<int>) ->
            let mn = SetNew.ofArray (Set.toArray m)
            identical mn m
            (SetNew.toList mn) |> should equal (Set.toList m)
        )

        testProperty "toArray" (fun (m : Set<int>) ->
            let mn = SetNew.ofArray (Set.toArray m)
            identical mn m
            (SetNew.toArray mn) |> should equal (Set.toArray m)
        )

        testProperty "contains" (fun (m : Set<int>) (k : int) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let a = mn |> SetNew.contains k
            let b = m |> Set.contains k
            a |> should equal b
        )
        
        testProperty "add" (fun (m : Set<int>) (k : int) ->
            let mutable mn = SetNew.empty
            for v in m do mn <- SetNew.add v mn
            identical mn m

            let a = mn |> SetNew.add k
            let b = m |> Set.add k
            identical a b
        )
    
        testProperty "remove" (fun (m : Set<int>) (k : int) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m
            
            let a = mn |> SetNew.remove k
            let b = m |> Set.remove k
            identical a b
        
            let mutable a = mn
            let mutable b = m
            for v in m do
                a <- SetNew.remove v a
                b <- Set.remove v b
                identical a b

        )
        
        testProperty "partition" (fun (m : Set<int>) ->
            let mn = SetNew.ofArray (Set.toArray m)
            identical mn m

            let ma, mb = m |> Set.partition (fun v -> v % 2 = 0)
            let na, nb = mn |> SetNew.partition (fun v -> v % 2 = 0)

            identical na ma
            identical nb mb


        )
        
        
        testProperty "fold" (fun (m : Set<int>)  ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let a = (0, mn) ||> SetNew.fold (fun s k -> s * 27 + k)
            let b = (0, m) ||> Set.fold (fun s k -> s * 27 + k)
            a |> should equal b
        )
        
        testProperty "foldBack" (fun (m : Set<int>)  ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let a = (mn, 0) ||> SetNew.foldBack (fun k s -> s * 27 + k)
            let b = (m, 0) ||> Set.foldBack (fun k s -> s * 27 + k)
            a |> should equal b
        )

        
        testProperty "exists" (fun (m : Set<int>)  ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let a = mn |> SetNew.exists (fun k -> k % 3 = 2)
            let b = m |> Set.exists (fun k -> k % 3 = 2)
            a |> should equal b
        )
        
        
        testProperty "forall" (fun (m : Set<int>)  ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let a = mn |> SetNew.forall (fun k -> k % 3 < 2)
            let b = m |> Set.forall (fun k -> k % 3 < 2)
            a |> should equal b
        )
        
        testProperty "count" (fun (m : Set<int>) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m
            SetNew.count mn |> should equal (Set.count m)
        )

        testProperty "tryMin/tryMax" (fun (m : Set<int>) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let mMin = m |> Seq.tryHead
            let nMin = SetNew.tryMin mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast
            let nMax = SetNew.tryMax mn
            nMax |> should equal mMax

            let inline toVOption o =
                match o with
                | Some v -> ValueSome v
                | None -> ValueNone

            let mMin = m |> Seq.tryHead |> toVOption
            let nMin = SetNew.tryMinV mn
            nMin |> should equal mMin

            let mMax = m |> Seq.tryLast |> toVOption
            let nMax = SetNew.tryMaxV mn
            nMax |> should equal mMax
        )
        
        testProperty "change" (fun (m : Set<int>) (k : int) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let inline change v update (s : Set<_>) =
                let o = Set.contains v s
                let n = update o
                if not o && n then Set.add v s
                elif o && not n then Set.remove v s
                else s
                    

            let m1 = m |> change k id
            let n1 = mn |> SetNew.change k id
            identical n1 m1

            
            let m1 = m |> Set.add k |> change k not
            let n1 = mn |> SetNew.add k |> SetNew.change k not
            identical n1 m1

            
            let m1 = m |> Set.remove k |> change k not
            let n1 = mn |> SetNew.remove k |> SetNew.change k not
            identical n1 m1
        )

        testProperty "withRange" (fun (m : Set<int>) ->
            let m = m |> Set.add 1 |> Set.add 3 |> Set.add 5
            let arr = Set.toArray m
            let mn = SetNew.ofArray arr
            identical mn m

            let l = arr.[arr.Length / 3]
            let h = arr.[(2 * arr.Length) / 3]

            let a = SetNew.withRange l h mn
            let b = m |> Set.filter (fun k -> k >= l && k <= h)
            identical a b
        )

        testProperty "union" (fun (a : Set<int>) (b : Set<int>) ->
            let na = SetNew.ofArray (Set.toArray a)
            let nb = SetNew.ofArray (Set.toArray b)
            
            identical (SetNew.union na nb) (Set.union a b)
        )

        testProperty "intersect" (fun (a : Set<int>) (b : Set<int>) ->
            let na = SetNew.ofArray (Set.toArray a)
            let nb = SetNew.ofArray (Set.toArray b)
            
            identical (SetNew.intersect na nb) (Set.intersect a b)
        )

        testProperty "difference" (fun (a : Set<int>) (b : Set<int>) ->
            let na = SetNew.ofArray (Set.toArray a)
            let nb = SetNew.ofArray (Set.toArray b)
            
            identical (SetNew.difference na nb) (Set.difference a b)
        )
        

        testProperty "tryAt" (fun (m : Set<int>) ->
            let mn = SetNew.ofSeq (Set.toSeq m)
            identical mn m

            let arr = m |> Set.toArray

            let inline tup v =
                match v with
                | ValueSome a -> Some a
                | ValueNone -> None

            for i in -1 .. arr.Length do
                mn 
                |> SetNew.tryAt i 
                |> should equal (Array.tryItem i arr)

                mn 
                |> SetNew.tryAtV i 
                |> tup
                |> should equal (Array.tryItem i arr)
        )

        
        testProperty "compare" (fun (la : list<int>) (lb : list<int>) ->
            let na = SetNew.ofList la
            let nb = SetNew.ofList lb
            let a = Set.ofList la
            let b = Set.ofList lb

            compare na nb |> should equal (compare a b)

            let somea = List.init 8 (fun _ -> SetNew.ofListRandomOrder rand la)
            let someb = List.init 8 (fun _ -> SetNew.ofListRandomOrder rand lb)

            for (va, vb) in List.allPairs somea someb do
                compare va na |> should equal 0
                compare vb nb |> should equal 0
                compare va vb |> should equal (compare a b)
        )

        
        testProperty "equals" (fun (la : list<int>) (lb : list<int>) ->
            let na = SetNew.ofList la
            let nb = SetNew.ofList lb
            let a = Set.ofList la
            let b = Set.ofList lb

            let e = Unchecked.equals a b
            let ne = Unchecked.equals na nb

            ne |> should equal e
            if ne then
                let ha = Unchecked.hash na
                let hb = Unchecked.hash nb
                ha |> should equal hb

            
            let somea = List.init 8 (fun _ -> SetNew.ofListRandomOrder rand la)
            let someb = List.init 8 (fun _ -> SetNew.ofListRandomOrder rand lb)

            for (a, b) in List.allPairs somea somea do
                a |> should equal b

            for (a, b) in List.allPairs someb someb do
                a |> should equal b


        )


    ]
