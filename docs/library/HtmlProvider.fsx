(**
---
category: Type Providers
categoryindex: 1
index: 2
---
*)
(*** condition: prepare ***)
#r "../../src/FSharp.Data/bin/Release/netstandard2.0/FSharp.Data.Http.dll"
#r "../../src/FSharp.Data/bin/Release/netstandard2.0/FSharp.Data.Csv.Core.dll"
#r "../../src/FSharp.Data/bin/Release/netstandard2.0/FSharp.Data.Html.Core.dll"
#r "../../src/FSharp.Data/bin/Release/netstandard2.0/FSharp.Data.dll"
(*** condition: fsx ***)
#if FSX
#r "nuget: FSharp.Data,{{fsdocs-package-version}}"
#endif
(*** condition: ipynb ***)
#if IPYNB
#r "nuget: FSharp.Data,{{fsdocs-package-version}}"

Formatter.SetPreferredMimeTypesFor(typeof<obj>, "text/plain")
Formatter.Register(fun (x: obj) (writer: TextWriter) -> fprintfn writer "%120A" x)
#endif
(**
[![Binder](../img/badge-binder.svg)](https://mybinder.org/v2/gh/fsprojects/FSharp.Data/gh-pages?filepath={{fsdocs-source-basename}}.ipynb)&emsp;
[![Script](../img/badge-script.svg)]({{root}}/{{fsdocs-source-basename}}.fsx)&emsp;
[![Notebook](../img/badge-notebook.svg)]({{root}}/{{fsdocs-source-basename}}.ipynb)

# HTML Type Provider

This article demonstrates how to use the HTML type provider to read HTML tables files
in a statically typed way.

The HTML Type Provider takes a sample HTML document as input and generates a type based on the data
present in the columns of that sample. The column names are obtained from the first (header) row.

## Introducing the provider

The type provider is located in the `FSharp.Data.dll` assembly. Assuming the assembly
is located in the `../../../bin` directory, we can load it in F# Interactive as follows:
*)

open FSharp.Data

(**

### Parsing Power Market Data

The Elexon - BM Reports website provides market data about the U.K's current power system. For simplicity, an example of this data below is shown in CSV format.

    [lang=text]
    Settlement Day,Period,IMBALNGC,Offer Volume Bid Volume,Accepted Offer Vol,Accepted Bid Vol,UAOV,UABV,PAOV,PABV
    2014-01-14,1,877.000,52378.500,-53779.500,348.200,-654.374,0.000,0.000,348.200,-654.374
    2014-01-14,2,196.000,52598.000,-53559.500,349.601,-310.862,0.000,0.000,316.701,-310.862
    2014-01-14,3,-190.000,52575.000,-53283.500,186.183,-2.426,0.000,0.000,162.767,-1.917
    2014-01-14,4,-61.000,52576.000,-53454.500,18.000,-24.158,0.000,0.000,18.000,-24.158


Usually with HTML files headers are demarked by using the `<th>` tag, however in this file this is not the case, so the provider assumes that the
first row is headers. (This behaviour is likely to get smarter in later releases). But it highlights a general problem about HTML's strictness.
*)

[<Literal>]
let F1_2017_URL =
    "https://en.wikipedia.org/wiki/2017_FIA_Formula_One_World_Championship"

type F1_2017 = HtmlProvider<F1_2017_URL>

(**
The generated type provides a type space of tables that it has managed to parse out of the given HTML Document.
Each type's name is derived from either the id, title, name, summary or caption attributes/tags provided. If none of these
entities exist then the table will simply be named `Tablexx` where xx is the position in the HTML document if all of the tables were flattened out into a list.
The `Load` method allows reading the data from a file or web resource. We could also have used a web URL instead of a local file in the sample parameter of the type provider.
The following sample calls the `Load` method with an URL that points to a live version of the same page on wikipedia.
*)
// Download the latest market depth information
let f1Calendar = F1_2017.Load(F1_2017_URL).Tables.``Season calendarEdit``

// Look at the most recent row. Note the 'Date' property
// is of type 'DateTime' and 'Open' has a type 'decimal'
let firstRow = f1Calendar.Rows |> Seq.head
let round = firstRow.Round
let grandPrix = firstRow.``Grand Prix``
let date = firstRow.Date

// Print the bid / offer volumes for each row
for row in f1Calendar.Rows do
    printfn "Race, round %A is hosted at %A on %A" row.Round row.``Grand Prix`` row.Date

(*** include-fsi-merged-output ***)

(**
The generated type has a property `Rows` that returns the data from the HTML file as a
collection of rows. We iterate over the rows using a `for` loop. As you can see the
(generated) type for rows has properties such as `Grand Prix`, `Circuit`, `Round` and `Date` that correspond
to the columns in the selected HTML table file.

As you can see, the type provider also infers types of individual rows. The `Date`
property is inferred to be a `DateTime` (because the values in the sample file can all
be parsed as dates) while other columns are inferred as the correct type where possible.
*)

(**

### Parsing Nuget package stats

This small sample shows how the HTML Type Provider can be used to scrape data from a website. In this example we analyze the download counts of the FSharp.Data package on NuGet.
Note that we're using the live URL as the sample, so we can just use the default constructor as the runtime data will be the same as the compile time data.

*)


// Configure the type provider
type NugetStats = HtmlProvider<"https://www.nuget.org/packages/FSharp.Data">

// load the live package stats for FSharp.Data
let rawStats = NugetStats().Tables.Table4

// helper function to analyze version numbers from nuget
let getMinorVersion (v: string) =
    System
        .Text
        .RegularExpressions
        .Regex(
            @"\d.\d"
        )
        .Match(
        v
    )
        .Value

// group by minor version and calculate download count
let stats =
    rawStats.Rows
    |> Seq.groupBy (fun r -> getMinorVersion r.Version)
    |> Seq.map (fun (k, xs) -> k, xs |> Seq.sumBy (fun x -> x.Downloads))
    |> Seq.toArray

(*** include-fsi-merged-output ***)

(**

### Getting statistics on Doctor Who

This sample shows some more screen scraping from Wikipedia:

*)

(*** define-output:doctorWhoChart ***)
[<Literal>]
let DrWho =
    "https://en.wikipedia.org/wiki/List_of_Doctor_Who_episodes_(1963%E2%80%931989)"

let doctorWho = new HtmlProvider<DrWho>()

// Get the average number of viewers for each doctor's series run
let viewersByDoctor =
    doctorWho.Tables.``Season 1 (1963-1964) Edit``.Rows
    |> Seq.groupBy (fun season -> season.``Directed by``)
    |> Seq.map (fun (doctor, seasons) ->
        let averaged =
            seasons
            |> Seq.averageBy (fun season -> season.``UK viewers (millions)``)

        doctor, averaged)
    |> Seq.toArray


(*** include-fsi-merged-output ***)

(**
## Related articles

 * [HTML Parser](HtmlParser.html) - provides more information about
   working with HTML documents dynamically.

*)
