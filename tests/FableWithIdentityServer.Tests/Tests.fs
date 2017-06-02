module Tests


open Expecto

[<Tests>]
let tests =
  testList "samples" [
    testCase "Say hello all" <| fun _ ->
      
      Expect.equal "Hello all" "Hello all" "You didn't say hello"
    
  ]