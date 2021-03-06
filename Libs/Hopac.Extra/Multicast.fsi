﻿// Copyright (C) by Housemarque, Inc.

namespace Hopac.Extra

open Hopac

[<AutoOpen>]
module MulticastTypes =
  /// A multicast channel for one to many communication.
  type MChan<'a>

  /// A port for listening to a multicast channel.
  type MPort<'a>

/// Operations on multicast channels.
module Multicast =
  /// Creates a new multicast channel.
  val create: unit -> Job<MChan<'a>>

  /// Creates a new port for listening to the given multicast channel.
  val port: MChan<'a> -> Job<MPort<'a>>

  /// Sends a message to all of the ports listening to the multicast
  /// channel.
  val multicast: MChan<'a> -> 'a -> Job<unit>

  /// Receives a message from the given port.
  val recv: MPort<'a> -> Job<'a>

  /// Selective operations on multicast channels.
  module Alt =
    /// Alternative to receive a message from the given port.
    val recv: MPort<'a> -> Alt<'a>
