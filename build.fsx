open System
open System.IO
//open System.Text.RegularExpressions

//let bodyContentRegex = new Regex("\$BODYCONTENT\$", RegexOptions.Multiline)
//let navBarContentRegex = new Regex("\$NAVBARCONTENT\$", RegexOptions.Multiline)

type Page = Page of FileName : string * DisplayName : string * Pages : Page list

let pages = [
    Page ("index.html", "Home", [])
    Page ("company.html", "Company", [])
    Page ("panconnect.html", "PanConnect", [])
    Page ("consulting.html", "Consulting", [])
    Page ("hiring.html", "We're hiring", [])
]

let generateNavBarContent currentPage =
    pages
    |> Seq.map (
        function
        | Page (fileName, displayName, _) as page when currentPage = page ->
            "<li class=\"active\"><a href=\"build_" + fileName + "\">" + displayName + """</a></li>"""
        | Page (fileName, displayName, _) ->
            "<li><a href=\"build_" + fileName + "\">" + displayName + """</a></li>""")
    |> String.concat ""

let rebasePath path = Path.Combine("/Users/nbevans/Downloads/byteflag-web", path)

let read page =
    match page with Page (fileName, _, _) when fileName <> "" -> File.ReadAllText(rebasePath fileName) | _ -> ""

printfn "%A" pages

pages
|> Seq.iter (
    function
    | Page (fileName, _, _) as page ->
        let template = File.ReadAllText("/Users/nbevans/Downloads/byteflag-web/template.html")
        let pageContent = template
        let pageContent = pageContent.Replace("$NAVBARCONTENT$", generateNavBarContent page)
        let pageContent = pageContent.Replace("$BODYCONTENT$", match read page with v when v <> "" -> v | _ -> "Nothing to display on this page yet. Come back later!")
        File.WriteAllText("build_" + fileName, pageContent))