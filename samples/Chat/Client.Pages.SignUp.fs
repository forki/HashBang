﻿namespace Chat.Client.Pages

open System
open FunScript.TypeScript
open TypeInferred.HashBang
open TypeInferred.HashBang.Html
open TypeInferred.HashBang.Bootstrap
open Chat.Domain.Command
open Chat.Domain
open Chat.Client
open Chat.Client.Stylesheets
open Chat.Server

[<FunScript.JS>]
type SignUpPage(authService : IAuthenticationService) =
    
    let password = ref None
    let passwordConfirmation = ref None

    let sets reference =
        FormQuery.tapValue (function 
            | Valid v -> reference := Some v
            | _ -> reference := None)

    let isSameAs reference msg f = 
        f |> Validate.checkThat (fun (text : string) ->
                match !reference with
                | None -> true
                | Some v -> text = v) msg

    let currentUri = Routes.Session.SignUp.CreateUri() 
    
    let isEmail = Validate.checkThat Validation.isEmail "Must be a valid email."

    let isNotRegistered = 
        Validate.checkThatAsync (fun email ->
            async { 
                let! isRegistered = authService.IsEmailRegistered email
                return not isRegistered
            }) "Email is already registered."

    let createPage() =
        Templates.insideNavBar 
            currentUri
            "Sign Up" 
            "Please fill in your details." [
            FormQuery.puree (fun email password _ firstName secondName -> 
                {
                    FirstName = firstName
                    SecondName = secondName
                    Email = email
                    Password = password
                })
            <*> Inputs.text "Email" "joebloggs@gmail.com" (Validate.isNotEmpty >> isEmail >> isNotRegistered)
            <*> Inputs.password "Password" "my_s3cr3t" (
                    Validate.isAtLeast 8 
                    >> sets password 
                    >> isSameAs passwordConfirmation "Passwords must match.")
            <*> Inputs.password "Confirm Password" "my_s3cr3t" (
                    Validate.isAtLeast 8 
                    >> sets passwordConfirmation 
                    >> isSameAs password "Passwords must match.")
            <*> Inputs.text "First Name" "Joe" Validate.isNotEmpty
            <*> Inputs.text "Second Name" "Bloggs" Validate.isNotEmpty
            |> FormQuery.withSubmitButton "Sign Up" (fun userDetails ->
                async {
                    try
                        let! _ = authService.SignUp userDetails
                        Globals.location.href <- Routes.Conversation.View.CreateUri()
                    with ex ->
                        ApplicationState.alerts.Trigger(ApplicationState.Danger, "Error", ex.Message)
                })
            |> FormQuery.withDisablingFieldset
            |> FormQuery.runBootstrap
        ]

    interface IPage with
        member __.TryHandle request = 
            Routes.Session.SignUp.CreateHandler (fun ps -> 
                async { return Some(createPage()) }
            ) request.Path request.QueryParams