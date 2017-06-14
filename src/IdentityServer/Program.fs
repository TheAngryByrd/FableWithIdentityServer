namespace IdentityServer



open System
open System.IO
open System.Collections.Generic
open System.Security.Claims
open System.Threading
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Authentication
open Microsoft.AspNetCore.Http.Features
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open IdentityServer4
open IdentityServer4.Models
open IdentityServer4.Services
open IdentityServer4.Test
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware



module Config =
    let appServerApi = "appServerApi"
    let getApiResources () =
        [
            ApiResource(appServerApi,"App Server API")
        ]
    let getIdentityResources () : IdentityResource list =
        [
            IdentityResources.OpenId()
            IdentityResources.Profile()
        ]
    let getClients () =
        [
            Client(
                ClientId = "client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = ResizeArray<_> [ Secret("secret".Sha256()) ],
                AllowedScopes = ResizeArray<_> [appServerApi])
            Client(
                ClientId = "AppClient",
                ClientName = "Fable App",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,
                RedirectUris = ResizeArray<_>["http://locahost:8080/callback.html"],
                PostLogoutRedirectUris = ResizeArray<_>["http://locahost:8080/index.html"],
                AllowedCorsOrigins = ResizeArray<_>["http://localhost:8080"],
                AllowedScopes = ResizeArray<_> [
                    IdentityServerConstants.StandardScopes.OpenId
                    IdentityServerConstants.StandardScopes.Profile
                    appServerApi
                ]
            )

        ]

    let getUsers () = 
        [
            TestUser (
                SubjectId = "1",
                Username  = "Admin",
                Password  = "Derp",
                Claims    = ResizeArray<_> [
                    Claim("name", "Admin")
                ]
            )
        ]


module ViewModels = 
    [<CLIMutable>]
    type LoginPost = {
        Username : string
        Password : string
        RememberMe : bool
    }
    [<CLIMutable>]
    type ReturnUrl = {
        ReturnUrl : string
    }

module Main =
    open ViewModels
    open Giraffe.HtmlEngine
    let layout (pageTitle) content =
        html [] [
            head [] [
                title [] (encodedText pageTitle)
            ]
            body [] content
        ]

    //TODO: CSRF Token
    let renderLogin () =
        [
            form [("method", "POST")] [
                input [
                    ("name", "Username")
                    ("placeholder", "username")
                ]

                input [
                    ("name", "Password")
                    ("type", "password")
                    ("placeholder", "password")
                ]
                button [
                    ("type", "submit")
                ] (encodedText "Login")
            ]
        ] |> layout "Login"
        
    let getLogin (ctx : HttpContext) = async {
        return! (() |> renderLogin, ctx) ||> renderHtml 
    }

    let postLogin (ctx : HttpContext) = async {
        let! loginForm = ctx.BindModel<LoginPost>()
        let! returnUrl = ctx.BindQueryString<ReturnUrl>()
        let tus = ctx.GetService<TestUserStore>()
        
        if tus.ValidateCredentials(loginForm.Username, loginForm.Password) then
            let props = 
                AuthenticationProperties(
                    IsPersistent = loginForm.RememberMe,
                    ExpiresUtc = Nullable<_>(DateTimeOffset.UtcNow.AddDays(1.))
                )
            let user = tus.FindByUsername(loginForm.Username)

            do! ctx.Authentication.SignInAsync(user.SubjectId, user.Username, props) |> Async.AwaitTask

            if ctx.GetService<IIdentityServerInteractionService>().IsValidReturnUrl(returnUrl.ReturnUrl) then

                return! redirectTo false returnUrl.ReturnUrl ctx
            else 
                return! redirectTo false ("~/") ctx
        else 
            return! getLogin ctx
    }

    let getConsent (ctx : HttpContext) = async {
        return! text "hello" ctx
    }
    let webApp = 
        choose [
            route "/account/login" >=> choose [
                GET >=> getLogin
                POST >=> postLogin
            ]
            route "/consent" >=> choose [
                GET >=> getConsent
            ]
            

        ]
    let configureServices (services : IServiceCollection) =
        
        services
            .AddCors()
            .AddIdentityServer()
            .AddTemporarySigningCredential()
            .AddInMemoryIdentityResources(Config.getIdentityResources())
            .AddInMemoryApiResources(Config.getApiResources())
            .AddInMemoryClients(Config.getClients())
            .AddTestUsers(ResizeArray<_>(Config.getUsers()))
            |> ignore

    let configureApp (app : IApplicationBuilder) = 
        app
            .UseCors(
                fun x -> 
                    
                    x   
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                    |> ignore
                )
            .UseIdentityServer() 
            .UseStaticFiles()
            .UseGiraffe webApp

        ()

    
    let configureLogging (loggerFactory : ILoggerFactory) =
        loggerFactory.AddConsole(LogLevel.Trace).AddDebug() |> ignore
        

    [<EntryPoint>]
    let main args =
        let url = "http://localhost:8123"
        WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .Configure(fun app -> app |> configureApp)
            .ConfigureServices(fun s -> s |> configureServices)
            .ConfigureLogging(Action<ILoggerFactory> configureLogging)
            .Build()
            .Run()
        0