namespace CSharpLanguageServer.Tests

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types

open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type WorkspaceSymbolTests() =

    [<Test>]
    member _.``workspace symbol request should provide symbols``() =
        use client =
            setupServerClient defaultClientProfile "TestData/testWorkspaceSymbolWorks"

        client.StartAndWaitForSolutionLoad()

        let serverCaps = client.GetState().ServerCapabilities.Value

        serverCaps.WorkspaceSymbolProvider
        |> should equal (true |> U2<bool, WorkspaceSymbolOptions>.C1 |> Some)

        use classFile = client.Open("Project/Class.cs")

        let completionParams0: WorkspaceSymbolParams =
            { WorkDoneToken = None
              PartialResultToken = None
              Query = "Class" }

        let symbols0: U2<SymbolInformation[], WorkspaceSymbol[]> option =
            client.Request("workspace/symbol", completionParams0)

        match symbols0 with
        | Some(U2.C1 sis) ->
            sis.Length |> should equal 1

            let sym0 = sis[0]
            sym0.Name |> should equal "Class"
            sym0.Kind |> should equal SymbolKind.Class
            sym0.Tags.IsSome |> should be False
            sym0.ContainerName.IsSome |> should be False
            sym0.Location.Uri |> should equal classFile.Uri
            ()

        | _ -> failwith "Some U2.C1 was expected"
