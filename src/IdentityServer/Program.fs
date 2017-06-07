namespace IdentityServer



open System
open System.IO
open System.Collections.Generic
open System.Threading
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Features
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open IdentityServer4
open IdentityServer4.Models
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
module Main =


    let webApp = 
        choose [
            GET >=> route "/account/login" >=> text "pity the foo"

        ]
    let configureServices (services : IServiceCollection) =
        
        services
            .AddCors()
            .AddIdentityServer()
            .AddTemporarySigningCredential()
            .AddInMemoryIdentityResources(Config.getIdentityResources())
            .AddInMemoryApiResources(Config.getApiResources())
            .AddInMemoryClients(Config.getClients())
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