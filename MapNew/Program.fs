
open System
open System.Text
open System.Text.RegularExpressions

module Parser =
    
    [<Struct>]
    type Text(data : string, offset : int, length : int) =
        
        member x.Substring(o : int, l : int) =
            if o < 0 || o + l > length then raise <| IndexOutOfRangeException("Substring")
            Text(data, offset + o, l)
            
        member x.Substring(o : int) =
            if o < 0 || o > length then raise <| IndexOutOfRangeException("Substring")
            Text(data, offset + o, length - o)

        member x.Match(rx : Regex) =
            rx.Match(data, offset, length)

        override x.ToString() =
            data.Substring(offset, length)

        new(data : string, offset : int) = Text(data, offset, data.Length - offset)
        new(data : string) = Text(data, 0, data.Length)

    type Parser<'a> = Text -> option<'a * Text>

    let pMap (mapping : 'a -> 'b) (p : Parser<'a>) : Parser<'b> =
        fun t ->
            match p t with
            | Some(a, t) -> Some(mapping a, t)
            | None -> None
            
    let pChoose (mapping : 'a -> option<'b>) (p : Parser<'a>) : Parser<'b> =
        fun t ->
            match p t with
            | Some(a, t) -> 
                match mapping a with
                | Some b -> Some(b, t)
                | None -> None
            | None -> None

    let pBind (mapping : 'a -> Parser<'b>) (p : Parser<'a>) : Parser<'b> =
        fun t ->
            match p t with
            | Some(a, t) -> mapping a t
            | None -> None
            
    let pRegex (pattern : string) : Parser<Match> =
        let pat = if pattern.StartsWith "^" then pattern else "^" + pattern
        let rx = Regex pat
        fun (t : Text) ->
            let m = t.Match rx
            if m.Success then Some(m, t.Substring m.Length)
            else None

    let pWhitespace =
        pRegex @"[ \t\r\n]*"
        |> pMap ignore

    let pFloat =
        pRegex @"[+-]?(?:\d+([.]\d*)?(?:[eE][+-]?\d+)?|[.]\d+(?:[eE][+-]?\d+)?)"
        |> pMap (fun m -> float m.Value)


    let (>.) (l : Parser<'a>) (r : Parser<'b>) : Parser<'b> =
        fun t ->
            match l t with
            | Some (_a, t1) ->
                match r t1 with
                | Some (b, t2) ->   
                    Some (b, t2)
                | None ->
                    None
            | None ->
                None
                
    let (.>) (l : Parser<'a>) (r : Parser<'b>) : Parser<'a> =
        fun t ->
            match l t with
            | Some (a, t1) ->
                match r t1 with
                | Some (_b, t2) ->   
                    Some (a, t2)
                | None ->
                    None
            | None ->
                None

    let (.>.) (l : Parser<'a>) (r : Parser<'b>) : Parser<'a * 'b> =
        fun t ->
            match l t with
            | Some (a, t1) ->
                match r t1 with
                | Some (b, t2) ->   
                    Some ((a,b), t2)
                | None ->
                    None
            | None ->
                None

    let rec pMany (p : Parser<'a>) =
        fun t ->
            match p t with
            | Some(head, t) ->
                match pMany p t with
                | Some (tail, t) ->
                    Some (head :: tail, t)
                | None ->
                    Some ([head], t)
            | None ->
                Some ([], t)

    let pMany1 (p : Parser<'a>) =
        p .>. pMany p |> pMap (fun (h,t) -> h :: t)
        
    let rec pSepBy (separator : Parser<'x>) (element : Parser<'a>) =
        fun t ->
            match element t with
            | Some (head, t) ->
                match separator t with
                | Some (_, ts) ->
                    match pSepBy1 separator element ts with
                    | Some (tail, ts) -> Some (head :: tail, ts)
                    | None -> Some([head], t)
                | None ->
                    Some([head], t)
            | None ->
                Some([], t)

    and pSepBy1 (separator : Parser<'x>) (element : Parser<'a>) =
        element .> separator .>. pSepBy separator element |> pMap (fun (h,t) -> h :: t)
    
open Parser

let parseMarkdownTable (content : string) =
    let lineBreak = Regex @"[\r\n]+"
    let lines = lineBreak.Split content

    let pLine =
        pWhitespace >.
        pRegex @"\|" >.
        pSepBy1 (pRegex @"\|") (
            pWhitespace >.
            (pRegex @"[^\|]+" |> pMap (fun m -> m.Value.Trim())) .>
            pWhitespace
        ) .> 
        pWhitespace .>
        pRegex @"\|" .>
        pWhitespace |>
        pMap List.toArray

    
    if lines.Length > 0 then
        let mutable i = 0
        let mutable header = None
        while i < lines.Length && Option.isNone header do
            match pLine (Text lines.[i]) with
            | Some (line, _) -> header <- Some line
            | None -> ()
            i <- i + 1

        // skip separators
        i <- i + 1

        match header with
        | Some header ->
            let mutable table = 
                header 
                |> Array.map (fun h -> h, [])
                |> Map.ofArray

            while i < lines.Length do
                match pLine (Text lines.[i]) with
                | Some (line, _) when line.Length = header.Length ->
                    for k in 0 .. line.Length - 1 do
                        let v = line.[k]
                        table <- Map.change header.[k] (function Some o -> Some (v :: o) | None -> Some [v]) table
                | _ -> ()
                i <- i + 1

            let table = table |> Map.map (fun _ l -> List.rev l |> List.toArray)

            let times   = table.["Mean"]
            let methods = table.["Method"]
            let count   = table.["Count"]

            let results = 
                let mutable results = Map.empty
                for (m, c, t) in Array.zip3 methods count times do
                    let c = int c
                    let t = if t.EndsWith "ns" then t.Substring(0, t.Length-2).Trim() else t 
                    let t = float (t.Replace(",", ""))

                    results <-
                        results |> Map.change c (function
                            | Some o -> Map.add m t o |> Some
                            | None -> Some (Map.ofList [m, t])
                        )
                results


            let methods = results |> Map.toSeq |> Seq.head |> snd |> Map.toSeq |> Seq.map fst |> Seq.toList
            printfn "%s" ("Count" :: methods |> String.concat ";")
            for (c, vs) in Map.toSeq results do
                let v = string c :: List.map string (Map.toList vs |> List.map snd) |> String.concat ";"
                printfn "%s" v



        | None ->
            printfn "bad"




[<EntryPoint>]
let main _argv =
    parseMarkdownTable (System.IO.File.ReadAllText @"C:\Users\Schorsch\Desktop\benchmark.md")
    exit 0
    BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark.MapBenchmark>()
    |> ignore
    0
