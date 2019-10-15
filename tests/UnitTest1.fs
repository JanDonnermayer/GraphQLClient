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

[<SetUp>]
let Setup() = ()
    

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

    
    let client = await HasuraWebSocketClient.ConnectAsync("ws://localhost:8080/v1/graphql", cts.Token);

    using var sub = client.Receiver
        .Where(m => m.Payload != null)
        .Subscribe(tcs.SetResult);

    client.Sender.OnNext(msg);

    var res = await tcs.Task;
    Assert.IsNotNull(res);
