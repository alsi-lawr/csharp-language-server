namespace CSharpLanguageServer.Tests

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types
open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type CodeActionTests() =

    static let assertCodeActionHasTitle (ca: CodeAction, title: string) =
            ca.Title |> should equal title
            ca.Kind |> should equal None
            ca.Diagnostics |> should equal None
            ca.Disabled |> should equal None
            ca.Edit.IsSome |> should be True

    static let mutable client: ClientController =
        setupServerClient defaultClientProfile "TestData/testCodeActions"

    let mutable caResults: TextDocumentCodeActionResult option = None

    [<OneTimeSetUp>]
    member _.Setup() =
        client.StartAndWaitForSolutionLoad()
        use classFile = client.Open "Project/Class.cs"

        let caArgs: CodeActionParams =
            { TextDocument = { Uri = classFile.Uri }
              Range =
                { Start = { Line = 1u; Character = 0u }
                  End = { Line = 1u; Character = 0u } }
              Context =
                { Diagnostics = [||]
                  Only = None
                  TriggerKind = None }
              WorkDoneToken = None
              PartialResultToken = None }

        caResults <-
            match client.Request("textDocument/codeAction", caArgs) with
            | Some opts -> opts
            | None -> failwith "Expected code actions, received none"

    [<Test>]
    member _.``generate overrides action should generate overrides``() =
        match caResults |> Option.bind (Array.tryItem 0) with
        | Some (U2.C2 ca) ->
            assertCodeActionHasTitle (ca, "Generate overrides...")
            // TODO: match extract base class edit structure

        | _ -> failwith "Generate overrides code action not found"

    [<Test>]
    member _.``generate constructor action should generate a constructor``() =
        match caResults |> Option.bind (Array.tryItem 2) with
        | Some (U2.C2 ca) ->
            assertCodeActionHasTitle (ca, "Generate constructor 'Class()'")
            // TODO: match extract base class edit structure

        | _ -> failwith "Generate constructor code action not found"

    [<Test>]
    member _.``add attribute action should add the attribute``() =
        match caResults |> Option.bind (Array.tryItem 4) with
        | Some (U2.C2 ca) ->
            assertCodeActionHasTitle (ca, "Add 'DebuggerDisplay' attribute")
            // TODO: match extract base class edit structure

        | _ -> failwith "Add attribute code action not found"

    [<Test>]
    member _.``extract base class request extracts base class``() =
        match caResults |> Option.bind (Array.tryItem 3) with
        | Some (U2.C2 ca) ->
             assertCodeActionHasTitle (ca, "Extract base class...")
            // TODO: match extract base class edit structure

        | _ -> failwith "Extract base class code action not found"

    [<Test>]
    member _.``extract interface code action should extract an interface``() =
        let codeAction =
            match caResults |> Option.bind (Array.tryItem 1) with
            | Some(U2.C2 ca) ->
                assertCodeActionHasTitle (ca, "Extract interface...")
                ca
            | _ -> failwith "Extract interface action not found"

        let expectedImplementInterfaceEdits =
            { Range =
                { Start = { Line = 0u; Character = 11u }
                  End = { Line = 0u; Character = 11u } }
              NewText = " : IClass" }

        let expectedCreateInterfaceEdits =
            { Range =
                { Start = { Line = 0u; Character = 0u }
                  End = { Line = 0u; Character = 0u } }
              NewText = "internal interface IClass\n{\n    void Method(string arg);\n}" }

        match codeAction.Edit with
        | Some { DocumentChanges = Some [| C1 create; C1 implement |] } ->
            match create.Edits, implement.Edits with
            | [| U2.C1 createEdits |], [| U2.C1 implementEdits |] ->
                createEdits
                |> TextEdit.normalizeNewText
                |> should equal expectedCreateInterfaceEdits

                implementEdits
                |> TextEdit.normalizeNewText
                |> should equal expectedImplementInterfaceEdits

            | _ -> failwith "Expected exactly one U2.C1 edit in both create/implement"

        | _ -> failwith "Unexpected edit structure"
