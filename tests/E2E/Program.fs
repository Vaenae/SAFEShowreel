module E2ETests

open Microsoft.Playwright
open System.Threading.Tasks
open System
open Hopac
open Logary
open Logary.Configuration
open Logary.Adapters.Facade
open Logary.Targets
open Expecto

type IPage with
    member this.Screenshot(name) =
        task {
            let! _ = this.ScreenshotAsync(PageScreenshotOptions(Path = $"results/{name}.png"))
            ()
        }

let baseUrl =
    match System.Environment.GetEnvironmentVariable("BASE_URL") with
    | null -> "http://localhost:8080/"
    | url -> url

let browserTest name (testFunction: IPage -> Task<unit>) =
    testTask name {
        do!
            task {
                use! web = Playwright.CreateAsync()
                let! browser = web.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true))
                let! page = browser.NewPageAsync()

                try
                    do! testFunction page
                    do! page.Screenshot($"success_{name}")
                with
                | ex ->
                    do! page.Screenshot($"failed_{name}")
                    raise ex
            }
    }

[<Tests>]
let testlist =
    testList
        "Server"
        [ browserTest "Page title exists"
          <| fun page ->
              task {
                  let! _ = page.GotoAsync(url = baseUrl)
                  let! title = page.TextContentAsync "h1"
                  Expect.equal title "SAFEShowreel" "Title is SAFEShowreel"
              } ]


[<EntryPoint>]
let main argv =
    let logary =
        Config.create "MyProject.Tests" "localhost"
        |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
        |> Config.processing (Events.events |> Events.sink [ "console" ])
        |> Config.build
        |> run

    LogaryFacadeAdapter.initialise<Expecto.Logging.Logger> logary

    // Invoke Expecto:
    runTestsInAssemblyWithCLIArgs [] argv
