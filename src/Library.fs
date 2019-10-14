namespace GraphQLClient


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
