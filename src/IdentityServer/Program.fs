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

module Config =
    let getApiResources () =
        [
            ApiResource("api","my api")
        ]

    let getClients () =
        [
            Client(
                ClientId = "client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = ResizeArray<_> [ Secret("secret".Sha256()) ],
                AllowedScopes = ResizeArray<_> ["api"])
        ]
module Main =

    let configureServices (services : IServiceCollection) =
        services
            .AddIdentityServer()
            .AddTemporarySigningCredential()
            .AddInMemoryApiResources(Config.getApiResources())
            .AddInMemoryClients(Config.getClients())
            |> ignore
    let configureApp (app : IApplicationBuilder) = 
    
        app.UseIdentityServer() |> ignore
        ()
        

    [<EntryPoint>]
    let main args =
        let url = "http://localhost:8123"
        WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .Configure(fun app -> app |> configureApp)
            .ConfigureServices(fun s -> s |> configureServices)
            .Build()
            .Run()
        0