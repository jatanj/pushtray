module Pushtray.Sms

open Pushtray.Pushbullet

let private selectSmsDevice (devices: Pushbullet.Device[]) =
  let numDevices = Array.length devices
  let rec readNumber showMessage =
    if showMessage then printfn "Please enter a number [1 - %d]" (numDevices - 1)
    try
      match int <| System.Console.ReadLine().Trim() with
      | n when n >= 1 && n < numDevices -> n
      | _ -> readNumber true
    with ex ->
      Logger.debug ex.Message
      readNumber true
  if numDevices > 1 then
    printfn "Choose device [1 - %d]: " (numDevices - 1)
    devices |> Array.iter (fun d -> printfn "1: %s %s" d.Manufacturer d.Nickname)
    devices.[readNumber false]
  else
    Array.head devices

let send number message =
  let device =
    devices
    |> Array.filter (fun d -> d.Type = "android")
    |> selectSmsDevice
  let request =
    Ephemeral.sendSms user.Iden device.Iden number message
  match request with
  | Some req -> req |> (Async.Ignore >> Async.RunSynchronously)
  | None -> Logger.error "Could not send SMS message"