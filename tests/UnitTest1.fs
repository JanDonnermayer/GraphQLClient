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


type s = {
    kind : string
    name : Option<string>
}

[<SetUp>]
let Setup() = ()
    

let getClient url =
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0));
    async {
        let! resClient = HasuraWebSocketClient.ConnectAsync url cts.Token         
        return match resClient with | Ok r -> r |Error e -> failwith(e)
    }




[<Test>]
let ``Connect to Hasura and subscribe using Subscribe method``() =

    let query = """ worker { name } """

    let tcs = new TaskCompletionSource<string>(TimeSpan.FromSeconds(20.0));

    async {
        use! client = getClient "ws://localhost:8080/v1/graphql"

        use _ =
            (client.Subscribe query) // <-- this is the important part
                .Do(fun s -> TestContext.Progress.WriteLine(s))
                .Subscribe(
                    tcs.TrySetResult >> ignore, //set the query result
                    (fun (ex : Exception) -> tcs.TrySetException ex) >> ignore)

        let! res = Async.AwaitTask tcs.Task        
        Assert.IsNotEmpty(res)        
    } |> Async.RunSynchronously |> ignore
