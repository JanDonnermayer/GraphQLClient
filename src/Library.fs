namespace GraphQLClient

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open FSharp.Data

module Model =

    type HasuraSchema = JsonProvider<"""
    [
        { "type" : "ka", "payload" : { "data" : "lel" },  "id" : "as0" },
        { "type" : "ka", "payload" : { "query" : ""   }},
        { "type" : "ka", "payload" : "Couldnt connect" },
        { "type" : "ka"}
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

        static member ConStart query id =
            { ``type`` = "start"
              id = Some id
              payload = Some(Q { query = query }) }


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

    let matchPayload (e: HasuraSchema.StringOrPayload) =
        match (e.Record, e.String) with
        | (Some r, _) ->
            match (r.Data, r.Query) with
            | (Some d, _) -> Some(D { data = d })
            | (_, Some q) -> Some(Q { query = q })
            | _ -> None
        | (_, Some s) -> Some(M s)
        | _ -> None

    let tryHasuraMessage json =
        HasuraSchema.Parse json
        |> Array.head
        |> (fun e ->
        { ``type`` = e.Type
          id = e.Id
          payload = matchPayload e.Payload })



open Model
open System.Text
open System.Threading.Tasks

module Clients =
    type WebSocketClient() =
        let receiver = new Subject<byte []>()
        let sender = new Subject<byte []>()

        member this.Receiver = receiver.AsObservable()
        member this.Sender = sender.AsObserver()

    type HasuraWebSocketClient(client: WebSocketClient) =

        let byteToMsg b = Encoding.UTF8.GetString b |> tryHasuraMessage

        let msgToByte m = [| (byte) 0 |]

        let sender = new Subject<HasuraMessage>()
        let receiver = new Subject<HasuraMessage>()

        let disposables =
            [ client.Receiver.Select(byteToMsg).Subscribe(receiver)
              sender.Select(msgToByte).Subscribe(client.Sender) ]

        member this.Receiver = receiver.AsObservable()
        member this.Sender = sender.AsObserver()

        interface IDisposable with
            member this.Dispose() = disposables |> List.iter (fun x -> x.Dispose())

    // needs refactor
    let handshakeAsync (client: HasuraWebSocketClient) (ct: CancellationToken) =
        async {
            let cts = TaskCompletionSource<HasuraMessage>()
            use _ = client.Receiver.FirstAsync((fun m -> m.Equals(HasuraMessage.ConAck))).Subscribe(cts.SetResult)
            client.Sender.OnNext(HasuraMessage.ConInit)

            let! res = Async.AwaitTask(Task.WhenAny(cts.Task, Task.Delay(-1, ct)))
            return if (res.Id = cts.Task.Id) then Ok client
                   else Error "Didn't receive the connection-acknowledged message!"
        }
