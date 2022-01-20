namespace App.Components

open Feliz
open Feliz.Bulma
open App.Components.SidePanelMenu
type Navigation =
    [<ReactComponent>]
    static member SidePanel(messageCount : int,activeView,setView) = 
        
        let menuItems = 
            [
                Overview, None
                Accounts,None
                Transfer, None
                Invest, None
                Pensions, None
                Messages, if messageCount > 0 then Some messageCount else None
                Profile, None
                DevSupport,None
            ] |> List.map(fun (view,notification) ->
                 {
                    Data = view
                    IsActive = view = activeView
                    Notification = notification
                    IconName = Some view.IconName
                }        
            )
        
        SidePanelMenu({
            MenuItems = menuItems
            MenuClicked = setView
        })

    [<ReactComponent>]
    static member Topbar(userButtonText : string,action) =
        
        Bulma.navbarMenu [
            Bulma.navbarStart.div [
                Bulma.navbarItem.div [
                    prop.className "icon credit-card-logo"
                ]
                Bulma.navbarItem.div[
                    prop.className "logo-text"
                    prop.children [
                        Html.span[
                            prop.className "app-name"
                            prop.text "%APPNAME%"
                        ]
                        Html.span[
                            prop.className "bank"
                            prop.text "Bank"
                        ]
                    ]
                ]
            ]
            Bulma.navbarEnd.div [
                Bulma.navbarItem.div [
                    Bulma.buttons [
                        Bulma.button.a [  
                            prop.onClick (fun _ -> action() )
                            prop.style [
                                style.backgroundColor.transparent
                                style.borderStyle.none
                                style.fontSize 18
                            ]
                            prop.children [
                                Html.span [ 
                                    prop.className "navbar-item"
                                    prop.text userButtonText
                                ]
                                Html.div [
                                    "power-off-white" |> sprintf "icon %s" |> prop.className
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
