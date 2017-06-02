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


    let webApp = 
        choose [
            subRoute "/api"(
                GET >=>
                    choose [
                        route "/foo" >=> text "foo"
                    ]
            )
        ]


    let configureApp (app : IApplicationBuilder) = 
    //     app.UseGiraffeErrorHandler errorHandler
    //     app.UseCookieAuthentication cookieAuth |> ignore
    //     app.UseStaticFiles() |> ignore
        app.UseGiraffe webApp
    [<EntryPoint>]
    let main args =
        let url = "http://localhost:8085"
        WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .Configure(fun app -> app |> configureApp)
            .Build()
            .Run()
        0