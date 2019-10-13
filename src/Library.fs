namespace GraphQLClient



type Payload =
    | M of string
    | D of int


type HasuraMessage =
    {
        ``type``    : string
        id          : string
        payload     : Payload
    }
