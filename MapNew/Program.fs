open System.Collections.Generic
open MapNew
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open System.Runtime.CompilerServices

module Mappy =
    

    module YamImplementation =
        [<AbstractClass; AllowNullLiteral>]
        type Node<'Key, 'Value> =
            val mutable public Height : int
            new(h) = { Height = h }

        type Inner<'Key, 'Value> =
            inherit Node<'Key, 'Value>
            val mutable public Left : Node<'Key, 'Value>
            val mutable public Right : Node<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value
            val mutable public Count : int
        
            static member GetHeight(node : Node<'Key, 'Value>) =
                if isNull node then
                    0
                else
                    node.Height

            static member GetCount(node : Node<'Key, 'Value>) =
                if isNull node then
                    0
                elif node.Height = 1 then 
                    1
                else
                    (node :?> Inner<'Key, 'Value>).Count


            new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                let h = 1 + max (Inner.GetHeight l) (Inner.GetHeight r)
                let c = 1 + (Inner.GetCount l) + (Inner.GetCount r)
                { inherit  Node<'Key, 'Value>(h); Left = l; Right = r; Key = k; Value = v; Count = c }

        type Leaf<'Key, 'Value> =
            inherit Node<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value
        
            new( k : 'Key, v : 'Value) =
                { inherit  Node<'Key, 'Value>(1); Key = k; Value = v }


        
        let inline height (n : Node<'Key, 'Value>) =
            if isNull n then 0
            else n.Height
            
        
        let inline count (n : Node<'Key, 'Value>) =
            if isNull n then 0
            elif n.Height = 1 then 1
            else (n :?> Inner<'Key, 'Value>).Count
            
        let inline balance (n : Inner<'Key, 'Value>) =
            height n.Right - height n.Left

        let rec unsafeRemoveMin (n : Node<'Key, 'Value>) =
            if n.Height = 1 then
                let n = n :?> Leaf<'Key, 'Value>
                struct(n.Key, n.Value, null)
            else
                let n = n :?> Inner<'Key, 'Value>
                if isNull n.Left then
                    struct(n.Key, n.Value, n.Right)
                else
                    let struct(k, v, newLeft) = unsafeRemoveMin n.Left
                    struct(k, v, binary newLeft n.Key n.Value n.Right)
                    
        and unsafeRemoveMax (n : Node<'Key, 'Value>) =
            if n.Height = 1 then
                let n = n :?> Leaf<'Key, 'Value>
                struct(n.Key, n.Value, null)
            else
                let n = n :?> Inner<'Key, 'Value>
                if isNull n.Left then
                    struct(n.Key, n.Value, n.Right)
                else
                    let struct(k, v, newRight) = unsafeRemoveMax n.Right
                    struct(k, v, binary n.Left n.Key n.Value newRight)

        and join (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) : Node<'Key, 'Value> =
            if isNull l then r
            elif isNull r then l
            else
                let lc = count l
                let rc = count r
                if lc > rc then
                    let struct(k, v, l) = unsafeRemoveMax l
                    binary l k v r
                else
                    let struct(k, v, r) = unsafeRemoveMin r
                    binary l k v r

        and binary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =
            let lh = height l
            let rh = height r
            let b = rh - lh
            if b > 2 then
                // rh > lh + 2
                let r = r :?> Inner<'Key, 'Value>
                let rb = balance r
                if rb > 0 then
                    // right right
                    let t0 = l
                    let t1 = r.Left
                    binary 
                        (binary t0 k v t1)
                        r.Key r.Value
                        r.Right
                else
                    // right left
                    let rl = r.Left :?> Inner<'Key, 'Value>
                    let t0 = l
                    let t1 = rl.Left
                    let t2 = rl.Right
                    let t3 = r.Right

                    binary
                        (binary t0 k v t1)
                        rl.Key rl.Value
                        (binary t2 r.Key r.Value t3)

            elif b < -2 then
                // lh > rh + 2
                let l = l :?> Inner<'Key, 'Value>
                let lb = balance l
                if lb < 0 then
                    // left left
                    let t2 = l.Right
                    let t3 = r
                    binary 
                        l.Left
                        l.Key l.Value
                        (binary t2 k v t3)
                else
                    // left right
                    let lr = l.Right :?> Inner<'Key, 'Value>
                    let t0 = l.Left
                    let t1 = lr.Left
                    let t2 = lr.Right
                    let t3 = r
                    binary 
                        (binary t0 l.Key l.Value t1)
                        lr.Key lr.Value
                        (binary t2 k v t3)

            else

                Inner(l, k, v, r) :> Node<_,_>

        let rec add (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
            if isNull node then
                // empty
                Leaf(key, value) :> Node<_,_>
            elif node.Height = 1 then
                // leaf
                let l = node :?> Leaf<'Key, 'Value>
                let c = cmp.Compare(key, l.Key)
                if c > 0 then
                    binary l key value null
                elif c < 0 then
                    binary null key value l
                else
                    Leaf(key, value) :> Node<_,_>
            else
                // node
                let n = node :?> Inner<'Key, 'Value>
                let c = cmp.Compare(key, n.Key)
                if c > 0 then
                    binary n.Left n.Key n.Value (add cmp key value n.Right)
                elif c < 0 then
                    binary (add cmp key value n.Left) n.Key n.Value n.Right
                else
                    Inner(n.Left, key, value, n.Right) :> Node<_,_>
             
        let rec remove (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
            if isNull node then 
                node
            elif node.Height = 1 then
                let leaf = node :?> Leaf<'Key, 'Value>
                let c = cmp.Compare(key, leaf.Key)
                if c = 0 then null
                else node
            else
                let node = node :?> Inner<'Key, 'Value>
                let c = cmp.Compare(key, node.Key)
                if c > 0 then binary node.Left node.Key node.Value (remove cmp key node.Right)
                elif c < 0 then binary (remove cmp key node.Left) node.Key node.Value node.Right
                else join node.Left node.Right

        let rec toListV (acc : list<struct('Key * 'Value)>) (node : Node<'Key, 'Value>) =
            if isNull node then acc
            elif node.Height = 1 then
                let node = node :?> Leaf<'Key, 'Value>
                struct(node.Key, node.Value) :: acc
            else
                let node = node :?> Inner<'Key, 'Value>
                toListV (struct(node.Key, node.Value) :: toListV acc node.Right) node.Left
                
        let rec toList (acc : list<'Key * 'Value>) (node : Node<'Key, 'Value>) =
            if isNull node then acc
            elif node.Height = 1 then
                let node = node :?> Leaf<'Key, 'Value>
                (node.Key, node.Value) :: acc
            else
                let node = node :?> Inner<'Key, 'Value>
                toList ((node.Key, node.Value) :: toList acc node.Right) node.Left

    type Yam<'Key, 'Value when 'Key : comparison>(comparer : IComparer<'Key>, root : YamImplementation.Node<'Key, 'Value>) =

        static let defaultComparer = LanguagePrimitives.FastGenericComparer<'Key>

        static member Empty = Yam(defaultComparer, null)
        
        member x.Count = 
            YamImplementation.count root

        member x.Add(key : 'Key, value : 'Value) =
            Yam(comparer, YamImplementation.add comparer key value root)
            
        member x.Remove(key : 'Key) =
            Yam(comparer, YamImplementation.remove comparer key root)

        member x.ToList() =
            YamImplementation.toList [] root
            
        member x.ToListV() =
            YamImplementation.toListV [] root






[<MethodImpl(MethodImplOptions.NoInlining)>]
let keep (v : 'a) =
    ()

let profiling() =
    let rand = System.Random()
    let arr = Array.init 1000 (fun i -> (rand.Next 1000, i))

    let randomKeys = Array.init 1000000 (fun i -> rand.Next 1000)

    let mutable i = 0
    let map = Yam.ofArray arr
    while true do
        Yam.add randomKeys.[i] 123 map |> keep
        i <- i + 1
        if i >= randomKeys.Length then i <- 0

open MapTests

[<MethodImpl(MethodImplOptions.NoInlining ||| MethodImplOptions.NoOptimization)>]
let memory (creator : unit -> 'a) =
    creator() |> ignore
    let mutable a = Unchecked.defaultof<_>
    System.GC.Collect(3, System.GCCollectionMode.Forced, true, true)
    System.GC.WaitForFullGCComplete() |> ignore
    let before = System.GC.GetTotalMemory(true)
    a <- creator()
    System.GC.Collect(3, System.GCCollectionMode.Forced, true, true)
    System.GC.WaitForFullGCComplete() |> ignore
    let size = System.GC.GetTotalMemory(true) - before
    size + int64 (float (Unchecked.hash a % 2) - 0.5)

open Temp.FSharp.Collections

[<EntryPoint>]
let main _argv =
    printfn "FSharp.Core: %A" typeof<list<int>>.Assembly.FullName
    
    //for e in 1 .. 20 do
    //    let size = 1 <<< e
    //    let l = List.init size (fun i -> i, i)
    //    let mm = memory (fun () -> Map.ofList l)
    //    let my = memory (fun () -> Yam.ofList l)

    //    printfn "%.2f%% %d %d" (100.0 * float my / float mm) mm my

    //exit 0


    //profiling()
    //let m = Map.empty
    //let mn = MapNew.empty

    //let a =  m |> Map.replaceRange 2 2 (fun l r -> struct(ValueSome(Unchecked.hash l), ValueSome(Unchecked.hash r)))
    //let b = mn |> MapNew.replaceRange 2 3 (fun l r -> struct(ValueSome(Unchecked.hash l), ValueSome(Unchecked.hash r)))

    //printfn "%A" (Map.toList a)
    //printfn "%A" (MapNew.toList b)
    //exit 0
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
