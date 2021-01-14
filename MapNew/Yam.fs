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
        val mutable public Count : int
        val mutable public Left : Node<'Key, 'Value>
        val mutable public Right : Node<'Key, 'Value>

        static member inline GetCnt(node : Node<'Key, 'Value>, height : byref<uint8>) =
            if isNull node then
                height <- 0uy
                0
            else
                height <- node.Height
                if height = 1uy then 1
                else (node :?> Inner<'Key, 'Value>).Count

        static member inline FixHeightAndCount(inner : Inner<'Key, 'Value>) =
            let mutable lh = 0uy
            let mutable rh = 0uy
            let lc = Inner.GetCnt(inner.Left, &lh)
            let rc = Inner.GetCnt(inner.Right, &rh)
            inner.Count <- 1 + lc + rc
            inner.Height <- 1uy + max lh rh

        new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>, h : byte, cnt : int) =
            { inherit  Node<'Key, 'Value>(k, v, h); Left = l; Right = r; Count = cnt }

        
    let inline height (n : Node<'Key, 'Value>) =
        if isNull n then 0uy
        else n.Height
            
    let inline count (n : Node<'Key, 'Value>) =
        if isNull n then 0
        elif n.Height = 1uy then 1
        else (n :?> Inner<'Key, 'Value>).Count
            
    let inline balance (n : Inner<'Key, 'Value>) =
        int (height n.Right) - int (height n.Left)

    // abs (balance l r) <= 3 (as caused by add/remove)
    let unsafeBinary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =
        let inline newBinary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =
            if isNull l && isNull r then Node(k, v)
            else 
                let mutable hl = 0uy
                let mutable hr = 0uy
                let cl = Inner.GetCnt(l, &hl)
                let cr = Inner.GetCnt(r, &hr)
                Inner(l, k, v, r, 1uy + max hl hr, 1 + cl + cr) :> Node<_,_>

        let mutable lh = 0uy
        let mutable rh = 0uy
        let lc = Inner.GetCnt(l, &lh)
        let rc = Inner.GetCnt(r, &rh)

        let b = int rh - int lh
        if b > 2 then
            // rh > lh + 2
            let r = r :?> Inner<'Key, 'Value>
            let rb = balance r
            if rb > 0 then
                // right right 
                newBinary 
                    (newBinary l k v r.Left)
                    r.Key r.Value
                    r.Right
            else
                // right left
                let rl = r.Left :?> Inner<'Key, 'Value>
                newBinary
                    (newBinary l k v rl.Left)
                    rl.Key rl.Value
                    (newBinary rl.Right r.Key r.Value r.Right)

        elif b < -2 then
            // lh > rh + 2
            let l = l :?> Inner<'Key, 'Value>
            let lb = balance l
            if lb < 0 then
                // left left
                newBinary 
                    l.Left
                    l.Key l.Value
                    (newBinary l.Right k v r)
            else
                // left right
                let lr = l.Right :?> Inner<'Key, 'Value>
                newBinary 
                    (newBinary l.Left l.Key l.Value lr.Left)
                    lr.Key lr.Value
                    (newBinary lr.Right k v r)

        elif lh = 0uy && rh = 0uy then Node(k, v)
        else Inner(l, k, v, r, 1uy + max lh rh, 1 + lc + rc) :> Node<_,_>

    let rec unsafeRemoveMin (n : Node<'Key, 'Value>) =
        if n.Height = 1uy then
            struct(n.Key, n.Value, null)
        else
            let n = n :?> Inner<'Key, 'Value>
            if isNull n.Left then
                struct(n.Key, n.Value, n.Right)
            else
                let struct(k, v, newLeft) = unsafeRemoveMin n.Left
                struct(k, v, unsafeBinary newLeft n.Key n.Value n.Right)
                    
    let rec unsafeRemoveMax (n : Node<'Key, 'Value>) =
        if n.Height = 1uy then
            struct(n.Key, n.Value, null)
        else
            let n = n :?> Inner<'Key, 'Value>
            if isNull n.Right then
                struct(n.Key, n.Value, n.Left)
            else
                let struct(k, v, newRight) = unsafeRemoveMax n.Right
                struct(k, v, unsafeBinary n.Left n.Key n.Value newRight)

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
                node.Count <- 1 + r.Count + (count t2)
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
                node.Count <- 1 + a.Count + b.Count
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
                node.Count <- 1 + (count t0) + a.Count
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
                node.Count <- 1 + a.Count + b.Count
                node.Height <- 1uy + max a.Height b.Height
        else
            Inner.FixHeightAndCount node
            


    // abs (balance l r) <= 3 (as caused by add/remove)
    let unsafeJoin (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) : Node<'Key, 'Value> =
        if isNull l then r
        elif isNull r then l
        else
            let lc = count l
            let rc = count r
            if lc > rc then
                let struct(k, v, l) = unsafeRemoveMax l
                unsafeBinary l k v r
            else
                let struct(k, v, r) = unsafeRemoveMin r
                unsafeBinary l k v r
                
    let rec binary (l : Node<'Key, 'Value>) (k : 'Key) (v : 'Value) (r : Node<'Key, 'Value>) =
    
        let mutable lh = 0uy
        let mutable rh = 0uy
        let lc = Inner.GetCnt(l, &lh)
        let rc = Inner.GetCnt(r, &rh)
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
        else Inner(l, k, v, r, 1uy + max lh rh, 1 + lc + rc) :> Node<_,_>
        
    let rec join (l : Node<'Key, 'Value>) (r : Node<'Key, 'Value>) : Node<'Key, 'Value> =
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

    let rec add (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
        if isNull node then
            // empty
            Node(key, value)

        elif node.Height = 1uy then
            // leaf
            let c = cmp.Compare(key, node.Key)
            if c > 0 then Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else Node(key, value)

        else
            // inner
            let n = node :?> Inner<'Key, 'Value>
            let c = cmp.Compare(key, n.Key)
            if c > 0 then
                unsafeBinary n.Left n.Key n.Value (add cmp key value n.Right)
            elif c < 0 then
                unsafeBinary (add cmp key value n.Left) n.Key n.Value n.Right
            else
                Inner(n.Left, key, value, n.Right, n.Height, n.Count) :> Node<_,_>
          
    let rec addInPlace (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (node : Node<'Key, 'Value>) =
        if isNull node then
            // empty
            Node(key, value)

        elif node.Height = 1uy then
            // leaf
            let c = cmp.Compare(key, node.Key)
            if c > 0 then Inner(node, key, value, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then Inner(null, key, value, node, 2uy, 2) :> Node<_,_>
            else 
                node.Key <- key
                node.Value <- value
                node

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
                    Inner(node, key, n, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then
                match update ValueNone with
                | ValueNone -> node
                | ValueSome n ->
                    Inner(null, key, n, node, 2uy, 2) :> Node<_,_>
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
                    Inner(node.Left, key, n, node.Right, node.Height, node.Count) :> Node<_,_>
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
                    Inner(node, key, n, null, 2uy, 2) :> Node<_,_>
            elif c < 0 then
                match update None with
                | None -> node
                | Some n ->
                    Inner(null, key, n, node, 2uy, 2) :> Node<_,_>
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
                    Inner(node.Left, key, n, node.Right, node.Height, node.Count) :> Node<_,_>
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

    member internal x.Root = root

    static member Empty = Yam<'Key, 'Value>(defaultComparer, null)
        
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
            let struct(k, v, rest) = YamImplementation.unsafeRemoveMin root
            Some (k,v,Yam(comparer, rest))
            
    member x.TryRemoveMax() =
        if isNull root then 
            None
        else 
            let struct(k, v, rest) = YamImplementation.unsafeRemoveMax root
            Some (k,v,Yam(comparer, rest))
            
    member x.TryRemoveMinV() =
        if isNull root then 
            ValueNone
        else 
            let struct(k, v, rest) = YamImplementation.unsafeRemoveMin root
            ValueSome struct(k,v,Yam(comparer, rest))
            
    member x.TryRemoveMaxV() =
        if isNull root then 
            ValueNone
        else 
            let struct(k, v, rest) = YamImplementation.unsafeRemoveMax root
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
    let inline exists (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.Exists predicate
    let inline forall (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.ForAll predicate
    let inline partition (predicate : 'Key -> 'Value -> bool) (map : Yam<'Key, 'Value>) = map.Partition predicate

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


