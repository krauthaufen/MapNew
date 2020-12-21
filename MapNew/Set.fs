namespace MapNew

open System
open System.Linq
open System.Collections.Generic

module SetNewImplementation = 

    

    [<AbstractClass>]
    type SetNode<'T>() =
        abstract member Count : int
        abstract member Height : int

        abstract member Add : comparer : IComparer<'T> * value : 'T -> SetNode<'T>
        abstract member AddIfNotPresent : comparer : IComparer<'T> * value : 'T -> SetNode<'T>
        abstract member Remove : comparer : IComparer<'T> * key : 'T -> SetNode<'T>
        abstract member AddInPlace : comparer : IComparer<'T> * value : 'T -> SetNode<'T>
        abstract member TryRemove : comparer : IComparer<'T> * key : 'T -> option<SetNode<'T>>
        abstract member TryRemoveV : comparer : IComparer<'T> * key : 'T -> voption<SetNode<'T>>

        abstract member Filter : predicate : ('T -> bool) -> SetNode<'T>

        abstract member UnsafeRemoveHeadV : unit -> struct('T * SetNode<'T>)
        abstract member UnsafeRemoveTailV : unit -> struct(SetNode<'T> * 'T)
        
        abstract member GetViewBetween : comparer : IComparer<'T> * min : 'T * minInclusive : bool * max : 'T * maxInclusive : bool -> SetNode<'T>
        abstract member WithMin : comparer : IComparer<'T> * min : 'T * minInclusive : bool -> SetNode<'T>
        abstract member WithMax : comparer : IComparer<'T> * max : 'T * maxInclusive : bool -> SetNode<'T>
        abstract member SplitV : comparer : IComparer<'T> * value : 'T -> struct(SetNode<'T> * SetNode<'T> * bool)
        
        abstract member Change : comparer : IComparer<'T> * value : 'T * (bool -> bool) -> SetNode<'T>
        
    and [<Sealed>]
        SetEmpty<'T> private() =
        inherit SetNode<'T>()

        static let instance = SetEmpty<'T>() :> SetNode<'T>

        static member Instance : SetNode<'T> = instance

        override x.Count = 0
        override x.Height = 0
        override x.Add(_, value) =
            SetLeaf(value) :> SetNode<'T>
            
        override x.AddIfNotPresent(_, value) =
            SetLeaf(value) :> SetNode<'T>

        override x.AddInPlace(_, value) =
            SetLeaf(value) :> SetNode<'T>

        override x.Remove(_,_) =
            x :> SetNode<'T>
            
        override x.TryRemove(_,_) =
            None

        override x.TryRemoveV(_,_) =
            ValueNone

        override x.Filter(_) = x :> SetNode<'T>

        override x.UnsafeRemoveHeadV() = failwith "empty"
        override x.UnsafeRemoveTailV() = failwith "empty"

        override x.GetViewBetween(_comparer : IComparer<'T>, _min : 'T, _minInclusive : bool, _max : 'T, _maxInclusive : bool) =
            x :> SetNode<'T>
        override x.WithMin(_comparer : IComparer<'T>, _min : 'T, _minInclusive : bool) =
            x :> SetNode<'T>
        override x.WithMax(_comparer : IComparer<'T>, _max : 'T, _maxInclusive : bool) =
            x :> SetNode<'T>

        override x.SplitV(_,_) =
            (x :> SetNode<'T>, x :> SetNode<'T>, false)

        override x.Change(_comparer, value, update) =
            match update false with
            | false -> x :> SetNode<'T>
            | true -> SetLeaf(value) :> SetNode<'T>
            
    and [<Sealed>]
        SetLeaf<'T> =
        class 
            inherit SetNode<'T>
            val mutable public Value : 'T

            override x.Height =
                1

            override x.Count =
                1

            override x.Add(comparer, value) =
                let c = comparer.Compare(value, x.Value)

                if c > 0 then
                    SetInner(x, value, SetEmpty.Instance) :> SetNode<'T>
                elif c < 0 then
                    SetInner(SetEmpty.Instance, value, x) :> SetNode<'T>
                else
                    SetLeaf(value) :> SetNode<'T>
                    
            override x.AddIfNotPresent(comparer, value) =
                let c = comparer.Compare(value, x.Value)

                if c > 0 then
                    SetInner(x, value, SetEmpty.Instance) :> SetNode<'T>
                elif c < 0 then
                    SetInner(SetEmpty.Instance, value, x) :> SetNode<'T>
                else
                    x :> SetNode<'T>
                    
            override x.AddInPlace(comparer, value) =
                let c = comparer.Compare(value, x.Value)

                if c > 0 then   
                    SetInner(x, value, SetEmpty.Instance) :> SetNode<'T>
                elif c < 0 then
                    SetInner(SetEmpty.Instance, value, x) :> SetNode<'T>
                else
                    x.Value <- value
                    x :> SetNode<'T>

                
            override x.Remove(comparer, value) =
                if comparer.Compare(value, x.Value) = 0 then SetEmpty.Instance
                else x :> SetNode<'T>
                
            override x.TryRemove(comparer, value) =
                if comparer.Compare(value, x.Value) = 0 then Some SetEmpty.Instance
                else None
                
            override x.TryRemoveV(comparer, value) =
                if comparer.Compare(value, x.Value) = 0 then ValueSome SetEmpty.Instance
                else ValueNone

            override x.Filter(predicate : 'T -> bool) =
                if predicate x.Value then
                    x :> SetNode<'T>
                else
                    SetEmpty.Instance

            override x.UnsafeRemoveHeadV() =
                struct(x.Value, SetEmpty<'T>.Instance)

            override x.UnsafeRemoveTailV() =
                struct(SetEmpty<'T>.Instance, x.Value)

            
            override x.GetViewBetween(comparer : IComparer<'T>, min : 'T, minInclusive : bool, max : 'T, maxInclusive : bool) =
                let cMin = comparer.Compare(x.Value, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    let cMax = comparer.Compare(x.Value, max)
                    if (if maxInclusive then cMax <= 0 else cMax < 0) then
                        x :> SetNode<'T>
                    else
                        SetEmpty.Instance
                else
                    SetEmpty.Instance
                    
            override x.WithMin(comparer : IComparer<'T>, min : 'T, minInclusive : bool) =
                let cMin = comparer.Compare(x.Value, min)
                if (if minInclusive then cMin >= 0 else cMin > 0) then
                    x :> SetNode<'T>
                else
                    SetEmpty.Instance
                    
            override x.WithMax(comparer : IComparer<'T>, max : 'T, maxInclusive : bool) =
                let cMax = comparer.Compare(x.Value, max)
                if (if maxInclusive then cMax <= 0 else cMax < 0) then
                    x :> SetNode<'T>
                else
                    SetEmpty.Instance
                    
            override x.SplitV(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(x.Value, value)
                if c > 0 then
                    struct(SetEmpty.Instance, x :> SetNode<'T>, false)
                elif c < 0 then
                    struct(x :> SetNode<'T>, SetEmpty.Instance, false)
                else
                    struct(SetEmpty.Instance, SetEmpty.Instance, true)
                 
            override x.Change(comparer, value, update) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    match update false with
                    | false -> x :> SetNode<'T>
                    | true -> SetInner(x, value, SetEmpty.Instance) :> SetNode<'T>
                elif c < 0 then
                    match update false with
                    | false -> x :> SetNode<'T>
                    | true -> SetInner(SetEmpty.Instance, value, x) :> SetNode<'T>
                else    
                    match update true with
                    | true ->
                        x :> SetNode<'T>
                    | false ->
                        SetEmpty.Instance

            new(v : 'T) = { Value = v}
        end

    and [<Sealed>]
        SetInner<'T> =
        class 
            inherit SetNode<'T>

            val mutable public Left : SetNode<'T>
            val mutable public Right : SetNode<'T>
            val mutable public Value : 'T
            val mutable public _Count : int
            val mutable public _Height : int

            static member Create(l : SetNode<'T>, v : 'T, r : SetNode<'T>) =
                let lh = l.Height
                let rh = r.Height
                let b = rh - lh

                if lh = 0 && rh = 0 then
                    SetLeaf(v) :> SetNode<'T>
                elif b > 2 then
                    // right heavy
                    let r = r :?> SetInner<'T> // must work
                    
                    if r.Right.Height >= r.Left.Height then
                        // right right case
                        SetInner.Create(
                            SetInner.Create(l, v, r.Left),
                            r.Value,
                            r.Right
                        ) 
                    else
                        // right left case
                        match r.Left with
                        | :? SetInner<'T> as rl ->
                            //let rl = r.Left :?> SetInner<'T>
                            let t1 = l
                            let t2 = rl.Left
                            let t3 = rl.Right
                            let t4 = r.Right

                            SetInner.Create(
                                SetInner.Create(t1, v, t2),
                                rl.Value,
                                SetInner.Create(t3, r.Value, t4)
                            )
                        | _ ->
                            failwith "impossible"
                            

                elif b < -2 then   
                    let l = l :?> SetInner<'T> // must work
                    
                    if l.Left.Height >= l.Right.Height then
                        SetInner.Create(
                            l.Left,
                            l.Value,
                            SetInner.Create(l.Right, v, r)
                        )

                    else
                        match l.Right with
                        | :? SetInner<'T> as lr -> 
                            let t1 = l.Left
                            let t2 = lr.Left
                            let t3 = lr.Right
                            let t4 = r
                            SetInner.Create(
                                SetInner.Create(t1, l.Value, t2),
                                lr.Value,
                                SetInner.Create(t3, v, t4)
                            )
                        | _ ->
                            failwith "impossible"

                else
                    SetInner(l, v, r) :> SetNode<'T>

            static member Join(l : SetNode<'T>, r : SetNode<'T>) =
                if l.Height = 0 then r
                elif r.Height = 0 then l
                elif l.Height > r.Height then
                    let struct(l1, v) = l.UnsafeRemoveTailV()
                    SetInner.Create(l1, v, r)
                else
                    let struct(v, r1) = r.UnsafeRemoveHeadV()
                    SetInner.Create(l, v, r1)

            override x.Count =
                x._Count

            override x.Height =
                x._Height
            
            override x.Add(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    SetInner.Create(
                        x.Left, 
                        x.Value,
                        x.Right.Add(comparer, value)
                    )
                elif c < 0 then
                    SetInner.Create(
                        x.Left.Add(comparer, value), 
                        x.Value,
                        x.Right
                    )
                else
                    SetInner(
                        x.Left, 
                        value,
                        x.Right
                    ) :> SetNode<'T>
               
            override x.AddIfNotPresent(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    SetInner.Create(
                        x.Left, 
                        x.Value,
                        x.Right.AddIfNotPresent(comparer, value)
                    )
                elif c < 0 then
                    SetInner.Create(
                        x.Left.AddIfNotPresent(comparer, value), 
                        x.Value,
                        x.Right
                    )
                else
                    x :> SetNode<'T>
                     
            override x.AddInPlace(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    x.Right <- x.Right.AddInPlace(comparer, value)

                    let bal = abs (x.Right.Height - x.Left.Height)
                    if bal < 2 then 
                        x._Height <- 1 + max x.Left.Height x.Right.Height
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> SetNode<'T>
                    else 
                        SetInner.Create(
                            x.Left, 
                            x.Value,
                            x.Right
                        )
                elif c < 0 then
                    x.Left <- x.Left.AddInPlace(comparer, value)
                    
                    let bal = abs (x.Right.Height - x.Left.Height)
                    if bal < 2 then 
                        x._Height <- 1 + max x.Left.Height x.Right.Height
                        x._Count <- 1 + x.Right.Count + x.Left.Count
                        x :> SetNode<'T>
                    else
                        SetInner.Create(
                            x.Left, 
                            x.Value,
                            x.Right
                        )
                else
                    x.Value <- value
                    x :> SetNode<'T>

            override x.Remove(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    SetInner.Create(
                        x.Left, 
                        x.Value,
                        x.Right.Remove(comparer, value)
                    )
                elif c < 0 then
                    SetInner.Create(
                        x.Left.Remove(comparer, value), 
                        x.Value,
                        x.Right
                    )
                else
                    SetInner.Join(x.Left, x.Right)
                    
            override x.TryRemove(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    match x.Right.TryRemoveV(comparer, value) with
                    | ValueSome newRight ->
                        let newNode = 
                            SetInner.Create(
                                x.Left, 
                                x.Value,
                                newRight
                            )
                        Some newNode
                    | ValueNone ->
                        None
                elif c < 0 then
                    match x.Left.TryRemoveV(comparer, value) with
                    | ValueSome newLeft ->
                        let newNode = 
                            SetInner.Create(
                                newLeft, 
                                x.Value,
                                x.Right
                            )
                        Some newNode
                    | ValueNone ->
                        None
                else
                    Some(SetInner.Join(x.Left, x.Right))
                           
            override x.TryRemoveV(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    match x.Right.TryRemoveV(comparer, value) with
                    | ValueSome newRight ->
                        let newNode = 
                            SetInner.Create(
                                x.Left, 
                                x.Value,
                                newRight
                            )
                        ValueSome newNode
                    | ValueNone ->
                        ValueNone
                elif c < 0 then
                    match x.Left.TryRemoveV(comparer, value) with
                    | ValueSome newLeft ->
                        let newNode = 
                            SetInner.Create(
                                newLeft, 
                                x.Value,
                                x.Right
                            )
                        ValueSome newNode
                    | ValueNone ->
                        ValueNone
                else
                    ValueSome(SetInner.Join(x.Left, x.Right))

            override x.Filter(predicate : 'T -> bool) =
                let l = x.Left.Filter(predicate)
                let self = predicate x.Value
                let r = x.Right.Filter(predicate)

                if self then
                    SetInner.Create(l, x.Value, r)
                else
                    SetInner.Join(l, r)

            override x.UnsafeRemoveHeadV() =
                if x.Left.Count = 0 then
                    struct(x.Value, x.Right)
                else
                    let struct(v,l1) = x.Left.UnsafeRemoveHeadV()
                    struct(v, SetInner.Create(l1, x.Value, x.Right))

            override x.UnsafeRemoveTailV() =   
                if x.Right.Count = 0 then
                    struct(x.Left, x.Value)
                else
                    let struct(r1,v) = x.Right.UnsafeRemoveTailV()
                    struct(SetInner.Create(x.Left, x.Value, r1), v)
                    

            override x.WithMin(comparer : IComparer<'T>, min : 'T, minInclusive : bool) =
                let c = comparer.Compare(x.Value, min)
                let greaterMin = if minInclusive then c >= 0 else c > 0
                if greaterMin then
                    SetInner.Create(
                        x.Left.WithMin(comparer, min, minInclusive),
                        x.Value,
                        x.Right
                    )
                else
                    x.Right.WithMin(comparer, min, minInclusive)

                
            override x.WithMax(comparer : IComparer<'T>, max : 'T, maxInclusive : bool) =
                let c = comparer.Compare(x.Value, max)
                let smallerMax = if maxInclusive then c <= 0 else c < 0
                if smallerMax then
                    SetInner.Create(
                        x.Left,
                        x.Value,
                        x.Right.WithMax(comparer, max, maxInclusive)
                    )
                else
                    x.Left.WithMax(comparer, max, maxInclusive)
                    
                    
            override x.SplitV(comparer : IComparer<'T>, value : 'T) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then
                    let struct(rl, rr, rv) = x.Right.SplitV(comparer, value)
                    struct(SetInner.Create(x.Left, x.Value, rl), rr, rv)
                elif c < 0 then
                    let struct(ll, lr, lv) = x.Left.SplitV(comparer, value)
                    struct(ll, SetInner.Create(lr, x.Value, x.Right), lv)
                else
                    struct(x.Left, x.Right, true)

            override x.GetViewBetween(comparer : IComparer<'T>, min : 'T, minInclusive : bool, max : 'T, maxInclusive : bool) =
                let cMin = comparer.Compare(x.Value, min)
                let cMax = comparer.Compare(x.Value, max)

                let greaterMin = if minInclusive then cMin >= 0 else cMin > 0
                let smallerMax = if maxInclusive then cMax <= 0 else cMax < 0

                if not greaterMin then
                    x.Right.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)

                elif not smallerMax then
                    x.Left.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)

                elif greaterMin && smallerMax then
                    let l = x.Left.WithMin(comparer, min, minInclusive)
                    let r = x.Right.WithMax(comparer, max, maxInclusive)
                    SetInner.Create(l, x.Value, r)

                elif greaterMin then
                    let l = x.Left.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)
                    let r = x.Right.WithMax(comparer, max, maxInclusive)
                    SetInner.Create(l, x.Value, r)

                elif smallerMax then
                    let l = x.Left.WithMin(comparer, min, minInclusive)
                    let r = x.Right.GetViewBetween(comparer, min, minInclusive, max, maxInclusive)
                    SetInner.Create(l, x.Value, r)
                    
                else
                    failwith "invalid range"

            override x.Change(comparer, value, update) =
                let c = comparer.Compare(value, x.Value)
                if c > 0 then   
                    SetInner.Create(
                        x.Left,
                        x.Value,
                        x.Right.Change(comparer, value, update)
                    )
                elif c < 0 then 
                    SetInner.Create(
                        x.Left.Change(comparer, value, update),
                        x.Value,
                        x.Right
                    )
                else    
                    match update true with
                    | true ->
                        x :> SetNode<_>
                    | false ->
                        SetInner.Join(x.Left, x.Right)
                        
            new(l : SetNode<'T>, v : 'T, r : SetNode<'T>) =
                assert(l.Count > 0 || r.Count > 0)      // not both empty
                assert(abs (r.Height - l.Height) <= 2)  // balanced
                {
                    Left = l
                    Right = r
                    Value = v
                    _Count = 1 + l.Count + r.Count
                    _Height = 1 + max l.Height r.Height
                }
        end

    
    let inline combineHash (a: int) (b: int) =
        uint32 a ^^^ uint32 b + 0x9e3779b9u + ((uint32 a) <<< 6) + ((uint32 a) >>> 2) |> int


    let hash (n : SetNode<'T>) =
        let rec hash (acc : int) (n : SetNode<'T>) =    
            match n with
            | :? SetLeaf<'T> as n ->
                combineHash acc (Unchecked.hash n.Value)

            | :? SetInner<'T> as n ->
                let acc = hash acc n.Left
                let acc = combineHash acc (Unchecked.hash n.Value)
                hash acc n.Right
            | _ ->
                acc

        hash 0 n

    let rec equals (cmp : IComparer<'T>) (l : SetNode<'T>) (r : SetNode<'T>) =
        if l.Count <> r.Count then
            false
        else
            // counts identical
            match l with
            | :? SetLeaf<'T> as l ->
                let r = r :?> SetLeaf<'T> // has to hold (r.Count = 1)
                cmp.Compare(l.Value, r.Value) = 0

            | :? SetInner<'T> as l ->
                match r with
                | :? SetInner<'T> as r ->
                    let struct(ll, lr, lv) = l.SplitV(cmp, r.Value)
                    if lv then
                        equals cmp ll r.Left &&
                        equals cmp lr r.Right
                    else
                        false
                | _ ->
                    false
            | _ ->
                true

open SetNewImplementation
open System.Diagnostics

[<DebuggerTypeProxy("Aardvark.Base.SetDebugView`1")>]
[<DebuggerDisplay("Count = {Count}")>]
[<Sealed>]
type SetNew<'T when 'T : comparison> internal(comparer : IComparer<'T>, root : SetNode<'T>) =
        
    static let defaultComparer = LanguagePrimitives.FastGenericComparer<'T>
    static let empty = SetNew<'T>(defaultComparer, SetEmpty.Instance)

    [<NonSerialized>]
    // This type is logically immutable. This field is only mutated during deserialization.
    let mutable comparer = comparer
    
    [<NonSerialized>]
    // This type is logically immutable. This field is only mutated during deserialization.
    let mutable root = root

    // WARNING: The compiled name of this field may never be changed because it is part of the logical
    // WARNING: permanent serialization format for this type.
    let mutable serializedData = null

    static let toArray(root : SetNode<'T>) =
        let arr = Array.zeroCreate root.Count
        let rec copyTo (arr : array<_>) (index : int) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let i = copyTo arr index n.Left
                arr.[i] <- n.Value
                
                copyTo arr (i+1) n.Right
            | :? SetLeaf<'T> as n ->
                arr.[index] <- n.Value
                index + 1
            | _ ->
                index

        copyTo arr 0 root |> ignore<int>
        arr

    static let fromArray (elements : 'T[]) =
        let cmp = defaultComparer
        match elements.Length with
        | 0 -> 
            SetEmpty.Instance
        | 1 ->
            let v = elements.[0]
            SetLeaf(v) :> SetNode<'T>
        | 2 -> 
            let v0 = elements.[0]
            let v1 = elements.[1]
            let c = cmp.Compare(v0, v1)
            if c > 0 then SetInner(SetEmpty.Instance, v1, SetLeaf(v0)) :> SetNode<'T>
            elif c < 0 then SetInner(SetLeaf(v0), v1, SetEmpty.Instance) :> SetNode<'T>
            else SetLeaf(v1):> SetNode<'T>
        | 3 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2)
        | 4 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            let v3 = elements.[3]
            SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3)
        | 5 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            let v3 = elements.[3]
            let v4 = elements.[4]
            SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3).AddInPlace(cmp, v4)
        | _ ->
            let struct(arr, len) = Sorting.sortHandleDuplicates false cmp elements elements.Length
            SetNew.CreateRoot(arr, len)

    [<System.Runtime.Serialization.OnSerializingAttribute>]
    member __.OnSerializing(context: System.Runtime.Serialization.StreamingContext) =
        ignore context
        serializedData <- toArray root

    [<System.Runtime.Serialization.OnDeserializedAttribute>]
    member __.OnDeserialized(context: System.Runtime.Serialization.StreamingContext) =
        ignore context
        comparer <- defaultComparer
        serializedData <- null
        root <- serializedData |> fromArray 

    static member Empty = empty

    static member private CreateTree(cmp : IComparer<'T>, arr : 'T[], cnt : int)=
        let rec create (arr : 'T[]) (l : int) (r : int) =
            if l = r then
                let v = arr.[l]
                SetLeaf(v) :> SetNode<'T>
            elif l > r then
                SetEmpty.Instance
            else
                let m = (l+r)/2
                let v = arr.[m]
                SetInner(
                    create arr l (m-1),
                    v,
                    create arr (m+1) r
                ) :> SetNode<'T>

        SetNew(cmp, create arr 0 (cnt-1))
  
    static member private CreateRoot(arr : 'T[], cnt : int)=
        let rec create (arr : 'T[]) (l : int) (r : int) =
            if l = r then
                let v = arr.[l]
                SetLeaf(v) :> SetNode<'T>
            elif l > r then
                SetEmpty.Instance
            else
                let m = (l+r)/2
                let v = arr.[m]
                SetInner(
                    create arr l (m-1),
                    v,
                    create arr (m+1) r
                ) :> SetNode<'T>

        create arr 0 (cnt-1)

    static member FromArray (elements : array<'T>) =
        let cmp = defaultComparer
        match elements.Length with
        | 0 -> 
            SetNew(cmp, SetEmpty.Instance)
        | 1 ->
            let v = elements.[0]
            SetNew(cmp, SetLeaf(v))
        | 2 -> 
            let v0 = elements.[0]
            let v1 = elements.[1]
            let c = cmp.Compare(v0, v1)
            if c > 0 then SetNew(cmp, SetInner(SetEmpty.Instance, v1, SetLeaf(v0)))
            elif c < 0 then SetNew(cmp, SetInner(SetLeaf(v0), v1, SetEmpty.Instance))
            else SetNew(cmp, SetLeaf(v1))
        | 3 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2))
        | 4 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            let v3 = elements.[3]
            SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3))
        | 5 ->
            let v0 = elements.[0]
            let v1 = elements.[1]
            let v2 = elements.[2]
            let v3 = elements.[3]
            let v4 = elements.[4]
            SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3).AddInPlace(cmp, v4))
        | _ ->
            let struct(arr, len) = Sorting.sortHandleDuplicates false cmp elements elements.Length
            SetNew.CreateTree(cmp, arr, len)
        
    static member FromList (elements : list<'T>) =
        let cmp = defaultComparer
        match elements with
        | [] -> 
            // cnt = 0
            SetNew(cmp, SetEmpty.Instance)

        | v0 :: rest ->
            // cnt >= 1
            match rest with
            | [] -> 
                // cnt = 1
                SetNew(cmp, SetLeaf(v0))
            | v1 :: rest ->
                // cnt >= 2
                match rest with
                | [] ->
                    // cnt = 2
                    let c = cmp.Compare(v0, v1)
                    if c < 0 then SetNew(cmp, SetInner(SetLeaf(v0), v1, SetEmpty.Instance))
                    elif c > 0 then SetNew(cmp, SetInner(SetEmpty.Instance, v1, SetLeaf(v0)))
                    else SetNew(cmp, SetLeaf(v1))
                | v2 :: rest ->
                    // cnt >= 3
                    match rest with
                    | [] ->
                        // cnt = 3
                        SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2))
                    | v3 :: rest ->
                        // cnt >= 4
                        match rest with
                        | [] ->
                            // cnt = 4
                            SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3))
                        | v4 :: rest ->
                            // cnt >= 5
                            match rest with
                            | [] ->
                                // cnt = 5
                                SetNew(cmp, SetLeaf(v0).AddInPlace(cmp, v1).AddInPlace(cmp, v2).AddInPlace(cmp, v3).AddInPlace(cmp, v4))
                            | v5 :: rest ->
                                // cnt >= 6
                                let mutable arr = Array.zeroCreate 16
                                let mutable cnt = 6
                                arr.[0] <- v0
                                arr.[1] <- v1
                                arr.[2] <- v2
                                arr.[3] <- v3
                                arr.[4] <- v4
                                arr.[5] <- v5
                                for t in rest do
                                    if cnt >= arr.Length then System.Array.Resize(&arr, arr.Length <<< 1)
                                    arr.[cnt] <- t
                                    cnt <- cnt + 1
                                    
                                let struct(arr1, cnt1) = Sorting.sortHandleDuplicates true cmp arr cnt
                                SetNew.CreateTree(cmp, arr1, cnt1)

    static member FromSeq (elements : seq<'T>) =
        match elements with
        | :? array<'T> as e -> SetNew.FromArray e
        | :? list<'T> as e -> SetNew.FromList e
        | _ ->
            let cmp = defaultComparer
            use e = elements.GetEnumerator()
            if e.MoveNext() then
                // cnt >= 1
                let t0 = e.Current
                if e.MoveNext() then
                    // cnt >= 2
                    let t1 = e.Current
                    if e.MoveNext() then
                        // cnt >= 3 
                        let t2 = e.Current
                        if e.MoveNext() then
                            // cnt >= 4
                            let t3 = e.Current
                            if e.MoveNext() then
                                // cnt >= 5
                                let t4 = e.Current
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

                                    let struct(arr1, cnt1) = Sorting.sortHandleDuplicates true cmp arr cnt
                                    SetNew.CreateTree(cmp, arr1, cnt1)

                                else
                                    // cnt = 5
                                    SetNew(cmp, SetLeaf(t0).AddInPlace(cmp, t1).AddInPlace(cmp, t2).AddInPlace(cmp, t3).AddInPlace(cmp, t4))

                            else
                                // cnt = 4
                                SetNew(cmp, SetLeaf(t0).AddInPlace(cmp, t1).AddInPlace(cmp, t2).AddInPlace(cmp, t3))
                        else
                            SetNew(cmp, SetLeaf(t0).AddInPlace(cmp, t1).AddInPlace(cmp, t2))
                    else
                        // cnt = 2
                        let c = cmp.Compare(t0, t1)
                        if c < 0 then SetNew(cmp, SetInner(SetLeaf(t0), t1, SetEmpty.Instance))
                        elif c > 0 then SetNew(cmp, SetInner(SetEmpty.Instance, t1, SetLeaf(t0)))
                        else SetNew(cmp, SetLeaf(t1))
                else
                    // cnt = 1
                    SetNew(cmp, SetLeaf(t0))

            else
                SetNew(cmp, SetEmpty.Instance)

    static member Union(l : SetNew<'T>, r : SetNew<'T>) =
        let rec union (cmp : IComparer<'T>) (l : SetNode<'T>) (r : SetNode<'T>) =
            match l with
            | :? SetEmpty<'T> ->  
                r
            | :? SetLeaf<'T> as l ->
                r.AddIfNotPresent(cmp, l.Value)
            | :? SetInner<'T> as l ->
                match r with
                | :? SetEmpty<'T> ->
                    l :> SetNode<'T>
                | :? SetLeaf<'T> as r ->
                    l.Add(cmp, r.Value)
                | :? SetInner<'T> as r ->
                    if l.Count > r.Count then
                        let struct(rl, rr, _rv) = r.SplitV(cmp, l.Value)
                        let r = ()
                        SetInner.Create(
                            union cmp l.Left rl, 
                            l.Value, 
                            union cmp l.Right rr
                        )
                    else
                        let struct(ll, lr, _lv) = l.SplitV(cmp, r.Value)
                        let l = ()
                        SetInner.Create(
                            union cmp ll r.Left, 
                            r.Value, 
                            union cmp lr r.Right
                        ) 
                | _ ->
                    failwith "unexpected node"
            | _ ->
                failwith "unexpected node"

        let cmp = defaultComparer
        SetNew(cmp, union cmp l.Root r.Root)
        
    static member Intersect(l : SetNew<'T>, r : SetNew<'T>) =
        let rec contains (cmp : IComparer<_>) value (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c > 0 then contains cmp value n.Right
                elif c < 0 then contains cmp value n.Left
                else true
            | :? SetLeaf<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c = 0 then true
                else false

            | _ ->
                false

        let rec intersect (cmp : IComparer<'T>) (l : SetNode<'T>) (r : SetNode<'T>) =
            match l with
            | :? SetEmpty<'T> ->  
                SetEmpty.Instance
            | :? SetLeaf<'T> as l ->
                if contains cmp l.Value r then l :> SetNode<_>
                else SetEmpty.Instance
            | :? SetInner<'T> as l ->
                match r with
                | :? SetEmpty<'T> ->
                    SetEmpty.Instance
                | :? SetLeaf<'T> as r ->
                    if contains cmp r.Value l then r :> SetNode<_>
                    else SetEmpty.Instance
                | :? SetInner<'T> as r ->
                    if l.Count > r.Count then
                        let struct(rl, rr, rv) = r.SplitV(cmp, l.Value)
                        let r = ()
                        if rv then 
                            SetInner.Create(
                                intersect cmp l.Left rl, 
                                l.Value, 
                                intersect cmp l.Right rr
                            )
                        else
                            SetInner.Join(
                                intersect cmp l.Left rl, 
                                intersect cmp l.Right rr
                            )

                    else
                        let struct(ll, lr, lv) = l.SplitV(cmp, r.Value)
                        let l = ()
                        if lv then
                            SetInner.Create(
                                intersect cmp ll r.Left, 
                                r.Value, 
                                intersect cmp lr r.Right
                            ) 
                        else     
                            SetInner.Join(
                                intersect cmp ll r.Left, 
                                intersect cmp lr r.Right
                            ) 
                | _ ->
                    failwith "unexpected node"
            | _ ->
                failwith "unexpected node"

        let cmp = defaultComparer
        SetNew(cmp, intersect cmp l.Root r.Root)

    static member Difference(l : SetNew<'T>, r : SetNew<'T>) =
        let rec contains (cmp : IComparer<_>) value (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c > 0 then contains cmp value n.Right
                elif c < 0 then contains cmp value n.Left
                else true
            | :? SetLeaf<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c = 0 then true
                else false

            | _ ->
                false

        let rec difference (cmp : IComparer<'T>) (l : SetNode<'T>) (r : SetNode<'T>) =
            match r with
            | :? SetEmpty<'T> ->
                l

            | :? SetLeaf<'T> as r ->
                match l.TryRemoveV(cmp, r.Value) with
                | ValueSome n -> n
                | ValueNone -> l
                
            | :? SetInner<'T> as r ->
                failwith "implement me"

            | _ ->
                l // unreachable

        let cmp = defaultComparer
        SetNew(cmp, difference cmp l.Root r.Root)

    member x.Count = root.Count
    member x.Root = root

    member x.Add(value : 'T) =
        SetNew(comparer, root.Add(comparer, value))
          
    member x.Remove(key : 'T) =
        SetNew(comparer, root.Remove(comparer, key))
        
    member x.Iter(action : 'T -> unit) =
        let rec iter (action : 'T -> unit) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                iter action n.Left
                action n.Value
                iter action n.Right
            | :? SetLeaf<'T> as n ->
                action n.Value
            | _ ->
                ()
        iter action root

    member x.Filter(predicate : 'T -> bool) =
        SetNew(comparer, root.Filter(predicate))

    member x.Exists(predicate : 'T -> bool) =
        let rec exists (predicate : 'T -> bool) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                exists predicate n.Left ||
                predicate n.Value ||
                exists predicate n.Right
            | :? SetLeaf<'T> as n ->
                predicate n.Value
            | _ ->
                false
        exists predicate root
        
    member x.Forall(predicate : 'T -> bool) =
        let rec forall (predicate : 'T -> bool) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                forall predicate n.Left &&
                predicate n.Value &&
                forall predicate n.Right
            | :? SetLeaf<'T> as n ->
                predicate n.Value
            | _ ->
                true
        forall predicate root

    member x.Fold(folder : 'State -> 'T -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_>.Adapt folder

        let rec fold (folder : OptimizedClosures.FSharpFunc<_,_,_>) seed (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let s1 = fold folder seed n.Left
                let s2 = folder.Invoke(s1, n.Value)
                fold folder s2 n.Right
            | :? SetLeaf<'T> as n ->
                folder.Invoke(seed, n.Value)
            | _ ->
                seed

        fold folder seed root
        
    member x.FoldBack(folder : 'T -> 'State -> 'State, seed : 'State) =
        let folder = OptimizedClosures.FSharpFunc<_,_,_>.Adapt folder

        let rec foldBack (folder : OptimizedClosures.FSharpFunc<_,_,_>) seed (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let s1 = foldBack folder seed n.Right
                let s2 = folder.Invoke(n.Value, s1)
                foldBack folder s2 n.Left
            | :? SetLeaf<'T> as n ->
                folder.Invoke(n.Value, seed)
            | _ ->
                seed

        foldBack folder seed root
        
    member x.TryPick(mapping : 'T -> option<'U>) =
        let rec run (mapping : 'T -> option<'U>) (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l ->
                mapping l.Value
                
            | :? SetInner<'T> as n ->
                match run mapping n.Left with
                | None ->
                    match mapping n.Value with
                    | Some _ as res -> res
                    | None -> run mapping n.Right
                | res -> 
                    res
            | _ ->
                None
        run mapping root
        
    member x.TryPickV(mapping : 'T -> voption<'U>) =
        let rec run (mapping : 'T -> voption<'U>) (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l ->
                mapping l.Value
                
            | :? SetInner<'T> as n ->
                match run mapping n.Left with
                | ValueNone ->
                    match mapping n.Value with
                    | ValueSome _ as res -> res
                    | ValueNone -> run mapping n.Right
                | res -> 
                    res
            | _ ->
                ValueNone
        run mapping root
        
    member x.Pick(mapping : 'T -> option<'U>) =
        match x.TryPick mapping with
        | Some v -> v
        | None -> raise <| KeyNotFoundException()
        
    member x.PickV(mapping : 'T -> voption<'U>) =
        match x.TryPickV mapping with
        | ValueSome v -> v
        | ValueNone -> raise <| KeyNotFoundException()

    member x.Partition(predicate : 'T -> bool) =

        let cnt = x.Count 
        let a0 = Array.zeroCreate cnt
        let a1 = Array.zeroCreate cnt
        x.CopyTo(a0, 0)

        let mutable i1 = 0
        let mutable i0 = 0
        for i in 0 .. cnt - 1 do
            let v = a0.[i]
            if predicate v then 
                a0.[i0] <- v
                i0 <- i0 + 1
            else
                a1.[i1] <- v
                i1 <- i1 + 1

        SetNew.CreateTree(comparer, a0, i0), SetNew.CreateTree(comparer, a1, i1)

    member x.Contains(value : 'T) =
        let rec contains (cmp : IComparer<_>) value (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c > 0 then contains cmp value n.Right
                elif c < 0 then contains cmp value n.Left
                else true
            | :? SetLeaf<'T> as n ->
                let c = cmp.Compare(value, n.Value)
                if c = 0 then true
                else false

            | _ ->
                false
        contains comparer value root

    member x.GetEnumerator() = new SetNewEnumerator<_>(root)

    member x.ToList() = 
        let rec toList acc (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                toList (n.Value :: toList acc n.Right) n.Left
            | :? SetLeaf<'T> as n ->
                n.Value :: acc
            | _ ->
                acc
        toList [] root

    member x.ToArray() =
        let arr = Array.zeroCreate x.Count
        let rec copyTo (arr : array<_>) (index : int) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- n.Value
                copyTo arr (index + 1) n.Right
            | :? SetLeaf<'T> as n ->
                arr.[index] <- n.Value
                index + 1
            | _ ->
                index

        copyTo arr 0 root |> ignore<int>
        arr

    member x.CopyTo(array : 'T[], startIndex : int) =
        if startIndex < 0 || startIndex + x.Count > array.Length then raise <| System.IndexOutOfRangeException("Map.CopyTo")
        let rec copyTo (arr : array<_>) (index : int) (n : SetNode<'T>) =
            match n with
            | :? SetInner<'T> as n ->
                let index = copyTo arr index n.Left
                arr.[index] <- n.Value
                copyTo arr (index + 1) n.Right
            | :? SetLeaf<'T> as n ->
                arr.[index] <- n.Value
                index + 1
            | _ ->
                index
        copyTo array startIndex root |> ignore<int>

    member x.GetViewBetween(minInclusive : 'T, maxInclusive : 'T) = 
        SetNew(comparer, root.GetViewBetween(comparer, minInclusive, true, maxInclusive, true))
       
    member x.GetSlice(min : option<'T>, max : option<'T>) =
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

    member x.WithMin(minInclusive : 'T) = 
        SetNew(comparer, root.WithMin(comparer, minInclusive, true))
        
    member x.WithMax(maxInclusive : 'T) = 
        SetNew(comparer, root.WithMax(comparer, maxInclusive, true))

    member x.TryMin() = 
        let rec run (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l -> 
                Some l.Value
            | :? SetInner<'T> as n ->
                if n.Left.Count = 0 then Some n.Value
                else run n.Left
            | _ ->
                None
        
        run root

    member x.TryMinV() =
        let rec run (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l -> 
                ValueSome l.Value
            | :? SetInner<'T> as n ->
                if n.Left.Count = 0 then ValueSome n.Value
                else run n.Left
            | _ ->
                ValueNone
        run root

    member x.TryMax() =
        let rec run (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l -> 
                Some l.Value
            | :? SetInner<'T> as n ->
                if n.Right.Count = 0 then Some n.Value
                else run n.Right
            | _ ->
                None
        
        run root

    member x.TryMaxV() = 
        let rec run (node : SetNode<'T>) =
            match node with
            | :? SetLeaf<'T> as l -> 
                ValueSome l.Value
            | :? SetInner<'T> as n ->
                if n.Right.Count = 0 then ValueSome n.Value
                else run n.Right
            | _ ->
                ValueNone
        run root

    member x.Change(key : 'T, update : bool -> bool) =
        SetNew(comparer, root.Change(comparer, key, update))
        
    member x.TryAt(index : int) =
        if index < 0 || index >= root.Count then None
        else 
            let rec search (index : int) (node : SetNode<'T>) =
                match node with
                | :? SetLeaf<'T> as l ->
                    if index = 0 then Some l.Value
                    else None
                | :? SetInner<'T> as n ->
                    let lc = index - n.Left.Count
                    if lc < 0 then search index n.Left
                    elif lc > 0 then search (lc - 1) n.Right
                    else Some n.Value
                | _ ->
                    None
            search index root
        
    member x.TryAtV(index : int) =
        if index < 0 || index >= root.Count then ValueNone
        else 
            let rec search (index : int) (node : SetNode<'T>) =
                match node with
                | :? SetLeaf<'T> as l ->
                    if index = 0 then ValueSome l.Value
                    else ValueNone
                | :? SetInner<'T> as n ->
                    let lc = index - n.Left.Count
                    if lc < 0 then search index n.Left
                    elif lc > 0 then search (lc - 1) n.Right
                    else ValueSome n.Value
                | _ ->
                    ValueNone
            search index root

    member x.CompareTo(other : SetNew<'T>) =
        let mutable le = x.GetEnumerator()
        let mutable re = other.GetEnumerator()

        let mutable result = 0 
        let mutable run = true
        while run do
            if le.MoveNext() then
                if re.MoveNext() then
                    let c = comparer.Compare(le.Current, re.Current)
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

    member x.IsProperSubsetOf (other : ICollection<'T>) =
        other.Count > x.Count &&
        x.Forall (fun v -> other.Contains v)
        
    member x.IsProperSubsetOf (other : SetNew<'T>) =
        other.Count > x.Count &&
        x.Forall (fun v -> other.Contains v)
        
    member x.IsProperSupersetOf (other : SetNew<'T>) =
        other.Count < x.Count &&
        other.Forall (fun v -> x.Contains v)
        
    member x.IsSubsetOf (other : ICollection<'T>) =
        other.Count >= x.Count &&
        x.Forall (fun v -> other.Contains v)
        
    member x.IsSubsetOf (other : SetNew<'T>) =
        other.Count >= x.Count &&
        x.Forall (fun v -> other.Contains v)
        
    member x.IsSupersetOf (other : SetNew<'T>) =
        other.Count <= x.Count &&
        other.Forall (fun v -> x.Contains v)

    member x.SetEquals (other : ICollection<'T>) =
        other.Count = x.Count &&
        x.Forall (fun v -> other.Contains v)
        
    member x.SetEquals (other : SetNew<'T>) =
        other.Count = x.Count &&
        x.Forall (fun v -> other.Contains v)

    member x.Overlaps (other : SetNew<'T>) =
        if x.Count < other.Count then x.Exists (fun v -> other.Contains v)
        else other.Exists (fun v -> x.Contains v)

    member x.IsProperSubsetOf(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.IsProperSubsetOf(other)
        | :? System.Collections.Generic.ICollection<'T> as other -> x.IsProperSubsetOf other
        | _ -> x.IsProperSubsetOf(SetNew.FromSeq other)
        
    member x.IsProperSupersetOf(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.IsProperSupersetOf(other)
        | _ -> x.IsProperSupersetOf(SetNew.FromSeq other)
        
    member x.IsSubsetOf(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.IsSubsetOf(other)
        | :? System.Collections.Generic.ICollection<'T> as other -> x.IsSubsetOf other
        | _ -> x.IsSubsetOf(SetNew.FromSeq other)
        
    member x.IsSupersetOf(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.IsSupersetOf(other)
        | _ -> x.IsSupersetOf(SetNew.FromSeq other)
        
    member x.SetEquals(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.SetEquals(other)
        | _ -> x.SetEquals(SetNew.FromSeq other)
        
    member x.Overlaps(other : seq<'T>) =
        match other with
        | :? SetNew<'T> as other -> x.Overlaps(other)
        | _ -> x.Overlaps(SetNew.FromSeq other)

    override x.GetHashCode() =
        hash root

    override x.Equals o =
        match o with
        | :? SetNew<'T> as o -> equals comparer root o.Root
        | _ -> false

    interface System.IComparable with
        member x.CompareTo o = x.CompareTo (o :?> SetNew<_>)
            
    interface System.IComparable<SetNew<'T>> with
        member x.CompareTo o = x.CompareTo o

    interface System.Collections.IEnumerable with
        member x.GetEnumerator() = new SetNewEnumerator<_>(root) :> _

    interface System.Collections.Generic.IEnumerable<'T> with
        member x.GetEnumerator() = new SetNewEnumerator<_>(root) :> _
        
    interface System.Collections.Generic.ICollection<'T> with
        member x.Count = x.Count
        member x.IsReadOnly = true
        member x.Clear() = failwith "readonly"
        member x.Add(_) = failwith "readonly"
        member x.Remove(_) = failwith "readonly"
        member x.Contains(kvp : 'T) = x.Contains kvp
        member x.CopyTo(array : 'T[], startIndex : int) = x.CopyTo(array, startIndex)
            
    interface System.Collections.Generic.ISet<'T> with
        member x.Add _ = failwith "readonly"
        member x.UnionWith _ = failwith "readonly"
        member x.ExceptWith _ = failwith "readonly"
        member x.IntersectWith _ = failwith "readonly"
        member x.SymmetricExceptWith _ = failwith "readonly"
        member x.IsProperSubsetOf o = x.IsProperSubsetOf o
        member x.IsProperSupersetOf o = x.IsProperSupersetOf o
        member x.IsSubsetOf o = x.IsSubsetOf o
        member x.IsSupersetOf o = x.IsSupersetOf o
        member x.SetEquals o = x.SetEquals o
        member x.Overlaps o = x.Overlaps o

    new(comparer : IComparer<'T>) = 
        SetNew<'T>(comparer, SetEmpty.Instance)

and SetNewEnumerator<'T> =
    struct
        val mutable public Root : SetNode<'T>
        val mutable public Stack : list<struct(SetNode<'T> * bool)>
        val mutable public Value : 'T

        member x.Current : 'T = x.Value

        member x.Reset() =
            if x.Root.Height > 0 then
                x.Stack <- [struct(x.Root, true)]
                x.Value <- Unchecked.defaultof<_>

        member x.Dispose() =
            x.Root <- SetEmpty.Instance
            x.Stack <- []
            x.Value <- Unchecked.defaultof<_>
                
        member inline private x.MoveNext(deep : bool, top : SetNode<'T>) =
            let mutable top = top
            let mutable run = true

            while run do
                match top with
                | :? SetLeaf<'T> as n ->
                    x.Value <- n.Value
                    run <- false

                | :? SetInner<'T> as n ->
                    if deep then
                        if n.Left.Height = 0 then
                            if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                            x.Value <- n.Value
                            run <- false
                        else
                            if n.Right.Height > 0 then x.Stack <- struct(n.Right, true) :: x.Stack
                            x.Stack <- struct(n :> SetNode<'T>, false) :: x.Stack
                            top <- n.Left
                    else    
                        x.Value <- n.Value
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

        interface System.Collections.Generic.IEnumerator<'T> with
            member x.Dispose() = x.Dispose()
            member x.Current = x.Current



        new(r : SetNode<'T>) =
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

and internal SetDebugView<'T when 'T : comparison> =

    [<DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>]
    val mutable public Entries : 'T[]

    new(m : SetNew<'T>) =
        {
            Entries = Seq.toArray (Seq.truncate 10000 m)
        }
       
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module SetNew =

    [<GeneralizableValue; CompiledName("Empty")>]
    let inline empty<'T when 'T : comparison> = SetNew<'T>.Empty
    
    [<CompiledName("IsEmpty")>]
    let inline isEmpty (set : SetNew<'T>) = set.Count <= 0
    
    [<CompiledName("Count")>]
    let inline count (set : SetNew<'T>) = set.Count
    
    [<CompiledName("Add")>]
    let inline add (value : 'T) (set : SetNew<'T>) = set.Add(value)
    
    [<CompiledName("Remove")>]
    let inline remove (key : 'T) (set : SetNew<'T>) = set.Remove(key)

    [<CompiledName("Change")>]
    let inline change (key : 'T) (update : bool -> bool) (set : SetNew<'T>) = set.Change(key, update)
    
    [<CompiledName("Contains")>]
    let inline contains (key : 'T) (set : SetNew<'T>) = set.Contains(key)
    
    [<CompiledName("Iter")>]
    let inline iter (action : 'T -> unit) (set : SetNew<'T>) = set.Iter(action)
    
    [<CompiledName("Filter")>]
    let inline filter (predicate : 'T -> bool) (set : SetNew<'T>) = set.Filter(predicate)

    [<CompiledName("Exists")>]
    let inline exists (predicate : 'T -> bool) (set : SetNew<'T>) = set.Exists(predicate)
    
    [<CompiledName("Forall")>]
    let inline forall (predicate : 'T -> bool) (set : SetNew<'T>) = set.Forall(predicate)

    [<CompiledName("Fold")>]
    let inline fold (folder : 'State -> 'T -> 'State) (seed : 'State) (set : SetNew<'T>) = 
        set.Fold(folder, seed)
    
    [<CompiledName("FoldBack")>]
    let inline foldBack (folder : 'T -> 'State -> 'State) (set : SetNew<'T>) (seed : 'State) = 
        set.FoldBack(folder, seed)

    [<CompiledName("OfSeq")>]
    let inline ofSeq (values : seq<'T>) = SetNew.FromSeq values
    
    [<CompiledName("OfList")>]
    let inline ofList (values : list<'T>) = SetNew.FromList values
    
    [<CompiledName("OfArray")>]
    let inline ofArray (values : 'T[]) = SetNew.FromArray values
    
    [<CompiledName("ToSeq")>]
    let inline toSeq (set : SetNew<'T>) = set :> seq<_>

    [<CompiledName("ToList")>]
    let inline toList (set : SetNew<'T>) = set.ToList()
    
    [<CompiledName("ToArray")>]
    let inline toArray (set : SetNew<'T>) = set.ToArray()
    
    [<CompiledName("WithMin")>]
    let inline withMin (minInclusive : 'T) (set : SetNew<'T>) = set.WithMin(minInclusive)
    
    [<CompiledName("WithMax")>]
    let inline withMax (maxInclusive : 'T) (set : SetNew<'T>) = set.WithMax(maxInclusive)
    
    [<CompiledName("WithRange")>]
    let inline withRange (minInclusive : 'T) (maxInclusive : 'T) (set : SetNew<'T>) = set.GetViewBetween(minInclusive, maxInclusive)
    
    [<CompiledName("Union")>]
    let inline union (set1 : SetNew<'T>) (set2 : SetNew<'T>) = SetNew.Union(set1, set2)
    
    [<CompiledName("Intersect")>]
    let inline intersect (set1 : SetNew<'T>) (set2 : SetNew<'T>) = SetNew.Intersect(set1, set2)
    
    [<CompiledName("Difference")>]
    let inline difference (set1 : SetNew<'T>) (set2 : SetNew<'T>) = SetNew.Intersect(set1, set2)
    
    [<CompiledName("UnionMany")>]
    let inline unionMany (sets : #seq<SetNew<'T>>) =
        use e = (sets :> seq<_>).GetEnumerator()
        if e.MoveNext() then
            let mutable m = e.Current
            while e.MoveNext() do
                m <- union m e.Current
            m
        else
            empty
            
    [<CompiledName("IntersectMany")>]
    let inline intersectMany (sets : #seq<SetNew<'T>>) =
        use e = (sets :> seq<_>).GetEnumerator()
        if e.MoveNext() then
            let mutable m = e.Current
            while e.MoveNext() do
                m <- intersect m e.Current
            m
        else
            empty

    [<CompiledName("TryMax")>]
    let inline tryMax (set : SetNew<'T>) = set.TryMax()

    [<CompiledName("TryMin")>]
    let inline tryMin (set : SetNew<'T>) = set.TryMin()

    [<CompiledName("TryMaxValue")>]
    let inline tryMaxV (set : SetNew<'T>) = set.TryMaxV()

    [<CompiledName("TryMinValue")>]
    let inline tryMinV (set : SetNew<'T>) = set.TryMinV()
    
    [<CompiledName("TryAt")>]
    let inline tryAt (index : int) (set : SetNew<'T>) = set.TryAt index
    
    [<CompiledName("TryAtValue")>]
    let inline tryAtV (index : int) (set : SetNew<'T>) = 
        set.TryAtV index

    [<CompiledName("TryPick")>]
    let inline tryPick (mapping : 'T -> option<'U>) (set : SetNew<'T>) =
        set.TryPick(mapping)
        
    [<CompiledName("TryPickValue")>]
    let inline tryPickV (mapping : 'T -> voption<'U>) (set : SetNew<'T>) =
        set.TryPickV(mapping)
        
    [<CompiledName("Pick")>]
    let inline pick (mapping : 'T -> option<'U>) (set : SetNew<'T>) =
        set.Pick(mapping)

    [<CompiledName("PickValue")>]
    let inline pickV (mapping : 'T -> voption<'U>) (set : SetNew<'T>) =
        set.PickV(mapping)
        
    [<CompiledName("Partition")>]
    let inline partition (predicate : 'T -> bool) (set : SetNew<'T>) =
        set.Partition(predicate)

