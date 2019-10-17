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
    

[<Test>]
let Test2() =
    let res = Requests.testRun()
    Assert.IsNotNull res 


[<Test>]
let Test1() =

    let query =
            """{
            arrk_task_tracker_module {
                id
                name
                module_workers {    
                    worker {
                        id
                        name
                    }
                }
            }        
        }"""



    let msg = HasuraMessage.Start "1" query 

    TestContext.Progress.WriteLine(getJson msg)

    let tcs = new TaskCompletionSource<HasuraMessage>(TimeSpan.FromSeconds(20.0));
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0));
    
    async {
        let! resClient = HasuraWebSocketClient.ConnectAsync "ws://localhost:8080/v1/graphql" cts.Token         
        use client = match resClient with | Ok r -> r |Error e -> failwith(e)

        use sub =
            client.Receiver
                .Do(getJson >> TestContext.Progress.WriteLine)
                .Subscribe(tcs.TrySetResult >> ignore);

        client.Sender.OnNext(msg);

        let! res = Async.AwaitTask tcs.Task
        
        Assert.IsNotNull(res);
    } |> Async.RunSynchronously |> ignore
    

