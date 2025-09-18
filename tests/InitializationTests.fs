namespace CSharpLanguageServer.Tests

open NUnit.Framework

open CSharpLanguageServer.Tests.Tooling
open Ionide.LanguageServerProtocol.Types
open FsUnit

[<TestFixture>]
type InitializationTests() =

    static let assertHoverWorks (client: ClientController) file pos expectedMarkupContent =
        use classFile = client.Open file

        let hover0Params: HoverParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = pos
              WorkDoneToken = None }

        let hover0: Hover option = client.Request("textDocument/hover", hover0Params)

        match hover0 with
        | Some { Contents = U3.C1 markupContent
                 Range = None } ->
            markupContent.Kind |> should equal MarkupKind.Markdown
            markupContent.Value |> should equal expectedMarkupContent

        | x -> failwithf "'{ Contents = U3.C1 markupContent; Range = None }' was expected but '%s' received" (string x)


    [<Test>]
    member _.testServerRegistersCapabilitiesWithTheClient() =
        use client =
            setupServerClient defaultClientProfile "TestData/testServerRegistersCapabilitiesWithTheClient"

        client.StartAndWaitForSolutionLoad()

        let serverInfo = client.GetState().ServerInfo.Value
        serverInfo.Name |> should equal "csharp-ls"

        let serverCaps = client.GetState().ServerCapabilities.Value

        serverCaps.TextDocumentSync
        |> should
            equal
            ({ Change = Some TextDocumentSyncKind.Incremental
               OpenClose = Some true
               Save = Some(U2.C2 { IncludeText = Some true })
               WillSave = None
               WillSaveWaitUntil = None }
             |> U2<TextDocumentSyncOptions, TextDocumentSyncKind>.C1
             |> Some)

        serverCaps.Workspace
        |> should
            equal
            ({ WorkspaceFolders = None
               FileOperations = None }
             |> Some)

        serverCaps.HoverProvider
        |> should equal (true |> U2<bool, HoverOptions>.C1 |> Some)

        serverCaps.ImplementationProvider
        |> should
            equal
            (true
             |> U3<bool, ImplementationOptions, ImplementationRegistrationOptions>.C1
             |> Some)

        serverCaps.DocumentSymbolProvider
        |> should equal (true |> U2<bool, DocumentSymbolOptions>.C1 |> Some)

        serverCaps.DefinitionProvider
        |> should equal (true |> U2<bool, DefinitionOptions>.C1 |> Some)

        serverCaps.InlineValueProvider |> should equal null

        serverCaps.DiagnosticProvider
        |> should
            equal
            ({ DocumentSelector =
                Some
                    [| U2.C1
                           { Language = None
                             Scheme = Some "file"
                             Pattern = Some "**/*.cs" } |]
               WorkDoneProgress = None
               Identifier = None
               InterFileDependencies = false
               WorkspaceDiagnostics = true
               Id = None }
             |> U2<DiagnosticOptions, DiagnosticRegistrationOptions>.C2
             |> Some)

        serverCaps.DocumentHighlightProvider
        |> should equal (true |> U2<bool, DocumentHighlightOptions>.C1 |> Some)

        serverCaps.CompletionProvider
        |> should
            equal
            ({ WorkDoneProgress = None
               TriggerCharacters = Some [| "."; "'" |]
               AllCommitCharacters = None
               ResolveProvider = Some true
               CompletionItem = None }
             |> Some)

        serverCaps.CodeActionProvider
        |> should
            equal
            ({ WorkDoneProgress = None
               CodeActionKinds = None
               ResolveProvider = Some true }
             |> U2<bool, CodeActionOptions>.C2
             |> Some)

        serverCaps.RenameProvider
        |> should equal (true |> U2<bool, RenameOptions>.C1 |> Some)

        serverCaps.DeclarationProvider |> should equal null

        serverCaps.DocumentFormattingProvider
        |> should equal (true |> U2<bool, DocumentFormattingOptions>.C1 |> Some)

        serverCaps.ReferencesProvider
        |> should equal (true |> U2<bool, ReferenceOptions>.C1 |> Some, serverCaps.ReferencesProvider)

        serverCaps.WorkspaceSymbolProvider
        |> should equal (true |> U2<bool, WorkspaceSymbolOptions>.C1 |> Some)

        serverCaps.SignatureHelpProvider
        |> should
            equal
            ({ WorkDoneProgress = None
               TriggerCharacters = Some [| "("; ","; "<"; "{"; "[" |]
               RetriggerCharacters = None }
             |> Some)

        serverCaps.MonikerProvider |> should equal null

        client.ServerDidRespondTo "initialize" |> should be True
        client.ServerDidRespondTo "initialized" |> should be True


    [<Test>]
    member _.testSlnxSolutionFileWillBeFoundAndLoaded() =
        use client = setupServerClient defaultClientProfile "TestData/testSlnx"
        client.StartAndWaitForSolutionLoad()

        client.ServerMessageLogContains(fun m -> m.Contains "1 solution(s) found")
        |> should be True

        client.ServerDidRespondTo "initialize" |> should be True
        client.ServerDidRespondTo "initialized" |> should be True

        assertHoverWorks
            client
            "Project/Class.cs"
            { Line = 2u; Character = 16u }
            "```csharp\nvoid Class.MethodA(string arg)\n```"


    [<Test>]
    member _.testMultiTargetProjectLoads() =
        use client =
            setupServerClient defaultClientProfile "TestData/testMultiTargetProjectLoads"

        client.StartAndWaitForSolutionLoad()

        client.ServerMessageLogContains(fun m -> m.Contains "loading project")
        |> should be True

        assertHoverWorks
            client
            "Project/Class.cs"
            { Line = 2u; Character = 16u }
            "```csharp\nvoid Class.Method(string arg)\n```"
