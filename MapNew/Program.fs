open System.Collections.Generic


let heapSortInPlace (cmp : IComparer<'Key>) (arr : struct('Key * 'Value)[]) =
    let heap : struct('Key * 'Value * int)[] = Array.zeroCreate arr.Length
    let mutable cnt = 0

    let inline compare (l : 'Key) (li : int) (r : 'Key) (ri : int) =
        let c = cmp.Compare(l, r)
        if c = 0 then compare li ri
        else c
    let cmp = ()

    let rec bubbleUp (i : int) (index : int) (key : 'Key) (value : 'Value) =
        if i > 0 then
            let pi = (i - 1) / 2
            let struct(pk, pv, pid) = heap.[pi] 
            let c = compare pk pid key index
            if c > 0 then
                heap.[i] <- struct(pk, pv, pid)
                bubbleUp pi index key value
            else
                heap.[i] <- struct(key, value, index)
        else
            heap.[i] <- struct(key, value, index)

    let rec pushDown (i : int) (index : int) (key : 'Key) (value : 'Value) =
        let ci0 = 2 * i + 1
        let ci1 = ci0 + 1
        if ci1 < cnt then
            let struct(k0, v0, id0) = heap.[ci0]
            let struct(k1, v1, id1) = heap.[ci1]

            let cmp0 = compare key index k0 id0
            let cmp1 = compare key index k1 id1

            if cmp0 > 0 && cmp1 > 0 then
                let cmp01 = compare k0 id0 k1 id1
                if cmp01 < 0 then
                    heap.[i] <- struct(k0, v0, id0)
                    pushDown ci0 index key value
                else
                    heap.[i] <- struct(k1, v1, id1)
                    pushDown ci1 index key value

            elif cmp0 > 0 then
                heap.[i] <- struct(k0, v0, id0)
                pushDown ci0 index key value
            elif cmp1 > 0 then
                heap.[i] <- struct(k1, v1, id1)
                pushDown ci1 index key value
            else
                heap.[i] <- struct(key, value, index)

        elif ci0 < cnt then
            let struct(k0, v0, id0) = heap.[ci0]
            let cmp0 = compare key index k0 id0
            if cmp0 > 0 then
                heap.[i] <- struct(k0, v0, id0)
                pushDown ci0 index key value
            else
                heap.[i] <- struct(key, value, index)
        else
            heap.[i] <- struct(key, value, index)
            
    let enqueue (index : int) (key : 'Key) (value : 'Value) =
        if cnt = 0 then
            heap.[0] <- struct(key, value, index)
            cnt <- 1
        else
            let id = cnt
            cnt <- cnt + 1
            bubbleUp id index key value

    let dequeue () =
        let struct(k,v,_) = heap.[0]
        let last = cnt - 1
        let struct(lk, lv, lid) = heap.[last]
        cnt <- last
        pushDown 0 lid lk lv
        struct(k,v)
        
    for i in 0 .. arr.Length - 1 do
        let struct(k, v) = arr.[i]
        enqueue i k v

    for i in 0 .. cnt - 1 do
        arr.[i] <- dequeue()

open MapNew

[<EntryPoint>]
let main _argv =
    printfn "FSharp.Core: %A" typeof<list<int>>.Assembly.FullName

    
    //let rand = System.Random()
    //let arr = Array.init 65 (fun i -> (rand.Next(8), i))

    //let cmp = LanguagePrimitives.FastGenericComparer<int>
    //let merge = MapNewImplementation.Array.mergeSort cmp (Array.copy arr)

    //let ref = Map.ofArray arr
    //printfn "%0A" (merge)
    //printfn "%0A" (Map.toArray ref)

    //printfn "%0A" tim
    //printfn "%A" (merge = tim)

    BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>()
    |> ignore
    0
