module GraphQlClient.Tests

open NUnit.Framework
open GraphQLClient
open System.Text.Json
open FSharp.Data
open FSharp.Core
open System
open GraphQLClient.Models.Hasura
open GraphQLClient.Models.GraphQl
open GraphQLClient.Clients
open System.Threading.Tasks
open System.Threading

open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency



[<SetUp>]
let Setup() = ()


let getClient url =
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0))
    async { return! Async.AwaitTask <| HasuraWebSocketClient.ConnectAsync url cts.Token }



[<Test>]
let ``Connect to Hasura and subscribe using Subscribe method``() =

    let query = """ worker { name colour } """

    let tcs = new TaskCompletionSource<string>(TimeSpan.FromSeconds(20.0))

    
    async {
        use! client = getClient "ws://localhost:8080/v1/graphql"

        use _ =
            (client.Subscribe query) // <-- this is the important part
                .Do(fun s -> TestContext.Progress.WriteLine(s))
                .Subscribe(tcs.TrySetResult >> ignore, (fun (ex: Exception) -> tcs.TrySetException ex) >> ignore) //set the query result

        let! res = Async.AwaitTask tcs.Task
        Assert.IsNotEmpty(res)
    }
    |> Async.RunSynchronously
    |> ignore


[<Test>]
let ``Connect to Hasura and subscribe very often``() =

    let query = """ worker { name } """

    let mutable results = 0
    let tcs = new TaskCompletionSource<string>(TimeSpan.FromSeconds(200.0))

    let incrOrComplete res =
        match results with
        | 4999 -> do tcs.TrySetResult(res) |> ignore
        | _ -> results <- results + 1

    async {
        use! client = getClient "ws://localhost:8080/v1/graphql"

        let disp =
            List.init 5000
                (fun s ->
                (client.Subscribe query).Subscribe // <-- this is the important part
                    (incrOrComplete,
                     //tcs.TrySetResult >> ignore, //set the query result
                     (fun (ex: Exception) -> tcs.TrySetException ex) >> ignore))


        let! res = Async.AwaitTask tcs.Task
        disp |> List.iter (fun s -> s.Dispose())
        Assert.IsNotEmpty(res)
    }
    |> Async.RunSynchronously
    |> ignore




    