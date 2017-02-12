module Pushtray.Cli

open System
open Pushtray.Utils

let usage = "\
usage:
  pushtray connect [options]
  pushtray sms <number> <message> [--device=<name>] [options]
  pushtray list devices [options]
  pushtray (-h | --help)
  pushtray --version"

let options = "\
options:
  --access-token=<token>      Set the access token. This will override the
                              config file value.
  --encrypt-pass=<pass>       Set the encrypt password. This will override the
                              config file value.
  --no-tray-icon              Don't show a tray icon.
  --enable-icon-animations    Show tray icon animations.
  --sms-notify-icon=<icon>    Change the stock icon for SMS notifications.
  --ignore-sms <numbers>      Don't show SMS notifications from these numbers
                              <numbers> is a comma-separated list or a single
                              asterisk to ignore all.
  --notify-format=<fmt>       Set notification format style (full | short)
  --notify-line-wrap=<wrap>   Set the line wrap width of notifications
                              (i.e. the maximum width)
  --notify-line-pad=<pad-to>  Set the minimum line width of notifications
  --icon-style=<style>        Customize the tray icon style (light | dark)
  --log=<log-level>           Enable all logging messages at <log-level>
                              and higher"

let version = sprintf "Pushtray %s" AssemblyInfo.Version

type Arguments =
  { Commands: Set<string>
    Positional: PositionalArgs
    Options: Options }

and PositionalArgs =
  { Number: string option
    Message: string option }

and Options =
  { Device: string option
    AccessToken: string option
    EncryptPass: string option
    NoTrayIcon: bool
    EnableIconAnimations: bool
    SmsNotifyIcon: string option
    IgnoreSms: Set<string>
    NotifyFormat: string
    NotifyLineWrap: int
    NotifyLinePad: int
    IconStyle: string
    Log: string }

let usageWithOptions =
  sprintf "%s\n\n%s" usage options

type DocoptArgs = Collections.Generic.IDictionary<string, DocoptNet.ValueObject>

let private parseArgs (argv: string[]) =
  let docopt = new DocoptNet.Docopt()
  docopt.PrintExit.Add(fun _ ->
    printfn "%s" usage
    exit 1)
  docopt.Apply(usageWithOptions, argv, help = false, exit = true)

let args =
  let docoptArgs: DocoptArgs option =
    #if INTERACTIVE
    None
    #else
    Some (parseArgs <| System.Environment.GetCommandLineArgs().[1..])
    #endif

  let valueOf func key =
    docoptArgs |> Option.bind (fun a ->
    if a.ContainsKey(key) then
      match a.[key] with
      | null -> None
      | v -> Some <| func v
    else
      None)

  let argAsString key = key |> valueOf (fun v -> v.ToString())
  let argAsIntWithDefault key = try argAsString key |> Option.map int with _ -> None
  let argExists key = key |> valueOf (fun v -> v.IsTrue) |> Option.exists id
  let argAsSet key =
    match argAsString key with
    | Some s -> Set.ofArray <| s.Split [| ',' |]
    | None -> Set.empty

  { Commands =
      Set [ "connect"
            "sms"
            "list"
            "devices"
            "-h"; "--help"
            "--version" ]
      |> Set.filter argExists
    Positional =
      { Number  = argAsString "<number>"
        Message = argAsString "<message>" }
    Options =
      { Device                = argAsString "--device"
        AccessToken           = argAsString "--access-token"
        EncryptPass           = argAsString "--encrypt-pass"
        NoTrayIcon            = argExists   "--no-tray-icon"
        EnableIconAnimations  = argExists   "--enable-icon-animations"
        SmsNotifyIcon         = argAsString "--sms-notify-icon"
        IgnoreSms             = argAsSet    "--ignore-sms"
        NotifyFormat          = argAsString "--notify-format"    |> Option.getOrElse "short"
        NotifyLineWrap        = argAsString "--notify-line-wrap" |> Option.getOrElse "40" |> int
        NotifyLinePad         = argAsString "--notify-line-pad"  |> Option.getOrElse "45" |> int
        IconStyle             = argAsString "--icon-style"       |> Option.getOrElse "light"
        Log                   = argAsString "--log"              |> Option.getOrElse "warn" } }

let required opt =
  match opt with
  | Some v -> v
  | None ->
    Logger.fatal "Required argument has no value"
    exit 1

let command key func =
  if args.Commands.Contains key then
    func()
    exit 0

let commands keys func =
  keys |> List.iter (fun k -> command k func)
