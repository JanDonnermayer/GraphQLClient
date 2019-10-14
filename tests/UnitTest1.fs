module GraphQlClient.Tests

open NUnit.Framework
open GraphQLClient
open System.Text.Json


[<SetUp>]
let Setup() = ()

[<Test>]
let Test1() =

    let json1 = """{ "type" : "ka", "id" : "0", "payload" : { "data" : "lel" }  }"""
    let json2 = """{ "type" : "ka", "payload" : "XBuben" }"""

    use jsonDoc1 = JsonDocument.Parse(json1)
    use jsonDoc2 = JsonDocument.Parse(json2)

    let (=>>) f1 f2 = match f1 with Some v -> f2 v | _ -> None 

    let _tryProp (name : string) (o : JsonElement) =
        match o.TryGetProperty name with
        | (true, p) -> Some p
        | _ -> None

    let tryObject (e: JsonElement) =
        match e.ValueKind with
        | JsonValueKind.Object -> Some e
        | _ -> None

    let tryProp (e: JsonElement) (name: string) =
        tryObject e =>> _tryProp name

    let tryString (e: JsonElement) =
        match e.ValueKind with
        | JsonValueKind.String -> e.GetString() |> Some
        | _ -> None

    let tryStringProp (e: JsonElement) (name: string) =
        tryProp (e) (name) =>> tryString

    let tryQueryPayload (e: JsonElement) =
        match (tryStringProp e "query") with
        | Some s -> Some { query = s }
        | _ -> None
        
    let tryDataPayload (e: JsonElement) =
        match (tryStringProp e "data") with
        | Some s -> Some { data = s }
        | _ -> None

    let tryMessagePayload (e: JsonElement) = tryString e

    let tryPayload (e: JsonElement) =
        match (tryDataPayload e, tryQueryPayload e, tryMessagePayload e) with
        | (Some d, _, _) -> Some(D d)
        | (_, Some q, _) -> Some(Q q)
        | (_, _, Some m) -> Some(M m)
        | _ -> None

    let tryHasuraMessage (e: JsonElement) =
        match e.ValueKind with
        | JsonValueKind.Object ->
            Some
                { ``type`` = tryStringProp e "type"
                  id = tryStringProp e "id"
                  payload = tryProp e "payload" =>> tryPayload
                }          
        | _ -> None

    let res = jsonDoc1.RootElement |> tryHasuraMessage
    let res2 = jsonDoc2.RootElement |> tryHasuraMessage

    Assert.IsNotNull(res)
