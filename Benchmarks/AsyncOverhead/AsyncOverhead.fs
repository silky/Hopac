﻿// Copyright (C) by Housemarque, Inc.

module AsyncOverhead

open Hopac
open Hopac.Extensions
open Hopac.Job.Infixes
open System
open System.IO
open System.Diagnostics
open System.Threading.Tasks

let runHopac numOps n =
  let timer = Stopwatch.StartNew ()
  let rec loop n = job {
    if 0 < n then
      let task = Task.Factory.StartNew (fun _ -> n-1)
      let! n = Task.awaitJ task
      return! loop n
  }
  do Job.Now.run (Array.create n (loop numOps) |> Job.inPar) |> ignore
  let d = timer.Elapsed
  let m = sprintf "Hopac: %d*%d %fs - %f ops/s\n"
           numOps n d.TotalSeconds (float (numOps*n) / d.TotalSeconds)
  do use w = new StreamWriter ("Results.txt", true)
     w.Write m
  printf "%s" m

let runAsync numOps n =
  let timer = Stopwatch.StartNew ()
  let rec loop n = async {
    if 0 < n then
      let task = Task.Factory.StartNew (fun _ -> n-1)
      let! n = Async.AwaitTask task
      return! loop n
  }
  Async.RunSynchronously (Array.create n (loop numOps) |> Async.Parallel) |> ignore
  let d = timer.Elapsed
  let m = sprintf "Async: %d*%d %fs - %f ops/s\n"
           numOps n d.TotalSeconds (float (numOps*n) / d.TotalSeconds)
//  do use w = new StreamWriter ("Results.txt", true)
//     w.Write m
  printf "%s" m

do [(2500000, 1)
    (2000000, 2)
    (1500000, 4)
    (1000000, 8)]
   |> List.iter (fun (numOps, n) ->
      GC.Collect ()
      runHopac numOps n
      GC.Collect ()
      runAsync numOps n)
