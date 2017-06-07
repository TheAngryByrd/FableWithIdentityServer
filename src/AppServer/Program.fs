namespace AppServer


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
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware


module Main =

    [<CLIMutable>]
    type Post = {
        userId : int
        id: int
        title: string
        body : string
    }

    type User = {
        userId : int
        username : string

    }
    let posts = 
        [
            {
                userId = 0
                id = 0
                title = "Foo"
                body = "baz"
            }
        ]
    let users = 
        [
            {
                userId = 0
                username = "covfefe"
            }
        ]
    let accessDenied = setStatusCode 401 >=> text "Access Denied"

    let mustBeUser = requiresAuthentication accessDenied
    let webApp = 
        choose [
            subRoute "/api"(
                GET >=>
                    choose [
                        route "/posts" >=> json posts
                        mustBeUser >=> route "/users" >=> json  users
                    ]
            )
        ]

    
    let identityServerAuthenticationOptions = 
        IdentityServerAuthenticationOptions(
            Authority = "http://localhost:8123",
            RequireHttpsMetadata = false,
            ApiName = "appServerApi"
    )

    let configureApp (app : IApplicationBuilder) = 
    //     app.UseGiraffeErrorHandler errorHandler
    //     app.UseCookieAuthentication cookieAuth |> ignore
    //     app.UseStaticFiles() |> ignore
        app.UseIdentityServerAuthentication identityServerAuthenticationOptions |> ignore
        app.UseGiraffe webApp

    let configureServices (services : IServiceCollection) =
        services.AddAuthentication()
        |> ignore
    let configureLogging (loggerFactory : ILoggerFactory) =
        loggerFactory.AddConsole(LogLevel.Trace).AddDebug() |> ignore
    [<EntryPoint>]
    let main args =
        let url = "http://localhost:8085"
        WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .Configure(fun app -> app |> configureApp)
            .ConfigureServices(fun s -> s |> configureServices)
            .ConfigureLogging(Action<ILoggerFactory> configureLogging)
            .Build()
            .Run()
        0