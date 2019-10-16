module GraphQlClient.Tests

open NUnit.Framework
open GraphQLClient
open System.Text.Json
open FSharp.Data
open System
open GraphQLClient.Model
open GraphQLClient.Clients
open System.Threading.Tasks
open System.Threading
open System.Reactive
open System.Reactive.Linq

[<SetUp>]
let Setup() = ()
    

[<Test>]
let assertMappings() =
    let json = """{ "type" : "ka" }"""
    //let json = """{ "type" : "Hello", "id" : "Xbuben", "payload" : { "query" : "lel"} }"""
    let msg = getHasuraMessage json
    
    Assert.AreEqual("ka", msg.``type``)
    Assert.AreEqual(None, msg.id)

    let roundJson = getJson msg
    //Assert.AreEqual(json, roundJson)
    Assert.Pass()

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

    let tcs = new TaskCompletionSource<HasuraMessage>(TimeSpan.FromSeconds(20.0));
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(20.0));
    
    async {
        let! resClient = HasuraWebSocketClient.ConnectAsync "ws://localhost:8080/v1/graphql" cts.Token         
        let client = match resClient with | Ok r -> r |Error e -> failwith(e)

        use sub =
            client.Receiver
                .Do(getJson >> TestContext.Progress.WriteLine)
                .Subscribe(tcs.TrySetResult >> ignore);

        client.Sender.OnNext(msg);

        let! res = Async.AwaitTask tcs.Task
        
        Assert.IsNotNull(res);
    } |> Async.RunSynchronously |> ignore
    

