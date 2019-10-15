namespace GraphQLClient

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open FSharp.Data
open FSharp.Data.JsonExtensions

module Model =

    type HasuraSchema = JsonProvider<"""
    [
        { "type" : "ka", "id" : "as0", "payload" : { "data" : "lel" }  },
        { "type" : "ka", "payload" : { "query" : "{ Super_GaphQLData } }" }  },
        { "type" : "ka", "payload" : "Couldnt connect" },
        { "type" : "ka" }
    ]""">

    type MessagePayload = string

    type QueryPayload =
        { query: string }

    type DataPayload =
        { data: string }

    type Payload =
        | D of DataPayload
        | Q of QueryPayload
        | M of MessagePayload

    type HasuraMessage =
        { ``type``: string
          id: Option<string>
          payload: Option<Payload> }

        static member ConAck =
            { ``type`` = "conection_ack"
              id = None
              payload = None }

        static member ConInit =
            { ``type`` = "conection_init"
              id = None
              payload = None }

        static member Start query id =
            { ``type`` = "start"
              id = Some id
              payload = Some(Q { query = query }) }

    let getPayload (e: HasuraSchema.StringOrPayload) =
        match (e.Record, e.String) with
        | (Some r, _) ->
            match (r.Data, r.Query) with
            | (Some d, _) -> Some(D { data = d })
            | (_, Some q) -> Some(Q { query = q })
            | _ -> None
        | (_, Some s) -> Some(M s)
        | _ -> None

    let getHasuraMessage json =
        HasuraSchema.Parse json
        |> Array.head
        |> (fun e ->
        { ``type`` = e.Type
          id = e.Id
          payload = getPayload e.Payload })

    let getRoot (m: HasuraMessage) =
        HasuraSchema.Root
            (m.``type``, m.id,
             (match m.payload with
              | Some p ->
                  match p with
                  | D d -> HasuraSchema.StringOrPayload(HasuraSchema.Payload(Some d.data, None))
                  | Q q -> HasuraSchema.StringOrPayload(HasuraSchema.Payload(None, Some q.query))
                  | M m -> HasuraSchema.StringOrPayload(m)
              | _ -> HasuraSchema.StringOrPayload()))


    let getJson (m: HasuraMessage) = (m |> getRoot).JsonValue.ToString JsonSaveOptions.None


open Model
open System.Text
open System.Threading.Tasks
open System.Net.WebSockets

module Clients =
    type WebSocketClient private (clientFactory: unit -> ClientWebSocket) =
        let receiver = new Subject<byte []>()
        let sender = new Subject<byte []>()
        let client = clientFactory()

        let subscribeReceiver() =
            let cts = new CancellationTokenSource()
            let receive =
                async {
                    while true do
                        let buffer = ArraySegment<byte>(Array.zeroCreate 8192)
                        let! _ = Async.AwaitTask(client.ReceiveAsync(buffer, cts.Token))
                        receiver.OnNext(buffer.Array)
                }
            Async.Start(receive, cts.Token)
            cts :> IDisposable

        let senderSubscribe() =
            let sendAsync m =
                client.SendAsync(new ArraySegment<byte>(m), WebSocketMessageType.Text, true, CancellationToken.None)
                      .ConfigureAwait(false) |> ignore
            sender.Subscribe(sendAsync)

        let disposables: IDisposable list =
            [ client :> IDisposable
              subscribeReceiver()
              senderSubscribe() ]

        member public __.Receiver = receiver.AsObservable()
        member public __.Sender = sender.AsObserver()

        interface IDisposable with
            member __.Dispose() = disposables |> List.iter (fun x -> x.Dispose())

        static member public ConnectAsync (url: string) (protocol: string) (ct: CancellationToken) =
            let client = new ClientWebSocket()
            client.Options.AddSubProtocol protocol |> ignore
            async {
                Async.AwaitTask(client.ConnectAsync(Uri(url), ct)) |> ignore
                return new WebSocketClient(fun _ -> client)
            }


    type HasuraWebSocketClient private (clientFactory: unit -> WebSocketClient) =
        let byteToMsg b = Encoding.UTF8.GetString b |> getHasuraMessage
        let msgToByte m = getJson m |> Encoding.UTF8.GetBytes

        let sender = new Subject<HasuraMessage>()
        let receiver = new Subject<HasuraMessage>()
        let client = clientFactory()

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
                return if (res.Id = cts.Task.Id) then Ok client
                       else Error "Didn't receive the connection-acknowledged message!"
            }

        static member public ConnectAsync (url: string) (ct: CancellationToken) =
            async {
                let! innerClient = WebSocketClient.ConnectAsync url "graphql-ws" ct
                let client = new HasuraWebSocketClient(fun _ -> innerClient)
                return! client.HandshakeAsync ct
            }
   
