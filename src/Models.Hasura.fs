namespace GraphQLClient.Models.Hasura

open FSharp.Data

type HasuraSchema = JsonProvider<"""
[
    { "type" : "ka", "id" : "as0", "payload" : { "data" : { } }  },
    { "type" : "ka", "id" : "" },    
    { "type" : "ka" },    
    { "type" : "ka", "payload" : { "query" : "{ Super_GaphQLData } }" }  },
    { "type" : "ka", "payload" : "Couldnt connect" }
]""">

type MessagePayload = string

type QueryPayload =
    { query: string }

type SubscriptionPayload =
    { subscription: string }

type DataPayload =
    { data: HasuraSchema.Data }

type Payload =
    | D of DataPayload
    | Q of QueryPayload
    | M of MessagePayload

type HasuraMessage =
    { kind: string
      id: Option<string>
      payload: Option<Payload> }

    static member Empty =
        { kind = "ka"
          id = None
          payload = None }

    static member ConAck =
        { kind = "connection_ack"
          id = None
          payload = None }

    static member ConInit =
        { kind = "connection_init"
          id = None
          payload = None }
    
    //  Start a subscription or query
    static member Start id query =
        { kind = "start"
          id = Some id
          payload = Some(Q { query = query }) }   

    // Stop as subscription or query      
    static member Stop id =
        { kind = "stop"
          id = Some id
          payload = None }   

[<AutoOpen>]
module Conversion =

    let rec dropNullFields = function
        | JsonValue.Record flds ->
            flds 
            |> Array.choose (fun (k, v) -> 
              if v = JsonValue.Null then None else
              Some(k, dropNullFields v) )
            |> JsonValue.Record
        | JsonValue.Array arr -> 
            arr |> Array.map dropNullFields |> JsonValue.Array
        | json -> json

    let tryValue v = match v with null -> None | _ -> Some v

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
        ("[" + json  + "]") 
        |> HasuraSchema.Parse 
        |> Array.head
        |> (fun e ->
        { kind = e.Type
          id = e.Id
          payload = e.Payload |> getPayload })

    let getRoot (m: HasuraMessage) =
        HasuraSchema.Root
            (m.kind, m.id,
             (match m.payload with
              | Some p ->
                  match p with
                  | D d -> HasuraSchema.StringOrPayload(HasuraSchema.Payload(Some d.data, None))
                  | Q q -> HasuraSchema.StringOrPayload(HasuraSchema.Payload(None, Some q.query))
                  | M m -> HasuraSchema.StringOrPayload(m)
              | _ -> HasuraSchema.StringOrPayload()))
              
   
    let stringDataPayload m =
        match m.payload with
        | Some p ->
            (match p with 
            | D d -> Some (d.data.JsonValue.ToString (JsonSaveOptions.None))
            | _ -> None )
        | None -> None

    let getJson (m: HasuraMessage) = ((m |> getRoot).JsonValue |> dropNullFields |> (fun v -> v.ToString)) JsonSaveOptions.DisableFormatting
