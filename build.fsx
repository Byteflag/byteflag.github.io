open System
open System.IO

type Page = Page of FileName : string * DisplayName : string * Pages : Page list

let pages = [
    Page ("index.html", "Home", [])
    Page ("panconnect.html", "PanConnect", [  Page ("panconnect_mobile.html", "PanConnect Mobile", [])
                                              Page ("panconnect_selfservice.html", "PanConnect Self-Service", []) ])
    Page ("news.html", "News", [])
    Page ("consulting.html", "Consulting", [])
    Page ("hiring.html", "We're hiring", [])
    Page ("company.html", "Company", [])
]

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
        | Page (fileName, _, _) as page when topLevelPage <> page ->
            Some ("<link rel=\"prefetch\" href=\"" + fileName + "\">")
        | _ -> None)
    |> String.concat ""

let generateNavBarContent topLevelPage =
    pages
    |> Seq.map (
        function
        | Page (fileName, displayName, _) as page when topLevelPage = page ->
            "<li class=\"active\"><a href=\"" + fileName + "\">" + displayName + """</a></li>"""
        | Page (fileName, displayName, _) ->
            "<li><a href=\"" + fileName + "\">" + displayName + """</a></li>""")
    |> String.concat ""

let baseFolderPath = __SOURCE_DIRECTORY__
let contentFolderPath = Path.Combine(baseFolderPath, "content")
let contentFileName path = Path.Combine(contentFolderPath, path)
let outputFileName path = Path.Combine(baseFolderPath, path)

let read page = match page with Page (fileName, _, _) when fileName <> "" -> File.ReadAllText(contentFileName fileName) | _ -> ""

//
// Script entry point.
//

while true do
    // Generate CSS concat'd bundle.
    generateCssBundle ()

    // Generate pages from template.
    let rec processPage topLevelPage =
        function
        | Page (fileName, _, pages) as page ->
            let template = File.ReadAllText(contentFileName "template.html")
            let pageContent = template
            let pageContent = pageContent.Replace("$PREFETCHLINKS$", generatePrefetchLinks topLevelPage)
            let pageContent = pageContent.Replace("$NAVBARCONTENT$", generateNavBarContent topLevelPage)
            let pageContent = pageContent.Replace("$BODYCONTENT$", match read page with v when v <> "" -> v | _ -> "Nothing to display on this page yet. Come back later!")
            File.WriteAllText(outputFileName fileName, pageContent)

            // now recurse into the nested pages
            pages |> Seq.iter (processPage topLevelPage)

            // trace
            printfn "%A" page

    pages |> Seq.iter (fun page -> processPage page page)

    System.Threading.Thread.Sleep 3000
    printfn "\r\nLooping...\r\n"