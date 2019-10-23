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
    

let getClient =
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0));
    async {
        let! resClient = HasuraWebSocketClient.ConnectAsync "ws://localhost:8080/v1/graphql" cts.Token         
        return match resClient with | Ok r -> r |Error e -> failwith(e)
    }



[<Test>]
let ``Connect to Hasura and subscribe manually``() =


    let subscription = """
        subscription {
        module {
            id
        }             
      }
    """

    let msg = HasuraMessage.Start "1" subscription 

    TestContext.Progress.WriteLine(getJson msg)

    let tcs = new TaskCompletionSource<HasuraMessage>(TimeSpan.FromSeconds(20.0));
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0));

    let parseData m =
        match m.payload with
        | Some p ->
            (match p with 
            | D d -> Some (d.data.JsonValue.ToString (JsonSaveOptions.None) |> getArrkModules )
            | _ -> None )
        | None -> None

    
    async {
        use! client = getClient

        use _ =
            client.Receiver
                .Do(parseData >> TestContext.Progress.WriteLine)
                .Do(getJson >> TestContext.Progress.WriteLine)
                .Where(parseData >> (fun d -> d.IsSome))
                .Subscribe(
                    tcs.TrySetResult >> ignore, //set the query result
                    (fun (ex : Exception) -> tcs.TrySetException ex) >> ignore)

        client.Sender.OnNext(msg);

        let! res = Async.AwaitTask tcs.Task
        
        Assert.IsNotNull(res);
    } |> Async.RunSynchronously |> ignore
    

[<Test>]
let ``Connect to Hasura and subscribe using Subscribe method``() =

    let query = """ worker { name } """

    let tcs = new TaskCompletionSource<string>(TimeSpan.FromSeconds(20.0));

    async {
        use! client = getClient

        use _ =
            (client.Subscribe query) // <-- this is the important part
                .Do(fun s -> TestContext.Progress.WriteLine(s))
                .Subscribe(
                    tcs.TrySetResult >> ignore, //set the query result
                    (fun (ex : Exception) -> tcs.TrySetException ex) >> ignore)

        let! res = Async.AwaitTask tcs.Task        
        Assert.IsNotEmpty(res)        
    } |> Async.RunSynchronously |> ignore

[<Test>]
let ``Connect to Hasura and subscribe using Subscribe method and wait``() =
    
    let query = """ worker { name } """

    let tcs = new TaskCompletionSource<string>(TimeSpan.FromSeconds(20.0));

    async {
        use! client = getClient

        use _ =
            (client.Subscribe query) // <-- this is the important part
                .Do(fun s -> TestContext.Progress.WriteLine(s))
                .Subscribe(
                    ignore, //set the query result
                    (fun (ex : Exception) -> tcs.TrySetException ex) >> ignore)

        let! res = Async.AwaitTask tcs.Task        
        Assert.IsNotEmpty(res)        
    } |> Async.RunSynchronously |> ignore