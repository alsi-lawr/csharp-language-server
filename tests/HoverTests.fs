namespace CSharpLanguageServer.Tests

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types

open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type HoverTests() =

    [<Test>]
    member _.testHoverWorks() =
        use client = setupServerClient defaultClientProfile "TestData/testHoverWorks"
        client.StartAndWaitForSolutionLoad()

        use classFile = client.Open("Project/Class.cs")

        //
        // check hover at method name
        //
        let hover0Params: HoverParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 2u; Character = 16u }
              WorkDoneToken = None }

        let hover0: Hover option = client.Request("textDocument/hover", hover0Params)

        hover0.IsSome |> should be True

        match hover0 with
        | Some hover ->
            match hover.Contents with
            | U3.C1 c ->
                c.Kind |> should equal MarkupKind.Markdown
                c.Value |> should equal "```csharp\nvoid Class.Method(string arg)\n```"
            | _ -> failwith "C1 was expected"

            hover.Range.IsNone |> should be True

        | _ -> failwith "Some (U3.C1 c) was expected"

        //
        // check hover on `string` value (external System.String type)
        //
        let hover1Params: HoverParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 4u; Character = 8u }
              WorkDoneToken = None }

        let hover1: Hover option = client.Request("textDocument/hover", hover1Params)

        hover1.IsSome |> should be True

        match hover1 with
        | Some hover ->
            match hover.Contents with
            | U3.C1 c ->
                c.Kind |> should equal MarkupKind.Markdown

                c.Value.ReplaceLineEndings("\n")
                |> should equal "```csharp\nstring\n```\n\nRepresents text as a sequence of UTF-16 code units."
            | _ -> failwith "C1 was expected"

            hover.Range.IsNone |> should be True

        | _ -> failwith "Some (U3.C1 c) was expected"

        //
        // check hover at beginning of the file (nothing should come up)
        //
        let hover2Params: HoverParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 0u; Character = 0u }
              WorkDoneToken = None }

        let hover2: Hover option = client.Request("textDocument/hover", hover2Params)

        hover2.IsNone |> should be True
