namespace GraphQLClient.Clients

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Text
open System.Threading.Tasks
open System.Net.WebSockets
open GraphQLClient.Models.Hasura

type WebSocketClient private (client : ClientWebSocket) =
    let receiver = new Subject<byte []>()
    let sender = new Subject<byte []>()

    let subscribeReceiver =
        let cts = new CancellationTokenSource()
        let receive =
            async {
                while true do
                    let buffer = new ArraySegment<byte>(Array.zeroCreate<byte> 8192)
                    let! _ = Async.AwaitTask(client.ReceiveAsync(buffer, cts.Token))
                    receiver.OnNext(buffer.Array |> Array.takeWhile (fun e -> e <> (byte)0))
            }
        Async.Start(receive, cts.Token)
        cts :> IDisposable

    let senderSubscribe =
        let sendAsync m =
            client.SendAsync(new ArraySegment<byte>(m), WebSocketMessageType.Text, true, CancellationToken.None)
                  .ConfigureAwait(false) |> ignore
        sender.Subscribe(sendAsync)

    let disposables: IDisposable list =
        [ client :> IDisposable
          subscribeReceiver
          senderSubscribe ]

    member public __.Receiver = receiver.AsObservable()
    member public __.Sender = sender.AsObserver()

    interface IDisposable with
        member __.Dispose() = disposables |> List.iter (fun x -> x.Dispose())

    static member public ConnectAsync (url: string) (protocol: string) (ct: CancellationToken) =
        let client = new ClientWebSocket()
        client.Options.AddSubProtocol protocol |> ignore
        async {
            do! Async.AwaitTask(client.ConnectAsync(Uri(url), ct)) 
            return new WebSocketClient(client)
        }


type HasuraWebSocketClient private (client: WebSocketClient) =
    let byteToMsg b = Encoding.UTF8.GetString b |> getHasuraMessage
    let msgToByte m = getJson m |> Encoding.UTF8.GetBytes

    let sender = new Subject<HasuraMessage>()
    let receiver = new Subject<HasuraMessage>()
 
    let subscribeReceiver = client.Receiver.Select(byteToMsg).Subscribe(receiver)
    let senderSubscribe = sender.Select(msgToByte).Subscribe(client.Sender)

    let disposables =
        [ client :> IDisposable
          subscribeReceiver
          senderSubscribe ]

    member public __.Receiver = receiver.AsObservable()
    member public __.Sender = sender.AsObserver()

    interface IDisposable with
        member __.Dispose() = disposables |> List.iter (fun x -> x.Dispose())

    member private __.HandshakeAsync(ct: CancellationToken) =
        async {
            let cts = TaskCompletionSource<HasuraMessage>()

            //Await the connection-acknowledged message
            use _ = __.Receiver.FirstAsync((fun m -> m.Equals(HasuraMessage.ConAck))).Subscribe(cts.SetResult)
            __.Sender.OnNext(HasuraMessage.ConInit)

            let! res = Async.AwaitTask(Task.WhenAny(cts.Task, Task.Delay(-1, ct)))
            return if (res.Id = cts.Task.Id) then Ok __
                   else Error "Didn't receive the connection-acknowledged message!"
        }

    static member public ConnectAsync (url: string) (ct: CancellationToken) =
        async {
            let! innerClient = WebSocketClient.ConnectAsync url "graphql-ws" ct
            let client = new HasuraWebSocketClient(innerClient)
            return! client.HandshakeAsync ct
        }

