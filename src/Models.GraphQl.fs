namespace GraphQLClient.Models.GraphQl

open FSharp.Data.GraphQL
open FSharp.Data

//type GraphSchema  = GraphQLProvider<"http://localhost:8080/v1/graphql">

type GraphSchema = JsonProvider<"""{
    "arrk_task_tracker_module": [
      {
        "id": "23d16e3e-09b3-4297-a040-37cf97296535",
        "name": "module_1",
        "module_workers": [
          {
            "worker": {
              "id": "3d00e538-c3d0-47b6-b885-f3f059c1a3fa",
              "name": "Elisa1"
            }
          }
        ]
      },
      {
        "id": "4c095eaf-e758-4cb6-b80c-f078699d8c24",
        "name": "module_2",
        "module_workers": []
      }
    ]
  }
""">

type ArrkWorker =
    { id: string
      name: string }

type ArrkModule =
    { id: string
      name: string
      workers: ArrkWorker [] }



[<AutoOpen>]
module Conversions =

    let workerToWorker (w: GraphSchema.Worker) =
        { id = w.Id.ToString()
          name = w.Name }

    let getWorker (m: GraphSchema.ModuleWorker) = m.Worker

    let moduleToModule (m: GraphSchema.ArrkTaskTrackerModule) =
        { id = m.Id.ToString()
          name = m.Name
          workers =
              m.ModuleWorkers
              |> Array.map getWorker
              |> Array.map workerToWorker }

    let getArrkModules (json) =
        GraphSchema.Parse json |> (fun m -> m.ArrkTaskTrackerModule |> Array.map moduleToModule)
