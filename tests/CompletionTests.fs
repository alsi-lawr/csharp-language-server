namespace CSharpLanguageServer.Tests

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types

open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type CompletionTests() =
    static let mutable client: ClientController =
        setupServerClient defaultClientProfile "TestData/testCompletions"

    [<OneTimeSetUp>]
    member _.Setup() = client.StartAndWaitForSolutionLoad()

    [<Test>]
    member _.``text document completions for class methods``() =
        // resolve provider is necessary for lsp client to resolve
        // detail and documentation props for a completion item
        let haveResolveProvider =
            client.GetState().ServerCapabilities
            |> Option.bind (fun c -> c.CompletionProvider)
            |> Option.bind (fun p -> p.ResolveProvider)
            |> Option.defaultValue false

        haveResolveProvider |> should be True

        use classFile = client.Open("Project/Class.cs")

        let completionParams0: CompletionParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 4u; Character = 13u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = None }

        let completion0: U2<CompletionItem array, CompletionList> option =
            client.Request("textDocument/completion", completionParams0)

        match completion0 with
        | Some(U2.C2 cl) ->
            cl.IsIncomplete |> should be True
            cl.ItemDefaults.IsSome |> should be False
            cl.Items.Length |> should equal 6

            let methodAItem = cl.Items |> Seq.tryFind (fun i -> i.Label = "MethodA")

            match methodAItem with
            | None -> failwith "an item with Label 'MethodA' was expected for completion at this position"
            | Some item ->
                item.Label |> should equal "MethodA"
                item.Detail.IsSome |> should be False
                item.Documentation.IsSome |> should be False
                item.Tags.IsSome |> should be False
                item.InsertText |> should equal (Some "MethodA")
                item.Kind |> should equal (Some CompletionItemKind.Method)
                item.SortText |> should equal (Some "MethodA")
                item.FilterText |> should equal (Some "MethodA")
                item.InsertTextFormat |> should equal None
                item.CommitCharacters.IsSome |> should be False
                item.TextEdit.IsSome |> should be False
                item.Data.IsSome |> should be True

                let itemResolved: CompletionItem = client.Request("completionItem/resolve", item)

                itemResolved.Detail |> should equal (Some "void Class2.MethodA(string arg)")
                itemResolved.Documentation.IsSome |> should be False

            let getHashCodeItem = cl.Items |> Seq.tryFind (fun i -> i.Label = "GetHashCode")

            match getHashCodeItem with
            | None -> failwith "an item with Label 'GetHashCode' was expected for completion at this position"
            | Some item ->
                item.Label |> should equal "GetHashCode"
                item.Detail.IsSome |> should be False
                item.Documentation.IsSome |> should be False
                item.Tags.IsSome |> should be False
                item.InsertText |> should equal (Some "GetHashCode")
                item.Kind |> should equal (Some CompletionItemKind.Method)
                item.SortText |> should equal (Some "GetHashCode")
                item.FilterText |> should equal (Some "GetHashCode")
                item.InsertTextFormat |> should equal None
                item.CommitCharacters.IsSome |> should be False
                item.TextEdit.IsSome |> should be False
                item.Data.IsSome |> should be True

                let itemResolved: CompletionItem = client.Request("completionItem/resolve", item)

                itemResolved.Detail |> should equal (Some "int object.GetHashCode()")
                itemResolved.Documentation.IsSome |> should be True

                match itemResolved.Documentation with
                | Some(U2.C2 markup) ->
                    markup.Kind |> should equal MarkupKind.PlainText
                    markup.Value |> should equal "Serves as the default hash function."
                | _ -> failwith "Documentation w/ Kind=Markdown was expected for GetHashCode"

                ()

        | _ -> failwith "Some U2.C1 was expected"

        ()

    [<Test>]
    member _.``text document completion for extension methods``() =

        use classFile = client.Open "Project/Class.cs"

        let completionParams0: CompletionParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 12u; Character = 13u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = None }

        let completion0: U2<CompletionItem array, CompletionList> option =
            client.Request("textDocument/completion", completionParams0)

        match completion0 with
        | Some(U2.C2 cl) ->
            cl.Items.Length |> should equal 7

            let methodBItem = cl.Items |> Seq.tryFind (fun i -> i.Label = "MethodB")

            match methodBItem with
            | None -> failwith "an item with Label 'MethodB' was expected for completion at this position"
            | Some item ->
                item.Label |> should equal "MethodB"
                item.Detail.IsSome |> should be False
                item.Documentation.IsSome |> should be False
                item.Kind |> should equal (Some CompletionItemKind.Method)

                let itemResolved: CompletionItem = client.Request("completionItem/resolve", item)

                itemResolved.Detail |> should equal (Some "(extension) string Class.MethodB()")
                itemResolved.Documentation.IsSome |> should be False

        | _ -> failwith "Some U2.C1 was expected"
