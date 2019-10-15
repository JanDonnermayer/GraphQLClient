namespace GraphQLClient
open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects


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
    { ``type``: Option<string>
      id: Option<string>
      payload: Option<Payload> }

type WebSocketClient() =
    let disposeHandle : IDisposable = null
    let receiver = new Subject<byte[]>()
    let sender = new Subject<byte[]>()

    member this.Receiver = receiver.AsObservable()
    member this.Sender = sender.AsObserver()

type HasuraWebSocketClient(client : WebSocketClient) =    
    let disposeHandle : IDisposable = null
    let sender  = new Subject<HasuraMessage>()
    let receiver = new Subject<HasuraMessage>()
    
    member this.Receiver = receiver.AsObservable()
    member this.Sender = sender.AsObserver()
   