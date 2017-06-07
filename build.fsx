#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open Fake.YarnHelper
open System

let execProcAndReturnMessages filename args =
    let args' = args |> String.concat " "
    ProcessHelper.ExecProcessAndReturnMessages 
                (fun psi ->
                    psi.FileName <- filename
                    psi.Arguments <-args'
                ) (TimeSpan.FromMinutes(1.))

let pkill args =
    execProcAndReturnMessages "pkill" args

let killParentsAndChildren processId=
    pkill [sprintf "-P %d" processId]


let release = LoadReleaseNotes "RELEASE_NOTES.md"
let srcServerGlob = "src/**/*Server.fsproj"
let srcClientGlob = "src/**/*Client.fsproj"
let testsGlob = "tests/**/*.fsproj"

Target "Clean" (fun _ ->
    ["bin"; "temp" ;"dist"]
    |> CleanDirs

    !! srcServerGlob
    ++ srcClientGlob
    ++ testsGlob
    |> Seq.collect(fun p -> 
        ["bin";"obj"] 
        |> Seq.map(fun sp ->
             IO.Path.GetDirectoryName p @@ sp)
        )
    |> CleanDirs

    )
    

let dotnet path cmd =
    DotNetCli.RunCommand(fun c ->
        { c with
            WorkingDir = path
        }
    ) cmd
let dotnetRestore proj =
    DotNetCli.Restore (fun c ->
            { c with
                Project = proj
            }) 



Target "ClientRestore" (fun _ ->
    Yarn (fun p ->
            { p with
                Command = Install Standard
                // WorkingDirectory = "./src/FAKESimple.Web/"
            })

    !! srcClientGlob
    |> Seq.iter dotnetRestore
)

Target "ServerRestore" (fun _ ->
    !! srcServerGlob
    ++ testsGlob
    |> Seq.iter dotnetRestore
)

Target "ClientBuild" (fun _ ->
    !! srcClientGlob
    |> Seq.map IO.Path.GetDirectoryName
    |> Seq.iter(fun p -> dotnet p "fable webpack -- -p")
)

Target "ServerBuild" (fun _ ->
    !! srcServerGlob
    |> Seq.iter (fun proj ->
        DotNetCli.Build (fun c ->
            { c with
                Project = proj
                //This makes sure that Proj2 references the correct version of Proj1
                AdditionalArgs = [sprintf "/p:PackageVersion=%s" release.NugetVersion]
            }) 
))

Target "RunWebsite" (fun _ ->
    let serversStart =
        !! srcServerGlob
        |> Seq.map IO.Path.GetDirectoryName
        |> Seq.map(fun p -> async {
            dotnet p "watch run"
            }
        )
        |> Seq.map  Async.Catch
        |> Seq.toList
    let clientsStart = 
        !! srcClientGlob
        |> Seq.map IO.Path.GetDirectoryName
        |> Seq.map(fun p -> async {
            dotnet p "fable webpack-dev-server"
        })
        |> Seq.map  Async.Catch
        |> Seq.toList

    serversStart @ clientsStart |> Async.Parallel  |> Async.Ignore |> Async.RunSynchronously 
    // Console.ReadLine() |> ignore
    // if isWindows |> not then
    //     startedProcesses
    //     |> Seq.iter(fst >> killParentsAndChildren >> ignore )
    // else
    //     //Hope windows handles this right?
    //     ()
)

let invoke f = f ()
let invokeAsync f = async { f () }

type TargetFramework =
| Full of string
| Core of string

let getTargetFramework tf =
    match tf with
    | "net45" | "net451" | "net452" 
    | "net46" | "net461" | "net462" -> 
        Full tf
    | "netcoreapp1.0" | "netcoreapp1.1" -> 
        Core tf
    | _ -> failwithf "Unknown TargetFramework %s" tf

let getTargetFrameworksFromProjectFile (projFile : string)=
    let doc = Xml.XmlDocument()
    doc.Load(projFile)
    doc.GetElementsByTagName("TargetFrameworks").[0].InnerText.Split(';')
    |> Seq.map getTargetFramework
    |> Seq.toList

let selectRunnerForFramework tf =
    let runMono = sprintf "mono -f %s --restore -c Release" 
    let runCore = sprintf "run -f %s -c Release"
    match tf with
    | Full t when isMono-> runMono t
    | Full t -> runCore t
    | Core t -> runCore t
        

let runTests modifyArgs =
    !! testsGlob
    |> Seq.map(fun proj -> proj, getTargetFrameworksFromProjectFile proj)
    |> Seq.collect(fun (proj, targetFrameworks) ->
        targetFrameworks
        |> Seq.map selectRunnerForFramework
        |> Seq.map(fun args -> fun () ->
            DotNetCli.RunCommand (fun c ->
            { c with
                WorkingDir = IO.Path.GetDirectoryName proj
            }) (modifyArgs args))
    )


Target "DotnetTest" (fun _ ->
    runTests id
    |> Seq.iter (invoke)
)


Target "WatchTests" (fun _ ->
    runTests (sprintf "watch %s")
    |> Seq.iter (invokeAsync >> Async.Catch >> Async.Ignore >> Async.Start)

    printfn "Press enter to stop..."
    Console.ReadLine() |> ignore

    if isWindows |> not then
        startedProcesses
        |> Seq.iter(fst >> killParentsAndChildren >> ignore )
    else
        //Hope windows handles this right?
        ()
)

Target "DotnetPack" (fun _ ->
    !! srcServerGlob
    |> Seq.iter (fun proj ->
        DotNetCli.Pack (fun c ->
            { c with
                Project = proj
                Configuration = "Release"
                OutputPath = IO.Directory.GetCurrentDirectory() @@ "dist"
                AdditionalArgs = 
                    [
                        sprintf "/p:PackageVersion=%s" release.NugetVersion
                        sprintf "/p:PackageReleaseNotes=\"%s\"" (String.Join("\n",release.Notes))
                    ]
            }) 
    )
)

Target "Publish" (fun _ ->
    Paket.Push(fun c ->
            { c with 
                PublishUrl = "https://www.nuget.org"
                WorkingDir = "dist"
            }
        )
)

Target "Release" (fun _ ->

    if Git.Information.getBranchName "" <> "master" then failwith "Not on master"

    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

"Clean"
  ==> "ClientRestore"
  ==> "ServerRestore"
  ==> "ServerBuild"
  ==> "ClientBuild"
  ==> "DotnetTest"
  ==> "DotnetPack"
  ==> "Publish"
  ==> "Release"

"ServerRestore"
 ==> "WatchTests"
"ClientRestore"
 ==> "ServerRestore"
 ==> "RunWebsite"

RunTargetOrDefault "DotnetTest"