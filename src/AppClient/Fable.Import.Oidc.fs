namespace Fable.Import
open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Import.JS

module Oidc =
    type [<AllowNullLiteral>] Logger =
        abstract error: ?message: obj * [<ParamArray>] optionalParams: obj[] -> unit
        abstract info: ?message: obj * [<ParamArray>] optionalParams: obj[] -> unit
        abstract debug: ?message: obj * [<ParamArray>] optionalParams: obj[] -> unit
        abstract warn: ?message: obj * [<ParamArray>] optionalParams: obj[] -> unit

    and [<AllowNullLiteral>] AccessTokenEvents =
        abstract load: container: User -> unit
        abstract unload: unit -> unit
        abstract addAccessTokenExpiring: callback: Func<obj, unit> -> unit
        abstract removeAccessTokenExpiring: callback: Func<obj, unit> -> unit
        abstract addAccessTokenExpired: callback: Func<obj, unit> -> unit
        abstract removeAccessTokenExpired: callback: Func<obj, unit> -> unit

    and [<AllowNullLiteral>] [<Import("InMemoryWebStorage","oidc-client")>] InMemoryWebStorage() =
        member __.length with get(): float option = jsNative and set(v: float option): unit = jsNative
        member __.getItem(key: string): obj = jsNative
        member __.setItem(key: string, value: obj): obj = jsNative
        member __.removeItem(key: string): obj = jsNative
        member __.key(index: float): obj = jsNative

    and [<AllowNullLiteral>] [<Import("Log","oidc-client")>] Log() =
        member __.NONE with get(): float = jsNative and set(v: float): unit = jsNative
        member __.ERROR with get(): float = jsNative and set(v: float): unit = jsNative
        member __.WARN with get(): float = jsNative and set(v: float): unit = jsNative
        member __.INFO with get(): float = jsNative and set(v: float): unit = jsNative
        member __.DEBUG with get(): float = jsNative and set(v: float): unit = jsNative
        member __.level with get(): float = jsNative and set(v: float): unit = jsNative
        member __.logger with get(): Logger = jsNative and set(v: Logger): unit = jsNative
        static member reset(): unit = jsNative
        static member debug(message: obj, [<ParamArray>] optionalParams: obj[]): unit = jsNative
        static member info(message: obj, [<ParamArray>] optionalParams: obj[]): unit = jsNative
        static member warn(message: obj, [<ParamArray>] optionalParams: obj[]): unit = jsNative
        static member error(message: obj, [<ParamArray>] optionalParams: obj[]): unit = jsNative

    and [<AllowNullLiteral>] MetadataService =
        abstract metadataUrl: string option with get, set
        [<Emit("new $0($1...)")>] abstract Create: settings: OidcClientSettings -> MetadataService
        abstract getMetadata: unit -> Promise<obj>
        abstract getIssuer: unit -> Promise<obj>
        abstract getAuthorizationEndpoint: unit -> Promise<obj>
        abstract getUserInfoEndpoint: unit -> Promise<obj>
        abstract getCheckSessionIframe: unit -> Promise<obj>
        abstract getEndSessionEndpoint: unit -> Promise<obj>
        abstract getSigningKeys: unit -> Promise<obj>

    and [<AllowNullLiteral>] MetadataServiceCtor =
        [<Emit("$0($1...)")>] abstract Invoke: settings: OidcClientSettings * ?jsonServiceCtor: obj -> MetadataService

    and [<AllowNullLiteral>] ResponseValidator =
        abstract validateSigninResponse: state: obj * response: obj -> Promise<obj>
        abstract validateSignoutResponse: state: obj * response: obj -> Promise<obj>

    and [<AllowNullLiteral>] ResponseValidatorCtor =
        [<Emit("$0($1...)")>] abstract Invoke: settings: OidcClientSettings * ?metadataServiceCtor: MetadataServiceCtor * ?userInfoServiceCtor: obj -> ResponseValidator

    and [<AllowNullLiteral>] [<Import("OidcClient","oidc-client")>] OidcClient(settings: OidcClientSettings) =
        member __.createSigninRequest(?args: obj): Promise<obj> = jsNative
        member __.processSigninResponse(): Promise<obj> = jsNative
        member __.createSignoutRequest(?args: obj): Promise<obj> = jsNative
        member __.processSignoutResponse(): Promise<obj> = jsNative
        member __.clearStaleState(?stateStore: obj): Promise<unit> = jsNative

    and [<AllowNullLiteral>] OidcClientSettings =
        abstract authority: string option with get, set
        abstract metadataUrl: string option with get, set
        abstract metadata: obj option with get, set
        abstract signingKeys: ResizeArray<obj> option with get, set
        abstract client_id: string option with get, set
        abstract response_type: string option with get, set
        abstract scope: string option with get, set
        abstract redirect_uri: string option with get, set
        abstract post_logout_redirect_uri: string option with get, set
        abstract popup_post_logout_redirect_uri: string option with get, set
        abstract prompt: string option with get, set
        abstract display: string option with get, set
        abstract max_age: float option with get, set
        abstract ui_locales: string option with get, set
        abstract acr_values: string option with get, set
        abstract filterProtocolClaims: bool option with get, set
        abstract loadUserInfo: bool option with get, set
        abstract staleStateAge: float option with get, set
        abstract clockSkew: float option with get, set
        abstract stateStore: WebStorageStateStore option with get, set
        abstract ResponseValidatorCtor: ResponseValidatorCtor option with get, set
        abstract MetadataServiceCtor: MetadataServiceCtor option with get, set

    and [<AllowNullLiteral>] [<Import("UserManager","oidc-client")>] UserManager(settings: UserManagerSettings) =
        inherit OidcClient(settings)
        member __.events with get(): UserManagerEvents = jsNative and set(v: UserManagerEvents): unit = jsNative
        member __.getUser(): Promise<User> = jsNative
        member __.removeUser(): Promise<unit> = jsNative
        member __.signinPopup(?args: obj): Promise<User> = jsNative
        member __.signinPopupCallback(?url: string): Promise<User> = jsNative
        member __.signinSilent(?args: obj): Promise<User> = jsNative
        member __.signinSilentCallback(?url: string): Promise<User> = jsNative
        member __.signinRedirect(?args: obj): Promise<unit> = jsNative
        member __.signinRedirectCallback(?url: string): Promise<User> = jsNative
        member __.signoutRedirect(?args: obj): Promise<unit> = jsNative
        member __.signoutRedirectCallback(?url: string): Promise<obj> = jsNative
        member __.signoutPopup(?args: obj): Promise<unit> = jsNative
        member __.signoutPopupCallback(?url: string, ?keepOpen: bool): Promise<unit> = jsNative
        member __.signoutPopupCallback(?keepOpen: bool): Promise<unit> = jsNative
        member __.querySessionStatus(?args: obj): Promise<obj> = jsNative

    and [<AllowNullLiteral>] UserManagerEvents =
        inherit AccessTokenEvents
        abstract load: user: User -> obj
        abstract unload: unit -> obj
        abstract addUserLoaded: callback: Func<obj, unit> -> unit
        abstract removeUserLoaded: callback: Func<obj, unit> -> unit
        abstract addUserUnloaded: callback: Func<obj, unit> -> unit
        abstract removeUserUnloaded: callback: Func<obj, unit> -> unit
        abstract addSilentRenewError: callback: Func<obj, unit> -> unit
        abstract removeSilentRenewError: callback: Func<obj, unit> -> unit
        abstract addUserSignedOut: callback: Func<obj, unit> -> unit
        abstract removeUserSignedOut: callback: Func<obj, unit> -> unit

    and [<AllowNullLiteral>] UserManagerSettings =
        inherit OidcClientSettings
        abstract popup_redirect_uri: string option with get, set
        abstract popupWindowFeatures: string option with get, set
        abstract popupWindowTarget: obj option with get, set
        abstract silent_redirect_uri: obj option with get, set
        abstract silentRequestTimeout: obj option with get, set
        abstract automaticSilentRenew: obj option with get, set
        abstract monitorSession: obj option with get, set
        abstract checkSessionInterval: obj option with get, set
        abstract revokeAccessTokenOnSignout: obj option with get, set
        abstract accessTokenExpiringNotificationTime: float option with get, set
        abstract redirectNavigator: obj option with get, set
        abstract popupNavigator: obj option with get, set
        abstract iframeNavigator: obj option with get, set
        abstract userStore: obj option with get, set

    and [<AllowNullLiteral>] WebStorageStateStoreSettings =
        abstract prefix: string option with get, set
        abstract store: obj option with get, set

    and [<AllowNullLiteral>] [<Import("WebStorageStateStore","oidc-client")>] WebStorageStateStore(settings: WebStorageStateStoreSettings) =
        member __.set(key: string, value: obj): Promise<unit> = jsNative
        member __.get(key: string): Promise<obj> = jsNative
        member __.remove(key: string): Promise<obj> = jsNative
        member __.getAllKeys(): Promise<ResizeArray<string>> = jsNative

    and [<AllowNullLiteral>] User =
        abstract id_token: string with get, set
        abstract session_state: obj with get, set
        abstract access_token: string with get, set
        abstract token_type: string with get, set
        abstract scope: string with get, set
        abstract profile: obj with get, set
        abstract expires_at: float with get, set
        abstract state: obj with get, set
        abstract expires_in: float with get, set
        abstract expired: bool with get, set
        abstract scopes: ResizeArray<string> with get, set
        abstract toStorageString: unit -> string

    and [<AllowNullLiteral>] [<Import("CordovaPopupWindow","oidc-client")>] CordovaPopupWindow(``params``: obj) =
        member __.promise with get(): Promise<obj> = jsNative and set(v: Promise<obj>): unit = jsNative
        member __.navigate(``params``: obj): Promise<obj> = jsNative

    and [<AllowNullLiteral>] [<Import("CordovaPopupNavigator","oidc-client")>] CordovaPopupNavigator() =
        member __.prepare(``params``: obj): Promise<CordovaPopupWindow> = jsNative

    and [<AllowNullLiteral>] [<Import("CordovaIFrameNavigator","oidc-client")>] CordovaIFrameNavigator() =
        member __.prepare(``params``: obj): Promise<CordovaPopupWindow> = jsNative


