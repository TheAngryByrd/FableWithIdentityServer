module AppClient

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Import.Oidc

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser 
open Elmish.React
open Elmish.Debug


module R = Fable.Helpers.React

open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types
open R.Props
open Fable.Core.JsInterop

type RCom = React.ComponentClass<obj>

type Flexbox = 
    abstract member Grid : RCom
    abstract member Row : RCom
    abstract member Col : RCom

let flexbox : Flexbox = importAll "react-flexbox-grid"

type MuiTable = 
    abstract member Table : RCom
    abstract member TableBody : RCom
    abstract member TableHeader : RCom
    abstract member TableHeaderColumn : RCom
    abstract member TableRow : RCom
    abstract member TableRowColumn : RCom
    
let muiTable : MuiTable = importAll "material-ui/Table"

let MuiThemeProvider = importDefault<RCom> "material-ui/styles/MuiThemeProvider"
let FlatButton = importDefault<RCom> "material-ui/FlatButton"
let deepOrange500 = importMember<string> "material-ui/styles/colors"
let getMuiTheme = importDefault<obj->obj> "material-ui/styles/getMuiTheme"
let CircularProgress = importDefault<RCom> "material-ui/CircularProgress"
importAll "./css/site.styl"
(importDefault "react-tap-event-plugin")()
// let Oidc = importAll<OidcClient> "oidc-client"
let inline (~%) x = createObj x
let optionDefault d o =
    match o with
    | Some x -> x
    | None -> d

let iodcConfig : Oidc.UserManagerSettings =
    createObj [
        "authority" ==> "http://localhost:8123"
        "client_id" ==> "AppClient"
        "redirect_uri" ==> "http://locahost:8080/callback.html"
        "response_type" ==> "id_token token"
        "scope" ==> "openid profile appServerApi"
        "post_logout_redirect_uri" ==> "http://locahost:8080/index.html"
    ] |> unbox
// [<Emit("new Oidc.UserManager($0)")>]
// let userManager (o : obj) : obj = jsNative
// let userMgr = userManager(iodcConfig)
// let iodcSettings =
//     Oidc.OidcClientSettings() 

let userManager =  Oidc.UserManager(iodcConfig)
let muiTheme =
    %["palette" ==>
        %["accent1Color" ==> deepOrange500]]
    |> getMuiTheme


type Post = {
    userId : int
    id: int
    title: string
    body : string
}

type Foos =
    | Inital of string
    | List of Post seq

type Model = {
    Foo : Foos
    Username : string
}

type Messages = 
| PulledPage of  Post seq
| FetchError of exn
| Login
| LoggedIn of unit
| LoginFailed of exn
| GotUser of User option
| GetUserError of exn

let pullPage url = promise {
    do! Async.Sleep(1000) |> Async.StartAsPromise
    let props =
        [
            RequestProperties.Method HttpMethod.GET
            // RequestProperties.Mode RequestMode.Nocors
        ]
    // printfn "%A" "About to start request"
    try
        let! response = Fetch.fetchAs<Post seq> url props
        printfn "%A" response
        // return "succcess"
        return response
        // printfn "%s" response
    with e ->
        e.ToString() |> printfn "%A" 
        return! failwithf "I duno"
    // return ""
    // return! response.text()
}

let pullPageCmd url = 
    Cmd.ofPromise pullPage url PulledPage FetchError


let getUsername () = promise {
    let! user = userManager.getUser()
    return FSharp.Core.Option.ofObj user
}
let getUserNameCmd () =
    Cmd.ofPromise getUsername () GotUser GetUserError


let login (userManager : UserManager) = promise {
    try
        do! userManager.signinRedirect()
        printfn "%s" "signinRedirect"
    with e -> 
        printfn "login failed %A" (e.Message)
        failwith "foo"
}
    
let loginCmd userManager =
    Cmd.ofPromise login userManager LoggedIn LoginFailed
let init result =
    {Foo = Inital ("loading...") ; Username = "Anonymous"} , 
        Cmd.batch [
            pullPageCmd "/api/posts"
            getUserNameCmd ()
        ]


let update message model =
    match message with
    | PulledPage s ->
        {model with Foo = (List s)}, Cmd.Empty
    | FetchError e-> 
        printfn "%A" e
        {model with Foo = (Inital "Failed...")}, Cmd.Empty
    | Login ->
        printfn "%s" "clickedlogin"
        model, loginCmd userManager
    | LoggedIn () ->
        printfn "%s" "logggedIn"
        model, Cmd.Empty
    | LoginFailed e ->
        printfn "Login failed %A" e
        model, Cmd.Empty
    | GotUser uo ->
        try
            let p = 
                uo 
                |> FSharp.Core.Option.map(fun u -> u.profile |> string) 
                |> optionDefault "What"
            // let up = u.profile |> string
            printfn "HAHAH"
            { model with Username = p}, Cmd.Empty
        with e -> 
            printfn "%A" e
            { model with Username = "error"}, Cmd.Empty
    | GetUserError e -> 
        printfn "GetUserError %A" e
        model, Cmd.Empty
    // model, Cmd.Empty

let text t =
    R.p [] [R.str t ]

let makeTable (headers : string list) body =
    let simpleTh s = R.from muiTable.TableHeaderColumn %[] [R.str s]
    let simpleTd s = R.from muiTable.TableRowColumn %[] [R.str s]
    R.from muiTable.Table  %[] [
        R.from muiTable.TableHeader %[] [
            R.from muiTable.TableRow %[] [
                yield! headers |> Seq.map simpleTh
            ]
        ]
        R.from muiTable.TableBody %[] [
            yield!
                body
                |> List.map(fun x -> R.from muiTable.TableRow %[] [
                                        yield! x |> List.map simpleTd]
                    )
        ]
    ]
let root model dispatch = 
    R.from MuiThemeProvider
        %["muiTheme" ==> muiTheme] [
            R.div [] [
                R.from flexbox.Grid %["fluid" ==> true] [
                    R.from flexbox.Row %[] [
                        R.from flexbox.Col %[] [
                            R.from FlatButton %[
                                "label" ==> "Login"
                                "primary" ==> true
                                "onTouchTap" ==> (fun _ -> Login |> dispatch)
                                ] []    
                            R.label [] [unbox model.Username]
                        ]
                    ]
                    R.from flexbox.Row %["center" ==> "xs"] [
                        R.from flexbox.Col %[] [
                            match model.Foo with
                            | Inital s -> 
                                yield 
                                    R.from CircularProgress %[] []
                            | List l ->    
                                yield 
                                    makeTable 
                                        ["Name"; "Body"] 
                                        (l |> List.ofSeq |> List.map(fun i -> [i.title; i.body]))
                        ]
                    ]
                ]
            ]
        ]
Program.mkProgram init update root
// |> Program.toNavigable (parseHash pageParser) urlUpdate
|> Program.withReact "elmish-app"
//-:cnd
#if DEBUG
|> Program.withDebugger
#endif
//+:cnd
|> Program.run