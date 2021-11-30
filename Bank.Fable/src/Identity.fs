module Identity

open Browser
open Oidc

let inline private split (delim : string) (s:string)  = 
    s.Split delim

let private options : Options = 
        let url = 
            window.location.href.Split([|'?';'#'|]) 
            |> Array.head
        {
            authority = "https://fablecriipto-test.criipto.id"               
            client_id = "urn:my:application:identifier:1744"               
            redirect_uri = url            
            response_type = "code" 
            post_logout_redirect_uri = url           
            acr_values = "urn:grn:authn:dk:mitid:low"
        }         
let private userManager = 
    
    UserManager(
        options
    )

let isAuthenticated = 
    let href = window.location.href
    href.Contains "?" && href.Contains "code="

let logOut () = 
    try
        userManager.signoutRedirect()
    with e ->
        eprintfn "Eror when logging out: %s" e.Message
let logIn () =
    userManager.signinRedirect()
    
let registerLogin(setUser : Oidc.UserInfo option -> unit) = 
    userManager.processSigninResponse().``then``(fun userInfo -> 
        let json = Fable.Core.JS.JSON.stringify userInfo
        localStorage.setItem(sprintf "oidc.user:%s:%s" options.authority options.client_id, sprintf "%A" json)
        userInfo |> Some |> setUser 
    ).catch(fun err ->
        Browser.Dom.console.error("Error:", err)
        None |> setUser
    )