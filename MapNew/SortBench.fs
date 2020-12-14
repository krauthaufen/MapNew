namespace Benchmark

open System.Collections.Generic
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open MapNew
open System.Linq

[<PlainExporter; MemoryDiagnoser>]
type SortBenchmark() =

    [<DefaultValue; Params(100)>]
    val mutable public Count : int

    let mutable data : (int * int)[] = [||]
    static let cmp = LanguagePrimitives.FastGenericComparer<int>
    static let func = System.Func<_,_,_>(fun (a,_) (b,_) -> cmp.Compare(a, b))
    static let comparer =
        { new IComparer<int * int> with
            member x.Compare((a,_), (b,_)) =
                cmp.Compare(a, b)
        }
    static let rand = System.Random()

    static let randomArray (maxValue : int) (n : int) =
        let s = System.Collections.Generic.HashSet<int>()
        let res = Array.zeroCreate n
        let mutable i = 0
        while i < n do
            let v = rand.Next(maxValue)
            if s.Add v then
                res.[i] <- (v, i)
                i <- i + 1
        res

    [<GlobalSetup>]
    member x.Setup() =
        data <- randomArray (1 <<< 30) x.Count

    [<Benchmark>]
    member x.MergeSort() =
        Sorting.mergeSortHandleDuplicates false cmp data data.Length
    
    [<Benchmark>]
    member x.TimSort() =
        let res = Array.copy data
        Aardvark.Base.Sorting.SortingExtensions.TimSort(res, func)
        res
        
    [<Benchmark>]
    member x.QuickSort() =
        let res = Array.copy data
        Aardvark.Base.Sorting.SortingExtensions.QuickSort(res, func)
        res
        
    [<Benchmark>]
    member x.SmoothSort() =
        let res = Array.copy data
        Aardvark.Base.Sorting.SortingExtensions.SmoothSort(res, func)
        res

    [<Benchmark>]
    member x.HeapSort() =
        let res = Array.copy data
        Aardvark.Base.Sorting.SortingExtensions.HeapSort(res, func)
        res

    [<Benchmark>]
    member x.ArraySortWith() =
        Array.sortWith (fun (a,_) (b,_) -> cmp.Compare(a,b)) data

    [<Benchmark>]
    member x.BCLSort() =
        let res = Array.copy data
        System.Array.Sort(res, comparer)
        res
    
    [<Benchmark>]
    member x.BCLOrderBy() =
        data.OrderBy(System.Func<_,_>(fun (a,_) -> a), cmp).ToArray()