namespace GraphQLClient


type MessagePayload = string

type QueryPayload =
    { query: string }

type DataPayload =
    { data: string }

type Payload =
    | M of MessagePayload
    | D of DataPayload
    | Q of QueryPayload

type HasuraMessage =
    { ``type``: Option<string>
      id: Option<string>
      payload: Option<Payload> }
