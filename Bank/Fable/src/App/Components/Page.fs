namespace App.Components

open Feliz
open Feliz.Bulma

type Page() =
    static let mutable accordions = [||]
    static let cancelSignatureOrder removeMessage userToken orderId= 
        let identifyMessage (msg : Models.Message) = 
            match msg.Type with
            Models.Signature(_,oid) when oid = orderId -> true
            | _ -> false
        
        fun () -> 
            async {
                let! res = Signatures.cancelSignatureOrder userToken orderId
                match res with
                Ok res -> 
                    printfn "Cancel: %A" res
                    removeMessage identifyMessage
                | Error e ->
                    eprintfn "Failed to cancel %s. Errors: %s" orderId e
            } |> Async.StartImmediate
         
    [<ReactComponent>]
    static member Overview(user : Models.User, activeView,setView,messages : Models.Message list, documents : Models.Document list, setMessages) =
        let removeMessage predicate =
            messages
            |> List.filter(predicate >> not)
            |> setMessages
        let cancelSignatureOrder = cancelSignatureOrder removeMessage user.Token
        let components = 
            match activeView with
            Overview ->
                
                [
                    Components.IdCard(user)
                    Message.List("New messages",messages, setMessages, setView, Some 2, cancelSignatureOrder)
                    Account.Box(user.Accounts,fun name -> name |> View.Account |> setView)
                ]
            | View.Account name ->
                let account = 
                    user.Accounts
                    |> List.find(fun a -> a.Name = name)
                [
                   Account.Transactions(account)
                ]
            | Messages ->
                [
                    Message.List("Messages",messages, setMessages,setView, None, cancelSignatureOrder)
                ]
            | Pensions ->
                printfn "Switching to %A" Pensions
                async{
                    
                    let doc = documents |> List.head
                    let docName = doc.Name.Replace("%USERNAME%", user.Name)
                    let content = doc.Content.Replace("%USERNAME%", user.Name)
                    let! signatureOrderResult = Signatures.createSignatureOrder user.Token "Signature order" [|{Title = docName; Content = content; Reference = None} |] 
                    
                    match signatureOrderResult with
                    Ok order  -> 
                        
                        async {
                            let userRef = 
                                user.Name
                                |> System.Text.Encoding.Unicode.GetBytes
                                |> System.Convert.ToBase64String
                            
                            let! signatoryAddedResult = 
                                Signatures.addSignatory user.Token order.id userRef    
                            match signatoryAddedResult with
                            Ok signatoryAdded ->
                                let linkToDoc = signatoryAdded.signatory.documentLink
                                let msg : Models.Message = 
                                    {
                                        Id = (userRef + System.DateTime.Now.Ticks.ToString())
                                        Subject = "Document to be signed"
                                        Content = "Your loan is awaiting your signature. To read the document and sign it press the button below"
                                        From = "Your bank advisor"
                                        Date = System.DateTime.Now
                                        Unread = true
                                        Type = Models.Signature(linkToDoc,order.id)
                                    }
                                msg::messages |> setMessages
                            | Error e -> 
                                eprintfn "Erros while adding signatory %s" e
                        } |> Async.StartImmediate
                    | Error e -> 
                        printfn "Error occurred while creating signature order %s" e
                    
                } |> Async.StartImmediate
                [
                    Components.IdCard(user)
                ]
            | v ->
                printfn "Switching to %A" v 
                [
                    Components.IdCard(user)
                ]
        
        Bulma.container components
        
    [<ReactComponent>]
    static member Layout() =
        let view,setView = React.useState Overview
        let user, _setUser = React.useState None
        let messages,_setMessages = React.useState []
        let documents,setDocuments = React.useState []
        let unreadCount,setUnread = React.useState 0
        let reduceUnreadCount() = 
            unreadCount - 1 |> setUnread
        let setMessages msgs = 
            let unreadCount = 
                msgs
                |> List.sumBy(fun (m:Models.Message) -> if m.Unread then 1 else 0)
            setUnread unreadCount
            _setMessages msgs
        Messages.fetch(setMessages)
        Documents.fetch(setDocuments)
        let setUser (oidcUser : Oidc.UserInfo option) = 
            let user = 
                oidcUser |> Option.map(fun ou -> 
                    {
                        Name = ou.profile.name
                        DateOfBirth = ou.profile.birthdate
                        Accounts = Statements.generate ou.profile.name 200
                        Token = ou.id_token
                    }  : Models.User
                ) 
            match user with
            Some _ -> user |> _setUser
            | None -> ()
        match user with
        None -> 
            if Identity.hasRequestedAuthentication() |> not then
                Html.div[
                    Navigation.Topbar("Log on",fun _ -> Identity.logIn())
                    Components.Splash()
                ]
            else
               Identity.isAuthenticated(setUser) |> ignore
               Html.div[]
        | Some user -> 
            Html.div[
                Navigation.Topbar("Log off",Identity.logOut)
                Bulma.container [
                    Bulma.columns[
                        prop.style [
                            style.marginTop 40
                        ]
                        columns.isCentered
                        prop.children[
                            Bulma.column [
                                prop.style[
                                    style.boxShadow.none
                                ]
                                column.isOneQuarter
                                column.isOneFifthFullHd
                                prop.children[
                                    Navigation.SidePanel (unreadCount,view,setView)
                                ]
                            ]
                            Bulma.column [
                                prop.children [
                                    Page.Overview (user,view,setView, messages,documents,setMessages)
                                ]
                            ]
                        ]
                    ]
                ]
            ]