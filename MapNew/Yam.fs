namespace MapNew

open System.Collections.Generic
open System.Runtime.InteropServices


module YamImplementation =
    module private Memory = 
        let mem() =
            let garbage() = 
                Array.init 10000 (fun i -> System.Guid.NewGuid()) |> ignore

            for i in 1 .. 10 do garbage()
            System.GC.Collect(3, System.GCCollectionMode.Forced, true, true)
            System.GC.WaitForFullGCComplete() |> ignore
            System.GC.GetTotalMemory(true)

        let bla(create : int -> 'A) =
            let cnt = 1000
            let arrayOverhead = 
                if typeof<'A>.IsValueType then 2L * int64 sizeof<nativeint>
                else (int64 cnt + 2L) * int64 sizeof<nativeint>

            let warmup() = Array.init cnt create |> ignore
            warmup()

            let before = mem()
            let arr = Array.init cnt create
            let diff = mem() - before - arrayOverhead
            let pseudo = float (Unchecked.hash arr % 2) - 0.5 |> int64
            float (diff + pseudo) / float cnt 

    [<AllowNullLiteral; NoEquality; NoComparison>]
    type Node<'Key, 'Value> =
        val mutable public Height : byte
        val mutable public Key : 'Key
        val mutable public Value : 'Value
        new(k, v, h) = { Key = k; Value = v; Height = h }
        new(k, v) = { Key = k; Value = v; Height = 1uy }
        
    [<Sealed; AllowNullLiteral; NoEquality; NoComparison>]
    type Inner<'Key, 'Value> =
        inherit Node<'Key, 'Value>
        #if COUNT
        val mutable public Count : int
        #endif
        val mutable public Left : Node<'Key, 'Value>
        val mutable public Right : Node<'Key, 'Value>
       
        #if COUNT
        static member inline GetCount(node : Node<'Key, 'Value>) =
            if isNull node then 0
            elif node.Height = 1uy then 1
            else (node :?> Inner<'Key, 'Value>).Count
        #endif

        static member inline GetHeight(node : Node<'Key, 'Value>) =
            if isNull node then 0uy
            else node.Height

        static member inline FixHeightAndCount(inner : Inner<'Key, 'Value>) =
            #if COUNT
            let lc = Inner.GetCount inner.Left
            let rc = Inner.GetCount inner.Right
            let lh = if lc > 0 then inner.Left.Height else 0uy
            let rh = if rc > 0 then inner.Right.Height else 0uy
            inner.Count <- 1 + lc + rc
            inner.Height <- 1uy + max lh rh
            #else
            inner.Height <- 1uy + max (Inner.GetHeight inner.Left) (Inner.GetHeight inner.Right)
            #endif

        #if COUNT
        new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>, h : byte, cnt : int) =
            { inherit  Node<'Key, 'Value>(k, v, h); Left = l; Right = r; Count = cnt }
        #else
        new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>, h : byte) =
            { inherit  Node<'Key, 'Value>(k, v, h); Left = l; Right = r}
        #endif

        static member Create(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
            if isNull l && isNull r then Node(k, v)
            else 
                #if COUNT
                let lc = Inner.GetCount l
                let rc = Inner.GetCount r
                let lh = if lc > 0 then l.Height else 0uy
                let rh = if rc > 0 then r.Height else 0uy
                Inner(l, k, v, r, 1uy + max lh rh, 1 + lc + rc) :> Node<_,_>
                #else
                let hl = Inner.GetHeight l
                let hr = Inner.GetHeight r
                Inner(l, k, v, r, 1uy + max hl hr) :> Node<_,_>
                #endif

        
    let inline height (n : Node<'Key, 'Value>) =
        if isNull n then 0uy
        else n.Height
            
    let rec count (n : Node<'Key, 'Value>) =
        if isNull n then 0
        elif n.Height = 1uy then 1
        #if COUNT
        else (n :?> Inner<'Key, 'Value>).Count
        #else
        else 
            let n = n :?> Inner<'Key, 'Value>
            1 + count n.Left + count n.Right
        #endif
            
    let inline balance (n : Inner<'Key, 'Value>) =
        int (height n.Right) - int (height n.Left)

    let unsafeBinary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =

        #if COUNT
        let lc = Inner.GetCount l
        let rc = Inner.GetCount r
        let lh = if lc > 0 then l.Height else 0uy
        let rh = if rc > 0 then r.Height else 0uy
        #else
        let lh = height l
        let rh = height r
        #endif

        let b = int rh - int lh
        if b > 2 then
            // rh > lh + 2
            let r = r :?> Inner<'Key, 'Value>
            let rb = balance r
            if rb > 0 then
                // right right 
                Inner.Create( 
                    Inner.Create(l, k, v, r.Left),
                    r.Key, r.Value,
                    r.Right
                )
            else
                // right left
                let rl = r.Left :?> Inner<'Key, 'Value>
                Inner.Create( 
                    Inner.Create(l, k, v, rl.Left),
                    rl.Key, rl.Value,
                    Inner.Create(rl.Right, r.Key, r.Value, r.Right)
                )

        elif b < -2 then
            // lh > rh + 2
            let l = l :?> Inner<'Key, 'Value>
            let lb = balance l
            if lb < 0 then
                // left left
                Inner.Create(
                    l.Left,
                    l.Key, l.Value,
                    Inner.Create(l.Right, k, v, r)
                )
            else
                // left right
                let lr = l.Right :?> Inner<'Key, 'Value>
                Inner.Create(
                    Inner.Create(l.Left, l.Key, l.Value, lr.Left),
                    lr.Key, lr.Value,
                    Inner.Create(lr.Right, k, v, r)
                )

        elif lh = 0uy && rh = 0uy then Node(k, v)
        #if COUNT
        else Inner(l, k, v, r, 1uy + max lh rh, 1 + lc + rc) :> Node<_,_>
        #else
        else Inner(l, k, v, r, 1uy + max lh rh) :> Node<_,_>
        #endif

    let rec unsafeRemoveMin (key : byref<'Key>) (value : byref<'Value>) (n : Node<'Key, 'Value>) =
        if n.Height = 1uy then
            key <- n.Key
            value <- n.Value
            null
        else
            let n = n :?> Inner<'Key, 'Value>
            if isNull n.Left then
                key <- n.Key
                value <- n.Value
                n.Right
            else
                let newLeft = unsafeRemoveMin &key &value n.Left
                unsafeBinary newLeft n.Key n.Value n.Right
                    
    let rec unsafeRemoveMax (key : byref<'Key>) (value : byref<'Value>) (n : Node<'Key, 'Value>) =
        if n.Height = 1uy then
            key <- n.Key
            value <- n.Value
            null
        else
            let n = n :?> Inner<'Key, 'Value>
            if isNull n.Right then
                key <- n.Key
                value <- n.Value
                n.Left
            else
                let newRight = unsafeRemoveMax &key &value n.Right
                unsafeBinary n.Left n.Key n.Value newRight


    let rebalanceUnsafe (node : Inner<'Key, 'Value>) =
        let lh = height node.Left
        let rh = height node.Right
        let b = int rh - int lh
        if b > 2 then
            let r = node.Right :?> Inner<'Key, 'Value>
            let br = balance r
            if br >= 0 then
                // right right
                //     (k01, v01)                           (k12,v12)
                //    t0        (k12,v12)      =>      (k01, v01)    t2
                //             t1       t2            t0        t1

                let t0 = node.Left
                let k01 = node.Key
                let v01 = node.Value
                let t1 = r.Left
                let k12 = r.Key
                let v12 = r.Value
                let t2 = r.Right

                r.Key <- k01
                r.Value <- v01
                r.Left <- t0
                r.Right <- t1
                Inner.FixHeightAndCount r

                node.Key <- k12
                node.Value <- v12
                node.Left <- r
                node.Right <- t2
                #if COUNT
                node.Count <- 1 + r.Count + (count t2)
                #endif
                node.Height <- 1uy + max r.Height (height t2)

            else
                let rl = r.Left :?> Inner<'Key, 'Value>
                // right left
                //     (k01, v01)                             (k12,v12)
                //    t0        (k23,v23)      =>      (k01,v01)     (k23,v23)
                //        (k12,v12)       t3         t0        t1   t2       t3
                //       t1       t2

                let t0 = node.Left
                let k01 = node.Key
                let v01 = node.Value
                let t1 = rl.Left
                let k12 = rl.Key
                let v12 = rl.Value
                let t2 = rl.Right
                let k23 = r.Key
                let v23 = r.Value
                let t3 = r.Right

                let a = rl
                let b = r

                a.Key <- k01
                a.Value <- v01
                a.Left <- t0
                a.Right <- t1
                Inner.FixHeightAndCount a

                b.Key <- k23
                b.Value <- v23
                b.Left <- t2
                b.Right <- t3
                Inner.FixHeightAndCount b
                
                node.Key <- k12
                node.Value <- v12
                node.Left <- a
                node.Right <- b
                #if COUNT
                node.Count <- 1 + a.Count + b.Count
                #endif
                node.Height <- 1uy + max a.Height b.Height

        elif b < -2 then
            let l = node.Left :?> Inner<'Key, 'Value>
            let bl = balance l
            if bl <= 0 then
                // left left
                //         (k12, v12)                   (k01,v01)
                //    (k01,v01)       t2    =>         t0       (k12,v12)
                //  t0        t1                               t1       t2

                let t0 = l.Left
                let k01 = l.Key
                let v01 = l.Value
                let t1 = l.Right
                let k12 = node.Key
                let v12 = node.Value
                let t2 = node.Right

                let a = l

                a.Key <- k12
                a.Value <- v12
                a.Left <- t1
                a.Right <- t2
                Inner.FixHeightAndCount a

                node.Key <- k01
                node.Value <- v01
                node.Left <- t0
                node.Right <- a
                #if COUNT
                node.Count <- 1 + (count t0) + a.Count
                #endif
                node.Height <- 1uy + max (height t0) a.Height

            else
                let lr = l.Right :?> Inner<'Key, 'Value>
                // left right
                //            (k23, v23)                         (k12,v12)
                //    (k01,v01)         t3    =>         (k01,v01)       (k23,v23)
                //  t0      (k12,v12)                   t0       t1     t2      t3
                //         t1       t2

                let t0 = l.Left
                let k01 = l.Key
                let v01 = l.Value
                let t1 = lr.Left
                let k12 = lr.Key
                let v12 = lr.Value
                let t2 = lr.Right
                let k23 = node.Key
                let v23 = node.Value
                let t3 = node.Right

                let a = l
                let b = lr

                a.Key <- k01
                a.Value <- v01
                a.Left <- t0
                a.Right <- t1
                Inner.FixHeightAndCount a

                b.Key <- k23
                b.Value <- v23
                b.Left <- t2
                b.Right <- t3
                Inner.FixHeightAndCount b


                node.Key <- k12
                node.Value <- v12
                node.Left <- a
                node.Right <- b
                #if COUNT
                node.Count <- 1 + a.Count + b.Count
                #endif
                node.Height <- 1uy + max a.Height b.Height
        else
            Inner.FixHeightAndCount node
            


    // abs (balance l r) <= 3 (as caused by add/remove)
    let unsafeJoin (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) : Node<'Key, 'Value> =
        if isNull l then r
        elif isNull r then l
        else
            let lc = l.Height
            let rc = r.Height
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            if lc > rc then
                let ln = unsafeRemoveMax &k &v l
                unsafeBinary ln k v r
            else
                let rn = unsafeRemoveMin &k &v r
                unsafeBinary l k v rn
                
    let rec binary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =
        #if COUNT
        let lc = Inner.GetCount l
        let rc = Inner.GetCount r
        let lh = if lc > 0 then l.Height else 0uy
        let rh = if rc > 0 then r.Height else 0uy
        #else
        let lh = height l
        let rh = height r
        #endif

        let b = int rh - int lh
        if b > 2 then
            // rh > lh + 2
            let r = r :?> Inner<'Key, 'Value>
            let rb = balance r
            if rb > 0 then
                // right right 
                binary 
                    (binary l k v r.Left)
                    r.Key r.Value
                    r.Right
            else
                // right left
                let rl = r.Left :?> Inner<'Key, 'Value>
                binary
                    (binary l k v rl.Left)
                    rl.Key rl.Value
                    (binary rl.Right r.Key r.Value r.Right)

        elif b < -2 then
            // lh > rh + 2
            let l = l :?> Inner<'Key, 'Value>
            let lb = balance l
            if lb < 0 then
                // left left
                binary 
                    l.Left
                    l.Key l.Value
                    (binary l.Right k v r)
            else
                // left right
                let lr = l.Right :?> Inner<'Key, 'Value>
                binary 
                    (binary l.Left l.Key l.Value lr.Left)
                    lr.Key lr.Value
                    (binary lr.Right k v r)

        elif lh = 0uy && rh = 0uy then Node(k, v)
        #if COUNT
        else Inner(l, k, v, r, 1uy + max lh rh, 1 + lc + rc) :> Node<_,_>
        #else
        else Inner(l, k, v, r, 1uy + max lh rh) :> Node<_,_>
        #endif
        
    let rec join (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) : Node<'Key, 'Value> =
        if isNull l then r
        elif isNull r then l
        else
            let lh = l.Height
            let rh = r.Height
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            if lh > rh then
                let ln = unsafeRemoveMax &k &v l
                binary ln k v r
            else
                let rn = unsafeRemoveMin &k &v r
                binary l k v rn

    let rec find (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            raise <| KeyNotFoundException()
        elif node.Height = 1uy then
            if cmp.Compare(key, node.Key) = 0 then node.Value
            else raise <| KeyNotFoundException()
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then find cmp key node.Right
            elif c < 0 then find cmp key node.Left
            else node.Value
            
    let rec tryGetValue (cmp : IComparer<'Key>) (key : 'Key) (result : outref<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            if cmp.Compare(key, node.Key) = 0 then 
                result <- node.Value
                true
            else 
                false
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then tryGetValue cmp key &result node.Right
            elif c < 0 then tryGetValue cmp key &result node.Left
            else 
                result <- node.Value
                true

    let rec tryFind (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            None
        elif node.Height = 1uy then
            if cmp.Compare(key, node.Key) = 0 then Some node.Value
            else None
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then tryFind cmp key node.Right
            elif c < 0 then tryFind cmp key node.Left
            else Some node.Value
            
    let rec tryGetItem (index : int) (key : byref<'Key>) (value : byref<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            if index = 0 then 
                key <- node.Key
                value <- node.Value
                true
            else
                false
        else
            let node = node :?> Inner<'Key, 'Value>
            let id = index - count node.Left
            if id > 0 then
                tryGetItem (id - 1) &key &value node.Right
            elif id < 0 then
                tryGetItem index &key &value node.Left
            else
                key <- node.Key
                value <- node.Value
                true

    let rec tryGetMin (minKey : byref<'Key>) (minValue : byref<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            minKey <- node.Key
            minValue <- node.Value
            true
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Left then 
                minKey <- node.Key
                minValue <- node.Value
                true
            else
                tryGetMin &minKey &minValue node.Left

    let rec tryGetMax (maxKey : byref<'Key>) (maxValue : byref<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            maxKey <- node.Key
            maxValue <- node.Value
            true
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Right then
                maxKey <- node.Key
                maxValue <- node.Value
                true
            else
                tryGetMax &maxKey &maxValue node.Right


    let rec containsKey (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            cmp.Compare(key, node.Key) = 0
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then containsKey cmp key node.Right
            elif c < 0 then containsKey cmp key node.Left
            else true

    let rec addIfNotPresent (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
        if isNull node then
            // empty
            Node(key, value)

        elif node.Height = 1uy then
            // leaf
            let c = cmp.Compare(key, node.Key)
            #if COUNT
            if c > 0 then Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else node
            #else
            if c > 0 then Inner(node, key, value, null, 2uy) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy) :> Node<_,_>
            else node
            #endif

        else
            // inner
            let n = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, n.Key)
            if c > 0 then
                unsafeBinary n.Left n.Key n.Value (addIfNotPresent cmp key value n.Right)
            elif c < 0 then
                unsafeBinary (addIfNotPresent cmp key value n.Left) n.Key n.Value n.Right
            else    
                node

    let rec add (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
        if isNull node then
            // empty
            Node(key, value)

        elif node.Height = 1uy then
            // leaf
            let c = cmp.Compare(key, node.Key)
            #if COUNT
            if c > 0 then Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else Node(key, value)
            #else
            if c > 0 then Inner(node, key, value, null, 2uy) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy) :> Node<_,_>
            else Node(key, value)
            #endif

        else
            // inner
            let n = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, n.Key)
            if c > 0 then
                unsafeBinary n.Left n.Key n.Value (add cmp key value n.Right)
            elif c < 0 then
                unsafeBinary (add cmp key value n.Left) n.Key n.Value n.Right
            else    
                #if COUNT
                Inner(n.Left, key, value, n.Right, n.Height, n.Count) :> Node<_,_>
                #else
                Inner(n.Left, key, value, n.Right, n.Height) :> Node<_,_>
                #endif
          
          
    let rec addInPlace (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
        if isNull node then
            // empty
            Node(key, value)

        elif node.Height = 1uy then
            // leaf
            let c = cmp.Compare(key, node.Key)
            #if COUNT 
            if c > 0 then Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else 
                node.Key <- key
                node.Value <- value
                node
            #else
            if c > 0 then Inner(node, key, value, null, 2uy) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy) :> Node<_,_>
            else 
                node.Key <- key
                node.Value <- value
                node
            #endif

        else
            // inner
            let n = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, n.Key)
            if c > 0 then
                n.Right <- addInPlace cmp key value n.Right
                rebalanceUnsafe n
                node

            elif c < 0 then
                n.Left <- addInPlace cmp key value n.Left
                rebalanceUnsafe n
                node

            else
                n.Key <- key
                n.Value <- value
                node
     
             
    let rec tryRemove' (cmp : IComparer<'Key>) (key : 'Key)  (result : byref<Node<'Key, 'Value>>) (node : Node<'Key, 'Value>) =
        if isNull node then 
            false
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c = 0 then 
                result <- null
                true
            else
                false
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then 
                if tryRemove' cmp key &result node.Right then
                    result <- unsafeBinary node.Left node.Key node.Value result
                    true
                else
                    false
            elif c < 0 then 
                if tryRemove' cmp key &result node.Left then
                    result <- unsafeBinary result node.Key node.Value node.Right
                    true
                else
                    false
            else 
                result <- unsafeJoin node.Left node.Right
                true
       
    let rec tryRemove (cmp : IComparer<'Key>) (key : 'Key) (removedValue : byref<'Value>) (result : byref<Node<'Key, 'Value>>) (node : Node<'Key, 'Value>) =
        if isNull node then 
            false
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c = 0 then 
                removedValue <- node.Value
                result <- null
                true
            else
                false
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then 
                if tryRemove cmp key &removedValue &result node.Right then
                    result <- unsafeBinary node.Left node.Key node.Value result
                    true
                else
                    false
            elif c < 0 then 
                if tryRemove cmp key &removedValue &result node.Left then
                    result <- unsafeBinary result node.Key node.Value node.Right
                    true
                else
                    false
            else 
                result <- unsafeJoin node.Left node.Right
                removedValue <- node.Value
                true
     
     
    let rec removeAt (index : int) (removedKey : byref<'Key>) (removedValue : byref<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then 
            null
        elif node.Height = 1uy then
            if index = 0 then
                removedKey <- node.Key
                removedValue <- node.Value
                null
            else
                node
        else
            let node = node :?> Inner<'Key, 'Value>
            let id = index - count node.Left
            if id > 0 then 
                let result = removeAt (id - 1) &removedKey &removedValue node.Right
                unsafeBinary node.Left node.Key node.Value result
            elif id < 0 then 
                let result = removeAt index &removedKey &removedValue node.Left
                unsafeBinary result node.Key node.Value node.Right
            else 
                removedKey <- node.Key
                removedValue <- node.Value
                unsafeJoin node.Left node.Right
     

    let rec changeV (cmp : IComparer<'Key>) (key : 'Key) (update : voption<'Value> -> voption<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            match update ValueNone with
            | ValueNone -> null
            | ValueSome v -> Node(key, v)

        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                match update ValueNone with
                | ValueNone -> node
                | ValueSome n ->
                    #if COUNT
                    Inner(node, key, n, null, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(node, key, n, null, 2uy) :> Node<_,_>
                    #endif
            elif c < 0 then
                match update ValueNone with
                | ValueNone -> node
                | ValueSome n ->
                    #if COUNT
                    Inner(null, key, n, node, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(null, key, n, node, 2uy) :> Node<_,_>
                    #endif
            else
                match update (ValueSome node.Value) with
                | ValueNone -> null
                | ValueSome v -> Node(key, v)
        else    
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                unsafeBinary node.Left node.Key node.Value (changeV cmp key update node.Right)
            elif c < 0 then
                unsafeBinary (changeV cmp key update node.Left) node.Key node.Value node.Right
            else
                match update (ValueSome node.Value) with
                | ValueSome n ->
                    #if COUNT
                    Inner(node.Left, key, n, node.Right, node.Height, node.Count) :> Node<_,_>
                    #else
                    Inner(node.Left, key, n, node.Right, node.Height) :> Node<_,_>
                    #endif
                | ValueNone ->
                    unsafeJoin node.Left node.Right

    let rec change (cmp : IComparer<'Key>) (key : 'Key) (update : option<'Value> -> option<'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then
            match update None with
            | None -> null
            | Some v -> Node(key, v)

        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                match update None with
                | None -> node
                | Some n ->
                    #if COUNT
                    Inner(node, key, n, null, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(node, key, n, null, 2uy) :> Node<_,_>
                    #endif
            elif c < 0 then
                match update None with
                | None -> node
                | Some n ->
                    #if COUNT
                    Inner(null, key, n, node, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(null, key, n, node, 2uy) :> Node<_,_>
                    #endif
            else
                match update (Some node.Value) with
                | None -> null
                | Some v -> Node(key, v)
        else    
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                unsafeBinary node.Left node.Key node.Value (change cmp key update node.Right)
            elif c < 0 then
                unsafeBinary (change cmp key update node.Left) node.Key node.Value node.Right
            else
                match update (Some node.Value) with
                | Some n ->
                    #if COUNT
                    Inner(node.Left, key, n, node.Right, node.Height, node.Count) :> Node<_,_>
                    #else
                    Inner(node.Left, key, n, node.Right, node.Height) :> Node<_,_>
                    #endif
                | None ->
                    unsafeJoin node.Left node.Right



    let rec copyToV (array : struct('Key * 'Value)[]) (index : int) (node : Node<'Key, 'Value>) =
        if isNull node then
            index
        elif node.Height = 1uy then
            array.[index] <- struct(node.Key, node.Value)
            index + 1
        else
            let node = node :?> Inner<'Key, 'Value>
            let i1 = copyToV array index node.Left
            array.[i1] <- struct(node.Key, node.Value)
            copyToV array (i1 + 1) node.Right
            
    let rec copyTo (array : ('Key * 'Value)[]) (index : int) (node : Node<'Key, 'Value>) =
        if isNull node then
            index
        elif node.Height = 1uy then
            array.[index] <- (node.Key, node.Value)
            index + 1
        else
            let node = node :?> Inner<'Key, 'Value>
            let i1 = copyTo array index node.Left
            array.[i1] <- (node.Key, node.Value)
            copyTo array (i1 + 1) node.Right

    let rec toListV (acc : list<struct('Key * 'Value)>) (node : Node<'Key, 'Value>) =
        if isNull node then acc
        elif node.Height = 1uy then
            struct(node.Key, node.Value) :: acc
        else
            let node = node :?> Inner<'Key, 'Value>
            toListV (struct(node.Key, node.Value) :: toListV acc node.Right) node.Left
                
    let rec toList (acc : list<'Key * 'Value>) (node : Node<'Key, 'Value>) =
        if isNull node then acc
        elif node.Height = 1uy then
            (node.Key, node.Value) :: acc
        else
            let node = node :?> Inner<'Key, 'Value>
            toList ((node.Key, node.Value) :: toList acc node.Right) node.Left
            
    let rec iter (action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit>) (node : Node<'Key, 'Value>) =
        if isNull node then
            ()
        elif node.Height = 1uy then
            action.Invoke(node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            iter action node.Left
            action.Invoke(node.Key, node.Value)
            iter action node.Right

    let rec map (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) (node : Node<'Key, 'Value>) =
        if isNull node then
            null
        elif node.Height = 1uy then
            Node(node.Key, mapping.Invoke(node.Key, node.Value))
        else
            let node = node :?> Inner<'Key, 'Value>
            let l = map mapping node.Left
            let s = mapping.Invoke(node.Key, node.Value)
            let r = map mapping node.Right
            #if COUNT
            Inner(l, node.Key, s, r, node.Height, node.Count) :> Node<_,_>
            #else
            Inner(l, node.Key, s, r, node.Height) :> Node<_,_>
            #endif
            
    let rec chooseV (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) (node : Node<'Key, 'Value>) =
        if isNull node then
            null
        elif node.Height = 1uy then
            match mapping.Invoke(node.Key, node.Value) with
            | ValueSome v -> Node(node.Key, v)
            | ValueNone -> null
        else
            let node = node :?> Inner<'Key, 'Value>
            let l = chooseV mapping node.Left
            let s = mapping.Invoke(node.Key, node.Value)
            let r = chooseV mapping node.Right
            match s with
            | ValueSome s -> binary l node.Key s r
            | ValueNone -> join l r

    let rec choose (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) (node : Node<'Key, 'Value>) =
        if isNull node then
            null
        elif node.Height = 1uy then
            match mapping.Invoke(node.Key, node.Value) with
            | Some v -> Node(node.Key, v)
            | None -> null
        else
            let node = node :?> Inner<'Key, 'Value>
            let l = choose mapping node.Left
            let s = mapping.Invoke(node.Key, node.Value)
            let r = choose mapping node.Right
            match s with
            | Some s -> binary l node.Key s r
            | None -> join l r

    let rec filter (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : Node<'Key, 'Value>) =
        if isNull node then
            null
        elif node.Height = 1uy then
            if predicate.Invoke(node.Key, node.Value) then
                node
            else
                null
        else
            let node = node :?> Inner<'Key, 'Value>
            let l = filter predicate node.Left
            let s = predicate.Invoke(node.Key, node.Value)
            let r = filter predicate node.Right
            if s then binary l node.Key node.Value r
            else join l r



    let rec fold (state : 'State) (folder : OptimizedClosures.FSharpFunc<'State, 'Key, 'Value, 'State>) (node : Node<'Key, 'Value>) =
        if isNull node then
            state
        elif node.Height = 1uy then
            folder.Invoke(state, node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            let s1 = fold state folder node.Left
            let s2 = folder.Invoke(s1, node.Key, node.Value)
            fold s2 folder node.Right

    let rec foldBack (state : 'State) (folder : OptimizedClosures.FSharpFunc<'Key, 'Value, 'State, 'State>) (node : Node<'Key, 'Value>) =
        if isNull node then
            state
        elif node.Height = 1uy then
            folder.Invoke(node.Key, node.Value, state)
        else
            let node = node :?> Inner<'Key, 'Value>
            let s1 = foldBack state folder node.Right
            let s2 = folder.Invoke(node.Key, node.Value, s1)
            foldBack s2 folder node.Left
    
    let rec exists (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : Node<'Key, 'Value>) =
        if isNull node then
            false
        elif node.Height = 1uy then
            predicate.Invoke(node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            exists predicate node.Left ||
            predicate.Invoke(node.Key, node.Value) ||
            exists predicate node.Right
            
    let rec forall (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : Node<'Key, 'Value>) =
        if isNull node then
            true
        elif node.Height = 1uy then
            predicate.Invoke(node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            forall predicate node.Left &&
            predicate.Invoke(node.Key, node.Value) &&
            forall predicate node.Right

    let rec partition (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (t : byref<Node<'Key, 'Value>>) (f : byref<Node<'Key, 'Value>>) (node : Node<'Key, 'Value>) =
        if isNull node then
            t <- null
            f <- null
        elif node.Height = 1uy then
            if predicate.Invoke(node.Key, node.Value) then 
                t <- node
                f <- null
            else 
                t <- null
                f <- node
        else
            let node = node :?> Inner<'Key, 'Value>

            let mutable lt = null
            let mutable lf = null

            partition predicate &lt &lf node.Left
            let c = predicate.Invoke(node.Key, node.Value)
            partition predicate &t &f node.Right

            if c then
                t <- binary lt node.Key node.Value t
                f <- join lf f
            else
                t <- join lt t
                f <- binary lf node.Key node.Value f


    let rec split 
        (cmp : IComparer<'Key>) (key : 'Key) 
        (left : byref<Node<'Key, 'Value>>) (self : byref<'Value>) (right : byref<Node<'Key, 'Value>>) 
        (node : Node<'Key, 'Value>) =
        
        if isNull node then
            left <- null
            right <- null
            false
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                left <- node
                right <- null
                false
            elif c < 0 then
                left <- null
                right <- node
                false
            else
                left <- null
                right <- null
                self <- node.Value
                true
        else
            let node = node :?> Inner<'Key,'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                let res = split cmp key &left &self &right node.Right
                left <- binary node.Left node.Key node.Value left
                res
            elif c < 0 then
                let res = split cmp key &left &self &right node.Left
                right <- binary right node.Key node.Value node.Right
                res
            else
                left <- node.Left
                right <- node.Right
                self <- node.Value
                true


    let rec private changeWithLeft
        (cmp : IComparer<'Key>)
        (key : 'Key)
        (value : 'Value)
        (resolve : OptimizedClosures.FSharpFunc<'Key, 'Value, 'Value, 'Value>) 
        (node : Node<'Key, 'Value>) =
        if isNull node then
            Node(key, value)
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then
                Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else
                let value = resolve.Invoke(key, value, node.Value)
                Node(key, value)
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                unsafeBinary node.Left node.Key node.Value (changeWithLeft cmp key value resolve node.Right)
            elif c < 0 then
                unsafeBinary (changeWithLeft cmp key value resolve node.Left) node.Key node.Value node.Right
            else
                let value = resolve.Invoke(key, value, node.Value)
                Inner(node.Left, node.Key, value, node.Right, node.Height, node.Count) :> Node<_,_>
                
    let rec private changeWithRight
        (cmp : IComparer<'Key>)
        (key : 'Key)
        (value : 'Value)
        (resolve : OptimizedClosures.FSharpFunc<'Key, 'Value, 'Value, 'Value>) 
        (node : Node<'Key, 'Value>) =
        if isNull node then
            Node(key, value)
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then
                Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else
                let value = resolve.Invoke(key, node.Value, value)
                Node(key, value)
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                unsafeBinary node.Left node.Key node.Value (changeWithRight cmp key value resolve node.Right)
            elif c < 0 then
                unsafeBinary (changeWithRight cmp key value resolve node.Left) node.Key node.Value node.Right
            else
                let value = resolve.Invoke(key, node.Value, value)
                Inner(node.Left, node.Key, value, node.Right, node.Height, node.Count) :> Node<_,_>
                
            
        

    let rec unionWith (cmp : IComparer<'Key>) (resolve : OptimizedClosures.FSharpFunc<'Key, 'Value, 'Value, 'Value>) (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) =
        if isNull l then r
        elif isNull r then l

        elif l.Height = 1uy then
            // left leaf
            changeWithLeft cmp l.Key l.Value resolve r

        elif r.Height = 1uy then
            // right leaf
            changeWithRight cmp r.Key r.Value resolve l

        else
            // both inner
            let l = l :?> Inner<'Key, 'Value>
            let r = r :?> Inner<'Key, 'Value>

            if l.Height < r.Height then
                let key = r.Key
                let mutable ll = null
                let mutable lv = Unchecked.defaultof<_>
                let mutable lr = null
                let hasValue = split cmp key &ll &lv &lr (l :> Node<_,_>)
                //let struct(ll, lv, lr) = splitV cmp key l

                let newLeft = unionWith cmp resolve ll r.Left

                let value =
                    if hasValue then resolve.Invoke(key, lv, r.Value)
                    else r.Value
                let newRight = unionWith cmp resolve lr r.Right

                binary newLeft key value newRight
            else
                let key = l.Key
                let mutable rl = null
                let mutable rv = Unchecked.defaultof<_>
                let mutable rr = null
                let hasValue = split cmp key &rl &rv &rr (r :> Node<_,_>)

                let newLeft = unionWith cmp resolve l.Left rl

                let value =
                    if hasValue then resolve.Invoke(key, l.Value, rv)
                    else l.Value

                let newRight = unionWith cmp resolve l.Right rr
                
                binary newLeft key value newRight
      
    let rec union (cmp : IComparer<'Key>) (map1 : Node<'Key, 'Value>) (map2 : Node<'Key, 'Value>) =
        if System.Object.ReferenceEquals(map1, map2) then map1

        elif isNull map1 then map2
        elif isNull map2 then map1

        elif map1.Height = 1uy then
            // map1 leaf
            map2 |> addIfNotPresent cmp map1.Key map1.Value
        elif map2.Height = 1uy then
            // map2 leaf
            map1 |> add cmp map2.Key map2.Value

        else
            // both inner
            let map1 = map1 :?> Inner<'Key, 'Value>
            let map2 = map2 :?> Inner<'Key, 'Value>

            if map1.Height < map2.Height then
                let key = map2.Key
                let mutable l1 = null
                let mutable v1 = Unchecked.defaultof<_>
                let mutable r1 = null
                split cmp key &l1 &v1 &r1 (map1 :> Node<_,_>) |> ignore
                let newLeft = union cmp l1 map2.Left
                let value = map2.Value
                let newRight = union cmp r1 map2.Right

                binary newLeft key value newRight
            else
                let key = map1.Key
                let mutable l2 = null
                let mutable v2 = Unchecked.defaultof<_>
                let mutable r2 = null
                let hasValue = split cmp key &l2 &v2 &r2 (map2 :> Node<_,_>)
                let newLeft = union cmp map1.Left l2
                let value = if hasValue then v2 else map1.Value
                let newRight = union cmp map1.Right r2
                binary newLeft key value newRight

    [<System.Flags>]
    type NeighbourFlags =
        | None = 0
        | Left = 1
        | Self = 2
        | Right = 4

    let rec getNeighbours 
        (cmp : IComparer<'Key>) (key : 'Key) 
        (flags : NeighbourFlags)
        (leftKey : byref<'Key>) (leftValue : byref<'Value>)
        (selfValue : byref<'Value>)
        (rightKey : byref<'Key>) (rightValue : byref<'Value>)
        (node : Node<'Key, 'Value>) =

        if isNull node then
            flags

        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                leftKey <- node.Key
                leftValue <- node.Value
                flags ||| NeighbourFlags.Left
            elif c < 0 then
                rightKey <- node.Key
                rightValue <- node.Value
                flags ||| NeighbourFlags.Right
            else
                selfValue <- node.Value
                flags ||| NeighbourFlags.Self
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                leftKey <- node.Key
                leftValue <- node.Value
                getNeighbours cmp key (flags ||| NeighbourFlags.Left) &leftKey &leftValue &selfValue &rightKey &rightValue node.Right
            elif c < 0 then
                rightKey <- node.Key
                rightValue <- node.Value
                getNeighbours cmp key (flags ||| NeighbourFlags.Right) &leftKey &leftValue &selfValue &rightKey &rightValue node.Left
            else    
                selfValue <- node.Value
                let mutable flags = flags 
                if tryGetMin &rightKey &rightValue node.Right then flags <- flags ||| NeighbourFlags.Right
                if tryGetMax &leftKey &leftValue node.Left then flags <- flags ||| NeighbourFlags.Left
                flags ||| NeighbourFlags.Self

    let rec getNeighboursAt
        (index : int) 
        (flags : NeighbourFlags)
        (leftKey : byref<'Key>) (leftValue : byref<'Value>)
        (selfKey : byref<'Key>) (selfValue : byref<'Value>)
        (rightKey : byref<'Key>) (rightValue : byref<'Value>)
        (node : Node<'Key, 'Value>) =

        if isNull node then
            flags

        elif node.Height = 1uy then
            if index > 0 then
                leftKey <- node.Key
                leftValue <- node.Value
                flags ||| NeighbourFlags.Left
            elif index < 0 then
                rightKey <- node.Key
                rightValue <- node.Value
                flags ||| NeighbourFlags.Right
            else
                selfKey <- node.Key
                selfValue <- node.Value
                flags ||| NeighbourFlags.Self
        else
            let node = node :?> Inner<'Key, 'Value>
            let id = index - count node.Left
            if id > 0 then
                leftKey <- node.Key
                leftValue <- node.Value
                getNeighboursAt (id - 1) (flags ||| NeighbourFlags.Left) &leftKey &leftValue &selfKey &selfValue &rightKey &rightValue node.Right
            elif id < 0 then
                rightKey <- node.Key
                rightValue <- node.Value
                getNeighboursAt index (flags ||| NeighbourFlags.Right) &leftKey &leftValue &selfKey &selfValue &rightKey &rightValue node.Left
            else    
                selfKey <- node.Key
                selfValue <- node.Value
                let mutable flags = flags 
                if tryGetMin &rightKey &rightValue node.Right then flags <- flags ||| NeighbourFlags.Right
                if tryGetMax &leftKey &leftValue node.Left then flags <- flags ||| NeighbourFlags.Left
                flags ||| NeighbourFlags.Self



    let rec withMin (cmp : IComparer<'Key>) (minKey : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, minKey)
            if c >= 0 then node
            else null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, minKey)
            if c > 0 then
                binary (withMin cmp minKey node.Left) node.Key node.Value node.Right
            elif c < 0 then
                withMin cmp minKey node.Right
            else
                withMin cmp minKey node.Right |> add cmp node.Key node.Value
                
    let rec withMax (cmp : IComparer<'Key>) (maxKey : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, maxKey)
            if c <= 0 then node
            else null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, maxKey)

            if c > 0 then
                withMax cmp maxKey node.Left
            elif c < 0 then
                binary node.Left node.Key node.Value (withMax cmp maxKey node.Right)
            else
                withMax cmp maxKey node.Left |> add cmp node.Key node.Value
  
    let rec withMinExclusiveN 
        (cmp : IComparer<'Key>) (minKey : 'Key) 
        (firstKey : byref<'Key>) (firstValue : byref<'Value>)
        (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, minKey)
            if c > 0 then 
                firstKey <- node.Key
                firstValue <- node.Value
                node
            else 
                null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, minKey)
            if c > 0 then
                let newLeft = withMinExclusiveN cmp minKey &firstKey &firstValue node.Left
                if isNull newLeft then
                    firstKey <- node.Key
                    firstValue <- node.Value
                binary newLeft node.Key node.Value node.Right
            else
                withMinExclusiveN cmp minKey &firstKey &firstValue node.Right
 
    let rec withMaxExclusiveN 
        (cmp : IComparer<'Key>) (maxKey : 'Key) 
        (lastKey : byref<'Key>) (lastValue : byref<'Value>)
        (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, maxKey)
            if c < 0 then 
                lastKey <- node.Key
                lastValue <- node.Value
                node
            else 
                null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, maxKey)
            if c < 0 then
                let newRight = withMaxExclusiveN cmp maxKey &lastKey &lastValue node.Right
                if isNull newRight then
                    lastKey <- node.Key
                    lastValue <- node.Value
                binary node.Left node.Key node.Value newRight
            else
                withMaxExclusiveN cmp maxKey &lastKey &lastValue node.Left


    let rec slice (cmp : IComparer<'Key>) (minKey : 'Key) (maxKey : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let cMin = cmp.Compare(minKey, node.Key)
            if cMin <= 0 then
                let cMax = cmp.Compare(maxKey, node.Key)
                if cMax >= 0 then
                    node
                else
                    null
            else
                null
        else
            let node = node :?> Inner<'Key, 'Value>
            let cMin = cmp.Compare(minKey, node.Key)
            let cMax = cmp.Compare(maxKey, node.Key)

            if cMin <= 0 && cMax >= 0 then
                // split-key contained
                binary (withMin cmp minKey node.Left) node.Key node.Value (withMax cmp maxKey node.Right)
            elif cMin > 0 then  
                // min larger than split-key
                slice cmp minKey maxKey node.Right
            else (* cMax < 0 *)
                // max smaller than split-key
                slice cmp minKey maxKey node.Left

    let inline private ofTwoOption (min : 'Key) (max : 'Key) (minValue : voption<'Value>) (maxValue : voption<'Value>) =
        match minValue with
        | ValueSome va ->
            match maxValue with
            | ValueSome vb ->
                #if COUNT
                Inner(null, min, va, Node(max, vb), 2uy, 2) :> Node<_,_>
                #else
                Inner(null, min, va, Node(max, vb), 2uy) :> Node<_,_>
                #endif

            | ValueNone ->
                Node(min, va)
        | ValueNone ->
            match maxValue with
            | ValueSome vb ->
                Node(max, vb)
            | ValueNone ->
                null

    let rec changeWithNeighbours
        (cmp : IComparer<'Key>) 
        (key : 'Key)
        (l : voption<struct('Key * 'Value)>)
        (r : voption<struct('Key * 'Value)>)
        (replacement : voption<struct('Key * 'Value)> -> voption<'Value> -> voption<struct('Key * 'Value)> -> voption<'Value>)
        (node : Node<'Key, 'Value>) =

        if isNull node then
            let newValue = replacement l ValueNone r
            match newValue with
            | ValueSome b -> Node(key, b)
            | ValueNone -> null

        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                let newValue = replacement (ValueSome struct(node.Key, node.Value)) ValueNone r
                match newValue with
                | ValueSome newValue ->
                    #if COUNT 
                    Inner(node, key, newValue, null, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(node, key, newValue, null, 2uy) :> Node<_,_>
                    #endif
                | ValueNone ->
                    node
            elif c < 0 then
                let newValue = replacement l ValueNone (ValueSome struct(node.Key, node.Value))
                match newValue with
                | ValueSome newValue ->
                    #if COUNT 
                    Inner(null, key, newValue, node, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(null, key, newValue, node, 2uy) :> Node<_,_>
                    #endif
                | ValueNone ->
                    node
            else
                let newValue = replacement l (ValueSome node.Value) r
                match newValue with
                | ValueSome newValue ->
                    Node(key, newValue)
                | ValueNone ->
                    null
                
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                binary node.Left node.Key node.Value (changeWithNeighbours cmp key (ValueSome struct(node.Key, node.Value)) r replacement node.Right)
            elif c < 0 then 
                binary (changeWithNeighbours cmp key l (ValueSome struct(node.Key, node.Value)) replacement node.Left) node.Key node.Value node.Right
            else
                let rNeighbour =
                    let mutable k = Unchecked.defaultof<_>
                    let mutable v = Unchecked.defaultof<_>
                    if tryGetMin &k &v node.Right then
                        ValueSome struct(k,v)
                    else
                        r

                let lNeighbour =
                    let mutable k = Unchecked.defaultof<_>
                    let mutable v = Unchecked.defaultof<_>
                    if tryGetMax &k &v node.Right then
                        ValueSome struct(k,v)
                    else
                        l

                let newValue = replacement lNeighbour (ValueSome node.Value) rNeighbour
                match newValue with
                | ValueSome newValue ->
                    #if COUNT 
                    Inner(node.Left, key, newValue, node.Right, node.Height, node.Count) :> Node<_,_>
                    #else
                    Inner(node.Left, key, newValue, node.Right, node.Height) :> Node<_,_>
                    #endif
                | ValueNone ->
                    unsafeJoin node.Left node.Right
           
    let rec replaceRange 
        (cmp : IComparer<'Key>) 
        (min : 'Key) (max : 'Key) 
        (l : voption<struct('Key * 'Value)>)
        (r : voption<struct('Key * 'Value)>)
        (replacement : voption<struct('Key * 'Value)> -> voption<struct('Key * 'Value)> -> struct(voption<'Value> * voption<'Value>))
        (node : Node<'Key, 'Value>) =
        
        if isNull node then
            let struct (a, b) = replacement l r
            ofTwoOption min max a b

        elif node.Height = 1uy then
            let cMin = cmp.Compare(node.Key, min)
            let cMax = cmp.Compare(node.Key, max)
            if cMin >= 0 && cMax <= 0 then
                let struct(a, b) = replacement l r
                ofTwoOption min max a b
            elif cMin < 0 then
                // node is left
                let struct(a, b) = replacement (ValueSome struct(node.Key, node.Value)) r

                match a with
                | ValueSome va ->
                    match b with
                    | ValueSome vb ->
                        #if COUNT
                        Inner(node, min, va, Node(max, vb), 2uy, 3) :> Node<_,_>
                        #else
                        Inner(node, min, va, Node(max, vb), 2uy) :> Node<_,_>
                        #endif
                    | ValueNone ->
                        #if COUNT
                        Inner(node, min, va, null, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(node, min, va, null, 2uy) :> Node<_,_>
                        #endif
                | ValueNone ->
                    match b with
                    | ValueSome vb ->
                        #if COUNT
                        Inner(node, max, vb, null, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(node, max, vb, null, 2uy) :> Node<_,_>
                        #endif
                    | ValueNone ->
                        node
            else
                // node is right
                let struct(a, b) = replacement l (ValueSome struct(node.Key, node.Value))

                match a with
                | ValueSome va ->
                    match b with
                    | ValueSome vb ->
                        #if COUNT
                        Inner(Node(min, va), max, vb, node, 2uy, 3) :> Node<_,_>
                        #else
                        Inner(Node(min, va), max, vb, node, 2uy) :> Node<_,_>
                        #endif
                    | ValueNone ->
                        #if COUNT
                        Inner(null, min, va, node, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(null, min, va, node, 2uy) :> Node<_,_>
                        #endif
                | ValueNone ->
                    match b with
                    | ValueSome vb ->
                        #if COUNT
                        Inner(null, max, vb, node, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(null, max, vb, node, 2uy) :> Node<_,_>
                        #endif
                    | ValueNone ->
                        node
        else
            let node = node :?> Inner<'Key, 'Value>
            let cMin = cmp.Compare(node.Key, min)
            let cMax = cmp.Compare(node.Key, max)
            if cMin >= 0 && cMax <= 0 then

                let mutable minKey = Unchecked.defaultof<_>
                let mutable minValue = Unchecked.defaultof<_>
                let mutable maxKey = Unchecked.defaultof<_>
                let mutable maxValue = Unchecked.defaultof<_>

                let l1 = withMaxExclusiveN cmp min &minKey &minValue node.Left
                let r1 = withMinExclusiveN cmp max &maxKey &maxValue node.Right

                let ln = 
                    if isNull l1 then l
                    else ValueSome struct(minKey, minValue)
                let rn = 
                    if isNull r1 then r
                    else ValueSome struct(maxKey, maxValue)

                let struct(a, b) = replacement ln rn

                match a with
                | ValueSome va ->
                    match b with
                    | ValueSome vb ->
                        if height l1 < height r1 then
                            binary (add cmp min va l1) max vb r1
                        else
                            binary l1 min va (add cmp max vb r1)
                    | ValueNone ->
                        binary l1 min va r1
                | ValueNone ->
                    match b with
                    | ValueSome vb ->
                        binary l1 max vb r1
                    | ValueNone ->
                        join l1 r1

            elif cMin < 0 then
                // only right
                let r1 = replaceRange cmp min max (ValueSome struct(node.Key, node.Value)) r replacement node.Right
                binary node.Left node.Key node.Value r1
            else    
                // only left
                let l1 = replaceRange cmp min max l (ValueSome struct(node.Key, node.Value)) replacement node.Left
                binary l1 node.Key node.Value node.Right

    let rec computeDelta
        (cmp : IComparer<'Key>) 
        (node1 : Node<'Key, 'Value1>)
        (node2 : Node<'Key, 'Value2>)
        (update : OptimizedClosures.FSharpFunc<'Key, 'Value1, 'Value2, voption<'OP>>)
        (invoke : OptimizedClosures.FSharpFunc<'Key, 'Value2, 'OP>)
        (revoke : OptimizedClosures.FSharpFunc<'Key, 'Value1, 'OP>) =
        if isNull node1 then
            map invoke node2
        elif isNull node2 then
            map revoke node1
        elif System.Object.ReferenceEquals(node1, node2) then
            null
        elif node1.Height = 1uy then
            // node1 is leaf
            if node2.Height = 1uy then
                // both are leaves
                let c = cmp.Compare(node2.Key, node1.Key)
                if c > 0 then
                    let a = revoke.Invoke(node1.Key, node1.Value)
                    let b = invoke.Invoke(node2.Key, node2.Value)
                    #if COUNT
                    Inner(Node(node1.Key, a), node2.Key, b, null, 2uy, 2) :> Node<_,_>
                    #else
                    Inner(Node(node1.Key, a), node2.Key, b, null, 2uy) :> Node<_,_>
                    #endif
                elif c < 0 then 
                    let b = invoke.Invoke(node2.Key, node2.Value)
                    let a = revoke.Invoke(node1.Key, node1.Value)
                    #if COUNT
                    Inner(null, node2.Key, b, Node(node1.Key, a), 2uy, 2) :> Node<_,_>
                    #else
                    Inner(null, node2.Key, b, Node(node1.Key, a), 2uy) :> Node<_,_>
                    #endif
                else
                    match update.Invoke(node1.Key, node1.Value, node2.Value) with
                    | ValueSome op -> Node(node1.Key, op)
                    | ValueNone -> null
            else
                // node1 is leaf
                // node2 is inner
                let node2 = node2 :?> Inner<'Key, 'Value2>
                let c = cmp.Compare(node1.Key, node2.Key)

                if c > 0 then
                    let l1 = map invoke node2.Left
                    let s = invoke.Invoke(node2.Key, node2.Value)
                    let r1 = computeDelta cmp node1 node2.Right update invoke revoke
                    binary l1 node2.Key s r1
                elif c < 0 then
                    let l1 = computeDelta cmp node1 node2.Left update invoke revoke
                    let s = invoke.Invoke(node2.Key, node2.Value)
                    let r1 = map invoke node2.Right
                    binary l1 node2.Key s r1
                else
                    let l1 = map invoke node2.Left
                    let s = update.Invoke(node1.Key, node1.Value, node2.Value)
                    let r1 = map invoke node2.Right
                    match s with
                    | ValueSome op ->
                        Inner(l1, node1.Key, op, r1, node2.Height, node2.Count) :> Node<_,_>
                    | ValueNone ->
                        join l1 r1
                        
        elif node2.Height = 1uy then
            // node2 is leaf
            // node1 is inner
            let node1 = node1 :?> Inner<'Key, 'Value1>
            let c = cmp.Compare(node2.Key, node1.Key)
            if c > 0 then
                let l1 = map revoke node1.Left
                let s = revoke.Invoke(node1.Key, node1.Value)
                let r1 = computeDelta cmp node1.Right node2 update invoke revoke
                binary l1 node1.Key s r1
            elif c < 0 then     
                let l1 = computeDelta cmp node1.Left node2 update invoke revoke
                let s = revoke.Invoke(node1.Key, node1.Value)
                let r1 = map revoke node1.Right
                binary l1 node1.Key s r1
            else
                let l1 = map revoke node1.Left
                let s = update.Invoke(node1.Key, node1.Value, node2.Value)
                let r1 = map revoke node1.Right
                match s with
                | ValueSome op ->
                    Inner(l1, node1.Key, op, r1, node1.Height, node1.Count) :> Node<_,_>
                | ValueNone ->
                    join l1 r1

        elif node1.Height > node2.Height then
            // both are inner h1 > h2
            let node1 = node1 :?> Inner<'Key, 'Value1>
            let mutable l2 = null
            let mutable s2 = Unchecked.defaultof<_>
            let mutable r2 = null
            let hasValue = split cmp node1.Key &l2 &s2 &r2 node2
            if hasValue then
                let ld = computeDelta cmp node1.Left l2 update invoke revoke
                let self = update.Invoke(node1.Key, node1.Value, s2)
                let rd = computeDelta cmp node1.Right r2 update invoke revoke
                match self with
                | ValueSome self -> binary ld node1.Key self rd
                | ValueNone -> join ld rd
            else
                let ld = computeDelta cmp node1.Left l2 update invoke revoke
                let op = revoke.Invoke(node1.Key, node1.Value)
                let rd = computeDelta cmp node1.Right r2 update invoke revoke
                binary ld node1.Key op rd
        else
            // both are inner h2 > h1
            let node2 = node2 :?> Inner<'Key, 'Value2>
            let mutable l1 = null
            let mutable s1 = Unchecked.defaultof<_>
            let mutable r1 = null
            let hasValue = split cmp node2.Key &l1 &s1 &r1 node1
            if hasValue then
                let ld = computeDelta cmp l1 node2.Left update invoke revoke
                let self = update.Invoke(node2.Key, s1, node2.Value)
                let rd = computeDelta cmp r1 node2.Right update invoke revoke
                match self with
                | ValueSome self -> binary ld node2.Key self rd
                | ValueNone -> join ld rd
            else
                let ld = computeDelta cmp l1 node2.Left update invoke revoke
                let self = invoke.Invoke(node2.Key, node2.Value)
                let rd = computeDelta cmp r1 node2.Right update invoke revoke
                binary ld node2.Key self rd
           
    module ApplyDelta = 
        let rec private applyDeltaSingletonState 
            (cmp : IComparer<'Key>)
            (specialKey : 'Key)
            (specialValue : 'T)
            (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) 
            (mapping2 : OptimizedClosures.FSharpFunc<'Key, 'T, 'Value, voption<'T>>) 
            (node : Node<'Key, 'Value>) = 
                if isNull node then
                    Node(specialKey, specialValue)
                elif node.Height = 1uy then
                    let c = cmp.Compare(specialKey, node.Key)
                    if c > 0 then
                        match mapping.Invoke(node.Key, node.Value) with
                        | ValueSome v -> 
                            Inner(null, node.Key, v, Node(specialKey, specialValue), 2uy, 2) :> Node<_,_>
                        | ValueNone ->
                            Node(specialKey, specialValue)
                    elif c < 0 then
                        match mapping.Invoke(node.Key, node.Value) with
                        | ValueSome v -> 
                            Inner(Node(specialKey, specialValue), node.Key, v, null, 2uy, 2) :> Node<_,_>
                        | ValueNone ->
                            Node(specialKey, specialValue)
                    else
                        match mapping2.Invoke(node.Key, specialValue, node.Value) with
                        | ValueSome v ->
                            Node(node.Key, v)
                        | ValueNone ->
                            null
                else
                    let node = node :?> Inner<'Key, 'Value>
                    let c = cmp.Compare(specialKey, node.Key)
                    if c > 0 then
                        let l = chooseV mapping node.Left
                        let s = mapping.Invoke(node.Key, node.Value)
                        let r = applyDeltaSingletonState cmp specialKey specialValue mapping mapping2 node.Right
                        match s with
                        | ValueSome value -> 
                            binary l node.Key value r
                        | ValueNone ->
                            join l r
                    elif c < 0 then
                        let l = applyDeltaSingletonState cmp specialKey specialValue mapping mapping2 node.Left
                        let s = mapping.Invoke(node.Key, node.Value)
                        let r = chooseV mapping node.Right
                        match s with
                        | ValueSome value -> 
                            binary l node.Key value r
                        | ValueNone ->
                            join l r
                   
                    else
                        let l = chooseV mapping node.Left
                        let self = mapping2.Invoke(node.Key, specialValue, node.Value)
                        let r = chooseV mapping node.Right
                        match self with
                        | ValueSome res ->
                            binary l node.Key res r
                        | ValueNone ->
                            join l r

                        
        let rec private applyDeltaSingle 
            (cmp : IComparer<'Key>) 
            (specialKey : 'Key) 
            (specialValue : 'T)
            (update : OptimizedClosures.FSharpFunc<'Key, 'T, voption<'Value>>)
            (update2 : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T, voption<'Value>>) 
            (node : Node<'Key, 'Value>) : Node<'Key, 'Value> =
            if isNull node then
                match update.Invoke(specialKey, specialValue) with
                | ValueNone -> null
                | ValueSome v -> Node(specialKey, v)

            elif node.Height = 1uy then
                let c = cmp.Compare(specialKey, node.Key)
                if c > 0 then
                    match update.Invoke(specialKey, specialValue) with
                    | ValueNone -> node
                    | ValueSome n ->
                        #if COUNT
                        Inner(node, specialKey, n, null, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(node, key, n, null, 2uy) :> Node<_,_>
                        #endif
                elif c < 0 then
                    match update.Invoke(specialKey, specialValue) with
                    | ValueNone -> node
                    | ValueSome n ->
                        #if COUNT
                        Inner(null, specialKey, n, node, 2uy, 2) :> Node<_,_>
                        #else
                        Inner(null, key, n, node, 2uy) :> Node<_,_>
                        #endif
                else
                    match update2.Invoke(specialKey, node.Value, specialValue) with
                    | ValueNone -> null
                    | ValueSome v -> Node(specialKey, v)
            else    
                let node = node :?> Inner<'Key, 'Value>
                let c = cmp.Compare(specialKey, node.Key)
                if c > 0 then
                    unsafeBinary node.Left node.Key node.Value (applyDeltaSingle cmp specialKey specialValue update update2 node.Right)
                elif c < 0 then
                    unsafeBinary (applyDeltaSingle cmp specialKey specialValue update update2 node.Left) node.Key node.Value node.Right
                else
                    match update2.Invoke(specialKey, node.Value, specialValue) with
                    | ValueSome n ->
                        #if COUNT
                        Inner(node.Left, specialKey, n, node.Right, node.Height, node.Count) :> Node<_,_>
                        #else
                        Inner(node.Left, key, n, node.Right, node.Height) :> Node<_,_>
                        #endif
                    | ValueNone ->
                        unsafeJoin node.Left node.Right


        let rec applyDelta
            (cmp : IComparer<'Key>)
            (state : Node<'Key, 'Value>)
            (delta : Node<'Key, 'OP>)
            (applyNoState : OptimizedClosures.FSharpFunc<'Key, 'OP, voption<'Value>>) 
            (apply : OptimizedClosures.FSharpFunc<'Key, 'Value, 'OP, voption<'Value>>) =

            if isNull delta then
                // delta empty
                state
            elif isNull state then
                // state empty
                chooseV applyNoState delta

            elif delta.Height = 1uy then
                // delta leaf
                applyDeltaSingle cmp delta.Key delta.Value applyNoState apply state

            elif state.Height = 1uy then
                // state leaf
                applyDeltaSingletonState cmp state.Key state.Value applyNoState apply delta
                
            else
                // both inner
                let state = state :?> Inner<'Key, 'Value>
                let mutable dl = null
                let mutable ds = Unchecked.defaultof<_>
                let mutable dr = null
                let hasValue = split cmp state.Key &dl &ds &dr delta
                let delta = ()

                if hasValue then
                    let l = applyDelta cmp state.Left dl applyNoState apply
                    let self = apply.Invoke(state.Key, state.Value, ds)
                    let r = applyDelta cmp state.Right dr applyNoState apply
                    match self with
                    | ValueSome self -> binary l state.Key self r
                    | ValueNone -> join l r
                else
                    let l = applyDelta cmp state.Left dl applyNoState apply
                    let r = applyDelta cmp state.Right dr applyNoState apply
                    binary l state.Key state.Value r
            

        let rec chooseVAndGetEffective 
            (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) 
            (effective : byref<Node<'Key, 'Value>>) 
            (node : Node<'Key, 'Value>) =
            if isNull node then
                effective <- null
                null
            elif node.Height = 1uy then
                match mapping.Invoke(node.Key, node.Value) with
                | ValueSome v -> 
                    effective <- node
                    Node(node.Key, v)
                | ValueNone -> 
                    effective <- null
                    null
            else
                let node = node :?> Inner<'Key, 'Value>
                let mutable re = null
                let l = chooseVAndGetEffective mapping &effective node.Left
                let s = mapping.Invoke(node.Key, node.Value)
                let r = chooseVAndGetEffective mapping &re node.Right
                
                match s with
                | ValueSome s -> 
                    effective <- binary effective node.Key node.Value re
                    binary l node.Key s r
                | ValueNone -> 
                    effective <- join effective re
                    join l r

        let rec private applyDeltaSingletonStateEff 
            (cmp : IComparer<'Key>)
            (specialKey : 'Key)
            (specialValue : 'Value)
            (mapping : OptimizedClosures.FSharpFunc<'Key, 'OP, voption<'Value>>) 
            (mapping2 : OptimizedClosures.FSharpFunc<'Key, 'Value, 'OP, voption<'Value>>) 
            (equal : OptimizedClosures.FSharpFunc<'Value, 'Value, bool>)
            (effective : byref<Node<'Key, 'OP>>)
            (delta : Node<'Key, 'OP>) = 
                if isNull delta then
                    effective <- null
                    Node(specialKey, specialValue)

                elif delta.Height = 1uy then
                    let c = cmp.Compare(specialKey, delta.Key)
                    if c > 0 then
                        match mapping.Invoke(delta.Key, delta.Value) with
                        | ValueSome v -> 
                            effective <- delta
                            Inner(null, delta.Key, v, Node(specialKey, specialValue), 2uy, 2) :> Node<_,_>
                        | ValueNone ->
                            effective <- null
                            Node(specialKey, specialValue)
                    elif c < 0 then
                        match mapping.Invoke(delta.Key, delta.Value) with
                        | ValueSome v -> 
                            effective <- delta
                            Inner(Node(specialKey, specialValue), delta.Key, v, null, 2uy, 2) :> Node<_,_>
                        | ValueNone ->
                            effective <- null
                            Node(specialKey, specialValue)
                    else
                        match mapping2.Invoke(delta.Key, specialValue, delta.Value) with
                        | ValueSome v ->
                            if equal.Invoke(specialValue, v) then effective <- null
                            else effective <- delta
                            Node(delta.Key, v)
                        | ValueNone ->
                            effective <- delta
                            null
                else
                    let delta = delta :?> Inner<'Key, 'OP>
                    let c = cmp.Compare(specialKey, delta.Key)
                    if c > 0 then
                        let mutable re = null
                        let l = chooseVAndGetEffective mapping &effective delta.Left
                        let s = mapping.Invoke(delta.Key, delta.Value)
                        let r = applyDeltaSingletonStateEff cmp specialKey specialValue mapping mapping2 equal &re delta.Right
                        match s with
                        | ValueSome value -> 
                            effective <- binary effective delta.Key delta.Value re
                            binary l delta.Key value r
                        | ValueNone ->
                            effective <- join effective re
                            join l r
                    elif c < 0 then
                        let mutable re = null
                        let l = applyDeltaSingletonStateEff cmp specialKey specialValue mapping mapping2 equal &effective delta.Left
                        let s = mapping.Invoke(delta.Key, delta.Value)
                        let r = chooseVAndGetEffective mapping &re delta.Right
                        match s with
                        | ValueSome value -> 
                            effective <- binary effective delta.Key delta.Value re
                            binary l delta.Key value r
                        | ValueNone ->
                            effective <- join effective re
                            join l r
                   
                    else
                        let mutable re = null
                        let l = chooseVAndGetEffective mapping &effective delta.Left
                        let self = mapping2.Invoke(delta.Key, specialValue, delta.Value)
                        let r = chooseVAndGetEffective mapping &re delta.Right
                        match self with
                        | ValueSome res ->
                            if equal.Invoke(res, specialValue) then effective <- join effective re
                            else effective <- binary effective delta.Key delta.Value re
                            binary l delta.Key res r
                        | ValueNone ->
                            effective <- binary effective delta.Key delta.Value re
                            join l r


        let rec private applyDeltaSingleEff
            (cmp : IComparer<'Key>) 
            (specialKey : 'Key) 
            (specialValue : 'T)
            (update : OptimizedClosures.FSharpFunc<'Key, 'T, voption<'Value>>)
            (update2 : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T, voption<'Value>>) 
            (equal : OptimizedClosures.FSharpFunc<'Value, 'Value, bool>)
            (original : Node<'Key, _>)
            (effective : byref<Node<'Key, _>>)
            (node : Node<'Key, 'Value>) : Node<'Key, 'Value> =
            if isNull node then
                match update.Invoke(specialKey, specialValue) with
                | ValueNone -> 
                    effective <- null
                    null
                | ValueSome v -> 
                    effective <- original
                    Node(specialKey, v)

            elif node.Height = 1uy then
                let c = cmp.Compare(specialKey, node.Key)
                if c > 0 then
                    match update.Invoke(specialKey, specialValue) with
                    | ValueNone -> 
                        effective <- null
                        node
                    | ValueSome n ->
                        effective <- original
                        Inner(node, specialKey, n, null, 2uy, 2) :> Node<_,_>
                elif c < 0 then
                    match update.Invoke(specialKey, specialValue) with
                    | ValueNone -> 
                        effective <- null
                        node
                    | ValueSome n ->
                        effective <- original
                        Inner(null, specialKey, n, node, 2uy, 2) :> Node<_,_>
                else
                    match update2.Invoke(specialKey, node.Value, specialValue) with
                    | ValueNone -> 
                        effective <- original
                        null
                    | ValueSome v -> 
                        effective <- if equal.Invoke(node.Value, v) then null else original
                        Node(specialKey, v)
            else    
                let node = node :?> Inner<'Key, 'Value>
                let c = cmp.Compare(specialKey, node.Key)
                if c > 0 then
                    unsafeBinary node.Left node.Key node.Value (applyDeltaSingleEff cmp specialKey specialValue update update2 equal original &effective node.Right)
                elif c < 0 then
                    unsafeBinary (applyDeltaSingleEff cmp specialKey specialValue update update2 equal original &effective node.Left) node.Key node.Value node.Right
                else
                    match update2.Invoke(specialKey, node.Value, specialValue) with
                    | ValueSome n ->
                        effective <- if equal.Invoke(node.Value, n) then null else original
                        Inner(node.Left, specialKey, n, node.Right, node.Height, node.Count) :> Node<_,_>
                    | ValueNone ->
                        effective <- original
                        unsafeJoin node.Left node.Right

        let rec applyDeltaAndGetEffective
            (cmp : IComparer<'Key>)
            (state : Node<'Key, 'Value>)
            (delta : Node<'Key, 'OP>)
            (applyNoState : OptimizedClosures.FSharpFunc<'Key, 'OP, voption<'Value>>) 
            (apply : OptimizedClosures.FSharpFunc<'Key, 'Value, 'OP, voption<'Value>>) 
            (equal : OptimizedClosures.FSharpFunc<'Value, 'Value, bool>)
            (effective : byref<Node<'Key, 'OP>>) =

            if isNull delta then
                // delta empty
                effective <- null
                state

            elif isNull state then
                // state empty
                chooseVAndGetEffective applyNoState &effective delta

            elif delta.Height = 1uy then
                // delta leaf
                applyDeltaSingleEff cmp delta.Key delta.Value applyNoState apply equal delta &effective state
      
            elif state.Height = 1uy then
                // state leaf
                applyDeltaSingletonStateEff cmp state.Key state.Value applyNoState apply equal &effective delta
                
            else
                // both inner
                let state = state :?> Inner<'Key, 'Value>
                let mutable dl = null
                let mutable ds = Unchecked.defaultof<_>
                let mutable dr = null
                let hasValue = split cmp state.Key &dl &ds &dr delta
                let delta = ()

                let mutable re = null

                if hasValue then
                    let l = applyDeltaAndGetEffective cmp state.Left dl applyNoState apply equal &effective
                    let self = apply.Invoke(state.Key, state.Value, ds)
                    let r = applyDeltaAndGetEffective cmp state.Right dr applyNoState apply equal &re
                    match self with
                    | ValueSome self -> 
                        if equal.Invoke(state.Value, self) then effective <- join effective re
                        else effective <- binary effective state.Key ds re
                        binary l state.Key self r
                    | ValueNone -> 
                        effective <- binary effective state.Key ds re
                        join l r
                else
                    let l = applyDeltaAndGetEffective cmp state.Left dl applyNoState apply equal &effective
                    let r = applyDeltaAndGetEffective cmp state.Right dr applyNoState apply equal &re
                    effective <- join effective re
                    binary l state.Key state.Value r
            




    type YamMappingEnumerator<'Key, 'Value, 'T> =
        struct
            val mutable internal Mapping : Node<'Key, 'Value> -> 'T
            val mutable internal Root : Node<'Key, 'Value>
            val mutable internal Stack : list<struct(Node<'Key, 'Value> * bool)>
            val mutable internal CurrentNode : Node<'Key, 'Value>
            
            member x.MoveNext() =
                match x.Stack with
                | struct(n, deep) :: t ->
                    x.Stack <- t

                    if n.Height > 1uy then
                        if deep then
                            let inner = n :?> Inner<'Key, 'Value>

                            if not (isNull inner.Right) then 
                                x.Stack <- struct(inner.Right, true) :: x.Stack
                                
                            if isNull inner.Left then 
                                x.CurrentNode <- n
                                true
                            else
                                x.Stack <- struct(inner.Left, true) :: struct(n, false) :: x.Stack
                                x.MoveNext()
                        else
                            x.CurrentNode <- n
                            true
                    else
                        x.CurrentNode <- n
                        true

                | [] ->
                    false


            member x.Reset() =
                if isNull x.Root then
                    x.Stack <- []
                    x.CurrentNode <- null
                else
                    x.Stack <- [struct(x.Root, true)]
                    x.CurrentNode <- null

            member x.Dispose() =
                x.Root <- null
                x.CurrentNode <- null
                x.Stack <- []

            member x.Current =
                x.Mapping x.CurrentNode

            interface System.Collections.IEnumerator with
                member x.MoveNext() = x.MoveNext()
                member x.Reset() = x.Reset()
                member x.Current = x.Current :> obj

            interface System.Collections.Generic.IEnumerator<'T> with
                member x.Current = x.Current
                member x.Dispose() = x.Dispose()

            new(root : Node<'Key, 'Value>, mapping : Node<'Key, 'Value> -> 'T) =
                {
                    Root = root
                    Stack = if isNull root then [] else [struct(root, true)]
                    CurrentNode = null
                    Mapping = mapping
                }

        end
   
    type YamMappingEnumerable<'Key, 'Value, 'T>(root : Node<'Key, 'Value>, mapping : Node<'Key, 'Value> -> 'T) =
        member x.GetEnumerator() = new YamMappingEnumerator<_,_,_>(root, mapping)

        interface System.Collections.IEnumerable with
            member x.GetEnumerator() = x.GetEnumerator() :> _
            
        interface System.Collections.Generic.IEnumerable<'T> with
            member x.GetEnumerator() = x.GetEnumerator() :> _



type Yam<'Key, 'Value when 'Key : comparison>(comparer : IComparer<'Key>, root : YamImplementation.Node<'Key, 'Value>) =


    static let defaultComparer = LanguagePrimitives.FastGenericComparer<'Key>
    static let empty = Yam<'Key, 'Value>(defaultComparer, null)

    member internal x.Root = root

    static member Empty = empty
        
    static member FromSeq(elements : #seq<'Key * 'Value>) =
        let cmp = defaultComparer
        let mutable root = null
        for (k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)

    static member FromList(elements : list<'Key * 'Value>) =
        let cmp = defaultComparer
        let mutable root = null
        for (k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)

    static member FromArray(elements : ('Key * 'Value)[]) =
        let cmp = defaultComparer
        let mutable root = null
        for (k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)
        
    static member FromSeqV(elements : #seq<struct('Key * 'Value)>) =
        let cmp = defaultComparer
        let mutable root = null
        for struct(k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)

    static member FromListV(elements : list<struct('Key * 'Value)>) =
        let cmp = defaultComparer
        let mutable root = null
        for struct(k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)

    static member FromArrayV(elements : struct('Key * 'Value)[]) =
        let cmp = defaultComparer
        let mutable root = null
        for struct(k, v) in elements do
            root <- YamImplementation.addInPlace cmp k v root
        Yam(cmp, root)

    member x.Count = 
        YamImplementation.count root

    member x.Add(key : 'Key, value : 'Value) =
        Yam(comparer, YamImplementation.add comparer key value root)
            
    member x.Remove(key : 'Key) =
        let mutable newRoot = null
        if YamImplementation.tryRemove' comparer key &newRoot root then
            Yam(comparer, newRoot)
        else    
            x

    member x.RemoveAt(index : int) =
        if index < 0 || index >= x.Count then x
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            Yam(comparer, YamImplementation.removeAt index &k &v root)
            
    member x.TryRemoveAt(index : int) =
        if index < 0 || index >= x.Count then 
            None
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let res = Yam(comparer, YamImplementation.removeAt index &k &v root)
            Some ((k, v), res)
            
    member x.TryRemoveAtV(index : int) =
        if index < 0 || index >= x.Count then 
            ValueNone
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let res = Yam(comparer, YamImplementation.removeAt index &k &v root)
            ValueSome struct(k, v, res)
            
    member x.TryRemove(key : 'Key, [<Out>] result : byref<Yam<'Key, 'Value>>, [<Out>] removedValue : byref<'Value>) =
        let mutable newRoot = null
        if YamImplementation.tryRemove comparer key &removedValue &newRoot root then
            result <- Yam(comparer, newRoot)
            true
        else    
            result <- x
            false

    member x.TryRemove(key : 'Key) =
        let mutable newRoot = null
        let mutable removedValue = Unchecked.defaultof<_>
        if YamImplementation.tryRemove comparer key &removedValue &newRoot root then
            Some(removedValue, Yam(comparer, newRoot))
        else    
            None

    member x.TryRemoveV(key : 'Key) =
        let mutable newRoot = null
        let mutable removedValue = Unchecked.defaultof<_>
        if YamImplementation.tryRemove comparer key &removedValue &newRoot root then
            ValueSome struct(removedValue, Yam(comparer, newRoot))
        else    
            ValueNone
        

        
    member x.Change(key : 'Key, update : option<'Value> -> option<'Value>) =
        Yam(comparer, YamImplementation.change comparer key update root)

    member x.Change(key : 'Key, update : voption<'Value> -> voption<'Value>) =
        Yam(comparer, YamImplementation.changeV comparer key update root)
            
    member x.Item
        with get(key : 'Key) = YamImplementation.find comparer key root

    member x.Find(key : 'Key) =
        YamImplementation.find comparer key root


    member x.TryItem(index : int) =
        if index < 0 || index >= x.Count then None
        else
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            YamImplementation.tryGetItem index &k &v root |> ignore
            Some (k, v)
            
    member x.TryItemV(index : int) =
        if index < 0 || index >= x.Count then ValueNone
        else
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            YamImplementation.tryGetItem index &k &v root |> ignore
            ValueSome struct(k, v)

    member x.TryGetItem(index : int, [<Out>] key : byref<'Key>, [<Out>] value : byref<'Value>) =
        YamImplementation.tryGetItem index &key &value root 

    member x.GetItem(index : int) =
        if index < 0 || index >= x.Count then
            raise <| System.IndexOutOfRangeException()
        else
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            YamImplementation.tryGetItem index &k &v root |> ignore
            (k, v)
            
    member x.GetItemV(index : int) =
        if index < 0 || index >= x.Count then
            raise <| System.IndexOutOfRangeException()
        else
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            YamImplementation.tryGetItem index &k &v root |> ignore
            struct(k, v)
        

    member x.TryGetValue(key : 'Key, [<Out>] value : byref<'Value>) =
        YamImplementation.tryGetValue comparer key &value root

    member x.TryFind(key : 'Key) =
        YamImplementation.tryFind comparer key root
        
    member x.TryFindV(key : 'Key) =
        let mutable res = Unchecked.defaultof<_>
        if YamImplementation.tryGetValue comparer key &res root then
            ValueSome res
        else
            ValueNone


    member x.ContainsKey(key : 'Key) =
        YamImplementation.containsKey comparer key root

    member x.TryMin() =
        let mutable minKey = Unchecked.defaultof<_>
        let mutable minValue = Unchecked.defaultof<_>
        if YamImplementation.tryGetMin &minKey &minValue root then
            Some (minKey, minValue)
        else    
            None
        
    member x.TryMax() =
        let mutable maxKey = Unchecked.defaultof<_>
        let mutable maxValue = Unchecked.defaultof<_>
        if YamImplementation.tryGetMax &maxKey &maxValue root then
            Some (maxKey, maxValue)
        else    
            None
        
    member x.TryMinV() =
        let mutable minKey = Unchecked.defaultof<_>
        let mutable minValue = Unchecked.defaultof<_>
        if YamImplementation.tryGetMin &minKey &minValue root then
            ValueSome struct(minKey, minValue)
        else    
            ValueNone
        
    member x.TryMaxV() =
        let mutable maxKey = Unchecked.defaultof<_>
        let mutable maxValue = Unchecked.defaultof<_>
        if YamImplementation.tryGetMax &maxKey &maxValue root then
            ValueSome struct(maxKey, maxValue)
        else    
            ValueNone

    member x.TryRemoveMin() =
        if isNull root then 
            None
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let rest = YamImplementation.unsafeRemoveMin &k &v root
            Some (k,v,Yam(comparer, rest))
            
    member x.TryRemoveMax() =
        if isNull root then 
            None
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let rest = YamImplementation.unsafeRemoveMax &k &v root
            Some (k,v,Yam(comparer, rest))
            
    member x.TryRemoveMinV() =
        if isNull root then 
            ValueNone
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let rest = YamImplementation.unsafeRemoveMin &k &v root
            ValueSome struct(k,v,Yam(comparer, rest))
            
    member x.TryRemoveMaxV() =
        if isNull root then 
            ValueNone
        else 
            let mutable k = Unchecked.defaultof<_>
            let mutable v = Unchecked.defaultof<_>
            let rest = YamImplementation.unsafeRemoveMax &k &v root
            ValueSome struct(k,v,Yam(comparer, rest))

    member x.ToSeq() =
        YamImplementation.YamMappingEnumerable(root, fun n -> (n.Key, n.Value)) :> seq<_>
            
    member x.ToSeqV() =
        YamImplementation.YamMappingEnumerable(root, fun n -> struct(n.Key, n.Value)) :> seq<_>

    member x.ToList() =
        YamImplementation.toList [] root
            
    member x.ToListV() =
        YamImplementation.toListV [] root

    member x.ToArray() =
        let arr = Array.zeroCreate (YamImplementation.count root)
        YamImplementation.copyTo arr 0 root |> ignore
        arr
        
    member x.ToArrayV() =
        let arr = Array.zeroCreate (YamImplementation.count root)
        YamImplementation.copyToV arr 0 root |> ignore
        arr
        
    member x.Iter(action : 'Key -> 'Value -> unit) =
        let action = OptimizedClosures.FSharpFunc<_,_,_>.Adapt(action)
        YamImplementation.iter action root
        
    member x.Fold(state : 'State, folder : 'State -> 'Key -> 'Value -> 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt(folder)
        YamImplementation.fold state folder root
        
    member x.FoldBack(state : 'State, folder : 'Key -> 'Value -> 'State -> 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt(folder)
        YamImplementation.foldBack state folder root

    member x.Map(mapping : 'Key -> 'Value -> 'T) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        Yam(comparer, YamImplementation.map mapping root)

    member x.ChooseV(mapping : 'Key -> 'Value -> voption<'T>) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        Yam(comparer, YamImplementation.chooseV mapping root)

    member x.Choose(mapping : 'Key -> 'Value -> option<'T>) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        Yam(comparer, YamImplementation.choose mapping root)

    member x.Filter(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        Yam(comparer, YamImplementation.filter predicate root)

    member x.Exists(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        YamImplementation.exists predicate root
        
    member x.ForAll(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        YamImplementation.forall predicate root
        
    member x.Partition(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        let mutable t = null
        let mutable f = null
        YamImplementation.partition predicate &t &f root
        Yam(comparer, t), Yam(comparer, f)



    member x.WithMin(min : 'Key) = Yam(comparer, YamImplementation.withMin comparer min root)
    member x.WithMax(max : 'Key) = Yam(comparer, YamImplementation.withMax comparer max root)
    member x.Slice(min : 'Key, max : 'Key) = Yam(comparer, YamImplementation.slice comparer min max root)
    
    member x.ReplaceRangeV(min : 'Key, max : 'Key, replace : voption<struct('Key * 'Value)> -> voption<struct('Key * 'Value)> -> struct(voption<'Value> * voption<'Value>)) =
        let c = comparer.Compare(min, max) 
        if c > 0 then
            // min > max
            x
        elif c < 0 then
            // min < max
            let newRoot = YamImplementation.replaceRange comparer min max ValueNone ValueNone replace root
            Yam(comparer, newRoot)
        else
            // min = max
            let replacement l _ r =
                let struct(a, b) = replace l r
                match b with
                | ValueNone -> a
                | b -> b

            let newRoot = YamImplementation.changeWithNeighbours comparer max ValueNone ValueNone replacement root
            Yam(comparer, newRoot)

    member x.ReplaceRange(min : 'Key, max : 'Key, replace : option<('Key * 'Value)> -> option<('Key * 'Value)> -> (option<'Value> * option<'Value>)) =
        let inline v (o : option<_>) =
            match o with
            | Some v -> ValueSome v
            | None -> ValueNone
        
        let replacement (l : voption<_>) (r : voption<_>) =
            match l with
            | ValueSome struct(lk, lv) ->
                match r with
                | ValueSome struct(rk, rv) ->
                    let (l, r) = replace (Some (lk, lv)) (Some (rk, rv))
                    struct(v l, v r)
                | ValueNone ->
                    let (l, r) = replace (Some (lk, lv)) None
                    struct(v l, v r)
            | ValueNone ->
                match r with
                | ValueSome struct(rk, rv) ->
                    let (l, r) = replace None (Some (rk, rv))
                    struct(v l, v r)
                | ValueNone ->
                    let (l, r) = replace None None
                    struct(v l, v r)
               
        x.ReplaceRangeV(min, max, replacement)

    member x.ChangeWithNeighboursV(key : 'Key, replace : voption<struct('Key * 'Value)> -> voption<'Value> -> voption<struct('Key * 'Value)> -> voption<'Value>) =
        let newRoot = YamImplementation.changeWithNeighbours comparer key ValueNone ValueNone replace root
        Yam(comparer, newRoot)

    member x.ChangeWithNeighbours(key : 'Key, replace : option<('Key * 'Value)> -> option<'Value> -> option<('Key * 'Value)> -> option<'Value>) =
        x.ChangeWithNeighboursV(key, fun l s r ->
            let l = match l with | ValueSome struct(k,v) -> Some (k,v) | _ -> None
            let r = match r with | ValueSome struct(k,v) -> Some (k,v) | _ -> None
            let s = match s with | ValueSome v -> Some v | _ -> None
            match replace l s r with
            | Some v -> ValueSome v
            | None -> ValueNone
        )

    member x.GetSlice(min : option<'Key>, max : option<'Key>) =
        match min with
        | Some min ->
            match max with
            | Some max -> x.Slice(min, max)
            | None -> x.WithMin min
        | None ->
            match max with
            | Some max -> x.WithMax max
            | None -> x

    member x.Neighbours(key : 'Key) =
        let mutable lKey = Unchecked.defaultof<_>
        let mutable rKey = Unchecked.defaultof<_>
        let mutable lValue = Unchecked.defaultof<_>
        let mutable rValue = Unchecked.defaultof<_>
        let mutable sValue = Unchecked.defaultof<_>
        let flags = YamImplementation.getNeighbours comparer key YamImplementation.NeighbourFlags.None &lKey &lValue &sValue &rKey &rValue root

        let left =
            if flags.HasFlag YamImplementation.NeighbourFlags.Left then Some (lKey, lValue)
            else None
            
        let self =
            if flags.HasFlag YamImplementation.NeighbourFlags.Self then Some (sValue)
            else None
            
        let right =
            if flags.HasFlag YamImplementation.NeighbourFlags.Right then Some (rKey, rValue)
            else None

        left, self, right
        

    member x.NeighboursV(key : 'Key) =
        let mutable lKey = Unchecked.defaultof<_>
        let mutable rKey = Unchecked.defaultof<_>
        let mutable lValue = Unchecked.defaultof<_>
        let mutable rValue = Unchecked.defaultof<_>
        let mutable sValue = Unchecked.defaultof<_>
        let flags = YamImplementation.getNeighbours comparer key YamImplementation.NeighbourFlags.None &lKey &lValue &sValue &rKey &rValue root

        let left =
            if flags.HasFlag YamImplementation.NeighbourFlags.Left then ValueSome struct(lKey, lValue)
            else ValueNone
            
        let self =
            if flags.HasFlag YamImplementation.NeighbourFlags.Self then ValueSome (sValue)
            else ValueNone
            
        let right =
            if flags.HasFlag YamImplementation.NeighbourFlags.Right then ValueSome struct(rKey, rValue)
            else ValueNone

        struct(left, self, right)

    member x.NeighboursAt(index : int) =
        let mutable lKey = Unchecked.defaultof<_>
        let mutable rKey = Unchecked.defaultof<_>
        let mutable sKey = Unchecked.defaultof<_>
        let mutable lValue = Unchecked.defaultof<_>
        let mutable rValue = Unchecked.defaultof<_>
        let mutable sValue = Unchecked.defaultof<_>
        let flags = YamImplementation.getNeighboursAt index YamImplementation.NeighbourFlags.None &lKey &lValue &sKey &sValue &rKey &rValue root

        let left =
            if flags.HasFlag YamImplementation.NeighbourFlags.Left then Some (lKey, lValue)
            else None
            
        let self =
            if flags.HasFlag YamImplementation.NeighbourFlags.Self then Some (sKey, sValue)
            else None
            
        let right =
            if flags.HasFlag YamImplementation.NeighbourFlags.Right then Some (rKey, rValue)
            else None

        left, self, right
        
    member x.NeighboursAtV(index : int) =
        let mutable lKey = Unchecked.defaultof<_>
        let mutable rKey = Unchecked.defaultof<_>
        let mutable sKey = Unchecked.defaultof<_>
        let mutable lValue = Unchecked.defaultof<_>
        let mutable rValue = Unchecked.defaultof<_>
        let mutable sValue = Unchecked.defaultof<_>
        let flags = YamImplementation.getNeighboursAt index YamImplementation.NeighbourFlags.None &lKey &lValue &sKey &sValue &rKey &rValue root

        let left =
            if flags.HasFlag YamImplementation.NeighbourFlags.Left then ValueSome struct(lKey, lValue)
            else ValueNone
            
        let self =
            if flags.HasFlag YamImplementation.NeighbourFlags.Self then ValueSome struct(sKey, sValue)
            else ValueNone
            
        let right =
            if flags.HasFlag YamImplementation.NeighbourFlags.Right then ValueSome struct(rKey, rValue)
            else ValueNone

        struct(left, self, right)
        


    static member Union(l : Yam<'Key, 'Value>, r : Yam<'Key, 'Value>) =
        let cmp = defaultComparer
        Yam(cmp, YamImplementation.union cmp l.Root r.Root)
        
    static member UnionWith(l : Yam<'Key, 'Value>, r : Yam<'Key, 'Value>, resolve : 'Key -> 'Value -> 'Value -> 'Value) =
        let cmp = defaultComparer
        let resolve = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt resolve
        Yam(cmp, YamImplementation.unionWith cmp resolve l.Root r.Root)


    member x.ComputeDeltaTo( r : Yam<'Key, 'Value2>, 
                             add : 'Key -> 'Value2 -> 'OP, 
                             update : 'Key -> 'Value -> 'Value2 -> voption<'OP>,
                             remove : 'Key -> 'Value -> 'OP) =
        let add = OptimizedClosures.FSharpFunc<_,_,_>.Adapt add
        let update = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt update
        let remove = OptimizedClosures.FSharpFunc<_,_,_>.Adapt remove
        Yam(comparer, YamImplementation.computeDelta comparer root r.Root update add remove)
        
    member x.ApplyDelta(delta : Yam<'Key, 'OP>, apply : 'Key -> voption<'Value> -> 'OP -> voption<'Value>) =
        let apply = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt apply
        let applyNoState = OptimizedClosures.FSharpFunc<_,_,_>.Adapt (fun k o -> apply.Invoke(k, ValueNone, o))
        let applyReal = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt (fun k v o -> apply.Invoke(k, ValueSome v, o))
        Yam(comparer, YamImplementation.ApplyDelta.applyDelta comparer root delta.Root applyNoState applyReal)
        
        
    member x.ApplyDeltaAndGetEffective(delta : Yam<'Key, 'OP>, apply : 'Key -> voption<'Value> -> 'OP -> voption<'Value>) =
        let apply = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt apply
        let applyNoState = OptimizedClosures.FSharpFunc<_,_,_>.Adapt (fun k o -> apply.Invoke(k, ValueNone, o))
        let applyReal = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt (fun k v o -> apply.Invoke(k, ValueSome v, o))
        let equal = OptimizedClosures.FSharpFunc<_,_,_>.Adapt Unchecked.equals

        let mutable effective = null
        let root = YamImplementation.ApplyDelta.applyDeltaAndGetEffective comparer root delta.Root applyNoState applyReal equal &effective
        Yam(comparer, root), Yam(comparer, effective)
        

    member x.GetEnumerator() = new YamEnumerator<_,_>(root)

    interface System.Collections.IEnumerable with
        member x.GetEnumerator() = x.GetEnumerator() :> _
            
    interface System.Collections.Generic.IEnumerable<KeyValuePair<'Key, 'Value>> with
        member x.GetEnumerator() = x.GetEnumerator() :> _

    interface System.Collections.Generic.IReadOnlyCollection<KeyValuePair<'Key, 'Value>> with
        member x.Count = x.Count

    interface System.Collections.Generic.IReadOnlyDictionary<'Key, 'Value> with
        member x.Keys = YamImplementation.YamMappingEnumerable(root, fun n -> n.Key) :> seq<_>
        member x.Values = YamImplementation.YamMappingEnumerable(root, fun n -> n.Value) :> seq<_>
        member x.Item with get (key : 'Key) = x.[key]
        member x.ContainsKey(key : 'Key) = x.ContainsKey key
        member x.TryGetValue(key : 'Key, value : byref<'Value>) = x.TryGetValue(key, &value)
        


and YamEnumerator<'Key, 'Value> =
    struct
        val mutable internal Root : YamImplementation.Node<'Key, 'Value>
        val mutable internal Stack : list<struct(YamImplementation.Node<'Key, 'Value> * bool)>
        val mutable internal CurrentNode : YamImplementation.Node<'Key, 'Value>


        member x.MoveNext() =
            match x.Stack with
            | struct(n, deep) :: t ->
                x.Stack <- t

                if n.Height > 1uy then
                    if deep then
                        let inner = n :?> YamImplementation.Inner<'Key, 'Value>

                        if not (isNull inner.Right) then 
                            x.Stack <- struct(inner.Right, true) :: x.Stack
                                
                        if isNull inner.Left then 
                            x.CurrentNode <- n
                            true
                        else
                            x.Stack <- struct(inner.Left, true) :: struct(n, false) :: x.Stack
                            x.MoveNext()
                    else
                        x.CurrentNode <- n
                        true
                else
                    x.CurrentNode <- n
                    true

            | [] ->
                false


        member x.Reset() =
            if isNull x.Root then
                x.Stack <- []
                x.CurrentNode <- null
            else
                x.Stack <- [struct(x.Root, true)]
                x.CurrentNode <- null

        member x.Dispose() =
            x.Root <- null
            x.CurrentNode <- null
            x.Stack <- []

        member x.Current =
            KeyValuePair(x.CurrentNode.Key, x.CurrentNode.Value)

        interface System.Collections.IEnumerator with
            member x.MoveNext() = x.MoveNext()
            member x.Reset() = x.Reset()
            member x.Current = x.Current :> obj

        interface System.Collections.Generic.IEnumerator<KeyValuePair<'Key, 'Value>> with
            member x.Current = x.Current
            member x.Dispose() = x.Dispose()

        new(root : YamImplementation.Node<'Key, 'Value>) =
            {
                Root = root
                Stack = if isNull root then [] else [struct(root, true)]
                CurrentNode = null
            }

    end


module Yam =
    let inline empty<'Key, 'Value when 'Key : comparison> = Yam<'Key, 'Value>.Empty
    
    let inline isEmpty (map : Yam<'Key, 'Value>) = map.Count = 0
    let inline count (map : Yam<'Key, 'Value>) = map.Count

    let inline add (key : 'Key) (value : 'Value) (map : Yam<'Key, 'Value>) = map.Add(key, value)
    let inline change (key : 'Key) (update : option<'Value> -> option<'Value>) (map : Yam<'Key, 'Value>) = map.Change(key, update)
    let inline changeV (key : 'Key) (update : voption<'Value> -> voption<'Value>) (map : Yam<'Key, 'Value>) = map.Change(key, update)

    let inline remove (key : 'Key) (map : Yam<'Key, 'Value>) = map.Remove(key)
    let inline tryRemove (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryRemove(key)
    let inline tryRemoveV (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryRemoveV(key)
    
    let inline removeAt (index : int) (map : Yam<'Key, 'Value>) = map.RemoveAt(index)
    let inline tryRemoveAt (index : int) (map : Yam<'Key, 'Value>) = map.TryRemoveAt(index)
    let inline tryRemoveAtV (index : int) (map : Yam<'Key, 'Value>) = map.TryRemoveAtV(index)

    let inline tryFind (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryFind key
    let inline tryFindV (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryFindV key
    let inline find (key : 'Key) (map : Yam<'Key, 'Value>) = map.Find key
    
    let inline tryItem (index : int) (map : Yam<'Key, 'Value>) = map.TryItem index
    let inline tryItemV (index : int) (map : Yam<'Key, 'Value>) = map.TryItemV index
    let inline item (index : int) (map : Yam<'Key, 'Value>) = map.GetItem index
    let inline itemV (index : int) (map : Yam<'Key, 'Value>) = map.GetItemV index

    let inline containsKey (key : 'Key) (map : Yam<'Key, 'Value>) = map.ContainsKey key

    let inline tryMin (map : Yam<'Key, 'Value>) = map.TryMin()
    let inline tryMax (map : Yam<'Key, 'Value>) = map.TryMax()
    let inline tryMinV (map : Yam<'Key, 'Value>) = map.TryMinV()
    let inline tryMaxV (map : Yam<'Key, 'Value>) = map.TryMaxV()

    let inline iter (action : 'Key -> 'Value -> unit) (map : Yam<'Key, 'Value>) = map.Iter action
    let inline fold (folder : 'State -> 'Key -> 'Value -> 'State) (state : 'State) (map : Yam<'Key, 'Value>) = map.Fold(state, folder)
    let inline foldBack (folder : 'Key -> 'Value -> 'State -> 'State) (map : Yam<'Key, 'Value>) (state : 'State) =  map.FoldBack(state, folder)
    
    let inline map (mapping : 'Key -> 'Value -> 'T) (map : Yam<'Key, 'Value>) = map.Map mapping
    let inline choose (mapping : 'Key -> 'Value -> option<'T>) (map : Yam<'Key, 'Value>) = map.Choose mapping
    let inline chooseV (mapping : 'Key -> 'Value -> voption<'T>) (map : Yam<'Key, 'Value>) = map.ChooseV mapping
    let inline filter (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.Filter predicate
    
    let inline exists (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.Exists predicate
    let inline forall (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.ForAll predicate
    let inline partition (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.Partition predicate
    
    let inline withMin (min : 'Key) (map : Yam<'Key, 'Value>) = map.WithMin min
    let inline withMax (min : 'Key) (map : Yam<'Key, 'Value>) = map.WithMax min
    let inline slice (min : 'Key) (max : 'Key) (map : Yam<'Key, 'Value>) = map.Slice(min, max)
    let inline replaceRange (min : 'Key) (max : 'Key) (replacement : option<'Key * 'Value> -> option<'Key * 'Value> -> (option<'Value> * option<'Value>)) (map : Yam<'Key, 'Value>) =
        map.ReplaceRange(min, max, replacement)

    let inline replaceRangeV (min : 'Key) (max : 'Key) (replacement : voption<struct('Key * 'Value)> -> voption<struct('Key * 'Value)> -> struct(voption<'Value> * voption<'Value>)) (map : Yam<'Key, 'Value>) =
        map.ReplaceRangeV(min, max, replacement)
        
    let inline changeWithNeighbours (key : 'Key) (update : option<'Key * 'Value> -> option<'Value> -> option<'Key * 'Value> -> option<'Value>) (map : Yam<'Key, 'Value>) =
        map.ChangeWithNeighbours(key, update)
        
    let inline changeWithNeighboursV (key : 'Key) (update : voption<struct('Key * 'Value)> -> voption<'Value> -> voption<struct('Key * 'Value)> -> voption<'Value>) (map : Yam<'Key, 'Value>) =
        map.ChangeWithNeighboursV(key, update)

    let inline neighbours (key : 'Key) (map : Yam<'Key, 'Value>) = map.Neighbours key
    let inline neighboursV (key : 'Key) (map : Yam<'Key, 'Value>) = map.NeighboursV key
    let inline neighboursAt (index : int) (map : Yam<'Key, 'Value>) = map.NeighboursAt index
    let inline neighboursAtV (index : int) (map : Yam<'Key, 'Value>) = map.NeighboursAtV index


    let inline ofSeq (elements : seq<'Key * 'Value>) = Yam.FromSeq elements
    let inline ofSeqV (elements : seq<struct('Key * 'Value)>) = Yam.FromSeqV elements
    let inline ofList (elements : list<'Key * 'Value>) = Yam.FromList elements
    let inline ofListV (elements : list<struct('Key * 'Value)>) = Yam.FromListV elements
    let inline ofArray (elements : ('Key * 'Value)[]) = Yam.FromArray elements
    let inline ofArrayV (elements : struct('Key * 'Value)[]) = Yam.FromArrayV elements
    
    let inline toSeq (map : Yam<'Key, 'Value>) = map.ToSeq()
    let inline toSeqV (map : Yam<'Key, 'Value>) = map.ToSeqV()
    let inline toList (map : Yam<'Key, 'Value>) = map.ToList()
    let inline toListV (map : Yam<'Key, 'Value>) = map.ToListV()
    let inline toArray (map : Yam<'Key, 'Value>) = map.ToArray()
    let inline toArrayV (map : Yam<'Key, 'Value>) = map.ToArrayV()
    
    let inline union (l : Yam<'Key, 'Value>) (r : Yam<'Key, 'Value>) = Yam.Union(l, r)
    let inline unionMany (maps : #seq<Yam<'Key, 'Value>>) = (empty, maps) ||> Seq.fold union
    let inline unionWith (resolve : 'Key -> 'Value -> 'Value -> 'Value) (l : Yam<'Key, 'Value>) (r : Yam<'Key, 'Value>) = Yam.UnionWith(l, r, resolve)
    
    let inline computeDelta 
        (add : 'Key -> 'Value2 -> 'OP)
        (update : 'Key -> 'Value1 -> 'Value2 -> voption<'OP>)
        (remove : 'Key -> 'Value1 -> 'OP)
        (l : Yam<'Key, 'Value1>) (r : Yam<'Key, 'Value2>) =
        l.ComputeDeltaTo(r, add, update, remove)
                     
    let inline applyDelta 
        (apply : 'Key -> voption<'Value> -> 'OP -> voption<'Value>)
        (state : Yam<'Key, 'Value>) (delta : Yam<'Key, 'OP>) =
        state.ApplyDelta(delta, apply)
                                
    let inline applyDeltaAndGetEffective
        (apply : 'Key -> voption<'Value> -> 'OP -> voption<'Value>)
        (state : Yam<'Key, 'Value>) (delta : Yam<'Key, 'OP>) =
        state.ApplyDeltaAndGetEffective(delta, apply)
                                
