open System
open System.IO

type Page = Page of Path : string seq * DisplayName : string * Pages : Page list

let page path displayName pages = Page (path, displayName, pages)

let pages = [
    page ["index.html"] "Home" []
    page ["panconnect"; "index.html"] "PanConnect" [
        page ["panconnect"; "mobile-working"; "index.html"] "PanConnect Mobile" []
        page ["panconnect"; "self-service"; "index.html"] "PanConnect Self-Service" []
    ]
    page ["news"; "index.html"] "News" []
    page ["consulting"; "index.html"] "Consulting" []
    page ["hiring"; "index.html"] "We're hiring" []
    page ["company"; "index.html"] "Company" []
]

let generateHtmlPath =
    function
    | Page (path, _, _) ->
        if Seq.length path > 1 then
            "/" + (path |> Seq.truncate ((Seq.length path) - 1) |> String.concat "/") + "/"
        else
            "/"

let generateCssBundle () =
    let path name = Path.Combine(__SOURCE_DIRECTORY__, "assets", "css", name)
    let content =
        [ "cronos-pro.css"; "bootstrap.min.css"; "font-awesome.min.css"; "bootstrap-theme.css"; "main.css" ]
        |> Seq.map (fun name -> File.ReadAllText(path name))
        |> String.concat "\n"
    File.WriteAllText(path "concat.css", content)

let generatePrefetchLinks topLevelPage =
    pages
    |> Seq.choose (
        function
        | Page _ as page when topLevelPage <> page ->
            Some ("<link rel=\"prefetch\" href=\"" + generateHtmlPath page + "\">")
        | _ -> None)
    |> String.concat ""

let generateNavBarContent topLevelPage =
    pages
    |> Seq.map (
        function
        | Page (_, displayName, _) as page when topLevelPage = page ->
            "<li class=\"active\"><a href=\"" + generateHtmlPath page + "\">" + displayName + """</a></li>"""
        | Page (_, displayName, _) as page ->
            "<li><a href=\"" + generateHtmlPath page + "\">" + displayName + """</a></li>""")
    |> String.concat ""

let generateRelativePath =
    function
    | Page (path, _, _) ->
        Seq.init ((Seq.length path) - 1) (fun _ -> "..") |> String.concat "/"

let baseFolderPath = __SOURCE_DIRECTORY__
let contentFolderPath = Path.Combine(baseFolderPath, "_content")
let contentFileName path = Path.Combine(Array.append [|contentFolderPath|] (Array.ofSeq path))
let outputFileName path =
    match Seq.length path with
    | 0 -> baseFolderPath
    | 1 -> Path.Combine(baseFolderPath, Seq.head path)
    | v ->
        Directory.CreateDirectory(Path.Combine(Array.append [|baseFolderPath|] (path |> Seq.truncate (v - 1) |> Seq.toArray))) |> ignore
        Path.Combine(Array.append [|baseFolderPath|] (Array.ofSeq path))

let read =
    function
    | Page (path, _, _) when path |> Seq.isEmpty |> not ->
        File.ReadAllText(contentFileName path)
    | _ -> ""

//
// Script entry point.
//

while true do
    // Generate CSS concat'd bundle.
    generateCssBundle ()

    // Generate pages from template.
    let rec processPage topLevelPage =
        function
        | Page (path, _, pages) as page ->
            let template = File.ReadAllText(contentFileName ["_template.html"])
            let pageContent = template
            let pageContent = pageContent.Replace("$PREFETCHLINKS$", generatePrefetchLinks topLevelPage)
            let pageContent = pageContent.Replace("$NAVBARCONTENT$", generateNavBarContent topLevelPage)
            let pageContent = pageContent.Replace("$BODYCONTENT$", match read page with v when v <> "" -> v | _ -> "Nothing to display on this page yet. Come back later!")
            let pageContent = pageContent.Replace("$RELATIVEPATH$", generateRelativePath page)
            File.WriteAllText(outputFileName path, pageContent)

            // Now recurse into the nested pages
            pages |> Seq.iter (processPage topLevelPage)

            // Trace
            printfn "%A" page

    pages |> Seq.iter (fun page -> processPage page page)

    System.Threading.Thread.Sleep 3000
    printfn "\r\nLooping...\r\n"