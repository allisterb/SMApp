﻿namespace SMApp.Web

open System.Collections.Generic

open WebSharper

open SMApp.Models
open SMApp.NLU

[<JavaScript>]
module User =
    let name = "User"
    let debug m = ClientExtensions.debug name m
    
    let moduleQuestions = [ 
        Question("inputPassPhrase", "", Verification false, name)
        Question("addUser", "Do you want me to add the user $0?", Verification true, name)
        Question("switchUser", "Do you want me to switch to the user $0?", Verification true, name)
    ]
    let getQuestion n = Dialogue.getQuestion moduleQuestions     
    let haveQuestion n = Dialogue.haveQuestion moduleQuestions

    /// Update the dialogue state
    let update d =        
        let (Dialogue.Dialogue(cui, props, dialogueQuestions, output, utterances)) = d
        debug <| sprintf "Starting utterances:%A. Starting questions: %A." utterances dialogueQuestions
   
        let echo = Dialogue.echo d
        let say' = Dialogue.say' d
        let say = Dialogue.say d
        let sayRandom = Dialogue.sayRandom d
        let sayRandom' = Dialogue.sayRandom' d

        (* Manage the dialogue state elements*)
        let have = Dialogue.have d
        let add k v  = Dialogue.add d k v
        let delete = Dialogue.delete d
        let prop k = Dialogue.prop d k
        let user() :User = prop "user"

        let pushu  = Dialogue.pushu d
        let popu() = Dialogue.popu d
        let pushq = Dialogue.pushq d moduleQuestions
        let popq() = Dialogue.popq d
        let popt() = Dialogue.popt d
        let ask = Dialogue.ask d moduleQuestions

        let dispatch = Dialogue.dispatch d debug
        let handle = Dialogue.handle d debug
        let handle' = Dialogue.handle' d debug
        let didNotUnderstand() = Dialogue.didNotUnderstand d debug name

        (* Base dialogue patterns *)
        let (|PropSet|_|) = Dialogue.(|PropSet_|_|) d
        let (|PropNotSet|_|) = Dialogue.(|PropNotSet_|_|) d
        let (|User|_|) = Dialogue.(|User_|_|) d
        let (|User'|_|) = Dialogue.(|User'_|_|) d
        let (|Response|_|) = Dialogue.(|Response_|_|) d moduleQuestions
        let (|Response'|_|) = Dialogue.(|Response'_|_|) d moduleQuestions

        (* User functions *)
        let loginUser u = 
            do sayRandom waitRetrievePhrases "user name"
            async { 
                match! Server.getUser u with 
                | Some u ->
                    do! Server.updateUserLastLogin u.Name |> Async.Ignore
                    add "user" u
                    sayRandom helloUserPhrases <| sprintf "%A" props.["user"]
                    if Option.isSome u.LastLoggedIn then 
                        let! h = Server.humanize u.LastLoggedIn.Value
                        say <| sprintf "You last logged in %s." h 
                | None _ -> 
                    say <| sprintf "I did not find a user with the name %s." u
                    ask "addUser" u
            } |> Async.Start
        
        let addUser u = 
            async { 
                do sayRandom waitAddPhrases "user"
                match! Server.addUser u with 
                | Ok _ -> 
                    add "user" u
                    say <| sprintf "Hello %A, nice to meet you." props.["user"]
                | Error _ -> 
                    say <| sprintf "Sorry I was not able to add the user %s to the system." u
            } |> Async.Start

        (* Symptom journal functions *) 
        let addSymptom s l m = 
            async { 
                do sayRandom waitAddPhrases "symptom entry"
                match! Server.addSymptomJournalEntry (user().Name) s l m with 
                | Ok _ -> 
                    say <| sprintf "OK I added that %s symptom to your journal." s 
                | Error _ -> 
                    say <| sprintf "Sorry I wasn't able to add that symptom to your journal. Could you try again?"
            } |> Async.Start 

        let getSymptomJournal u =  
            async {
                do sayRandom waitRetrievePhrases "symptom journal"
                return! Server.getSymptomJournal u 
        }
            
        (* Interpreter logic begins here *)
        match utterances |> Seq.take (if utterances.Count >= 5 then 5 else utterances.Count) |> List.ofSeq with
        
        (* User login *)
        | User'(Intent "greet" (_, Entity1Of1 "name" u))::[] ->  handle "Login" (fun _ -> loginUser u.Value)
        
        (* User add *)
        | Yes(Response' "addUser" (_, Str user))::[] -> addUser user
        | No(Response' "addUser" (_, Str user))::[] -> say <| sprintf "Ok I did not add the user %s. But you must login for me to help you." user

        //| Assert'(_) ::[] -> say "Could you introduce yourself so we can get started?"

        (* User switch *)
        
        | User(Intent "hello" (None, Entity1Of1 "name" u))::[] -> 
            async {
                match! Server.getUser u.Value with
                | Some user -> ask "switchUser" user.Name
                | None -> say <| sprintf "Sorry, the user %s does not exist." u.Value
            } |> Async.Start
        | Yes(Response "switchUser" (_, Str user))::[] ->
            props.["user"] <- user
            say <| sprintf "Ok I switched to user %A." user  
        | No(Response "switchUser" (_, Str user))::[] -> 
            say <| sprintf "Ok I did not switch to user %s." user
        

        (* KB query *)

        | User'(Intent "kbquery" (_, _) as u)::[]
        | User(Intent "kbquery" (_, _) as u)::[] -> 
            async {
                let! a = QnAMaker.getAnswer u.Text 
                let! html = a.answers.[0].answer |> Server.mdtohtml
                let! text = a.answers.[0].answer |> Server.mdtotext
                echo html
                say text
            } |> Async.Start

        (* Symptoms *)

        | User(Intent "symptom" (_, Entity1OfAny "symptom_name" s))::[] ->
            async {
                say "Ok I'll add that entry to your symptom journal"
                addSymptom s.Value None (None)
                //let! j = getSymptomJournal (user().Name)
                //say <| sprintf "I see this is the 3rd time today you've had pain %s" (user())
                //ask "painVideo" ""
            } 
            |> Async.Start

        | Yes(Response "painVideo"(_, _))::[] -> cui.EchoHtml'("""<iframe width="560" height="315" src="https://www.youtube.com/embed/SkAqOditKN0" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>""")
            
        (* Meds *)

        | User(Intent "medjournal" (_, Some en))::[] ->
            say "ok I added that entry to your medication journal."
            say "You should be careful not to take too many painkillers over a short period of time."

        | _ -> didNotUnderstand()

 
