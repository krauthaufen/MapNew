namespace Aardvark.Base

open System
open System.Collections.Generic

module MapNewImplementation = 

    [<AbstractClass>]
    type Node<'Key, 'Value>() =
        abstract member Count : int
        abstract member Height : int

        abstract member Add : comparer : IComparer<'Key> * key : 'Key * value : 'Value -> Node<'Key, 'Value>
        abstract member Remove : comparer : IComparer<'Key> * key : 'Key -> Node<'Key, 'Value>

        abstract member AddInPlace : comparer : IComparer<'Key> * key : 'Key * value : 'Value -> Node<'Key, 'Value>

        abstract member ToList : list<'Key * 'Value> -> list<'Key * 'Value>
        abstract member ToListV : list<struct('Key * 'Value)> -> list<struct('Key * 'Value)>

        abstract member CopyTo : dst : ('Key * 'Value)[] * index : int -> int
        abstract member CopyToV : dst : struct('Key * 'Value)[] * index : int -> int
        abstract member CopyToKeyValue : dst : KeyValuePair<'Key, 'Value>[] * index : int -> int

        abstract member TryFind : comparer : IComparer<'Key> * key : 'Key -> option<'Value>
        abstract member TryFindV : comparer : IComparer<'Key> * key : 'Key -> voption<'Value>
        abstract member ContainsKey  : comparer : IComparer<'Key> * key : 'Key -> bool

        abstract member Iter : action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit> -> unit
        abstract member Map : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T> -> Node<'Key, 'T>
        abstract member Filter : predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> Node<'Key, 'Value>
        abstract member Choose : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>> -> Node<'Key, 'T>
        abstract member ChooseV : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>> -> Node<'Key, 'T>

        abstract member Exists : predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> bool
        abstract member Forall : predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> bool
        abstract member Fold : folder : OptimizedClosures.FSharpFunc<'State, 'Key, 'Value, 'State> * seed : 'State -> 'State
        abstract member FoldBack : folder : OptimizedClosures.FSharpFunc<'Key, 'Value, 'State, 'State> * seed : 'State -> 'State

        abstract member TryRemoveHeadV : unit -> voption<struct('Key * 'Value * Node<'Key, 'Value>)>
        abstract member TryRemoveTailV : unit -> voption<struct(Node<'Key, 'Value> * 'Key * 'Value)>
        
        abstract member UnsafeRemoveHeadV : unit -> struct('Key * 'Value * Node<'Key, 'Value>)
        abstract member UnsafeRemoveTailV : unit -> struct(Node<'Key, 'Value> * 'Key * 'Value)

    type MapEmpty<'Key, 'Value> private() =
        inherit Node<'Key, 'Value>()

        static let instance = MapEmpty<'Key, 'Value>() :> Node<_,_>

        static member Instance = instance

        override x.Count = 0
        override x.Height = 0
        override x.Add(_, key, value) =
            MapLeaf(key, value) :> Node<_,_>

        override x.AddInPlace(_, key, value) =
            MapLeaf(key, value) :> Node<_,_>

        override x.Remove(_,_) =
            x :> Node<_,_>

        override x.Iter(_) = ()
        override x.Map(_) = MapEmpty.Instance
        override x.Filter(_) = x :> Node<_,_>
        override x.Choose(_) = MapEmpty.Instance
        override x.ChooseV(_) = MapEmpty.Instance

        override x.Exists(_) = false
        override x.Forall(_) = true
        override x.Fold(_folder, seed) = seed
        override x.FoldBack(_folder, seed) = seed

        override x.ToList(acc) = acc
        override x.ToListV(acc) = acc
        override x.CopyTo(_dst, index) = index
        override x.CopyToV(_dst, index) = index
        override x.CopyToKeyValue(_dst, index) = index

        override x.TryFind(_, _) = None
        override x.TryFindV(_, _) = ValueNone
        override x.ContainsKey(_, _) = false

        override x.TryRemoveHeadV() = ValueNone
        override x.TryRemoveTailV() = ValueNone
        override x.UnsafeRemoveHeadV() = failwith "empty"
        override x.UnsafeRemoveTailV() = failwith "empty"

    and MapLeaf<'Key, 'Value> =
        class 
            inherit Node<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value

            override x.Height =
                1

            override x.Count =
                1

            override x.Add(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then
                    MapNode(x, key, value, MapEmpty.Instance) :> Node<'Key,'Value>
                elif c < 0 then
                    MapNode(MapEmpty.Instance, key, value, x) :> Node<'Key,'Value>
                else
                    MapLeaf(key, value) :> Node<'Key,'Value>
                    
            override x.AddInPlace(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then
                    MapNode(x, key, value, MapEmpty.Instance) :> Node<'Key,'Value>
                elif c < 0 then
                    MapNode(MapEmpty.Instance, key, value, x) :> Node<'Key,'Value>
                else
                    x.Key <- key
                    x.Value <- value
                    x :> Node<'Key,'Value>

                
            override x.Remove(comparer, key) =
                if comparer.Compare(key, x.Key) = 0 then MapEmpty.Instance
                else x :> Node<_,_>


            override x.Iter(action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit>) =
                action.Invoke(x.Key, x.Value)

            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapLeaf(x.Key, mapping.Invoke(x.Key, x.Value)) :> Node<_,_>
                
            override x.Filter(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                if predicate.Invoke(x.Key, x.Value) then
                    x :> Node<_,_>
                else
                    MapEmpty.Instance

            override x.Choose(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) =
                match mapping.Invoke(x.Key, x.Value) with
                | Some v -> 
                    MapLeaf(x.Key, v) :> Node<_,_>
                | None ->
                    MapEmpty.Instance

            override x.ChooseV(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) =
                match mapping.Invoke(x.Key, x.Value) with
                | ValueSome v -> 
                    MapLeaf(x.Key, v) :> Node<_,_>
                | ValueNone ->
                    MapEmpty.Instance
                    
            override x.Exists(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                predicate.Invoke(x.Key, x.Value)

            override x.Forall(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                predicate.Invoke(x.Key, x.Value)

            override x.Fold(folder, seed) = folder.Invoke(seed, x.Key, x.Value)
            override x.FoldBack(folder, seed) = folder.Invoke(x.Key, x.Value, seed)

            override x.ToList(acc) = (x.Key, x.Value) :: acc
            override x.ToListV(acc) = struct(x.Key, x.Value) :: acc

            override x.CopyTo(dst, index) =
                dst.[index] <- (x.Key, x.Value)
                index + 1

            override x.CopyToV(dst, index) =
                dst.[index] <- struct(x.Key, x.Value)
                index + 1

            override x.CopyToKeyValue(dst, index) =
                dst.[index] <- KeyValuePair(x.Key, x.Value)
                index + 1

            override x.TryFind(cmp : IComparer<'Key>, key : 'Key) =
                if cmp.Compare(x.Key, key) = 0 then
                    Some x.Value
                else
                    None
                    
            override x.TryFindV(cmp : IComparer<'Key>, key : 'Key) =
                if cmp.Compare(x.Key, key) = 0 then
                    ValueSome x.Value
                else
                    ValueNone

            override x.ContainsKey(cmp : IComparer<'Key>, key : 'Key) =
                cmp.Compare(x.Key, key) = 0
                    
            override x.TryRemoveHeadV() =
                ValueSome(struct(x.Key, x.Value, MapEmpty<'Key, 'Value>.Instance))

            override x.TryRemoveTailV() =
                ValueSome(struct(MapEmpty<'Key, 'Value>.Instance, x.Key, x.Value))
                
            override x.UnsafeRemoveHeadV() =
                struct(x.Key, x.Value, MapEmpty<'Key, 'Value>.Instance)

            override x.UnsafeRemoveTailV() =
                struct(MapEmpty<'Key, 'Value>.Instance, x.Key, x.Value)

            new(k : 'Key, v : 'Value) = { Key = k; Value = v}
        end

    and MapNode<'Key, 'Value> =
        class 
            inherit Node<'Key, 'Value>

            val mutable public Left : Node<'Key, 'Value>
            val mutable public Right : Node<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value
            val mutable public _Count : int
            val mutable public _Height : int

            static member Create(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                let b = r.Height - l.Height

                if b >= 2 then
                    let r = r :?> MapNode<'Key, 'Value> // must work
                    
                    if r.Right.Height > r.Left.Height then
                        MapNode.Create(
                            MapNode.Create(l, k, v, r.Left),
                            r.Key, r.Value,
                            r.Right
                        )
                    else
                        let rl = r.Left :?> MapNode<'Key, 'Value>

                        let t1 = l
                        let t2 = rl.Left
                        let t3 = rl.Right
                        let t4 = r.Right

                        MapNode.Create(
                            MapNode.Create(t1, k, v, t2),
                            rl.Key, rl.Value,
                            MapNode.Create(t3, r.Key, r.Value, t4)
                        )

                elif b <= -2 then   
                    let l = l :?> MapNode<'Key, 'Value> // must work
                    
                    if l.Right.Height > l.Left.Height then
                        let lr = l.Right :?> MapNode<'Key, 'Value>

                        let t1 = l.Left
                        let t2 = lr.Left
                        let t3 = lr.Right
                        let t4 = r

                        MapNode.Create(
                            MapNode.Create(t1, l.Key, l.Value, t2),
                            lr.Key, lr.Value,
                            MapNode.Create(t3, k, v, t4)
                        )
                    else
                        MapNode.Create(
                            l.Left,
                            l.Key, l.Value,
                            MapNode.Create(l.Right, k, v, r)
                        )
                else
                    MapNode(l, k, v, r) :> Node<_,_>

            static member CreateUnsafe(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                let b = r.Height - l.Height

                if b >= 2 then
                    let r = r :?> MapNode<'Key, 'Value> // must work
                    
                    if r.Right.Height >= r.Left.Height then
                        MapNode(
                            MapNode(l, k, v, r.Left),
                            r.Key, r.Value,
                            r.Right
                        ) :> Node<_,_>
                    else
                        let rl = r.Left :?> MapNode<'Key, 'Value>
                        let t1 = l
                        let t2 = rl.Left
                        let t3 = rl.Right
                        let t4 = r.Right

                        MapNode(
                            MapNode(t1, k, v, t2),
                            rl.Key, rl.Value,
                            MapNode(t3, r.Key, r.Value, t4)
                        ) :> Node<_,_>

                elif b <= -2 then   
                    let l = l :?> MapNode<'Key, 'Value> // must work
                    
                    if l.Left.Height >= l.Right.Height then
                        MapNode(
                            l.Left,
                            l.Key, l.Value,
                            MapNode(l.Right, k, v, r)
                        ) :> Node<_,_>

                    else
                        let lr = l.Right :?> MapNode<'Key, 'Value>
                        let t1 = l.Left
                        let t2 = lr.Left
                        let t3 = lr.Right
                        let t4 = r
                        MapNode(
                            MapNode(t1, l.Key, l.Value, t2),
                            lr.Key, lr.Value,
                            MapNode(t3, k, v, t4)
                        ) :> Node<_,_>
                else
                    MapNode(l, k, v, r) :> Node<_,_>

            static member Join(l : Node<'Key, 'Value>, r : Node<'Key, 'Value>) =
                if l.Height = 0 then r
                elif r.Height = 0 then l
                elif l.Height > r.Height then
                    let struct(l1, k, v) = l.UnsafeRemoveTailV()
                    MapNode.Create(l1, k, v, r)
                else
                    let struct(k, v, r1) = r.UnsafeRemoveHeadV()
                    MapNode.Create(l, k, v, r1)

            override x.Count =
                x._Count

            override x.Height =
                x._Height
            
            override x.Add(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    MapNode.CreateUnsafe(
                        x.Left, 
                        x.Key, x.Value,
                        x.Right.Add(comparer, key, value)
                    )
                elif c < 0 then
                    MapNode.CreateUnsafe(
                        x.Left.Add(comparer, key, value), 
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    MapNode(
                        x.Left, 
                        key, value,
                        x.Right
                    ) :> Node<_,_>
                    
            override x.AddInPlace(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    x.Right <- x.Right.AddInPlace(comparer, key, value)

                    let bal = abs (x.Right.Height - x.Left.Height)
                    if bal < 2 then 
                        x._Height <- max x._Height (1 + x.Right.Height)
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> Node<_,_>
                    else 
                        MapNode.CreateUnsafe(
                            x.Left, 
                            x.Key, x.Value,
                            x.Right
                        )
                elif c < 0 then
                    x.Left <- x.Left.AddInPlace(comparer, key, value)
                    
                    let bal = abs (x.Right.Height - x.Left.Height)
                    if bal < 2 then 
                        x._Height <- max x._Height (1 + x.Right.Height)
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> Node<_,_>
                    else
                        MapNode.CreateUnsafe(
                            x.Left, 
                            x.Key, x.Value,
                            x.Right
                        )
                else
                    x.Key <- key
                    x.Value <- value
                    x :> Node<_,_>

            override x.Remove(comparer : IComparer<'Key>, key : 'Key) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    MapNode.CreateUnsafe(
                        x.Left, 
                        x.Key, x.Value,
                        x.Right.Remove(comparer, key)
                    )
                elif c < 0 then
                    MapNode.CreateUnsafe(
                        x.Left.Remove(comparer, key), 
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    MapNode.Join(x.Left, x.Right)

            override x.Iter(action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit>) =
                x.Left.Iter(action)
                action.Invoke(x.Key, x.Value)
                x.Right.Iter(action)
                
            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapNode(
                    x.Left.Map(mapping),
                    x.Key, mapping.Invoke(x.Key, x.Value),
                    x.Right.Map(mapping)
                ) :> Node<_,_>
                
            override x.Filter(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                let l = x.Left.Filter(predicate)
                let self = predicate.Invoke(x.Key, x.Value)
                let r = x.Right.Filter(predicate)

                if self then
                    MapNode.Create(l, x.Key, x.Value, r)
                else
                    MapNode.Join(l, r)

            override x.Choose(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) =
                let l = x.Left.Choose(mapping)
                let self = mapping.Invoke(x.Key, x.Value)
                let r = x.Right.Choose(mapping)
                match self with
                | Some value ->
                    MapNode.Create(l, x.Key, value, r)
                | None ->
                    MapNode.Join(l, r)
                    

            override x.ChooseV(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) =
                let l = x.Left.ChooseV(mapping)
                let self = mapping.Invoke(x.Key, x.Value)
                let r = x.Right.ChooseV(mapping)
                match self with
                | ValueSome value ->
                    MapNode.Create(l, x.Key, value, r)
                | ValueNone ->
                    MapNode.Join(l, r)
                    
            override x.Exists(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                x.Left.Exists(predicate) ||
                predicate.Invoke(x.Key, x.Value) ||
                x.Right.Exists(predicate)

            override x.Forall(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                x.Left.Forall(predicate) &&
                predicate.Invoke(x.Key, x.Value) &&
                x.Right.Forall(predicate)

            override x.Fold(folder, seed) = 
                let s1 = x.Left.Fold(folder, seed)
                let s2 = folder.Invoke(s1, x.Key, x.Value)
                x.Right.Fold(folder, s2)

            override x.FoldBack(folder, seed) = 
                let s1 = x.Right.FoldBack(folder, seed)
                let s2 = folder.Invoke(x.Key, x.Value, s1)
                x.Left.FoldBack(folder, s2)

            override x.ToList(acc) =
                x.Left.ToList ((x.Key, x.Value) :: x.Right.ToList acc)
                
            override x.ToListV(acc) =
                x.Left.ToListV (struct(x.Key, x.Value) :: x.Right.ToListV acc)
            
            override x.CopyTo(dst, index) =
                let i1 = x.Left.CopyTo(dst, index)
                dst.[i1] <- (x.Key, x.Value)
                x.Right.CopyTo(dst, i1 + 1)
                
            override x.CopyToV(dst, index) =
                let i1 = x.Left.CopyToV(dst, index)
                dst.[i1] <- struct(x.Key, x.Value)
                x.Right.CopyToV(dst, i1 + 1)
                
            override x.CopyToKeyValue(dst, index) =
                let i1 = x.Left.CopyToKeyValue(dst, index)
                dst.[i1] <- KeyValuePair(x.Key, x.Value)
                x.Right.CopyToKeyValue(dst, i1 + 1)

            override x.TryFind(cmp : IComparer<'Key>, key : 'Key) =
                let c = cmp.Compare(key, x.Key)
                if c > 0 then x.Right.TryFind(cmp, key)
                elif c < 0 then x.Left.TryFind(cmp, key)
                else Some x.Value

            override x.TryFindV(cmp : IComparer<'Key>, key : 'Key) =
                let c = cmp.Compare(key, x.Key)
                if c > 0 then x.Right.TryFindV(cmp, key)
                elif c < 0 then x.Left.TryFindV(cmp, key)
                else ValueSome x.Value
                
            override x.ContainsKey(cmp : IComparer<'Key>, key : 'Key) =
                let c = cmp.Compare(key, x.Key)
                if c > 0 then x.Right.ContainsKey(cmp, key)
                elif c < 0 then x.Left.ContainsKey(cmp, key)
                else true
                
            override x.TryRemoveHeadV() =
                match x.Left.TryRemoveHeadV() with
                | ValueSome struct(k, v, l1) ->
                    ValueSome (struct(k, v, MapNode.CreateUnsafe(l1, x.Key, x.Value, x.Right)))
                | ValueNone ->
                    ValueSome (struct(x.Key, x.Value, x.Right))

            override x.TryRemoveTailV() =   
                match x.Right.TryRemoveTailV() with
                | ValueSome struct(r1, k, v) ->
                    ValueSome struct(MapNode.CreateUnsafe(x.Left, x.Key, x.Value, r1), k, v)
                | ValueNone ->
                    ValueSome struct(x.Left, x.Key, x.Value)
                    
            override x.UnsafeRemoveHeadV() =
                if x.Left.Height = 0 then
                    struct(x.Key, x.Value, x.Right)
                else
                    let struct(k,v,l1) = x.Left.UnsafeRemoveHeadV()
                    struct(k, v, MapNode.CreateUnsafe(l1, x.Key, x.Value, x.Right))

            override x.UnsafeRemoveTailV() =   
                if x.Right.Height = 0 then
                    struct(x.Left, x.Key, x.Value)
                else
                    let struct(r1,k,v) = x.Right.UnsafeRemoveTailV()
                    struct(MapNode.CreateUnsafe(x.Left, x.Key, x.Value, r1), k, v)


            new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                assert(l.Height > 0 || r.Height > 0)    // not both empty
                assert(abs (r.Height - l.Height) < 2)   // balanced
                {
                    Left = l
                    Right = r
                    Key = k
                    Value = v
                    _Count = 1 + l.Count + r.Count
                    _Height = 1 + max l.Height r.Height
                }
        end

open MapNewImplementation

type MapNew<'Key, 'Value when 'Key : comparison> private(comparer : IComparer<'Key>, root : Node<'Key, 'Value>) =
        
    static let defaultComparer = LanguagePrimitives.FastGenericComparer<'Key>
    static let empty = MapNew<'Key, 'Value>(defaultComparer, MapEmpty.Instance)

    static member Empty = empty
      
    static member FromSeq (elements : seq<'Key * 'Value>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)

    static member FromList (elements : list<'Key * 'Value>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)
            
    static member FromArray (elements : array<'Key * 'Value>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)
        
    static member FromSeqV (elements : seq<struct('Key * 'Value)>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)

    static member FromListV (elements : list<struct('Key * 'Value)>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)
            
    static member FromArrayV (elements : array<struct('Key * 'Value)>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)

    member x.Count = root.Count

    member x.Add(key : 'Key, value : 'Value) =
        MapNew(comparer, root.Add(comparer, key, value))
            
    member x.Remove(key : 'Key) =
        MapNew(comparer, root.Remove(comparer, key))

    member x.Iter(action : 'Key -> 'Value -> unit) =
        let action = OptimizedClosures.FSharpFunc<_,_,_>.Adapt action
        root.Iter action

    member x.Map(mapping : 'Key -> 'Value -> 'T) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        MapNew(comparer, root.Map(mapping))
        
    member x.Filter(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        MapNew(comparer, root.Filter(predicate))

    member x.Choose(mapping : 'Key -> 'Value -> option<'T>) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        MapNew(comparer, root.Choose(mapping))

    member x.ChooseV(mapping : 'Key -> 'Value -> voption<'T>) =
        let mapping = OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping
        MapNew(comparer, root.ChooseV(mapping))

    member x.Exists(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        root.Exists predicate
        
    member x.Forall(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        root.Forall predicate

    member x.Fold(folder : 'State -> 'Key -> 'Value -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder
        root.Fold(folder, seed)
        
    member x.FoldBack(folder : 'Key -> 'Value -> 'State -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder
        root.FoldBack(folder, seed)

    member x.TryFind(key : 'Key) =
        root.TryFind(comparer, key)

    member x.TryFindV(key : 'Key) =
        root.TryFindV(comparer, key)
            
    member x.ContainsKey(key : 'Key) =
        root.ContainsKey(comparer, key)

    member x.GetEnumerator() = new MapNewEnumerator<_,_>(root)

    member x.ToList() = root.ToList []
    member x.ToListV() = root.ToListV []

    member x.ToArray() =
        let arr = Array.zeroCreate x.Count
        root.CopyTo(arr, 0) |> ignore
        arr

    member x.ToArrayV() =
        let arr = Array.zeroCreate x.Count
        root.CopyToV(arr, 0) |> ignore
        arr

    member x.CopyTo(array : ('Key * 'Value)[], startIndex : int) =
        if startIndex < 0 || startIndex + x.Count >= array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
        root.CopyTo(array, startIndex) |> ignore

    member x.CopyToV(array : struct('Key * 'Value)[], startIndex : int) =
        if startIndex < 0 || startIndex + x.Count >= array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
        root.CopyToV(array, startIndex) |> ignore

    interface System.Collections.IEnumerable with
        member x.GetEnumerator() = new MapNewEnumerator<_,_>(root) :> _

    interface System.Collections.Generic.IEnumerable<KeyValuePair<'Key, 'Value>> with
        member x.GetEnumerator() = new MapNewEnumerator<_,_>(root) :> _
        
    interface System.Collections.Generic.ICollection<KeyValuePair<'Key, 'Value>> with
        member x.Count = x.Count
        member x.IsReadOnly = true
        member x.Clear() = failwith "readonly"
        member x.Add(_) = failwith "readonly"
        member x.Remove(_) = failwith "readonly"
        member x.Contains(kvp : KeyValuePair<'Key, 'Value>) =
            match x.TryFindV kvp.Key with
            | ValueSome v -> Unchecked.equals v kvp.Value
            | ValueNone -> false
        member x.CopyTo(array : KeyValuePair<'Key, 'Value>[], arrayIndex : int) =
            root.CopyToKeyValue(array, arrayIndex) |> ignore
            
    interface System.Collections.Generic.IDictionary<'Key, 'Value> with
        member x.TryGetValue(key : 'Key,  value : byref<'Value>) =
            match x.TryFindV key with
            | ValueSome v ->
                value <- v
                true
            | ValueNone ->
                false

        member x.Add(_,_) =
            failwith "readonly"

        member x.Remove(_) =
            failwith "readonly"

        member x.Keys =
            failwith "implement me"
            
        member x.Values =
            failwith "implement me"

        member x.ContainsKey key =
            x.ContainsKey key

        member x.Item
            with get (key : 'Key) = x.TryFindV key |> ValueOption.get
            and set _ _ = failwith "readonly"

    new(comparer : IComparer<'Key>) = 
        MapNew<'Key, 'Value>(comparer, MapEmpty.Instance)

and MapNewEnumerator<'Key, 'Value> =
    struct
        val mutable public Root : Node<'Key, 'Value>
        val mutable public Stack : list<struct(Node<'Key, 'Value> * bool)>
        val mutable public Value : KeyValuePair<'Key, 'Value>

        member inline x.Current = x.Value

        member inline x.Reset() =
            if x.Root.Height > 0 then
                x.Stack <- [struct(x.Root, true)]
                x.Value <- Unchecked.defaultof<_>

        member inline x.Dispose() =
            x.Root <- MapEmpty.Instance
            x.Stack <- []
            x.Value <- Unchecked.defaultof<_>
                
        member inline private x.MoveNext(top : Node<'Key, 'Value>) =
            let mutable top = top
            let mutable run = true

            while run do
                match top with
                | :? MapLeaf<'Key, 'Value> as n ->
                    x.Value <- KeyValuePair(n.Key, n.Value)
                    run <- false

                | :? MapNode<'Key, 'Value> as n ->
                    if n.Left.Height = 0 then
                        if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                        x.Value <- KeyValuePair(n.Key, n.Value)
                        run <- false
                    else
                        if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                        x.Stack <- struct(n :> Node<_,_>, false) :: x.Stack
                        top <- n.Left
                | _ ->
                    failwith "empty node"
    
            
        member inline x.MoveNext() =
            match x.Stack with
            | struct(n, deep) :: rest ->
                x.Stack <- rest
                match n with
                | :? MapLeaf<'Key, 'Value> as n ->
                    x.Value <- KeyValuePair(n.Key, n.Value)
                    true

                | :? MapNode<'Key, 'Value> as n ->
                    if deep then
                        x.MoveNext n
                        true
                    else
                        x.Value <- KeyValuePair(n.Key, n.Value)
                        true
                | _ ->
                    false
            | [] ->
                false
                            
            
        interface System.Collections.IEnumerator with
            member x.MoveNext() = x.MoveNext()
            member x.Reset() = x.Reset()
            member x.Current = x.Current :> obj

        interface System.Collections.Generic.IEnumerator<KeyValuePair<'Key, 'Value>> with
            member x.Dispose() = x.Dispose()
            member x.Current = x.Current



        new(r : Node<'Key, 'Value>) =
            if r.Height = 0 then
                { 
                    Root = r
                    Stack = []
                    Value = Unchecked.defaultof<_>
                }
            else       
                { 
                    Root = r
                    Stack = [struct(r, true)]
                    Value = Unchecked.defaultof<_>
                }

    end



[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module MapNew =

    [<GeneralizableValue; CompiledName("Empty")>]
    let inline empty<'Key, 'Value when 'Key : comparison> = MapNew<'Key, 'Value>.Empty
    
    [<CompiledName("IsEmpty")>]
    let inline isEmpty (map : MapNew<'Key, 'Value>) = map.Count <= 0
    
    [<CompiledName("Count")>]
    let inline count (map : MapNew<'Key, 'Value>) = map.Count
    
    [<CompiledName("Add")>]
    let inline add (key : 'Key) (value : 'Value) (map : MapNew<'Key, 'Value>) = map.Add(key, value)
    
    [<CompiledName("Remove")>]
    let inline remove (key : 'Key) (map : MapNew<'Key, 'Value>) = map.Remove(key)
    
    [<CompiledName("TryFind")>]
    let inline tryFind (key : 'Key) (map : MapNew<'Key, 'Value>) = map.TryFind(key)
    
    [<CompiledName("TryFindValue")>]
    let inline tryFindV (key : 'Key) (map : MapNew<'Key, 'Value>) = map.TryFindV(key)
    
    [<CompiledName("ContainsKey")>]
    let inline containsKey (key : 'Key) (map : MapNew<'Key, 'Value>) = map.ContainsKey(key)
    
    [<CompiledName("Iter")>]
    let inline iter (action : 'Key -> 'Value -> unit) (map : MapNew<'Key, 'Value>) = map.Iter(action)
    
    [<CompiledName("Map")>]
    let inline map (mapping : 'Key -> 'Value -> 'T) (map : MapNew<'Key, 'Value>) = map.Map(mapping)
    
    [<CompiledName("Choose")>]
    let inline choose (mapping : 'Key -> 'Value -> option<'T>) (map : MapNew<'Key, 'Value>) = map.Choose(mapping)
    
    [<CompiledName("ChooseValue")>]
    let inline chooseV (mapping : 'Key -> 'Value -> voption<'T>) (map : MapNew<'Key, 'Value>) = map.ChooseV(mapping)

    [<CompiledName("Filter")>]
    let inline filter (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) = map.Filter(predicate)

    [<CompiledName("Exists")>]
    let inline exists (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) = map.Exists(predicate)
    
    [<CompiledName("Forall")>]
    let inline forall (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) = map.Forall(predicate)

    [<CompiledName("Fold")>]
    let inline fold (folder : 'State -> 'Key -> 'Value -> 'State) (seed : 'State) (map : MapNew<'Key, 'Value>) = 
        map.Fold(folder, seed)
    
    [<CompiledName("FoldBack")>]
    let inline foldBack (folder : 'Key -> 'Value -> 'State -> 'State) (map : MapNew<'Key, 'Value>) (seed : 'State) = 
        map.FoldBack(folder, seed)


    [<CompiledName("OfSeq")>]
    let inline ofSeq (values : seq<'Key * 'Value>) = MapNew.FromSeq values
    
    [<CompiledName("OfList")>]
    let inline ofList (values : list<'Key * 'Value>) = MapNew.FromList values
    
    [<CompiledName("OfArray")>]
    let inline ofArray (values : ('Key * 'Value)[]) = MapNew.FromArray values
    
    [<CompiledName("OfSeqValue")>]
    let inline ofSeqV (values : seq<struct('Key * 'Value)>) = MapNew.FromSeqV values
    
    [<CompiledName("OfListValue")>]
    let inline ofListV (values : list<struct('Key * 'Value)>) = MapNew.FromListV values
    
    [<CompiledName("OfArrayValue")>]
    let inline ofArrayV (values : struct('Key * 'Value)[]) = MapNew.FromArrayV values

    

    [<CompiledName("ToSeq")>]
    let inline toSeq (map : MapNew<'Key, 'Value>) = map |> Seq.map (fun (KeyValue(k,v)) -> k, v)

    [<CompiledName("ToSeqValue")>]
    let inline toSeqV (map : MapNew<'Key, 'Value>) = map |> Seq.map (fun (KeyValue(k,v)) -> struct (k, v))

    [<CompiledName("ToList")>]
    let inline toList (map : MapNew<'Key, 'Value>) = map.ToList()
    
    [<CompiledName("ToListValue")>]
    let inline toListV (map : MapNew<'Key, 'Value>) = map.ToListV()
    
    [<CompiledName("ToArray")>]
    let inline toArray (map : MapNew<'Key, 'Value>) = map.ToArray()
    
    [<CompiledName("ToArrayValue")>]
    let inline toArrayV (map : MapNew<'Key, 'Value>) = map.ToArrayV()




