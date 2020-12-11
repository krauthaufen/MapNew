namespace MapNew

open System
open System.Collections.Generic

module MapNewImplementation = 

    module Seq =
        let mapToArray (mapping : 'a -> 'b) (s : seq<'a>) =
            use e = s.GetEnumerator()
            if e.MoveNext() then
                let v0 = mapping e.Current
                if e.MoveNext() then
                    let mutable res = Array.zeroCreate 16
                    let mutable cnt = 2
                    res.[0] <- v0
                    res.[1] <- mapping e.Current

                    while e.MoveNext() do
                        if cnt >= res.Length then System.Array.Resize(&res, res.Length <<< 1)
                        res.[cnt] <- mapping e.Current
                        cnt <- cnt + 1

                    if cnt < res.Length then System.Array.Resize(&res, cnt)
                    res
                else    
                    [| v0 |]
            else
                [||]

    module List =
        let mapToArray (mapping : 'a -> 'b) (l : list<'a>) =
            match l with
            | [] -> [||]
            | [a] -> [| mapping a |]
            | h::l ->
                let mutable arr = Array.zeroCreate 16
                arr.[0] <- mapping h
                let mutable cnt = 1
                for v in l do
                    if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                    arr.[cnt] <- mapping v
                    cnt <- cnt + 1

                if cnt < arr.Length then System.Array.Resize(&arr, cnt)
                arr

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

        abstract member GetViewBetween : comparer : IComparer<'Key> * min : 'Key * minInclusive : bool * max : 'Key * maxInclusive : bool -> Node<'Key, 'Value>
        abstract member WithMin : comparer : IComparer<'Key> * min : 'Key * minInclusive : bool -> Node<'Key, 'Value>
        abstract member WithMax : comparer : IComparer<'Key> * max : 'Key * maxInclusive : bool -> Node<'Key, 'Value>

        abstract member TryMinKeyValue : unit -> option<'Key * 'Value>
        abstract member TryMaxKeyValue : unit -> option<'Key * 'Value>
        abstract member TryMinKeyValueV : unit -> voption<struct('Key * 'Value)>
        abstract member TryMaxKeyValueV : unit -> voption<struct('Key * 'Value)>
        
        abstract member TryAt : index : int -> option<'Key * 'Value>
        abstract member TryAtV : index : int -> voption<struct('Key * 'Value)>

        abstract member Change : comparer : IComparer<'Key> * key : 'Key * (option<'Value> -> option<'Value>) -> Node<'Key, 'Value>
        abstract member ChangeV : comparer : IComparer<'Key> * key : 'Key * (voption<'Value> -> voption<'Value>) -> Node<'Key, 'Value>
        
    [<Sealed>]
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

        
        override x.GetViewBetween(_comparer : IComparer<'Key>, _min : 'Key, _minInclusive : bool, _max : 'Key, _maxInclusive : bool) =
            x :> Node<_,_>
        override x.WithMin(_comparer : IComparer<'Key>, _min : 'Key, _minInclusive : bool) =
            x :> Node<_,_>
        override x.WithMax(_comparer : IComparer<'Key>, _max : 'Key, _maxInclusive : bool) =
            x :> Node<_,_>

        override x.TryMinKeyValue() = None
        override x.TryMaxKeyValue() = None
        override x.TryMinKeyValueV() = ValueNone
        override x.TryMaxKeyValueV() = ValueNone

        override x.Change(comparer, key, update) =
            match update None with
            | None -> x :> Node<_,_>
            | Some v -> MapLeaf(key, v) :> Node<_,_>
            
        override x.ChangeV(comparer, key, update) =
            match update ValueNone with
            | ValueNone -> x :> Node<_,_>
            | ValueSome v -> MapLeaf(key, v) :> Node<_,_>

        override x.TryAt(_index) = None
        override x.TryAtV(_index) = ValueNone

    and 
        [<Sealed>]
        MapLeaf<'Key, 'Value> =
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
                    #if TWO
                    MapTwo(x.Key, x.Value, key, value) :> Node<'Key,'Value>
                    #else
                    MapInner(x, key, value, MapEmpty.Instance) :> Node<'Key,'Value>
                    #endif
                elif c < 0 then
                    #if TWO
                    MapTwo(key, value, x.Key, x.Value) :> Node<'Key,'Value>
                    #else
                    MapInner(MapEmpty.Instance, key, value, x) :> Node<'Key,'Value>
                    #endif
                else
                    MapLeaf(key, value) :> Node<'Key,'Value>
                    
            override x.AddInPlace(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then   
                    #if TWO
                    MapTwo(x.Key, x.Value, key, value) :> Node<'Key,'Value>
                    #else
                    MapInner(x, key, value, MapEmpty.Instance) :> Node<'Key,'Value>
                    #endif
                elif c < 0 then
                    #if TWO
                    MapTwo(key, value, x.Key, x.Value) :> Node<'Key,'Value>
                    #else
                    MapInner(MapEmpty.Instance, key, value, x) :> Node<'Key,'Value>
                    #endif
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

            
            override x.GetViewBetween(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool, max : 'Key, maxInclusive : bool) =
                let cMin = comparer.Compare(x.Key, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    let cMax = comparer.Compare(x.Key, max)
                    if (if maxInclusive then cMax <= 0 else cMax < 0) then
                        x :> Node<_,_>
                    else
                        MapEmpty.Instance
                else
                    MapEmpty.Instance
                    
            override x.WithMin(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool) =
                let cMin = comparer.Compare(x.Key, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    x :> Node<_,_>
                else
                    MapEmpty.Instance
                    
            override x.WithMax(comparer : IComparer<'Key>, max : 'Key, maxInclusive : bool) =
                let cMax = comparer.Compare(x.Key, max)
                if (if maxInclusive then cMax <= 0 else cMax < 0) then
                    x :> Node<_,_>
                else
                    MapEmpty.Instance

            
            override x.TryMinKeyValue() = Some(x.Key, x.Value)
            override x.TryMaxKeyValue() = Some(x.Key, x.Value)
            override x.TryMinKeyValueV() = ValueSome struct(x.Key, x.Value)
            override x.TryMaxKeyValueV() = ValueSome struct(x.Key, x.Value)

            
            override x.Change(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match update None with
                    | None -> x :> Node<_,_>
                    | Some v ->
                        #if TWO
                        MapTwo(x.Key, x.Value, key, v) :> Node<_,_>
                        #else
                        MapInner(x, key, v, MapEmpty.Instance) :> Node<_,_>
                        #endif
                elif c < 0 then
                    match update None with
                    | None -> x :> Node<_,_>
                    | Some v ->
                        #if TWO
                        MapTwo(key, v, x.Key, x.Value) :> Node<_,_>
                        #else
                        MapInner(MapEmpty.Instance, key, v, x) :> Node<_,_>
                        #endif
                else    
                    match update (Some x.Value) with
                    | Some v ->
                        MapLeaf(key, v) :> Node<_,_>
                    | None ->
                        MapEmpty.Instance

            override x.ChangeV(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match update ValueNone with
                    | ValueNone -> x :> Node<_,_>
                    | ValueSome v ->
                        #if TWO
                        MapTwo(x.Key, x.Value, key, v) :> Node<_,_>
                        #else
                        MapInner(x, key, v, MapEmpty.Instance) :> Node<_,_>
                        #endif
                elif c < 0 then
                    match update ValueNone with
                    | ValueNone -> x :> Node<_,_>
                    | ValueSome v ->
                        #if TWO
                        MapTwo(key, v, x.Key, x.Value) :> Node<_,_>
                        #else
                        MapInner(MapEmpty.Instance, key, v, x) :> Node<_,_>
                        #endif
                else    
                    match update (ValueSome x.Value) with
                    | ValueSome v ->
                        MapLeaf(key, v) :> Node<_,_>
                    | ValueNone ->
                        MapEmpty.Instance

            override x.TryAt(index) =
                if index = 0 then Some (x.Key, x.Value)
                else None

            override x.TryAtV(index) =
                if index = 0 then ValueSome struct(x.Key, x.Value)
                else ValueNone

            new(k : 'Key, v : 'Value) = { Key = k; Value = v}
        end

    #if TWO
    and 
        [<Sealed>]
        MapTwo<'Key, 'Value> =
            inherit Node<'Key, 'Value>
            val mutable public K0 : 'Key
            val mutable public V0 : 'Value
            val mutable public K1 : 'Key
            val mutable public V1 : 'Value

            override x.Height =
                1

            override x.Count =
                2

            override x.Add(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c0 = comparer.Compare(key, x.K0)
                let c1 = comparer.Compare(key, x.K1)

                if c0 > 0 && c1 > 0 then
                    MapInner(MapLeaf(x.K0, x.V0), x.K1, x.V1, MapLeaf(key, value)) :> Node<_,_>
                elif c0 > 0 && c1 < 0 then
                    MapInner(MapLeaf(x.K0, x.V0), key, value, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 < 0 && c1 < 0 then
                    MapInner(MapLeaf(key, value), x.K0, x.V0, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 = 0 then
                    MapTwo(key, value, x.K1, x.V1) :> Node<_,_>
                elif c1 = 0 then
                    MapTwo(x.K0, x.V0, key, value) :> Node<_,_>
                else
                    failwith "inconsistent"
                    
            override x.Remove(comparer : IComparer<'Key>, key : 'Key) =
                let c0 = comparer.Compare(key, x.K0)
                let c1 = comparer.Compare(key, x.K1)

                if c0 = 0 then MapLeaf(x.K1, x.V1) :> Node<_,_>
                elif c1 = 0 then MapLeaf(x.K0, x.V0) :> Node<_,_>
                else x :> Node<_,_>
               
            override x.AddInPlace(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c0 = comparer.Compare(key, x.K0)
                let c1 = comparer.Compare(key, x.K1)

                if c0 > 0 && c1 > 0 then
                    MapInner(MapLeaf(x.K0, x.V0), x.K1, x.V1, MapLeaf(key, value)) :> Node<_,_>
                elif c0 > 0 && c1 < 0 then
                    MapInner(MapLeaf(x.K0, x.V0), key, value, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 < 0 && c1 < 0 then
                    MapInner(MapLeaf(key, value), x.K0, x.V0, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 = 0 then
                    x.K1 <- key
                    x.V1 <- value
                    x :> Node<_,_>
                elif c1 = 0 then
                    x.K0 <- key
                    x.V0 <- value
                    x :> Node<_,_>
                else
                    failwith "inconsistent"

            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapTwo(x.K0, mapping.Invoke(x.K0, x.V0), x.K1, mapping.Invoke(x.K1, x.V1)) :> Node<_,_>
                
            override x.Choose(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) =
                match mapping.Invoke(x.K0, x.V0) with
                | Some v0 ->
                    match mapping.Invoke(x.K1, x.V1) with
                    | Some v1 -> 
                        MapTwo(x.K0, v0, x.K1, v1) :> Node<_,_>
                    | None ->
                        MapLeaf(x.K0, v0) :> Node<_,_>
                | None ->
                    match mapping.Invoke(x.K1, x.V1) with
                    | Some v1 -> 
                        MapLeaf(x.K1, v1) :> Node<_,_>
                    | None ->
                        MapEmpty.Instance
                        
            override x.ChooseV(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) =
                match mapping.Invoke(x.K0, x.V0) with
                | ValueSome v0 ->
                    match mapping.Invoke(x.K1, x.V1) with
                    | ValueSome v1 -> 
                        MapTwo(x.K0, v0, x.K1, v1) :> Node<_,_>
                    | ValueNone ->
                        MapLeaf(x.K0, v0) :> Node<_,_>
                | ValueNone ->
                    match mapping.Invoke(x.K1, x.V1) with
                    | ValueSome v1 -> 
                        MapLeaf(x.K1, v1) :> Node<_,_>
                    | ValueNone ->
                        MapEmpty.Instance

            override x.Filter(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                if predicate.Invoke(x.K0, x.V0) then
                    if predicate.Invoke(x.K1, x.V1) then
                        MapTwo(x.K0, x.V0, x.K1, x.V1) :> Node<_,_>
                    else
                        MapLeaf(x.K0, x.V0) :> Node<_,_>
                else
                    if predicate.Invoke(x.K1, x.V1) then
                        MapLeaf(x.K1, x.V1) :> Node<_,_>
                    else
                        MapEmpty.Instance
                 
            override x.ContainsKey(comparer : IComparer<'Key>, key : 'Key) =
                let c0 = comparer.Compare(key, x.K0)
                if c0 > 0 then comparer.Compare(key, x.K1) = 0
                else c0 = 0
                
            override x.TryFind(comparer : IComparer<'Key>, key : 'Key) =
                let c0 = comparer.Compare(key, x.K0)
                if c0 > 0 then 
                    if comparer.Compare(key, x.K1) = 0 then Some x.V1
                    else None
                elif c0 = 0 then
                    Some x.V0
                else
                    None
                    
            override x.TryFindV(comparer : IComparer<'Key>, key : 'Key) =
                let c0 = comparer.Compare(key, x.K0)
                if c0 > 0 then 
                    if comparer.Compare(key, x.K1) = 0 then ValueSome x.V1
                    else ValueNone
                elif c0 = 0 then
                    ValueSome x.V0
                else
                    ValueNone

            override x.ToList(acc) =
                (x.K0, x.V0) :: (x.K1, x.V1) :: acc

            override x.ToListV(acc) =
                struct(x.K0, x.V0) :: struct(x.K1, x.V1) :: acc

            override x.TryRemoveHeadV() =
                ValueSome struct(x.K0, x.V0, MapLeaf(x.K1, x.V1) :> Node<_,_>)

            override x.TryRemoveTailV() =
                ValueSome struct(MapLeaf(x.K0, x.V0) :> Node<_,_>, x.K1, x.V1)
                
            override x.UnsafeRemoveHeadV() =
                struct(x.K0, x.V0, MapLeaf(x.K1, x.V1) :> Node<_,_>)

            override x.UnsafeRemoveTailV() =
                struct(MapLeaf(x.K0, x.V0) :> Node<_,_>, x.K1, x.V1)

            override x.CopyTo(dst : ('Key * 'Value)[], index : int) =
                dst.[index] <- (x.K0, x.V0)
                dst.[index + 1] <- (x.K1, x.V1)
                index + 2

            override x.CopyToV(dst : struct('Key * 'Value)[], index : int) =
                dst.[index] <- struct(x.K0, x.V0)
                dst.[index + 1] <- struct(x.K1, x.V1)
                index + 2

            override x.CopyToKeyValue(dst : KeyValuePair<'Key, 'Value>[], index : int) =
                dst.[index] <- KeyValuePair(x.K0, x.V0)
                dst.[index + 1] <- KeyValuePair(x.K1, x.V1)
                index + 2

            override x.Exists(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                predicate.Invoke(x.K0, x.V0) ||
                predicate.Invoke(x.K1, x.V1)
                
            override x.Forall(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                predicate.Invoke(x.K0, x.V0) &&
                predicate.Invoke(x.K1, x.V1)
                
            override x.Fold(folder : OptimizedClosures.FSharpFunc<'State, 'Key, 'Value, 'State>, seed : 'State) =
                folder.Invoke(folder.Invoke(seed, x.K0, x.V0), x.K1, x.V1)
                
            override x.FoldBack(folder : OptimizedClosures.FSharpFunc<'Key, 'Value, 'State, 'State>, seed : 'State) =
                folder.Invoke(x.K0, x.V0, folder.Invoke(x.K1, x.V1, seed))
                
            override x.Iter(action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit>) =
                action.Invoke(x.K0, x.V0)
                action.Invoke(x.K1, x.V1)

            override x.GetViewBetween(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool, max : 'Key, maxInclusive : bool) =
                let l0 = comparer.Compare(min, x.K0)
                let h1 = comparer.Compare(max, x.K1)
                let lower0 = if minInclusive then l0 >= 0 else l0 > 0
                let upper1 = if maxInclusive then h1 <= 0 else h1 < 0

                if lower0 && upper1 then 
                    x :> Node<_,_>
                elif lower0 then
                    let h0 = comparer.Compare(max, x.K0)
                    let upper0 = if maxInclusive then h0 <= 0 else h0 < 0
                    if upper0 then 
                        MapLeaf(x.K0, x.V0) :> Node<_,_>
                    else    
                        MapEmpty.Instance
                elif upper1 then
                    let l1 = comparer.Compare(min, x.K1)
                    let lower1 = if minInclusive then l1 >= 0 else l1 > 0
                    if lower1 then
                        MapLeaf(x.K1, x.V1) :> Node<_,_>
                    else
                        MapEmpty.Instance
                else    
                    MapEmpty.Instance
    
            override x.WithMin(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool) =
                let l0 = comparer.Compare(min, x.K0)
                let lower0 = if minInclusive then l0 <= 0 else l0 < 0
                if lower0 then 
                    x :> Node<_,_>
                else
                    let l1 = comparer.Compare(min, x.K1)
                    let lower1 = if minInclusive then l1 <= 0 else l1 < 0
                    if lower1 then MapLeaf(x.K1, x.V1) :> Node<_,_>
                    else MapEmpty.Instance
                    
            override x.WithMax(comparer : IComparer<'Key>, max : 'Key, maxInclusive : bool) =
                let h1 = comparer.Compare(max, x.K1)
                let upper1 = if maxInclusive then h1 >= 0 else h1 >0
                if upper1 then 
                    x :> Node<_,_>
                else
                    let h0 = comparer.Compare(max, x.K0)
                    let upper0 = if maxInclusive then h0 >= 0 else h0 > 0
                    if upper0 then MapLeaf(x.K0, x.V0) :> Node<_,_>
                    else MapEmpty.Instance
                    
                    
            override x.TryMinKeyValue() = Some(x.K0, x.V0)
            override x.TryMaxKeyValue() = Some(x.K1, x.V1)
            override x.TryMinKeyValueV() = ValueSome struct(x.K0, x.V0)
            override x.TryMaxKeyValueV() = ValueSome struct(x.K1, x.V1)

            override x.TryAt(index : int) =
                if index = 0 then Some(x.K0, x.V0)
                elif index = 1 then Some(x.K1, x.V1)
                else None
                
            override x.TryAtV(index : int) =
                if index = 0 then ValueSome struct(x.K0, x.V0)
                elif index = 1 then ValueSome struct(x.K1, x.V1)
                else ValueNone

            override x.Change(comparer : IComparer<'Key>, key : 'Key, update : option<'Value> -> option<'Value>) =
                let c0 = comparer.Compare(key, x.K0)
                if c0 < 0 then
                    match update None with
                    | None -> x :> Node<_,_>
                    | Some v ->
                        MapInner(MapLeaf(key, v), x.K0, x.V0, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 > 0 then
                    let c1 = comparer.Compare(key, x.K1)
                    if c1 < 0 then
                        match update None with
                        | None -> x :> Node<_,_> 
                        | Some v -> 
                            MapInner(MapLeaf(x.K0, x.V0), key, v, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                    elif c1 > 0 then
                        match update None with
                        | None -> x :> Node<_,_> 
                        | Some v -> 
                            MapInner(MapLeaf(x.K0, x.V0), x.K1, x.V1, MapLeaf(key, v)) :> Node<_,_>
                    else
                        match update (Some x.V1) with
                        | None -> MapLeaf(x.K0, x.V0) :> Node<_,_>
                        | Some v -> MapTwo(x.K0, x.V0, key, v) :> Node<_,_>

                else
                    match update (Some x.V0) with
                    | None -> MapLeaf(x.K1, x.V1) :> Node<_,_>
                    | Some v -> MapTwo(key, v, x.K1, x.V1) :> Node<_,_>
                    

            override x.ChangeV(comparer : IComparer<'Key>, key : 'Key, update : voption<'Value> -> voption<'Value>) =
                let c0 = comparer.Compare(key, x.K0)
                if c0 < 0 then
                    match update ValueNone with
                    | ValueNone -> x :> Node<_,_>
                    | ValueSome v ->
                        MapInner(MapLeaf(key, v), x.K0, x.V0, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                elif c0 > 0 then
                    let c1 = comparer.Compare(key, x.K1)
                    if c1 < 0 then
                        match update ValueNone with
                        | ValueNone -> x :> Node<_,_> 
                        | ValueSome v -> 
                            MapInner(MapLeaf(x.K0, x.V0), key, v, MapLeaf(x.K1, x.V1)) :> Node<_,_>
                    elif c1 > 0 then
                        match update ValueNone with
                        | ValueNone -> x :> Node<_,_> 
                        | ValueSome v -> 
                            MapInner(MapLeaf(x.K0, x.V0), x.K1, x.V1, MapLeaf(key, v)) :> Node<_,_>
                    else
                        match update (ValueSome x.V1) with
                        | ValueNone -> MapLeaf(x.K0, x.V0) :> Node<_,_>
                        | ValueSome v -> MapTwo(x.K0, x.V0, key, v) :> Node<_,_>

                else
                    match update (ValueSome x.V0) with
                    | ValueNone -> MapLeaf(x.K1, x.V1) :> Node<_,_>
                    | ValueSome v -> MapTwo(key, v, x.K1, x.V1) :> Node<_,_>


            new(k0 : 'Key, v0 : 'Value, k1 : 'Key, v1 : 'Value) =
                { K0 = k0; V0 = v0; K1 = k1; V1 = v1 }
    #endif

    and [<Sealed>]
        MapInner<'Key, 'Value> =
        class 
            inherit Node<'Key, 'Value>

            val mutable public Left : Node<'Key, 'Value>
            val mutable public Right : Node<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value
            val mutable public _Count : int
            val mutable public _Height : int

            static member Create(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                let lh = l.Height
                let rh = r.Height
                let b = rh - lh

                let cnt = l.Count + r.Count + 1

                #if TWO
                if cnt <= 2 then
                    match l with
                    | :? MapLeaf<'Key, 'Value> as l ->
                        MapTwo(l.Key, l.Value, k, v) :> Node<_,_>
                    | _ ->
                        match r with
                        | :? MapLeaf<'Key, 'Value> as r ->
                            MapTwo(k, v, r.Key, r.Value) :> Node<_,_>
                        | _ ->
                            MapLeaf(k, v) :> Node<_,_>
                #else
                if lh = 0 && rh = 0 then
                    MapLeaf(k, v) :> Node<_,_>
                #endif
                elif b > 2 then
                    // right heavy
                    let r = r :?> MapInner<'Key, 'Value> // must work
                    
                    if r.Right.Height >= r.Left.Height then
                        // right right case
                        MapInner.Create(
                            MapInner.Create(l, k, v, r.Left),
                            r.Key, r.Value,
                            r.Right
                        ) 
                    else
                        // right left case
                        match r.Left with
                        | :? MapInner<'Key, 'Value> as rl ->
                            //let rl = r.Left :?> MapInner<'Key, 'Value>
                            let t1 = l
                            let t2 = rl.Left
                            let t3 = rl.Right
                            let t4 = r.Right

                            MapInner.Create(
                                MapInner.Create(t1, k, v, t2),
                                rl.Key, rl.Value,
                                MapInner.Create(t3, r.Key, r.Value, t4)
                            )
                        | _ ->
                            failwith "impossible"
                            

                elif b < -2 then   
                    let l = l :?> MapInner<'Key, 'Value> // must work
                    
                    if l.Left.Height >= l.Right.Height then
                        MapInner.Create(
                            l.Left,
                            l.Key, l.Value,
                            MapInner.Create(l.Right, k, v, r)
                        )

                    else
                        match l.Right with
                        | :? MapInner<'Key, 'Value> as lr -> 
                            let t1 = l.Left
                            let t2 = lr.Left
                            let t3 = lr.Right
                            let t4 = r
                            MapInner.Create(
                                MapInner.Create(t1, l.Key, l.Value, t2),
                                lr.Key, lr.Value,
                                MapInner.Create(t3, k, v, t4)
                            )
                        | _ ->
                            failwith "impossible"

                else
                    MapInner(l, k, v, r) :> Node<_,_>

            static member Join(l : Node<'Key, 'Value>, r : Node<'Key, 'Value>) =
                if l.Height = 0 then r
                elif r.Height = 0 then l
                elif l.Height > r.Height then
                    let struct(l1, k, v) = l.UnsafeRemoveTailV()
                    MapInner.Create(l1, k, v, r)
                else
                    let struct(k, v, r1) = r.UnsafeRemoveHeadV()
                    MapInner.Create(l, k, v, r1)

            override x.Count =
                x._Count

            override x.Height =
                x._Height
            
            override x.Add(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    MapInner.Create(
                        x.Left, 
                        x.Key, x.Value,
                        x.Right.Add(comparer, key, value)
                    )
                elif c < 0 then
                    MapInner.Create(
                        x.Left.Add(comparer, key, value), 
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    MapInner(
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
                        MapInner.Create(
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
                        MapInner.Create(
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
                    MapInner.Create(
                        x.Left, 
                        x.Key, x.Value,
                        x.Right.Remove(comparer, key)
                    )
                elif c < 0 then
                    MapInner.Create(
                        x.Left.Remove(comparer, key), 
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    MapInner.Join(x.Left, x.Right)

            override x.Iter(action : OptimizedClosures.FSharpFunc<'Key, 'Value, unit>) =
                x.Left.Iter(action)
                action.Invoke(x.Key, x.Value)
                x.Right.Iter(action)
                
            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapInner(
                    x.Left.Map(mapping),
                    x.Key, mapping.Invoke(x.Key, x.Value),
                    x.Right.Map(mapping)
                ) :> Node<_,_>
                
            override x.Filter(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                let l = x.Left.Filter(predicate)
                let self = predicate.Invoke(x.Key, x.Value)
                let r = x.Right.Filter(predicate)

                if self then
                    MapInner.Create(l, x.Key, x.Value, r)
                else
                    MapInner.Join(l, r)

            override x.Choose(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) =
                let l = x.Left.Choose(mapping)
                let self = mapping.Invoke(x.Key, x.Value)
                let r = x.Right.Choose(mapping)
                match self with
                | Some value ->
                    MapInner.Create(l, x.Key, value, r)
                | None ->
                    MapInner.Join(l, r)
                    

            override x.ChooseV(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) =
                let l = x.Left.ChooseV(mapping)
                let self = mapping.Invoke(x.Key, x.Value)
                let r = x.Right.ChooseV(mapping)
                match self with
                | ValueSome value ->
                    MapInner.Create(l, x.Key, value, r)
                | ValueNone ->
                    MapInner.Join(l, r)
                    
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
                    ValueSome (struct(k, v, MapInner.Create(l1, x.Key, x.Value, x.Right)))
                | ValueNone ->
                    ValueSome (struct(x.Key, x.Value, x.Right))

            override x.TryRemoveTailV() =   
                match x.Right.TryRemoveTailV() with
                | ValueSome struct(r1, k, v) ->
                    ValueSome struct(MapInner.Create(x.Left, x.Key, x.Value, r1), k, v)
                | ValueNone ->
                    ValueSome struct(x.Left, x.Key, x.Value)
                    
            override x.UnsafeRemoveHeadV() =
                if x.Left.Height = 0 then
                    struct(x.Key, x.Value, x.Right)
                else
                    let struct(k,v,l1) = x.Left.UnsafeRemoveHeadV()
                    struct(k, v, MapInner.Create(l1, x.Key, x.Value, x.Right))

            override x.UnsafeRemoveTailV() =   
                if x.Right.Height = 0 then
                    struct(x.Left, x.Key, x.Value)
                else
                    let struct(r1,k,v) = x.Right.UnsafeRemoveTailV()
                    struct(MapInner.Create(x.Left, x.Key, x.Value, r1), k, v)
                    

            override x.WithMin(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool) =
                let c = comparer.Compare(x.Key, min)
                let greaterMin = if minInclusive then c >= 0 else c > 0
                if greaterMin then
                    MapInner.Create(
                        x.Left.WithMin(comparer, min, minInclusive),
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    x.Right.WithMin(comparer, min, minInclusive)

                
            override x.WithMax(comparer : IComparer<'Key>, max : 'Key, maxInclusive : bool) =
                let c = comparer.Compare(x.Key, max)
                let smallerMax = if maxInclusive then c <= 0 else c < 0
                if smallerMax then
                    MapInner.Create(
                        x.Left,
                        x.Key, x.Value,
                        x.Right.WithMax(comparer, max, maxInclusive)
                    )
                else
                    x.Left.WithMax(comparer, max, maxInclusive)
                    


            override x.GetViewBetween(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool, max : 'Key, maxInclusive : bool) =
                let cMin = comparer.Compare(x.Key, min)
                let cMax = comparer.Compare(x.Key, max)

                let greaterMin = if minInclusive then cMin >= 0 else cMin > 0
                let smallerMax = if maxInclusive then cMax <= 0 else cMax < 0

                if not greaterMin then
                    x.Right.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)

                elif not smallerMax then
                    x.Left.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)

                elif greaterMin && smallerMax then
                    let l = x.Left.WithMin(comparer, min, minInclusive)
                    let r = x.Right.WithMax(comparer, max, maxInclusive)
                    MapInner.Create(l, x.Key, x.Value, r)

                elif greaterMin then
                    let l = x.Left.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)
                    let r = x.Right.WithMax(comparer, max, maxInclusive)
                    MapInner.Create(l, x.Key, x.Value, r)

                elif smallerMax then
                    let l = x.Left.WithMin(comparer, min, minInclusive)
                    let r = x.Right.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)
                    MapInner.Create(l, x.Key, x.Value, r)
                    
                else
                    failwith ""
                    
                    
            override x.TryMinKeyValue() = 
                if x.Left.Count = 0 then Some(x.Key, x.Value)
                else x.Left.TryMinKeyValue()

            override x.TryMaxKeyValue() = 
                if x.Right.Count = 0 then Some(x.Key, x.Value)
                else x.Right.TryMaxKeyValue()
                
            override x.TryMinKeyValueV() = 
                if x.Left.Count = 0 then ValueSome(x.Key, x.Value)
                else x.Left.TryMinKeyValueV()

            override x.TryMaxKeyValueV() = 
                if x.Right.Count = 0 then ValueSome(x.Key, x.Value)
                else x.Right.TryMaxKeyValueV()
                
            override x.Change(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then   
                    MapInner.Create(
                        x.Left,
                        x.Key, x.Value,
                        x.Right.Change(comparer, key, update)
                    )
                elif c < 0 then 
                    MapInner.Create(
                        x.Left.Change(comparer, key, update),
                        x.Key, x.Value,
                        x.Right
                    )
                else    
                    match update (Some x.Value) with
                    | Some v ->
                        MapInner(
                            x.Left,
                            key, v,
                            x.Right
                        ) :> Node<_,_>
                    | None ->
                        MapInner.Join(x.Left, x.Right)
                        
            override x.ChangeV(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then   
                    MapInner.Create(
                        x.Left,
                        x.Key, x.Value,
                        x.Right.ChangeV(comparer, key, update)
                    )
                elif c < 0 then 
                    MapInner.Create(
                        x.Left.ChangeV(comparer, key, update),
                        x.Key, x.Value,
                        x.Right
                    )
                else    
                    match update (ValueSome x.Value) with
                    | ValueSome v ->
                        MapInner(
                            x.Left,
                            key, v,
                            x.Right
                        ) :> Node<_,_>
                    | ValueNone ->
                        MapInner.Join(x.Left, x.Right)

                        
            override x.TryAt(index) =
                let lc = index - x.Left.Count
                if lc < 0 then x.Left.TryAt(index)
                elif lc > 0 then x.Right.TryAt(lc - 1)
                else Some (x.Key, x.Value)
                
            override x.TryAtV(index) =
                let lc = index - x.Left.Count
                if lc < 0 then x.Left.TryAtV(index)
                elif lc > 0 then x.Right.TryAtV(lc - 1)
                else ValueSome struct(x.Key, x.Value)


            new(l : Node<'Key, 'Value>, k : 'Key, v : 'Value, r : Node<'Key, 'Value>) =
                assert(l.Height > 0 || r.Height > 0)    // not both empty
                assert(abs (r.Height - l.Height) <= 2)   // balanced
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
open System.Diagnostics

[<DebuggerTypeProxy("Aardvark.Base.MapDebugView`2")>]
[<DebuggerDisplay("Count = {Count}")>]
[<Sealed>]
type MapNew<'Key, 'Value when 'Key : comparison> private(comparer : IComparer<'Key>, root : Node<'Key, 'Value>) =
        
    static let defaultComparer = LanguagePrimitives.FastGenericComparer<'Key>
    static let empty = MapNew<'Key, 'Value>(defaultComparer, MapEmpty.Instance)

    static member Empty = empty
      
    static member private FromSortedArray(cmp : IComparer<'Key>, arr : struct('Key * 'Value)[]) =
        let mutable i = 1
        let mutable o = 1
        let mutable struct(lastKey,_) = arr.[0]
        while i < arr.Length do
            let struct(k,v) = arr.[i]
            if cmp.Compare(lastKey, k) = 0 then
                arr.[o-1] <- struct(k,v)
            else
                arr.[o] <- struct(k,v)
                o <- o + 1
            i <- i + 1

        let rec create (arr : struct('Key * 'Value)[]) (l : int) (r : int) =
            if l = r then
                let struct(k,v) = arr.[l]
                MapLeaf(k, v) :> Node<_,_>
            elif l > r then
                MapEmpty.Instance
            else
                let m = (l+r)/2
                let struct(k,v) = arr.[m]
                MapInner(
                    create arr l (m-1),
                    k, v,
                    create arr (m+1) r
                ) :> Node<_,_>
        MapNew(cmp, create arr 0 (o-1))

    static member FromSeq (elements : seq<'Key * 'Value>) =
        let cmp = defaultComparer
        let arr = elements |> Seq.mapToArray (fun (k,v) -> struct(k,v))
        if arr.Length <= 0 then
            MapNew(cmp, MapEmpty.Instance)
        elif arr.Length = 1 then
            let struct(k,v) = arr.[0]
            MapNew(cmp, MapLeaf(k,v))
        else
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)

    static member FromList (elements : list<'Key * 'Value>) =
        match elements with
        | [] -> MapNew(defaultComparer, MapEmpty.Instance)
        | [(k,v)] -> MapNew(defaultComparer, MapLeaf(k, v))
        | elements ->
            let cmp = defaultComparer
            let arr = elements |> List.mapToArray (fun (k,v) -> struct(k,v))
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)
            
    static member FromArrayOld (elements : array<'Key * 'Value>) =
        let comparer = defaultComparer
        let mutable r = MapEmpty.Instance
        for (k, v) in elements do
            r <- r.AddInPlace(comparer, k, v)
        MapNew(comparer, r)
        
    static member FromArray (elements : array<'Key * 'Value>) =
        if elements.Length <= 0 then
            MapNew(defaultComparer, MapEmpty.Instance)

        elif elements.Length = 1 then
            let (k,v) = elements.[0]
            MapNew(defaultComparer, MapLeaf(k, v))

        else
            let cmp = defaultComparer
            let arr = elements |> Array.map (fun (k,v) -> struct(k,v))
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)
            
    static member FromArrayV (elements : array<struct('Key * 'Value)>) =
        if elements.Length <= 0 then
            MapNew(defaultComparer, MapEmpty.Instance)

        elif elements.Length = 1 then
            let struct(k,v) = elements.[0]
            MapNew(defaultComparer, MapLeaf(k, v))
        else
            let cmp = defaultComparer
            let arr = Array.copy elements
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)

    static member FromSeqV (elements : seq<struct('Key * 'Value)>) =
        let cmp = defaultComparer
        let arr = elements |> Seq.toArray
        if arr.Length <= 0 then
            MapNew(cmp, MapEmpty.Instance)
        elif arr.Length = 1 then
            let struct(k,v) = arr.[0]
            MapNew(cmp, MapLeaf(k,v))
        else
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)

    static member FromListV (elements : list<struct('Key * 'Value)>) =
        match elements with
        | [] -> MapNew(defaultComparer, MapEmpty.Instance)
        | [struct(k,v)] -> MapNew(defaultComparer, MapLeaf(k, v))
        | elements ->
            let cmp = defaultComparer
            let arr = elements |> List.toArray
            Array.sortInPlaceBy (fun struct(k,_) -> k) arr
            MapNew.FromSortedArray(cmp, arr)
            
    member x.Count = root.Count
    member x.Root = root

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

    member x.GetViewBetween(minInclusive : 'Key, maxInclusive : 'Key) = 
        MapNew(comparer, root.GetViewBetween(comparer, minInclusive, true, maxInclusive, true))
        
    member x.WithMin(minInclusive : 'Key) = 
        MapNew(comparer, root.WithMin(comparer, minInclusive, true))
        
    member x.WithMax(maxInclusive : 'Key) = 
        MapNew(comparer, root.WithMax(comparer, maxInclusive, true))

    member x.TryMinKeyValue() = root.TryMinKeyValue()
    member x.TryMaxKeyValue() = root.TryMaxKeyValue()
    member x.TryMinKeyValueV() = root.TryMinKeyValueV()
    member x.TryMaxKeyValueV() = root.TryMaxKeyValueV()

    member x.Change(key : 'Key, update : option<'Value> -> option<'Value>) =
        MapNew(comparer, root.Change(comparer, key, update))
        
    member x.ChangeV(key : 'Key, update : voption<'Value> -> voption<'Value>) =
        MapNew(comparer, root.ChangeV(comparer, key, update))

    member x.TryAt(index : int) =
        if index < 0 || index >= root.Count then None
        else root.TryAt index
        
    member x.TryAtV(index : int) =
        if index < 0 || index >= root.Count then ValueNone
        else root.TryAtV index

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
                
        member inline private x.MoveNext(deep : bool, top : Node<'Key, 'Value>) =
            let mutable top = top
            let mutable run = true

            while run do
                match top with
                | :? MapLeaf<'Key, 'Value> as n ->
                    x.Value <- KeyValuePair(n.Key, n.Value)
                    run <- false

                | :? MapInner<'Key, 'Value> as n ->
                    if deep then
                        if n.Left.Height = 0 then
                            if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                            x.Value <- KeyValuePair(n.Key, n.Value)
                            run <- false
                        else
                            if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                            x.Stack <- struct(n :> Node<_,_>, false) :: x.Stack
                            top <- n.Left
                    else    
                        x.Value <- KeyValuePair(n.Key, n.Value)
                        run <- false

                #if TWO
                | :? MapTwo<'Key, 'Value> as n ->
                    if deep then
                        x.Value <- KeyValuePair(n.K0, n.V0)
                        x.Stack <- struct(n :> Node<_,_>, false) :: x.Stack
                        run <- false
                    else
                        x.Value <- KeyValuePair(n.K1, n.V1)
                        run <- false
                #endif
                | _ ->
                    failwith "empty node"
    
            
        member inline x.MoveNext() =
            match x.Stack with
            | struct(n, deep) :: rest ->
                x.Stack <- rest
                x.MoveNext(deep, n)
                true
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

and MapDebugView<'Key, 'Value when 'Key : comparison> =

    [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
    val mutable public Entries : KeyValuePairDebugFriendly<'Key, 'Value>[]

    new(m : MapNew<'Key, 'Value>) =
        {
            Entries = Seq.toArray (Seq.map KeyValuePairDebugFriendly (Seq.truncate 10000 m))
        }
        
and
    [<DebuggerDisplay("{keyValue.Value}", Name = "[{keyValue.Key}]", Type = "")>]
    KeyValuePairDebugFriendly<'Key, 'Value>(keyValue : KeyValuePair<'Key, 'Value>) =

        [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
        member x.KeyValue = keyValue

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

    [<CompiledName("Change")>]
    let inline change (key : 'Key) (update : option<'Value> -> option<'Value>) (map : MapNew<'Key, 'Value>) = map.Change(key, update)
    
    [<CompiledName("ChangeValue")>]
    let inline changeV (key : 'Key) (update : voption<'Value> -> voption<'Value>) (map : MapNew<'Key, 'Value>) = map.ChangeV(key, update)

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

    
    [<CompiledName("WithMin")>]
    let inline withMin (minInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.WithMin(minInclusive)
    
    [<CompiledName("WithMax")>]
    let inline withMax (maxInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.WithMax(maxInclusive)
    
    [<CompiledName("WithRange")>]
    let inline withRange (minInclusive : 'Key) (maxInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.GetViewBetween(minInclusive, maxInclusive)

    
    [<CompiledName("TryMax")>]
    let inline tryMax (map : MapNew<'Key, 'Value>) = map.TryMaxKeyValue()

    [<CompiledName("TryMin")>]
    let inline tryMin (map : MapNew<'Key, 'Value>) = map.TryMinKeyValue()

    [<CompiledName("TryMaxValue")>]
    let inline tryMaxV (map : MapNew<'Key, 'Value>) = map.TryMaxKeyValueV()

    [<CompiledName("TryMinValue")>]
    let inline tryMinV (map : MapNew<'Key, 'Value>) = map.TryMinKeyValueV()
    
    [<CompiledName("TryAt")>]
    let inline tryAt (index : int) (map : MapNew<'Key, 'Value>) = map.TryAt index
    
    [<CompiledName("TryAtValue")>]
    let inline tryAtV (index : int) (map : MapNew<'Key, 'Value>) = map.TryAtV index