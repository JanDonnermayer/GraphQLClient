module GraphQlClient.Tests

open NUnit.Framework
open GraphQLClient
open Newtonsoft.Json

[<SetUp>]
let Setup () =
    ()

[<Test>]
let Test1 () =
    let obj = { ``type`` = "ka"; id = "0"; payload = D { data = "" } }
    let json = JsonConvert.SerializeObject(obj)
    let res = JsonConvert.DeserializeObject<HasuraMessage>(json)
    Assert.AreEqual(obj, res)
    
