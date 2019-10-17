namespace GraphQLClient.Models.GraphQl

open FSharp.Data.GraphQL

type GraphSchema  = GraphQLProvider<"http://localhost:8080/v1/graphql">


[<AutoOpen>]
module Requests =

    let graphType =
        GraphSchema.Types.Arrk_task_tracker_module    

    let operation =
        GraphSchema.Operation<"""
        subscription Modules {
            arrk_task_tracker_module {
                id
                name
                module_workers {    
                    worker {
                        id
                        name
                    }
                }
            }        
        }""">()

    let getContext() =
        GraphSchema.GetContext "http://localhost:8080/v1/graphql"
    
        
    
    let testRun =
        let context = getContext()
        let res = operation.Run context
        
        res.Data.Value.ToString
    
                       
 