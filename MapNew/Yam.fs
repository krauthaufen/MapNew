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

    let rec tryFindV (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            ValueNone
        elif node.Height = 1uy then
            if cmp.Compare(key, node.Key) = 0 then ValueSome node.Value
            else ValueNone
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then tryFindV cmp key node.Right
            elif c < 0 then tryFindV cmp key node.Left
            else ValueSome node.Value
            
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
            
    let rec tryMin (node : Node<'Key, 'Value>) =
        if isNull node then 
            None
        elif node.Height = 1uy then
            Some (node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Left then Some (node.Key, node.Value)
            else tryMin node.Left
              
    let rec tryMax (node : Node<'Key, 'Value>) =
        if isNull node then 
            None
        elif node.Height = 1uy then
            Some (node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Right then Some (node.Key, node.Value)
            else tryMax node.Right
      
    let rec tryMinV (node : Node<'Key, 'Value>) =
        if isNull node then 
            ValueNone
        elif node.Height = 1uy then
            ValueSome struct(node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Left then ValueSome struct(node.Key, node.Value)
            else tryMinV node.Left
              
    let rec tryMaxV (node : Node<'Key, 'Value>) =
        if isNull node then 
            ValueNone
        elif node.Height = 1uy then
            ValueSome struct(node.Key, node.Value)
        else
            let node = node :?> Inner<'Key, 'Value>
            if isNull node.Right then ValueSome struct(node.Key, node.Value)
            else tryMaxV node.Right

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
             
    let rec remove (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then 
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c = 0 then null
            else node
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then unsafeBinary node.Left node.Key node.Value (remove cmp key node.Right)
            elif c < 0 then unsafeBinary (remove cmp key node.Left) node.Key node.Value node.Right
            else unsafeJoin node.Left node.Right

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

    let rec partition (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : Node<'Key, 'Value>) =
        if isNull node then
            struct(null, null)
        elif node.Height = 1uy then
            if predicate.Invoke(node.Key, node.Value) then struct(node, null)
            else struct(null, node)
        else
            let node = node :?> Inner<'Key, 'Value>

            let struct(lt, lf) = partition predicate node.Left
            let c = predicate.Invoke(node.Key, node.Value)
            let struct(rt, rf) = partition predicate node.Right

            if c then
                struct( binary lt node.Key node.Value rt,  join lf rf )
            else
                struct( join lt rt, binary lf node.Key node.Value rf )


    let rec splitV (cmp : IComparer<'Key>) (key : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            struct(null, ValueNone, null)
        elif node.Height = 1uy then
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                struct(node, ValueNone, null)
            elif c < 0 then
                struct(null, ValueNone, node)
            else
                struct(null, ValueSome node.Value, null)
        else
            let node = node :?> Inner<'Key,'Value>
            let c = cmp.Compare(key, node.Key)
            if c > 0 then
                let struct(rl, v, rr) = splitV cmp key node.Right
                struct(binary node.Left node.Key node.Value rl, v, rr)
            elif c < 0 then
                let struct(ll, v, lr) = splitV cmp key node.Left
                struct(ll, v, binary lr node.Key node.Value node.Right)
            else
                struct(node.Left, ValueSome node.Value, node.Right)
                
    let rec unionWith (cmp : IComparer<'Key>) (resolve : OptimizedClosures.FSharpFunc<'Key, 'Value, 'Value, 'Value>) (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) =
        if isNull l then r
        elif isNull r then l

        elif l.Height = 1uy then
            // left leaf
            r |> changeV cmp l.Key (function
                | ValueSome rv -> ValueSome (resolve.Invoke(l.Key, l.Value, rv))
                | ValueNone -> ValueSome l.Value
            )
        elif r.Height = 1uy then
            // right leaf
            l |> changeV cmp r.Key (function
                | ValueSome lv -> ValueSome (resolve.Invoke(r.Key, lv, r.Value))
                | ValueNone -> ValueSome r.Value
            )

        else
            // both inner
            let l = l :?> Inner<'Key, 'Value>
            let r = r :?> Inner<'Key, 'Value>

            if l.Height < r.Height then
                let key = r.Key
                let struct(ll, lv, lr) = splitV cmp key l

                let newLeft = unionWith cmp resolve ll r.Left

                let value =
                    match lv with
                    | ValueSome lv -> resolve.Invoke(key, lv, r.Value)
                    | ValueNone -> r.Value
                let newRight = unionWith cmp resolve lr r.Right

                binary newLeft key value newRight
            else
                let key = l.Key
                let struct(rl, rv, rr) = splitV cmp key r

                let newLeft = unionWith cmp resolve l.Left rl

                let value =
                    match rv with
                    | ValueSome rv -> resolve.Invoke(key, l.Value, rv)
                    | ValueNone -> l.Value

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
                let struct(l1, v1, r1) = splitV cmp key map1
                let newLeft = union cmp l1 map2.Left
                let value = map2.Value
                let newRight = union cmp r1 map2.Right

                binary newLeft key value newRight
            else
                let key = map1.Key
                let struct(l2, v2, r2) = splitV cmp key map2
                let newLeft = union cmp map1.Left l2
                let value =
                    match v2 with
                    | ValueSome rv -> rv
                    | ValueNone -> map1.Value
                let newRight = union cmp map1.Right r2
                binary newLeft key value newRight

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

           
    let rec withMinExclusive (cmp : IComparer<'Key>) (minKey : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, minKey)
            if c > 0 then node
            else null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, minKey)
            if c > 0 then
                binary (withMinExclusive cmp minKey node.Left) node.Key node.Value node.Right
            else
                withMinExclusive cmp minKey node.Right
                              
    let rec withMaxExclusive (cmp : IComparer<'Key>) (maxKey : 'Key) (node : Node<'Key, 'Value>) =
        if isNull node then
            node
        elif node.Height = 1uy then
            let c = cmp.Compare(node.Key, maxKey)
            if c < 0 then node
            else null
        else
            let node = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(node.Key, maxKey)

            if c >= 0 then
                withMaxExclusive cmp maxKey node.Left
            else
                binary node.Left node.Key node.Value (withMaxExclusive cmp maxKey node.Right)


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


    let rec replaceRange 
        (cmp : IComparer<'Key>) 
        (min : 'Key) (max : 'Key) 
        (l : voption<struct('Key * 'Value)>)
        (r : voption<struct('Key * 'Value)>)
        (replacement : voption<struct('Key * 'Value)> -> voption<struct('Key * 'Value)> -> struct(voption<'Value> * voption<'Value>))
        (node : Node<'Key, 'Value>) =
        
        if isNull node then
            null
        elif node.Height = 1uy then
            let cMin = cmp.Compare(node.Key, min)
            let cMax = cmp.Compare(node.Key, max)
            if cMin >= 0 && cMax <= 0 then
                let struct(a, b) = replacement l r

                match a with
                | ValueSome va ->
                    match b with
                    | ValueSome vb ->
                        #if COUNT
                        Inner(null, min, va, Node(max, vb), 2uy, 2) :> Node<_,_>
                        #else
                        Inner(null, min, va, Node(max, vb), 2uy) :> Node<_,_>
                        #endif

                    | ValueNone ->
                        Node(min, va)
                | ValueNone ->
                    match b with
                    | ValueSome vb ->
                        Node(max, vb)
                    | ValueNone ->
                        null
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
                let l = withMaxExclusive cmp min node.Left
                let r = withMinExclusive cmp max node.Right

                let ln = tryMaxV l
                let rn = tryMinV r
                let struct(a, b) = replacement ln rn

                match a with
                | ValueSome va ->
                    match b with
                    | ValueSome vb ->
                        if height l < height r then
                            binary (add cmp min va l) max vb r
                        else
                            binary l min va (add cmp max vb r)
                    | ValueNone ->
                        binary l min va r
                | ValueNone ->
                    match b with
                    | ValueSome vb ->
                        binary l max vb r
                    | ValueNone ->
                        join l r

            elif cMin < 0 then
                // only left
                let l1 = replaceRange cmp min max l (ValueSome struct(node.Key, node.Value)) replacement node.Left
                binary l1 node.Key node.Value node.Right
            else    
                // only right
                let r1 = replaceRange cmp min max (ValueSome struct(node.Key, node.Value)) r replacement node.Right
                binary node.Left node.Key node.Value r1



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
        Yam(comparer, YamImplementation.remove comparer key root)
        
    member x.Change(key : 'Key, update : option<'Value> -> option<'Value>) =
        Yam(comparer, YamImplementation.change comparer key update root)

    member x.Change(key : 'Key, update : voption<'Value> -> voption<'Value>) =
        Yam(comparer, YamImplementation.changeV comparer key update root)
            
    member x.Item
        with get(key : 'Key) = YamImplementation.find comparer key root

    member x.Find(key : 'Key) =
        YamImplementation.find comparer key root

    member x.TryGetValue(key : 'Key, [<Out>] value : byref<'Value>) =
        YamImplementation.tryGetValue comparer key &value root

    member x.TryFind(key : 'Key) =
        YamImplementation.tryFind comparer key root
        
    member x.TryFindV(key : 'Key) =
        YamImplementation.tryFindV comparer key root

    member x.ContainsKey(key : 'Key) =
        YamImplementation.containsKey comparer key root

    member x.TryMin() =
        YamImplementation.tryMin root
        
    member x.TryMax() =
        YamImplementation.tryMax root
        
    member x.TryMinV() =
        YamImplementation.tryMinV root
        
    member x.TryMaxV() =
        YamImplementation.tryMaxV root

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
        let struct(f, t) = YamImplementation.partition predicate root
        Yam(comparer, f), Yam(comparer, t)

    member x.WithMin(min : 'Key) = Yam(comparer, YamImplementation.withMin comparer min root)
    member x.WithMax(max : 'Key) = Yam(comparer, YamImplementation.withMax comparer max root)
    member x.Slice(min : 'Key, max : 'Key) = Yam(comparer, YamImplementation.slice comparer min max root)

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

    static member Union(l : Yam<'Key, 'Value>, r : Yam<'Key, 'Value>) =
        let cmp = defaultComparer
        Yam(cmp, YamImplementation.union cmp l.Root r.Root)
        
    static member UnionWith(l : Yam<'Key, 'Value>, r : Yam<'Key, 'Value>, resolve : 'Key -> 'Value -> 'Value -> 'Value) =
        let cmp = defaultComparer
        let resolve = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt resolve
        Yam(cmp, YamImplementation.unionWith cmp resolve l.Root r.Root)



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
    let inline remove (key : 'Key) (map : Yam<'Key, 'Value>) = map.Remove(key)
    let inline change (key : 'Key) (update : option<'Value> -> option<'Value>) (map : Yam<'Key, 'Value>) = map.Change(key, update)
    let inline changeV (key : 'Key) (update : voption<'Value> -> voption<'Value>) (map : Yam<'Key, 'Value>) = map.Change(key, update)

    let inline tryFind (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryFind key
    let inline tryFindV (key : 'Key) (map : Yam<'Key, 'Value>) = map.TryFindV key
    let inline containsKey (key : 'Key) (map : Yam<'Key, 'Value>) = map.ContainsKey key
    let inline find (key : 'Key) (map : Yam<'Key, 'Value>) = map.Find key

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

    let inline slice (min : 'Key) (max : 'Key) (map : Yam<'Key, 'Value>) = map.Slice(min, max)


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
