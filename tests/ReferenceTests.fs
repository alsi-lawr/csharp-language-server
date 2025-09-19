namespace CSharpLanguageServer.Tests

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types

open CSharpLanguageServer.Tests.Tooling
open FsUnit

[<TestFixture>]
type ReferenceTests() =

    [<TestCase("TestData/testReferenceWorksDotnet9")>]
    [<TestCase("TestData/testReferenceWorksDotnet8")>]
    member _.``get references request should provide references``(testDataDir: string) =
        use client = setupServerClient defaultClientProfile testDataDir
        client.StartAndWaitForSolutionLoad()

        use classFile = client.Open("Project/Class.cs")

        //
        // try references request at empty line line 1 -- should return 0 results
        //
        let referenceParams0: ReferenceParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 0u; Character = 0u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = { IncludeDeclaration = false } }

        let locations0: Location[] option =
            client.Request("textDocument/references", referenceParams0)

        locations0.IsNone |> should be True

        //
        // try references request at MethodA declaration on line 2
        //
        let referenceParams1: ReferenceParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 2u; Character = 16u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = { IncludeDeclaration = false } }

        let locations1: Location[] option =
            client.Request("textDocument/references", referenceParams1)

        let expectedLocations1: Location array =
            [| { Uri = classFile.Uri
                 Range =
                   { Start = { Line = 8u; Character = 8u }
                     End = { Line = 8u; Character = 15u } } } |]

        locations1.Value |> should equal expectedLocations1

        //
        // try references request at MethodA declaration on line 2
        // (with IncludeDeclaration=true)
        //
        let referenceParams2: ReferenceParams =
            { TextDocument = { Uri = classFile.Uri }
              Position = { Line = 2u; Character = 16u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = { IncludeDeclaration = true } }

        let locations2: Location[] option =
            client.Request("textDocument/references", referenceParams2)

        let expectedLocations2: Location array =
            [| { Uri = classFile.Uri
                 Range =
                   { Start = { Line = 2u; Character = 16u }
                     End = { Line = 2u; Character = 23u } } }

               { Uri = classFile.Uri
                 Range =
                   { Start = { Line = 8u; Character = 8u }
                     End = { Line = 8u; Character = 15u } } } |]

        locations2.Value |> should equal expectedLocations2

    [<Test>]
    [<Ignore("Appears to be flaky")>]
    member _.``reference request should provide references for aspnet``() =
        use client =
            setupServerClient defaultClientProfile "TestData/testReferenceWorksToAspNetRazorPageReferencedValue"

        client.StartAndWaitForSolutionLoad()

        use testIndexViewModelCsFile = client.Open("Project/Models/Test/IndexViewModel.cs")
        use testControllerCsFile = client.Open("Project/Controllers/TestController.cs")
        use viewsTestIndexCshtmlFile = client.Open("Project/Views/Test/Index.cshtml")

        let referenceParams0: ReferenceParams =
            { TextDocument = { Uri = testIndexViewModelCsFile.Uri }
              Position = { Line = 3u; Character = 20u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = { IncludeDeclaration = false } }

        let locations0: Location[] option =
            client.Request("textDocument/references", referenceParams0)

        locations0.IsSome |> should be True
        locations0.Value.Length |> should equal 2

        let expectedLocations0: Location array =
            [| { Uri = testControllerCsFile.Uri
                 Range =
                   { Start = { Line = 11u; Character = 12u }
                     End = { Line = 11u; Character = 18u } } }

               { Uri = viewsTestIndexCshtmlFile.Uri
                 Range =
                   { Start = { Line = 1u; Character = 7u }
                     End = { Line = 1u; Character = 13u } } } |]

        locations0.Value |> should equal expectedLocations0

        //
        // do same but with IncludeDeclaration=true
        //
        let referenceParams1: ReferenceParams =
            { TextDocument = { Uri = testIndexViewModelCsFile.Uri }
              Position = { Line = 3u; Character = 20u }
              WorkDoneToken = None
              PartialResultToken = None
              Context = { IncludeDeclaration = true } }

        let locations1: Location[] option =
            client.Request("textDocument/references", referenceParams1)

        locations1.IsSome |> should be True
        locations1.Value.Length |> should equal 5

        let expectedLocations1: Location array =
            [| { Uri = viewsTestIndexCshtmlFile.Uri
                 Range =
                   { Start = { Line = 1u; Character = 7u }
                     End = { Line = 1u; Character = 13u } } }

               { Uri = testIndexViewModelCsFile.Uri
                 Range =
                   { Start = { Line = 3u; Character = 19u }
                     End = { Line = 3u; Character = 25u } } }

               { Uri = testIndexViewModelCsFile.Uri
                 Range =
                   { Start = { Line = 3u; Character = 28u }
                     End = { Line = 3u; Character = 31u } } }

               { Uri = testIndexViewModelCsFile.Uri
                 Range =
                   { Start = { Line = 3u; Character = 33u }
                     End = { Line = 3u; Character = 36u } } }

               { Uri = testControllerCsFile.Uri
                 Range =
                   { Start = { Line = 11u; Character = 12u }
                     End = { Line = 11u; Character = 18u } } } |]

        let sortedLocations1 =
            locations1.Value
            |> Array.sortBy (fun f -> (f.Range.Start.Line, f.Range.Start.Character))

        sortedLocations1 |> should equal expectedLocations1
