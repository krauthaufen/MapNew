namespace MapNew

open System
open System.Linq
open System.Collections.Generic

module MapNewImplementation = 

    

    [<AbstractClass>]
    type MapNode<'Key, 'Value>() =
        abstract member Count : int
        abstract member Height : int

        abstract member Add : comparer : IComparer<'Key> * key : 'Key * value : 'Value -> MapNode<'Key, 'Value>
        abstract member AddIfNotPresent : comparer : IComparer<'Key> * key : 'Key * value : 'Value -> MapNode<'Key, 'Value>
        abstract member Remove : comparer : IComparer<'Key> * key : 'Key -> MapNode<'Key, 'Value>
        abstract member AddInPlace : comparer : IComparer<'Key> * key : 'Key * value : 'Value -> MapNode<'Key, 'Value>
        abstract member TryRemove : comparer : IComparer<'Key> * key : 'Key -> option<MapNode<'Key,'Value> * 'Value>
        abstract member TryRemoveV : comparer : IComparer<'Key> * key : 'Key -> voption<struct(MapNode<'Key,'Value> * 'Value)>

        abstract member Map : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T> -> MapNode<'Key, 'T>
        abstract member Filter : predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> MapNode<'Key, 'Value>
        abstract member Choose : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>> -> MapNode<'Key, 'T>
        abstract member ChooseV : mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>> -> MapNode<'Key, 'T>

        abstract member UnsafeRemoveHeadV : unit -> struct('Key * 'Value * MapNode<'Key, 'Value>)
        abstract member UnsafeRemoveTailV : unit -> struct(MapNode<'Key, 'Value> * 'Key * 'Value)
        
        abstract member GetViewBetween : comparer : IComparer<'Key> * min : 'Key * minInclusive : bool * max : 'Key * maxInclusive : bool -> MapNode<'Key, 'Value>
        abstract member WithMin : comparer : IComparer<'Key> * min : 'Key * minInclusive : bool -> MapNode<'Key, 'Value>
        abstract member WithMax : comparer : IComparer<'Key> * max : 'Key * maxInclusive : bool -> MapNode<'Key, 'Value>
        abstract member SplitV : comparer : IComparer<'Key> * key : 'Key -> struct(MapNode<'Key, 'Value> * MapNode<'Key, 'Value> * voption<'Value>)
        
        abstract member Change : comparer : IComparer<'Key> * key : 'Key * (option<'Value> -> option<'Value>) -> MapNode<'Key, 'Value>
        abstract member ChangeV : comparer : IComparer<'Key> * key : 'Key * (voption<'Value> -> voption<'Value>) -> MapNode<'Key, 'Value>
        
        //// find, findKey tryFindKey, pick, partition, tryPick
        //abstract member TryFindKey : pick : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> option<'Key>
        //abstract member TryFindKeyV : pick : OptimizedClosures.FSharpFunc<'Key, 'Value, bool> -> voption<'Key>
        //abstract member TryPick : pick : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>> -> option<'T>
        //abstract member TryPickV : pick : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>> -> voption<'T>


    and [<Sealed>]
        MapEmpty<'Key, 'Value> private() =
        inherit MapNode<'Key, 'Value>()

        static let instance = MapEmpty<'Key, 'Value>() :> MapNode<_,_>

        static member Instance : MapNode<'Key, 'Value> = instance

        override x.Count = 0
        override x.Height = 0
        override x.Add(_, key, value) =
            MapLeaf(key, value) :> MapNode<_,_>
            
        override x.AddIfNotPresent(_, key, value) =
            MapLeaf(key, value) :> MapNode<_,_>

        override x.AddInPlace(_, key, value) =
            MapLeaf(key, value) :> MapNode<_,_>

        override x.Remove(_,_) =
            x :> MapNode<_,_>
            
        override x.TryRemove(_,_) =
            None

        override x.TryRemoveV(_,_) =
            ValueNone

        override x.Map(_) = MapEmpty.Instance
        override x.Filter(_) = x :> MapNode<_,_>
        override x.Choose(_) = MapEmpty.Instance
        override x.ChooseV(_) = MapEmpty.Instance

        override x.UnsafeRemoveHeadV() = failwith "empty"
        override x.UnsafeRemoveTailV() = failwith "empty"

        override x.GetViewBetween(_comparer : IComparer<'Key>, _min : 'Key, _minInclusive : bool, _max : 'Key, _maxInclusive : bool) =
            x :> MapNode<_,_>
        override x.WithMin(_comparer : IComparer<'Key>, _min : 'Key, _minInclusive : bool) =
            x :> MapNode<_,_>
        override x.WithMax(_comparer : IComparer<'Key>, _max : 'Key, _maxInclusive : bool) =
            x :> MapNode<_,_>

        override x.SplitV(_,_) =
            (x :> MapNode<_,_>, x :> MapNode<_,_>, ValueNone)

        override x.Change(_comparer, key, update) =
            match update None with
            | None -> x :> MapNode<_,_>
            | Some v -> MapLeaf(key, v) :> MapNode<_,_>
            
        override x.ChangeV(comparer, key, update) =
            match update ValueNone with
            | ValueNone -> x :> MapNode<_,_>
            | ValueSome v -> MapLeaf(key, v) :> MapNode<_,_>

    and [<Sealed>]
        MapLeaf<'Key, 'Value> =
        class 
            inherit MapNode<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value

            override x.Height =
                1

            override x.Count =
                1

            override x.Add(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then
                    MapInner(x, key, value, MapEmpty.Instance) :> MapNode<'Key,'Value>
                elif c < 0 then
                    MapInner(MapEmpty.Instance, key, value, x) :> MapNode<'Key,'Value>
                else
                    MapLeaf(key, value) :> MapNode<'Key,'Value>
                    
            override x.AddIfNotPresent(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then
                    MapInner(x, key, value, MapEmpty.Instance) :> MapNode<'Key,'Value>
                elif c < 0 then
                    MapInner(MapEmpty.Instance, key, value, x) :> MapNode<'Key,'Value>
                else
                    x :> MapNode<'Key,'Value>
                    
            override x.AddInPlace(comparer, key, value) =
                let c = comparer.Compare(key, x.Key)

                if c > 0 then   
                    MapInner(x, key, value, MapEmpty.Instance) :> MapNode<'Key,'Value>
                elif c < 0 then
                    MapInner(MapEmpty.Instance, key, value, x) :> MapNode<'Key,'Value>
                else
                    x.Key <- key
                    x.Value <- value
                    x :> MapNode<'Key,'Value>

                
            override x.Remove(comparer, key) =
                if comparer.Compare(key, x.Key) = 0 then MapEmpty.Instance
                else x :> MapNode<_,_>
                
            override x.TryRemove(comparer, key) =
                if comparer.Compare(key, x.Key) = 0 then Some(MapEmpty.Instance, x.Value)
                else None
                
            override x.TryRemoveV(comparer, key) =
                if comparer.Compare(key, x.Key) = 0 then ValueSome(MapEmpty.Instance, x.Value)
                else ValueNone

            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapLeaf(x.Key, mapping.Invoke(x.Key, x.Value)) :> MapNode<_,_>
                
            override x.Filter(predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) =
                if predicate.Invoke(x.Key, x.Value) then
                    x :> MapNode<_,_>
                else
                    MapEmpty.Instance

            override x.Choose(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) =
                match mapping.Invoke(x.Key, x.Value) with
                | Some v -> 
                    MapLeaf(x.Key, v) :> MapNode<_,_>
                | None ->
                    MapEmpty.Instance

            override x.ChooseV(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) =
                match mapping.Invoke(x.Key, x.Value) with
                | ValueSome v -> 
                    MapLeaf(x.Key, v) :> MapNode<_,_>
                | ValueNone ->
                    MapEmpty.Instance

            override x.UnsafeRemoveHeadV() =
                struct(x.Key, x.Value, MapEmpty<'Key, 'Value>.Instance)

            override x.UnsafeRemoveTailV() =
                struct(MapEmpty<'Key, 'Value>.Instance, x.Key, x.Value)

            
            override x.GetViewBetween(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool, max : 'Key, maxInclusive : bool) =
                let cMin = comparer.Compare(x.Key, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    let cMax = comparer.Compare(x.Key, max)
                    if (if maxInclusive then cMax <= 0 else cMax < 0) then
                        x :> MapNode<_,_>
                    else
                        MapEmpty.Instance
                else
                    MapEmpty.Instance
                    
            override x.WithMin(comparer : IComparer<'Key>, min : 'Key, minInclusive : bool) =
                let cMin = comparer.Compare(x.Key, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    x :> MapNode<_,_>
                else
                    MapEmpty.Instance
                    
            override x.WithMax(comparer : IComparer<'Key>, max : 'Key, maxInclusive : bool) =
                let cMax = comparer.Compare(x.Key, max)
                if (if maxInclusive then cMax <= 0 else cMax < 0) then
                    x :> MapNode<_,_>
                else
                    MapEmpty.Instance
                    
            override x.SplitV(comparer : IComparer<'Key>, key : 'Key) =
                let c = comparer.Compare(x.Key, key)
                if c > 0 then
                    struct(MapEmpty.Instance, x :> MapNode<_,_>, ValueNone)
                elif c < 0 then
                    struct(x :> MapNode<_,_>, MapEmpty.Instance, ValueNone)
                else
                    struct(MapEmpty.Instance, MapEmpty.Instance, ValueSome x.Value)
                 
            override x.Change(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match update None with
                    | None -> x :> MapNode<_,_>
                    | Some v -> MapInner(x, key, v, MapEmpty.Instance) :> MapNode<_,_>
                elif c < 0 then
                    match update None with
                    | None -> x :> MapNode<_,_>
                    | Some v -> MapInner(MapEmpty.Instance, key, v, x) :> MapNode<_,_>
                else    
                    match update (Some x.Value) with
                    | Some v ->
                        MapLeaf(key, v) :> MapNode<_,_>
                    | None ->
                        MapEmpty.Instance

            override x.ChangeV(comparer, key, update) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match update ValueNone with
                    | ValueNone -> x :> MapNode<_,_>
                    | ValueSome v -> MapInner(x, key, v, MapEmpty.Instance) :> MapNode<_,_>
                elif c < 0 then
                    match update ValueNone with
                    | ValueNone -> x :> MapNode<_,_>
                    | ValueSome v -> MapInner(MapEmpty.Instance, key, v, x) :> MapNode<_,_>
                else    
                    match update (ValueSome x.Value) with
                    | ValueSome v ->
                        MapLeaf(key, v) :> MapNode<_,_>
                    | ValueNone ->
                        MapEmpty.Instance

            new(k : 'Key, v : 'Value) = { Key = k; Value = v}
        end

    and [<Sealed>]
        MapInner<'Key, 'Value> =
        class 
            inherit MapNode<'Key, 'Value>

            val mutable public Left : MapNode<'Key, 'Value>
            val mutable public Right : MapNode<'Key, 'Value>
            val mutable public Key : 'Key
            val mutable public Value : 'Value
            val mutable public _Count : int
            val mutable public _Height : int

            static member Create(l : MapNode<'Key, 'Value>, k : 'Key, v : 'Value, r : MapNode<'Key, 'Value>) =
                let lh = l.Height
                let rh = r.Height
                let b = rh - lh

                if lh = 0 && rh = 0 then
                    MapLeaf(k, v) :> MapNode<_,_>
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
                    MapInner(l, k, v, r) :> MapNode<_,_>

            static member Join(l : MapNode<'Key, 'Value>, r : MapNode<'Key, 'Value>) =
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
                    ) :> MapNode<_,_>
               
            override x.AddIfNotPresent(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    MapInner.Create(
                        x.Left, 
                        x.Key, x.Value,
                        x.Right.AddIfNotPresent(comparer, key, value)
                    )
                elif c < 0 then
                    MapInner.Create(
                        x.Left.AddIfNotPresent(comparer, key, value), 
                        x.Key, x.Value,
                        x.Right
                    )
                else
                    x :> MapNode<_,_>
                     
            override x.AddInPlace(comparer : IComparer<'Key>, key : 'Key, value : 'Value) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    x.Right <- x.Right.AddInPlace(comparer, key, value)

                    let bal = abs (x.Right.Height - x.Left.Height)
                    if bal < 2 then 
                        x._Height <- 1 + max x.Left.Height x.Right.Height
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> MapNode<_,_>
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
                        x._Height <- 1 + max x.Left.Height x.Right.Height
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> MapNode<_,_>
                    else
                        MapInner.Create(
                            x.Left, 
                            x.Key, x.Value,
                            x.Right
                        )
                else
                    x.Key <- key
                    x.Value <- value
                    x :> MapNode<_,_>

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
                    
            override x.TryRemove(comparer : IComparer<'Key>, key : 'Key) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match x.Right.TryRemoveV(comparer, key) with
                    | ValueSome struct(newRight, value) ->
                        let newNode = 
                            MapInner.Create(
                                x.Left, 
                                x.Key, x.Value,
                                newRight
                            )
                        Some(newNode, value)
                    | ValueNone ->
                        None
                elif c < 0 then
                    match x.Left.TryRemoveV(comparer, key) with
                    | ValueSome struct(newLeft, value) ->
                        let newNode = 
                            MapInner.Create(
                                newLeft, 
                                x.Key, x.Value,
                                x.Right
                            )
                        Some(newNode, value)
                    | ValueNone ->
                        None
                else
                    Some(MapInner.Join(x.Left, x.Right), x.Value)
                           
            override x.TryRemoveV(comparer : IComparer<'Key>, key : 'Key) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    match x.Right.TryRemoveV(comparer, key) with
                    | ValueSome struct(newRight, value) ->
                        let newNode = 
                            MapInner.Create(
                                x.Left, 
                                x.Key, x.Value,
                                newRight
                            )
                        ValueSome(newNode, value)
                    | ValueNone ->
                        ValueNone
                elif c < 0 then
                    match x.Left.TryRemoveV(comparer, key) with
                    | ValueSome struct(newLeft, value) ->
                        let newNode = 
                            MapInner.Create(
                                newLeft, 
                                x.Key, x.Value,
                                x.Right
                            )
                        ValueSome(newNode, value)
                    | ValueNone ->
                        ValueNone
                else
                    ValueSome(MapInner.Join(x.Left, x.Right), x.Value)

            override x.Map(mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, 'T>) =
                MapInner(
                    x.Left.Map(mapping),
                    x.Key, mapping.Invoke(x.Key, x.Value),
                    x.Right.Map(mapping)
                ) :> MapNode<_,_>
                
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

            override x.UnsafeRemoveHeadV() =
                if x.Left.Count = 0 then
                    struct(x.Key, x.Value, x.Right)
                else
                    let struct(k,v,l1) = x.Left.UnsafeRemoveHeadV()
                    struct(k, v, MapInner.Create(l1, x.Key, x.Value, x.Right))

            override x.UnsafeRemoveTailV() =   
                if x.Right.Count = 0 then
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
                    
                    
            override x.SplitV(comparer : IComparer<'Key>, key : 'Key) =
                let c = comparer.Compare(key, x.Key)
                if c > 0 then
                    let struct(rl, rr, rv) = x.Right.SplitV(comparer, key)
                    struct(MapInner.Create(x.Left, x.Key, x.Value, rl), rr, rv)
                elif c < 0 then
                    let struct(ll, lr, lv) = x.Left.SplitV(comparer, key)
                    struct(ll, MapInner.Create(lr, x.Key, x.Value, x.Right), lv)
                else
                    struct(x.Left, x.Right, ValueSome x.Value)

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
                    failwith "invalid range"

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
                        ) :> MapNode<_,_>
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
                        ) :> MapNode<_,_>
                    | ValueNone ->
                        MapInner.Join(x.Left, x.Right)

            new(l : MapNode<'Key, 'Value>, k : 'Key, v : 'Value, r : MapNode<'Key, 'Value>) =
                assert(l.Count > 0 || r.Count > 0)      // not both empty
                assert(abs (r.Height - l.Height) <= 2)  // balanced
                {
                    Left = l
                    Right = r
                    Key = k
                    Value = v
                    _Count = 1 + l.Count + r.Count
                    _Height = 1 + max l.Height r.Height
                }
        end

    
    let inline combineHash (a: int) (b: int) =
        uint32 a ^^^ uint32 b + 0x9e3779b9u + ((uint32 a) <<< 6) + ((uint32 a) >>> 2) |> int


    let hash (n : MapNode<'K, 'V>) =
        let rec hash (acc : int) (n : MapNode<'K, 'V>) =    
            match n with
            | :? MapLeaf<'K, 'V> as n ->
                combineHash acc (combineHash (Unchecked.hash n.Key) (Unchecked.hash n.Value))

            | :? MapInner<'K, 'V> as n ->
                let acc = hash acc n.Left
                let acc = combineHash acc (combineHash (Unchecked.hash n.Key) (Unchecked.hash n.Value))
                hash acc n.Right
            | _ ->
                acc

        hash 0 n

    let rec equals (cmp : IComparer<'K>) (l : MapNode<'K,'V>) (r : MapNode<'K,'V>) =
        if l.Count <> r.Count then
            false
        else
            // counts identical
            match l with
            | :? MapLeaf<'K, 'V> as l ->
                let r = r :?> MapLeaf<'K, 'V> // has to hold (r.Count = 1)
                cmp.Compare(l.Key, r.Key) = 0 &&
                Unchecked.equals l.Value r.Value

            | :? MapInner<'K, 'V> as l ->
                match r with
                | :? MapInner<'K, 'V> as r ->
                    let struct(ll, lr, lv) = l.SplitV(cmp, r.Key)
                    match lv with
                    | ValueSome lv when Unchecked.equals lv r.Value ->
                        equals cmp ll r.Left &&
                        equals cmp lr r.Right
                    | _ ->
                        false
                | _ ->
                    false
            | _ ->
                true

open MapNewImplementation
open System.Diagnostics

[<DebuggerTypeProxy("Aardvark.Base.MapDebugView`2")>]
[<DebuggerDisplay("Count = {Count}")>]
[<Sealed>]
type MapNew<'Key, 'Value when 'Key : comparison> private(comparer : IComparer<'Key>, root : MapNode<'Key, 'Value>) =
        
    static let defaultComparer = LanguagePrimitives.FastGenericComparer<'Key>
    static let empty = MapNew<'Key, 'Value>(defaultComparer, MapEmpty.Instance)

    [<NonSerialized>]
    // This type is logically immutable. This field is only mutated during deserialization.
    let mutable comparer = comparer
    
    [<NonSerialized>]
    // This type is logically immutable. This field is only mutated during deserialization.
    let mutable root = root

    // WARNING: The compiled name of this field may never be changed because it is part of the logical
    // WARNING: permanent serialization format for this type.
    let mutable serializedData = null

    static let toKeyValueArray(root : MapNode<_,_>) =
        let arr = Array.zeroCreate root.Count
        let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let i = copyTo arr index n.Left
                arr.[i] <- KeyValuePair(n.Key, n.Value)
                
                copyTo arr (i+1) n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                arr.[index] <- KeyValuePair(n.Key, n.Value)
                index + 1
            | _ ->
                index

        copyTo arr 0 root |> ignore<int>
        arr

    static let fromArray (elements : struct('Key * 'Value)[]) =
        let cmp = defaultComparer
        match elements.Length with
        | 0 -> 
            MapEmpty.Instance
        | 1 ->
            let struct(k,v) = elements.[0]
            MapLeaf(k, v) :> MapNode<_,_>
        | 2 -> 
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let c = cmp.Compare(k0, k1)
            if c > 0 then MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)) :> MapNode<_,_>
            elif c < 0 then MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance) :> MapNode<_,_>
            else MapLeaf(k1, v1):> MapNode<_,_>
        | 3 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2)
        | 4 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            let struct(k3,v3) = elements.[3]
            MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3)
        | 5 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            let struct(k3,v3) = elements.[3]
            let struct(k4,v4) = elements.[4]
            MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4)
        | _ ->
            let struct(arr, cnt) = Sorting.mergeSortHandleDuplicatesV false cmp elements elements.Length
            MapNew.CreateRoot(arr, cnt)

    [<System.Runtime.Serialization.OnSerializingAttribute>]
    member __.OnSerializing(context: System.Runtime.Serialization.StreamingContext) =
        ignore context
        serializedData <- toKeyValueArray root

    [<System.Runtime.Serialization.OnDeserializedAttribute>]
    member __.OnDeserialized(context: System.Runtime.Serialization.StreamingContext) =
        ignore context
        comparer <- defaultComparer
        serializedData <- null
        root <- serializedData |> Array.map (fun kvp -> struct(kvp.Key, kvp.Value)) |> fromArray 

    static member Empty = empty

    static member private CreateTree(cmp : IComparer<'Key>, arr : ('Key * 'Value)[], cnt : int)=
        let rec create (arr : ('Key * 'Value)[]) (l : int) (r : int) =
            if l > r then
                MapEmpty.Instance
            elif l = r then
                let (k,v) = arr.[l]
                MapLeaf(k, v) :> MapNode<_,_>
            else
                let m = (l+r)/2
                let (k,v) = arr.[m]
                MapInner(
                    create arr l (m-1),
                    k, v,
                    create arr (m+1) r
                ) :> MapNode<_,_>

        MapNew(cmp, create arr 0 (cnt-1))
        
    static member private CreateTree(cmp : IComparer<'Key>, arr : struct('Key * 'Value)[], cnt : int)=
        let rec create (arr : struct('Key * 'Value)[]) (l : int) (r : int) =
            if l = r then
                let struct(k,v) = arr.[l]
                MapLeaf(k, v) :> MapNode<_,_>
            elif l > r then
                MapEmpty.Instance
            else
                let m = (l+r)/2
                let struct(k,v) = arr.[m]
                MapInner(
                    create arr l (m-1),
                    k, v,
                    create arr (m+1) r
                ) :> MapNode<_,_>

        MapNew(cmp, create arr 0 (cnt-1))
  
    static member private CreateRoot(arr : struct('Key * 'Value)[], cnt : int)=
        let rec create (arr : struct('Key * 'Value)[]) (l : int) (r : int) =
            if l = r then
                let struct(k,v) = arr.[l]
                MapLeaf(k, v) :> MapNode<_,_>
            elif l > r then
                MapEmpty.Instance
            else
                let m = (l+r)/2
                let struct(k,v) = arr.[m]
                MapInner(
                    create arr l (m-1),
                    k, v,
                    create arr (m+1) r
                ) :> MapNode<_,_>

        create arr 0 (cnt-1)

    static member FromArray (elements : array<'Key * 'Value>) =
        let cmp = defaultComparer
        match elements.Length with
        | 0 -> 
            MapNew(cmp, MapEmpty.Instance)
        | 1 ->
            let (k,v) = elements.[0]
            MapNew(cmp, MapLeaf(k, v))
        | 2 -> 
            let (k0,v0) = elements.[0]
            let (k1,v1) = elements.[1]
            let c = cmp.Compare(k0, k1)
            if c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
            elif c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
            else MapNew(cmp, MapLeaf(k1, v1))
        | 3 ->
            let (k0,v0) = elements.[0]
            let (k1,v1) = elements.[1]
            let (k2,v2) = elements.[2]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
        | 4 ->
            let (k0,v0) = elements.[0]
            let (k1,v1) = elements.[1]
            let (k2,v2) = elements.[2]
            let (k3,v3) = elements.[3]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
        | 5 ->
            let (k0,v0) = elements.[0]
            let (k1,v1) = elements.[1]
            let (k2,v2) = elements.[2]
            let (k3,v3) = elements.[3]
            let (k4,v4) = elements.[4]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))
        | _ ->
            let struct(arr, cnt) = Sorting.mergeSortHandleDuplicates false cmp elements elements.Length
            MapNew.CreateTree(cmp, arr, cnt)
        
    static member FromArrayV (elements : array<struct('Key * 'Value)>) =
        let cmp = defaultComparer
        match elements.Length with
        | 0 -> 
            MapNew(cmp, MapEmpty.Instance)
        | 1 ->
            let struct(k,v) = elements.[0]
            MapNew(cmp, MapLeaf(k, v))
        | 2 -> 
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let c = cmp.Compare(k0, k1)
            if c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
            elif c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
            else MapNew(cmp, MapLeaf(k1, v1))
        | 3 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
        | 4 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            let struct(k3,v3) = elements.[3]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
        | 5 ->
            let struct(k0,v0) = elements.[0]
            let struct(k1,v1) = elements.[1]
            let struct(k2,v2) = elements.[2]
            let struct(k3,v3) = elements.[3]
            let struct(k4,v4) = elements.[4]
            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))
        | _ ->
            let struct(arr, cnt) = Sorting.mergeSortHandleDuplicatesV false cmp elements elements.Length
            MapNew.CreateTree(cmp, arr, cnt)
        
    static member FromList (elements : list<'Key * 'Value>) =
        let rec atMost (cnt : int) (l : list<_>) =
            match l with
            | [] -> true
            | _ :: t ->
                if cnt > 0 then atMost (cnt - 1) t
                else false

        let cmp = defaultComparer
        match elements with
        | [] -> 
            // cnt = 0
            MapNew(cmp, MapEmpty.Instance)

        | ((k0, v0) as t0) :: rest ->
            // cnt >= 1
            match rest with
            | [] -> 
                // cnt = 1
                MapNew(cmp, MapLeaf(k0, v0))
            | ((k1, v1) as t1) :: rest ->
                // cnt >= 2
                match rest with
                | [] ->
                    // cnt = 2
                    let c = cmp.Compare(k0, k1)
                    if c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
                    elif c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
                    else MapNew(cmp, MapLeaf(k1, v1))
                | ((k2, v2) as t2) :: rest ->
                    // cnt >= 3
                    match rest with
                    | [] ->
                        // cnt = 3
                        MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
                    | ((k3, v3) as t3) :: rest ->
                        // cnt >= 4
                        match rest with
                        | [] ->
                            // cnt = 4
                            MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
                        | ((k4, v4) as t4) :: rest ->
                            // cnt >= 5
                            match rest with
                            | [] ->
                                // cnt = 5
                                MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))
                            | t5 :: rest ->
                                // cnt >= 6
                                let mutable arr = Array.zeroCreate 16
                                let mutable cnt = 6
                                arr.[0] <- t0
                                arr.[1] <- t1
                                arr.[2] <- t2
                                arr.[3] <- t3
                                arr.[4] <- t4
                                arr.[5] <- t5
                                for t in rest do
                                    if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                                    arr.[cnt] <- t
                                    cnt <- cnt + 1
                                    
                                let struct(arr1, cnt1) = Sorting.mergeSortHandleDuplicates true cmp arr cnt
                                MapNew.CreateTree(cmp, arr1, cnt1)
                                
                                
                    
                

    static member FromListV (elements : list<struct('Key * 'Value)>) =
        let rec atMost (cnt : int) (l : list<_>) =
            match l with
            | [] -> true
            | _ :: t ->
                if cnt > 0 then atMost (cnt - 1) t
                else false

        let cmp = defaultComparer
        match elements with
        | [] -> 
            // cnt = 0
            MapNew(cmp, MapEmpty.Instance)

        | struct(k0, v0) :: rest ->
            // cnt >= 1
            match rest with
            | [] -> 
                // cnt = 1
                MapNew(cmp, MapLeaf(k0, v0))
            | struct(k1, v1) :: rest ->
                // cnt >= 2
                match rest with
                | [] ->
                    // cnt = 2
                    let c = cmp.Compare(k0, k1)
                    if c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
                    elif c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
                    else MapNew(cmp, MapLeaf(k1, v1))
                | struct(k2, v2) :: rest ->
                    // cnt >= 3
                    match rest with
                    | [] ->
                        // cnt = 3
                        MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
                    | struct(k3, v3) :: rest ->
                        // cnt >= 4
                        match rest with
                        | [] ->
                            // cnt = 4
                            MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
                        | struct(k4, v4) :: rest ->
                            // cnt >= 5
                            match rest with
                            | [] ->
                                // cnt = 5
                                MapNew(cmp, MapLeaf(k0,v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))
                            | t5 :: rest ->
                                // cnt >= 6
                                let mutable arr = Array.zeroCreate 16
                                let mutable cnt = 6
                                arr.[0] <- struct(k0, v0)
                                arr.[1] <- struct(k1, v1)
                                arr.[2] <- struct(k2, v2)
                                arr.[3] <- struct(k3, v3)
                                arr.[4] <- struct(k4, v4)
                                arr.[5] <- t5
                                for t in rest do
                                    if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                                    arr.[cnt] <- t
                                    cnt <- cnt + 1
                                    
                                let struct(arr1, cnt1) = Sorting.mergeSortHandleDuplicatesV true cmp arr cnt
                                MapNew.CreateTree(cmp, arr1, cnt1)
 
    static member FromSeq (elements : seq<'Key * 'Value>) =
        match elements with
        | :? array<'Key * 'Value> as e -> MapNew.FromArray e
        | :? list<'Key * 'Value> as e -> MapNew.FromList e
        | _ ->
            let cmp = defaultComparer
            use e = elements.GetEnumerator()
            if e.MoveNext() then
                // cnt >= 1
                let t0 = e.Current
                let (k0,v0) = t0
                if e.MoveNext() then
                    // cnt >= 2
                    let t1 = e.Current
                    let (k1,v1) = t1
                    if e.MoveNext() then
                        // cnt >= 3 
                        let t2 = e.Current
                        let (k2,v2) = t2
                        if e.MoveNext() then
                            // cnt >= 4
                            let t3 = e.Current
                            let (k3, v3) = t3
                            if e.MoveNext() then
                                // cnt >= 5
                                let t4 = e.Current
                                let (k4, v4) = t4
                                if e.MoveNext() then
                                    // cnt >= 6
                                    let mutable arr = Array.zeroCreate 16
                                    let mutable cnt = 6
                                    arr.[0] <- t0
                                    arr.[1] <- t1
                                    arr.[2] <- t2
                                    arr.[3] <- t3
                                    arr.[4] <- t4
                                    arr.[5] <- e.Current

                                    while e.MoveNext() do
                                        if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                                        arr.[cnt] <- e.Current
                                        cnt <- cnt + 1

                                    let struct(arr1, cnt1) = Sorting.mergeSortHandleDuplicates true cmp arr cnt
                                    MapNew.CreateTree(cmp, arr1, cnt1)

                                else
                                    // cnt = 5
                                    MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))

                            else
                                // cnt = 4
                                MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
                        else
                            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
                    else
                        // cnt = 2
                        let c = cmp.Compare(k0, k1)
                        if c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
                        elif c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
                        else MapNew(cmp, MapLeaf(k1, v1))
                else
                    // cnt = 1
                    MapNew(cmp, MapLeaf(k0, v0))

            else
                MapNew(cmp, MapEmpty.Instance)

    static member FromSeqV (elements : seq<struct('Key * 'Value)>) =
        match elements with
        | :? array<struct('Key * 'Value)> as e -> MapNew.FromArrayV e
        | :? list<struct('Key * 'Value)> as e -> MapNew.FromListV e
        | _ ->
            let cmp = defaultComparer
            use e = elements.GetEnumerator()
            if e.MoveNext() then
                // cnt >= 1
                let struct(k0,v0) = e.Current
                if e.MoveNext() then
                    // cnt >= 2
                    let struct(k1,v1) = e.Current
                    if e.MoveNext() then
                        // cnt >= 3 
                        let struct(k2,v2) = e.Current
                        if e.MoveNext() then
                            // cnt >= 4
                            let struct(k3, v3) = e.Current
                            if e.MoveNext() then
                                // cnt >= 5
                                let struct(k4, v4) = e.Current
                                if e.MoveNext() then
                                    // cnt >= 6
                                    let mutable arr = Array.zeroCreate 16
                                    let mutable cnt = 6
                                    arr.[0] <- struct(k0, v0)
                                    arr.[1] <- struct(k1, v1)
                                    arr.[2] <- struct(k2, v2)
                                    arr.[3] <- struct(k3, v3)
                                    arr.[4] <- struct(k4, v4)
                                    arr.[5] <- e.Current

                                    while e.MoveNext() do
                                        if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                                        arr.[cnt] <- e.Current
                                        cnt <- cnt + 1

                                    let struct(arr1, cnt1) = Sorting.mergeSortHandleDuplicatesV true cmp arr cnt
                                    MapNew.CreateTree(cmp, arr1, cnt1)

                                else
                                    // cnt = 5
                                    MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3).AddInPlace(cmp, k4, v4))

                            else
                                // cnt = 4
                                MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2).AddInPlace(cmp, k3, v3))
                        else
                            MapNew(cmp, MapLeaf(k0, v0).AddInPlace(cmp, k1, v1).AddInPlace(cmp, k2, v2))
                    else
                        // cnt = 2
                        let c = cmp.Compare(k0, k1)
                        if c < 0 then MapNew(cmp, MapInner(MapLeaf(k0, v0), k1, v1, MapEmpty.Instance))
                        elif c > 0 then MapNew(cmp, MapInner(MapEmpty.Instance, k1, v1, MapLeaf(k0, v0)))
                        else MapNew(cmp, MapLeaf(k1, v1))
                else
                    // cnt = 1
                    MapNew(cmp, MapLeaf(k0, v0))

            else
                MapNew(cmp, MapEmpty.Instance)

    static member Union(l : MapNew<'Key, 'Value>, r : MapNew<'Key, 'Value>) =
        let rec union (cmp : IComparer<'Key>) (l : MapNode<'Key, 'Value>) (r : MapNode<'Key, 'Value>) =
            match l with
            | :? MapEmpty<'Key, 'Value> ->  
                r
            | :? MapLeaf<'Key, 'Value> as l ->
                r.AddIfNotPresent(cmp, l.Key, l.Value)
            | :? MapInner<'Key, 'Value> as l ->
                match r with
                | :? MapEmpty<'Key, 'Value> ->
                    l :> MapNode<_,_>
                | :? MapLeaf<'Key, 'Value> as r ->
                    l.Add(cmp, r.Key, r.Value)
                | :? MapInner<'Key, 'Value> as r ->
                    if l.Count > r.Count then
                        let struct(rl, rr, rv) = r.SplitV(cmp, l.Key)
                        let r = ()
                        match rv with
                        | ValueSome rv ->
                            MapInner.Create(
                                union cmp l.Left rl, 
                                l.Key, rv, 
                                union cmp l.Right rr
                            )
                        | ValueNone ->
                            MapInner.Create(
                                union cmp l.Left rl, 
                                l.Key, l.Value, 
                                union cmp l.Right rr
                            )
                    else
                        let struct(ll, lr, _lv) = l.SplitV(cmp, r.Key)
                        let l = ()
                        MapInner.Create(
                            union cmp ll r.Left, 
                            r.Key, r.Value, 
                            union cmp lr r.Right
                        ) 
                | _ ->
                    failwith "unexpected node"
            | _ ->
                failwith "unexpected node"

        let cmp = defaultComparer
        MapNew(cmp, union cmp l.Root r.Root)

    static member UnionWith(l : MapNew<'Key, 'Value>, r : MapNew<'Key, 'Value>, resolve : 'Key -> 'Value -> 'Value -> 'Value) =
        let resolve = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt resolve
        
        let rec union (cmp : IComparer<'Key>) (resolve : OptimizedClosures.FSharpFunc<_,_,_,_>) (l : MapNode<'Key, 'Value>) (r : MapNode<'Key, 'Value>) =
            match l with
            | :? MapEmpty<'Key, 'Value> ->  
                r
            | :? MapLeaf<'Key, 'Value> as l ->
                r.ChangeV(cmp, l.Key, function
                    | ValueSome rv -> resolve.Invoke(l.Key, l.Value, rv) |> ValueSome
                    | ValueNone -> l.Value |> ValueSome
                )
            | :? MapInner<'Key, 'Value> as l ->
                match r with
                | :? MapEmpty<'Key, 'Value> ->
                    l :> MapNode<_,_>
                | :? MapLeaf<'Key, 'Value> as r ->
                    l.ChangeV(cmp, r.Key, function
                        | ValueSome lv -> resolve.Invoke(r.Key, lv, r.Value) |> ValueSome
                        | ValueNone -> r.Value |> ValueSome
                    )
                | :? MapInner<'Key, 'Value> as r ->
                    if l.Count > r.Count then
                        let struct(rl, rr, rv) = r.SplitV(cmp, l.Key)
                        let r = ()
                        match rv with
                        | ValueSome rv ->
                            MapInner.Create(
                                union cmp resolve l.Left rl, 
                                l.Key, resolve.Invoke(l.Key, l.Value, rv), 
                                union cmp resolve l.Right rr
                            )
                        | ValueNone ->
                            MapInner.Create(
                                union cmp resolve l.Left rl, 
                                l.Key, l.Value, 
                                union cmp resolve l.Right rr
                            )
                    else
                        let struct(ll, lr, lv) = l.SplitV(cmp, r.Key)
                        let l = ()
                        match lv with
                        | ValueSome lv ->
                            MapInner.Create(
                                union cmp resolve ll r.Left, 
                                r.Key, resolve.Invoke(r.Key, lv, r.Value), 
                                union cmp resolve lr r.Right
                            )
                        | ValueNone ->
                            MapInner.Create(
                                union cmp resolve ll r.Left, 
                                r.Key, r.Value, 
                                union cmp resolve lr r.Right
                            )
                            
                | _ ->
                    failwith "unexpected node"
            | _ ->
                failwith "unexpected node"

        let cmp = defaultComparer
        MapNew(cmp, union cmp resolve l.Root r.Root)
        
    member x.Count = root.Count
    member x.Root = root

    static member ComputeDelta<'T>(l : MapNew<'Key, 'Value>, r : MapNew<'Key, 'Value>, add : MapNew<'Key, 'Value> -> MapNew<'Key, 'T>, remove : MapNew<'Key, 'Value> -> MapNew<'Key, 'T>, update : 'Key -> 'Value -> 'Value -> voption<'T>) : MapNew<'Key, 'T> =
        
        let inline add (cmp : IComparer<_>) (a : MapNode<'Key, 'Value>) = 
            if a.Count > 0 then add(MapNew<'Key, 'Value>(cmp, a)).Root
            else MapEmpty.Instance

        let inline remove (cmp : IComparer<_>) (a : MapNode<'Key, 'Value>) = 
            if a.Count > 0 then remove(MapNew<'Key, 'Value>(cmp, a)).Root
            else MapEmpty.Instance

        let rec computeDelta (cmp : IComparer<_>) (update : OptimizedClosures.FSharpFunc<_,_,_,_>) (l : MapNode<'Key, 'Value>) (r : MapNode<'Key, 'Value>) =
            match l with
            | :? MapLeaf<'Key, 'Value> as l ->
                match r with
                | :? MapLeaf<'Key, 'Value> as r ->
                    let c = cmp.Compare(l.Key, r.Key)
                    if c < 0 then
                        MapInner<'Key, 'T>.Join(remove cmp l, add cmp r)
                    elif c > 0 then
                        MapInner<'Key, 'T>.Join(add cmp r, remove cmp l)
                    else
                        match update.Invoke(l.Key, l.Value, r.Value) with
                        | ValueSome o -> MapLeaf(l.Key, o) :> MapNode<_,_>
                        | ValueNone -> MapEmpty.Instance
                | :? MapInner<'Key, 'Value> as r ->
                    let struct(rl, rr, rv) = r.SplitV(cmp, l.Key)

                    let a = computeDelta cmp update MapEmpty.Instance rl
                    let splitter = 
                        match rv with
                        | ValueSome rv -> update.Invoke(l.Key, l.Value, rv)
                        | ValueNone -> ValueNone
                    let b = computeDelta cmp update MapEmpty.Instance rr

                    match splitter with
                    | ValueSome v -> MapInner.Create(a, l.Key, v, b)
                    | ValueNone -> MapInner.Join(a, b)
                | _ ->
                    remove cmp l

            | :? MapInner<'Key, 'Value> as l ->
                match r with
                | :? MapLeaf<'Key, 'Value> as r ->
                    let struct(ll, lr, lv) = l.SplitV(cmp, r.Key)
                    let a = computeDelta cmp update ll MapEmpty.Instance
                    let splitter = 
                        match lv with
                        | ValueSome lv -> update.Invoke(l.Key, lv, r.Value)
                        | ValueNone -> ValueNone
                    let b = computeDelta cmp update lr MapEmpty.Instance

                    match splitter with
                    | ValueSome v -> MapInner.Create(a, l.Key, v, b)
                    | ValueNone -> MapInner.Join(a, b)
                | :? MapInner<'Key, 'Value> as r ->
                    if l.Count > r.Count then
                        let struct(rl, rr, rv) = r.SplitV(cmp, l.Key)
                        let r = ()
                        let a = computeDelta cmp update l.Left rl
                        let splitter = 
                            match rv with
                            | ValueSome rv -> update.Invoke(l.Key, l.Value, rv)
                            | ValueNone -> ValueNone
                        let b = computeDelta cmp update l.Right rr
                        match splitter with
                        | ValueSome v -> MapInner.Create(a, l.Key, v, b)
                        | ValueNone -> MapInner.Join(a, b)
                    else
                        let struct(ll, lr, lv) = l.SplitV(cmp, r.Key)
                        let l = ()
                        let a = computeDelta cmp update ll r.Left
                        let splitter = 
                            match lv with
                            | ValueSome lv -> update.Invoke(r.Key, lv, r.Value)
                            | ValueNone -> ValueNone
                        let b = computeDelta cmp update lr r.Right
                        match splitter with
                        | ValueSome v -> MapInner.Create(a, r.Key, v, b)
                        | ValueNone -> MapInner.Join(a, b)
                | _ ->
                    remove cmp l
            | _ ->
                add cmp r

        let cmp = defaultComparer
        let update = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt update

        MapNew(cmp, computeDelta cmp update l.Root r.Root)


    member x.Add(key : 'Key, value : 'Value) =
        MapNew(comparer, root.Add(comparer, key, value))
       
    member x.AddMatch(key : 'Key, value : 'Value) =
        let rec add (cmp : IComparer<'Key>) (key : 'Key) (value : 'Value) (n : MapNode<'Key, 'Value>) =
            match n with
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then
                    MapInner(n, key, value, MapEmpty.Instance) :> MapNode<_,_>
                elif c < 0 then
                    MapInner(MapEmpty.Instance, key, value, n) :> MapNode<_,_>
                else
                    MapLeaf(key, value) :> MapNode<_,_>
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then
                    MapInner.Create(
                        n.Left,
                        n.Key, n.Value,
                        add cmp key value n.Right
                    )
                elif c < 0 then
                    MapInner.Create(
                        add cmp key value n.Left,
                        n.Key, n.Value,
                        n.Right
                    )
                else
                    MapInner(
                        n.Left,
                        key, value,
                        n.Right
                    ) :> MapNode<_,_>
            | _ ->
                MapLeaf(key, value) :> MapNode<_,_>
                
        MapNew(comparer, add comparer key value root)
            
    member x.Remove(key : 'Key) =
        MapNew(comparer, root.Remove(comparer, key))
        
    member x.RemoveMatch(key : 'Key) =
    
        let rec remove (cmp : IComparer<'Key>) (key : 'Key) (n : MapNode<'Key, 'Value>) =
            match n with
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c = 0 then MapEmpty.Instance
                else n :> MapNode<_,_>
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then
                    MapInner.Create(
                        n.Left,
                        n.Key, n.Value,
                        remove cmp key n.Right
                    )
                elif c < 0 then
                    MapInner.Create(
                        remove cmp key n.Left,
                        n.Key, n.Value,
                        n.Right
                    )
                else
                    MapInner.Join(n.Left, n.Right)
            | _ ->
                MapEmpty.Instance
                
        MapNew(comparer, remove comparer key root)

    member x.Iter(action : 'Key -> 'Value -> unit) =
        let action = OptimizedClosures.FSharpFunc<_,_,_>.Adapt action
        let rec iter (action : OptimizedClosures.FSharpFunc<_,_,_>) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                iter action n.Left
                action.Invoke(n.Key, n.Value)
                iter action n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                action.Invoke(n.Key, n.Value)
            | _ ->
                ()
        iter action root

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
        let rec exists (predicate : OptimizedClosures.FSharpFunc<_,_,_>) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                exists predicate n.Left ||
                predicate.Invoke(n.Key, n.Value) ||
                exists predicate n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                predicate.Invoke(n.Key, n.Value)
            | _ ->
                false
        exists predicate root
        
    member x.Forall(predicate : 'Key -> 'Value -> bool) =
        let rec forall (predicate : OptimizedClosures.FSharpFunc<_,_,_>) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                forall predicate n.Left &&
                predicate.Invoke(n.Key, n.Value) &&
                forall predicate n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                predicate.Invoke(n.Key, n.Value)
            | _ ->
                true
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate
        forall predicate root

    member x.Fold(folder : 'State -> 'Key -> 'Value -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder

        let rec fold (folder : OptimizedClosures.FSharpFunc<_,_,_,_>) seed (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let s1 = fold folder seed n.Left
                let s2 = folder.Invoke(s1, n.Key, n.Value)
                fold folder s2 n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                folder.Invoke(seed, n.Key, n.Value)
            | _ ->
                seed

        fold folder seed root
        
    member x.FoldBack(folder : 'Key -> 'Value -> 'State -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder

        let rec foldBack (folder : OptimizedClosures.FSharpFunc<_,_,_,_>) seed (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let s1 = foldBack folder seed n.Right
                let s2 = folder.Invoke(n.Key, n.Value, s1)
                foldBack folder s2 n.Left
            | :? MapLeaf<'Key, 'Value> as n ->
                folder.Invoke(n.Key, n.Value, seed)
            | _ ->
                seed

        foldBack folder seed root
        
    member x.TryFind(key : 'Key) =
        let rec tryFind (cmp : IComparer<_>) key (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then tryFind cmp key n.Right
                elif c < 0 then tryFind cmp key n.Left
                else Some n.Value
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c = 0 then Some n.Value
                else None
            | _ ->
                None
        tryFind comparer key root
        
    member x.Find(key : 'Key) =
        let rec find (cmp : IComparer<_>) key (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then find cmp key n.Right
                elif c < 0 then find cmp key n.Left
                else n.Value
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c = 0 then n.Value
                else raise <| KeyNotFoundException(string key)
            | _ ->
                raise <| KeyNotFoundException(string key)
        find comparer key root

    member x.Item
        with get(key : 'Key) = x.Find key

    member x.TryFindV(key : 'Key) =
        let rec tryFind (cmp : IComparer<_>) key (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then tryFind cmp key n.Right
                elif c < 0 then tryFind cmp key n.Left
                else ValueSome n.Value
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c = 0 then ValueSome n.Value
                else ValueNone
            | _ ->
                ValueNone
        tryFind comparer key root
     
    member x.TryFindKey(predicate : 'Key -> 'Value -> bool) =
        let rec run (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l ->
                if predicate.Invoke(l.Key, l.Value) then Some l.Key
                else None
            | :? MapInner<'Key, 'Value> as n ->
                match run predicate n.Left with
                | None ->
                    if predicate.Invoke(n.Key, n.Value) then Some n.Key
                    else run predicate n.Right
                | res -> 
                    res
            | _ ->
                None
        run (OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate) root
        
    member x.TryFindKeyV(predicate : 'Key -> 'Value -> bool) =
        let rec run (predicate : OptimizedClosures.FSharpFunc<'Key, 'Value, bool>) (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l ->
                if predicate.Invoke(l.Key, l.Value) then ValueSome l.Key
                else ValueNone
            | :? MapInner<'Key, 'Value> as n ->
                match run predicate n.Left with
                | ValueNone ->
                    if predicate.Invoke(n.Key, n.Value) then ValueSome n.Key
                    else run predicate n.Right
                | res -> 
                    res
            | _ ->
                ValueNone
        run (OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate) root
        
    member x.FindKey(predicate : 'Key -> 'Value -> bool) =
        match x.TryFindKeyV predicate with
        | ValueSome k -> k
        | ValueNone -> raise <| KeyNotFoundException()
        
    member x.TryPick(mapping : 'Key -> 'Value -> option<'T>) =
        let rec run (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, option<'T>>) (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l ->
                mapping.Invoke(l.Key, l.Value)
                
            | :? MapInner<'Key, 'Value> as n ->
                match run mapping n.Left with
                | None ->
                    match mapping.Invoke(n.Key, n.Value) with
                    | Some _ as res -> res
                    | None -> run mapping n.Right
                | res -> 
                    res
            | _ ->
                None
        run (OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping) root
        
    member x.Keys() =
        let rec run (n : MapNode<'Key, 'Value>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                SetNewImplementation.SetInner(
                    run n.Left,
                    n.Key,
                    run n.Right
                ) :> SetNewImplementation.SetNode<_>
            | :? MapLeaf<'Key, 'Value> as n ->
                SetNewImplementation.SetLeaf(n.Key) :> SetNewImplementation.SetNode<_>
            | _ ->
                SetNewImplementation.SetEmpty.Instance
        SetNew(comparer, run root)

    member x.TryPickV(mapping : 'Key -> 'Value -> voption<'T>) =
        let rec run (mapping : OptimizedClosures.FSharpFunc<'Key, 'Value, voption<'T>>) (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l ->
                mapping.Invoke(l.Key, l.Value)
                
            | :? MapInner<'Key, 'Value> as n ->
                match run mapping n.Left with
                | ValueNone ->
                    match mapping.Invoke(n.Key, n.Value) with
                    | ValueSome _ as res -> res
                    | ValueNone -> run mapping n.Right
                | res -> 
                    res
            | _ ->
                ValueNone
        run (OptimizedClosures.FSharpFunc<_,_,_>.Adapt mapping) root
        
    member x.Pick(mapping : 'Key -> 'Value -> option<'T>) =
        match x.TryPick mapping with
        | Some k -> k
        | None -> raise <| KeyNotFoundException()
        
    member x.PickV(mapping : 'Key -> 'Value -> voption<'T>) =
        match x.TryPickV mapping with
        | ValueSome k -> k
        | ValueNone -> raise <| KeyNotFoundException()
        
    member x.Partition(predicate : 'Key -> 'Value -> bool) =
        let predicate = OptimizedClosures.FSharpFunc<_,_,_>.Adapt predicate

        let cnt = x.Count 
        let a0 = Array.zeroCreate cnt
        let a1 = Array.zeroCreate cnt
        x.CopyToV(a0, 0)

        let mutable i1 = 0
        let mutable i0 = 0
        for i in 0 .. cnt - 1 do
            let struct(k,v) = a0.[i]
            if predicate.Invoke(k, v) then 
                a0.[i0] <- struct(k,v)
                i0 <- i0 + 1
            else
                a1.[i1] <- struct(k,v)
                i1 <- i1 + 1

        MapNew.CreateTree(comparer, a0, i0), MapNew.CreateTree(comparer, a1, i1)

    member x.ContainsKey(key : 'Key) =
        let rec contains (cmp : IComparer<_>) key (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c > 0 then contains cmp key n.Right
                elif c < 0 then contains cmp key n.Left
                else true
            | :? MapLeaf<'Key, 'Value> as n ->
                let c = cmp.Compare(key, n.Key)
                if c = 0 then true
                else false

            | _ ->
                false
        contains comparer key root

    member x.GetEnumerator() = new MapNewEnumerator<_,_>(root)

    member x.ToList() = 
        let rec toList acc (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                toList ((n.Key, n.Value) :: toList acc n.Right) n.Left
            | :? MapLeaf<'Key, 'Value> as n ->
                (n.Key, n.Value) :: acc
            | _ ->
                acc
        toList [] root

    member x.ToListV() = 
        let rec toList acc (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                toList (struct(n.Key, n.Value) :: toList acc n.Right) n.Left
            | :? MapLeaf<'Key, 'Value> as n ->
                struct(n.Key, n.Value) :: acc
            | _ ->
                acc
        toList [] root
       
    member x.ToArray() =
        let arr = Array.zeroCreate x.Count
        let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- (n.Key, n.Value)
                copyTo arr (index + 1) n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                arr.[index] <- (n.Key, n.Value)
                index + 1
            | _ ->
                index

        copyTo arr 0 root |> ignore<int>
        arr

    member x.ToArrayV() =
        let arr = Array.zeroCreate x.Count
        let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- struct(n.Key, n.Value)
                copyTo arr (index + 1) n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                arr.[index] <- struct(n.Key, n.Value)
                index + 1
            | _ ->
                index

        copyTo arr 0 root |> ignore<int>
        arr

    member x.CopyTo(array : ('Key * 'Value)[], startIndex : int) =
        if startIndex < 0 || startIndex + x.Count > array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
        let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- (n.Key, n.Value)
                copyTo arr (index + 1) n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                arr.[index] <- (n.Key, n.Value)
                index + 1
            | _ ->
                index
        copyTo array startIndex root |> ignore<int>

    member x.CopyToV(array : struct('Key * 'Value)[], startIndex : int) =
        if startIndex < 0 || startIndex + x.Count > array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
        let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
            match n with
            | :? MapInner<'Key, 'Value> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- struct(n.Key, n.Value)
                copyTo arr (index + 1) n.Right
            | :? MapLeaf<'Key, 'Value> as n ->
                arr.[index] <- struct(n.Key, n.Value)
                index + 1
            | _ ->
                index
        copyTo array startIndex root |> ignore<int>

    member x.GetViewBetween(minInclusive : 'Key, maxInclusive : 'Key) = 
        MapNew(comparer, root.GetViewBetween(comparer, minInclusive, true, maxInclusive, true))
       
    member x.GetSlice(min : option<'Key>, max : option<'Key>) =
        match min with
        | Some min ->
            match max with
            | Some max ->
                x.GetViewBetween(min, max)
            | None ->
                x.WithMin min
        | None ->
            match max with
            | Some max ->
                x.WithMax max
            | None ->
                x

    member x.WithMin(minInclusive : 'Key) = 
        MapNew(comparer, root.WithMin(comparer, minInclusive, true))
        
    member x.WithMax(maxInclusive : 'Key) = 
        MapNew(comparer, root.WithMax(comparer, maxInclusive, true))

    member x.TryMinKeyValue() = 
        let rec run (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l -> 
                Some (l.Key, l.Value)
            | :? MapInner<'Key, 'Value> as n ->
                if n.Left.Count = 0 then Some (n.Key, n.Value)
                else run n.Left
            | _ ->
                None
        
        run root

    member x.TryMinKeyValueV() =
        let rec run (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l -> 
                ValueSome struct(l.Key, l.Value)
            | :? MapInner<'Key, 'Value> as n ->
                if n.Left.Count = 0 then ValueSome struct(n.Key, n.Value)
                else run n.Left
            | _ ->
                ValueNone
        run root

    member x.TryMaxKeyValue() =
        let rec run (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l -> 
                Some (l.Key, l.Value)
            | :? MapInner<'Key, 'Value> as n ->
                if n.Right.Count = 0 then Some (n.Key, n.Value)
                else run n.Right
            | _ ->
                None
        
        run root

    member x.TryMaxKeyValueV() = 
        let rec run (node : MapNode<'Key, 'Value>) =
            match node with
            | :? MapLeaf<'Key, 'Value> as l -> 
                ValueSome struct(l.Key, l.Value)
            | :? MapInner<'Key, 'Value> as n ->
                if n.Right.Count = 0 then ValueSome struct(n.Key, n.Value)
                else run n.Right
            | _ ->
                ValueNone
        run root

    member x.Change(key : 'Key, update : option<'Value> -> option<'Value>) =
        MapNew(comparer, root.Change(comparer, key, update))
        
    member x.ChangeV(key : 'Key, update : voption<'Value> -> voption<'Value>) =
        MapNew(comparer, root.ChangeV(comparer, key, update))

    member x.TryAt(index : int) =
        if index < 0 || index >= root.Count then None
        else 
            let rec search (index : int) (node : MapNode<'Key, 'Value>) =
                match node with
                | :? MapLeaf<'Key, 'Value> as l ->
                    if index = 0 then Some(l.Key, l.Value)
                    else None
                | :? MapInner<'Key, 'Value> as n ->
                    let lc = index - n.Left.Count
                    if lc < 0 then search index n.Left
                    elif lc > 0 then search (lc - 1) n.Right
                    else Some (n.Key, n.Value)
                | _ ->
                    None
            search index root
        
    member x.TryAtV(index : int) =
        if index < 0 || index >= root.Count then ValueNone
        else 
            let rec search (index : int) (node : MapNode<'Key, 'Value>) =
                match node with
                | :? MapLeaf<'Key, 'Value> as l ->
                    if index = 0 then ValueSome(struct(l.Key, l.Value))
                    else ValueNone
                | :? MapInner<'Key, 'Value> as n ->
                    let lc = index - n.Left.Count
                    if lc < 0 then search index n.Left
                    elif lc > 0 then search (lc - 1) n.Right
                    else ValueSome (struct(n.Key, n.Value))
                | _ ->
                    ValueNone
            search index root

    member x.CompareTo(other : MapNew<'Key, 'Value>) =
        let mutable le = x.GetEnumerator()
        let mutable re = other.GetEnumerator()

        let mutable result = 0 
        let mutable run = true
        while run do
            if le.MoveNext() then
                if re.MoveNext() then
                    let c = comparer.Compare(le.Current.Key, re.Current.Key)
                    if c <> 0 then 
                        result <- c
                        run <- false
                    else
                        let c = Unchecked.compare le.Current.Value re.Current.Value
                        if c <> 0 then 
                            result <- c
                            run <- false
                else
                    result <- 1
                    run <- false
            elif re.MoveNext() then
                result <- -1
                run <- false
            else
                run <- false
        result

    override x.GetHashCode() =
        hash root

    override x.Equals o =
        match o with
        | :? MapNew<'Key, 'Value> as o -> equals comparer root o.Root
        | _ -> false

    interface System.IComparable with
        member x.CompareTo o = x.CompareTo (o :?> MapNew<_,_>)
            
    interface System.IComparable<MapNew<'Key, 'Value>> with
        member x.CompareTo o = x.CompareTo o

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
        member x.CopyTo(array : KeyValuePair<'Key, 'Value>[], startIndex : int) =
            if startIndex < 0 || startIndex + x.Count > array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
            let rec copyTo (arr : array<_>) (index : int) (n : MapNode<_,_>) =
                match n with
                | :? MapInner<'Key, 'Value> as n ->
                    let index = copyTo arr index n.Left
                    arr.[index] <- KeyValuePair(n.Key, n.Value)
                    copyTo arr (index + 1) n.Right
                | :? MapLeaf<'Key, 'Value> as n ->
                    arr.[index] <- KeyValuePair(n.Key, n.Value)
                    index + 1
                | _ ->
                    index
            copyTo array startIndex root |> ignore<int>
            
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
        val mutable public Root : MapNode<'Key, 'Value>
        val mutable public Stack : list<struct(MapNode<'Key, 'Value> * bool)>
        val mutable public Value : KeyValuePair<'Key, 'Value>

        member x.Current : KeyValuePair<'Key, 'Value> = x.Value

        member x.Reset() =
            if x.Root.Height > 0 then
                x.Stack <- [struct(x.Root, true)]
                x.Value <- Unchecked.defaultof<_>

        member x.Dispose() =
            x.Root <- MapEmpty.Instance
            x.Stack <- []
            x.Value <- Unchecked.defaultof<_>
                
        member inline private x.MoveNext(deep : bool, top : MapNode<'Key, 'Value>) =
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
                            x.Stack <- struct(n :> MapNode<_,_>, false) :: x.Stack
                            top <- n.Left
                    else    
                        x.Value <- KeyValuePair(n.Key, n.Value)
                        run <- false

                | _ ->
                    failwith "empty node"
    
            
        member x.MoveNext() : bool =
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



        new(r : MapNode<'Key, 'Value>) =
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

and internal MapDebugView<'Key, 'Value when 'Key : comparison> =

    [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
    val mutable public Entries : KeyValuePairDebugFriendly<'Key, 'Value>[]

    new(m : MapNew<'Key, 'Value>) =
        {
            Entries = Seq.toArray (Seq.map KeyValuePairDebugFriendly (Seq.truncate 10000 m))
        }
        
and 
    [<DebuggerDisplay("{keyValue.Value}", Name = "[{keyValue.Key}]", Type = "")>]
    internal  KeyValuePairDebugFriendly<'Key, 'Value>(keyValue : KeyValuePair<'Key, 'Value>) =

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

    [<CompiledName("Keys")>]
    let inline keys (map : MapNew<'Key, 'Value>) = map.Keys()

    [<CompiledName("WithMin")>]
    let inline withMin (minInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.WithMin(minInclusive)
    
    [<CompiledName("WithMax")>]
    let inline withMax (maxInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.WithMax(maxInclusive)
    
    [<CompiledName("WithRange")>]
    let inline withRange (minInclusive : 'Key) (maxInclusive : 'Key) (map : MapNew<'Key, 'Value>) = map.GetViewBetween(minInclusive, maxInclusive)
    
    [<CompiledName("Union")>]
    let inline union (map1 : MapNew<'Key, 'Value>) (map2 : MapNew<'Key, 'Value>) = MapNew.Union(map1, map2)
    
    [<CompiledName("UnionMany")>]
    let inline unionMany (maps : #seq<MapNew<'Key, 'Value>>) =
        use e = (maps :> seq<_>).GetEnumerator()
        if e.MoveNext() then
            let mutable m = e.Current
            while e.MoveNext() do
                m <- union m e.Current
            m
        else
            empty
    
    [<CompiledName("UnionWith")>]
    let inline unionWith (resolve : 'Key -> 'Value -> 'Value -> 'Value) (map1 : MapNew<'Key, 'Value>) (map2 : MapNew<'Key, 'Value>) = MapNew.UnionWith(map1, map2, resolve)
    
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
    let inline tryAtV (index : int) (map : MapNew<'Key, 'Value>) = 
        map.TryAtV index
        
    [<CompiledName("Find")>]
    let inline find (key : 'Key) (map : MapNew<'Key, 'Value>) =
        map.Find key
        
    [<CompiledName("FindKey")>]
    let inline findKey (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) =
        map.FindKey(predicate)
        
    [<CompiledName("TryFindKey")>]
    let inline tryFindKey (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) =
        map.TryFindKey(predicate)
        
    [<CompiledName("TryFindKeyValue")>]
    let inline tryFindKeyV (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) =
        map.TryFindKeyV(predicate)

    [<CompiledName("TryPick")>]
    let inline tryPick (mapping : 'Key -> 'Value -> option<'T>) (map : MapNew<'Key, 'Value>) =
        map.TryPick(mapping)
        
    [<CompiledName("TryPickValue")>]
    let inline tryPickV (mapping : 'Key -> 'Value -> voption<'T>) (map : MapNew<'Key, 'Value>) =
        map.TryPickV(mapping)
        
    [<CompiledName("Pick")>]
    let inline pick (mapping : 'Key -> 'Value -> option<'T>) (map : MapNew<'Key, 'Value>) =
        map.Pick(mapping)

    [<CompiledName("PickValue")>]
    let inline pickV (mapping : 'Key -> 'Value -> voption<'T>) (map : MapNew<'Key, 'Value>) =
        map.PickV(mapping)
        
    [<CompiledName("Partition")>]
    let inline partition (predicate : 'Key -> 'Value -> bool) (map : MapNew<'Key, 'Value>) =
        map.Partition(predicate)

