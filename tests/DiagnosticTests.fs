namespace CSharpLanguageServer.Tests

open System.Threading

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.Server

open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type DiagnosticTests() =

    [<Test>]
    member _.``push diagnostics should provide diagnostics``() =
        use client =
            setupServerClient defaultClientProfile "TestData/testPushDiagnosticsWork"

        client.StartAndWaitForSolutionLoad()

        //
        // open Class.cs file and wait for diagnostics to be pushed
        //
        use classFile = client.Open("Project/Class.cs")

        Thread.Sleep(4000)

        let state = client.GetState()
        let version0, diagnosticList0 = state.PushDiagnostics |> Map.find classFile.Uri

        version0 |> should equal None

        diagnosticList0.Length |> should equal 3

        let diagnostic0 = diagnosticList0.[0]
        diagnostic0.Message |> should equal "Identifier expected"
        diagnostic0.Severity |> should equal (Some DiagnosticSeverity.Error)
        diagnostic0.Range.Start.Line |> should equal 0
        diagnostic0.Range.Start.Character |> should equal 3

        let diagnostic1 = diagnosticList0.[1]
        diagnostic1.Message |> should equal "; expected"

        let diagnostic2 = diagnosticList0.[2]

        diagnostic2.Message
        |> should
            equal
            "The type or namespace name 'XXX' could not be found (are you missing a using directive or an assembly reference?)"

        //
        // now change the file to contain no content (and thus no diagnostics)
        //
        classFile.DidChange ""

        Thread.Sleep 4000

        let state = client.GetState()
        let version1, diagnosticList1 = state.PushDiagnostics |> Map.find classFile.Uri

        version1 |> should equal None
        diagnosticList1.Length |> should equal 0
        ()


    [<Test>]
    member _.testPullDiagnosticsWork() =
        use client =
            setupServerClient defaultClientProfile "TestData/testPullDiagnosticsWork"

        client.StartAndWaitForSolutionLoad()

        //
        // open Class.cs file and pull diagnostics
        //
        use classFile: FileController = client.Open("Project/Class.cs")

        let diagnosticParams: DocumentDiagnosticParams =
            { WorkDoneToken = None
              PartialResultToken = None
              TextDocument = { Uri = classFile.Uri }
              Identifier = None
              PreviousResultId = None }

        let report0: DocumentDiagnosticReport option =
            client.Request("textDocument/diagnostic", diagnosticParams)

        match report0 with
        | Some(U2.C1 report) ->
            report.Kind |> should equal "full"
            report.ResultId |> should equal None
            report.Items.Length |> should equal 3

            let diagnostic0 = report.Items.[0]
            diagnostic0.Range.Start.Line |> should equal 0
            diagnostic0.Range.Start.Character |> should equal 3
            diagnostic0.Severity |> should equal (Some DiagnosticSeverity.Error)
            diagnostic0.Message |> should equal "Identifier expected"

            diagnostic0.CodeDescription.Value.Href
            |> should equal "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS1001)"

            let diagnostic1 = report.Items.[1]
            diagnostic1.Message |> should equal "; expected"

            let diagnostic2 = report.Items.[2]

            diagnostic2.Message
            |> should
                equal
                "The type or namespace name 'XXX' could not be found (are you missing a using directive or an assembly reference?)"
        | _ -> failwith "U2.C1 is expected"

        //
        // now try to do the same but with file fixed to contain no content (and thus no diagnostics)
        //
        classFile.DidChange("")

        let report1: DocumentDiagnosticReport option =
            client.Request("textDocument/diagnostic", diagnosticParams)

        match report1 with
        | Some(U2.C1 report) ->
            report.Kind |> should equal "full"
            report.ResultId |> should equal None
            report.Items.Length |> should equal 0
        | _ -> failwith "U2.C1 is expected"

        ()

    [<Test>]
    member _.testWorkspaceDiagnosticsWork() =
        use client =
            setupServerClient defaultClientProfile "TestData/testWorkspaceDiagnosticsWork"

        client.StartAndWaitForSolutionLoad()

        let diagnosticParams: WorkspaceDiagnosticParams =
            { WorkDoneToken = None
              PartialResultToken = None
              Identifier = None
              PreviousResultIds = Array.empty }

        let report0: WorkspaceDiagnosticReport option =
            client.Request("workspace/diagnostic", diagnosticParams)

        match report0 with
        | Some report0 ->
            report0.Items.Length |> should equal 3

            match report0.Items[0] with
            | U2.C1 fullReport ->
                fullReport.Kind |> should equal "full"
                fullReport.ResultId |> should equal None
                fullReport.Items.Length |> should equal 3

                let diagnostic0 = fullReport.Items[0]
                diagnostic0.Code.IsSome |> should equal true
                diagnostic0.Message |> should equal "Identifier expected"

            | _ -> failwith "'U2.C1' was expected"

        | _ -> failwith "'Some' was expected"


    [<Test>]
    member _.testWorkspaceDiagnosticsWorkWithStreaming() =
        use client =
            setupServerClient defaultClientProfile "TestData/testWorkspaceDiagnosticsWork"

        client.StartAndWaitForSolutionLoad()

        let partialResultToken: ProgressToken = System.Guid.NewGuid() |> string |> U2.C2

        let diagnosticParams: WorkspaceDiagnosticParams =
            { WorkDoneToken = None
              PartialResultToken = Some partialResultToken
              Identifier = None
              PreviousResultIds = Array.empty }

        let report0: WorkspaceDiagnosticReport option =
            client.Request("workspace/diagnostic", diagnosticParams)

        // report should have 0 results, all of them streamed to lsp client via $/progress instead
        match report0 with
        | Some report0 -> report0.Items.Length |> should equal 0
        | _ -> failwith "'Some' was expected"

        let progress = client.GetProgressParams partialResultToken
        progress.Length |> should equal 3

        let report0 = progress[0].Value |> deserialize<WorkspaceDiagnosticReport>
        report0.Items.Length |> should equal 1

        match report0.Items[0] with
        | U2.C1 fullReport ->
            fullReport.Kind |> should equal "full"
            fullReport.ResultId |> should equal None
            fullReport.Items.Length |> should equal 3

            let diagnostic0 = fullReport.Items[0]
            diagnostic0.Code.IsSome |> should equal true
            diagnostic0.Message |> should equal "Identifier expected"

        | _ -> failwith "'U2.C1' was expected"

        let report1 =
            progress[1].Value |> deserialize<WorkspaceDiagnosticReportPartialResult>

        report1.Items.Length |> should equal 1
