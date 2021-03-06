﻿// Copyright (C) by Housemarque, Inc.

namespace Hopac.Core {
  using Microsoft.FSharp.Core;
  using System.Diagnostics;
  using System.Runtime.CompilerServices;
  using System.Threading;
  using System;

  /// Internal implementation detail.
  internal static class Scheduler {
    internal static volatile int Waiters;
    internal static volatile int Lock;
    internal static int TimedCount;
    internal static Work WorkStack;
    internal static WorkTimed TopTimed;
    internal static WorkerEvent[] Events;

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void Init() {
      if (Volatile.Read(ref Events) == null)
        ReallyInit();
    }

    private static void ReallyInit() {
      SpinlockTTAS.Enter(ref Scheduler.Lock);
      if (Volatile.Read(ref Events) == null) {
        var numWorkers = Environment.ProcessorCount;
        Waiters = -1;
        Events = new WorkerEvent[numWorkers];
        var threads = new Thread[numWorkers];
        for (int i = 0; i < numWorkers; ++i) {
          Events[i] = new WorkerEvent(i);
          var index = i;
          var thread = new Thread(() => Worker.Run(index));
          threads[i] = thread;
          thread.IsBackground = true;
        }
        for (int i=0; i < numWorkers; ++i)
          threads[i].Start();
        SpinlockTTAS.Exit(ref Scheduler.Lock);
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void Push(Work work) {
      SpinlockTTAS.Enter(ref Lock);
      work.Next = WorkStack;
      WorkStack = work;
      var waiters = Waiters;
      if (0 <= waiters)
        Events[waiters].Set();
      SpinlockTTAS.Exit(ref Lock);
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void SynchronizedPushTimed(ref Worker wr, WorkTimed x) {
      SpinlockTTAS.Enter(ref Scheduler.Lock);
      var h = TopTimed;
      if (null == h) {
        TopTimed = x;
        x.Up = x;
        x.Next = x;
        var waiter = Waiters;
        if (0 <= Waiters)
          Events[waiter].Set();
      } else if (x.Ticks - h.Ticks < 0) {
        x.Next = h.Up;
        h.Up = x;
        x.Up = x;
        TopTimed = x;
        var waiter = Waiters;
        if (0 <= Waiters)
          Events[waiter].Set();
      } else if (x.Ticks - h.Up.Ticks > 0) {
        x.Up = h.Up;
        h.Up = x;
        x.Next = x;
      } else {
        var y = h.Up;
        Work z = h.Up;
        while (x.Ticks - y.Up.Ticks < 0) {
          var t = y.Next;
          y.Next = z;
          z = t;
        }
        x.Up = y.Up;
        x.Next = z;
        y.Up = x;
        h.Up = x;
      }
      SpinlockTTAS.Exit(ref Scheduler.Lock);
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void DropTimed() {
      var h = TopTimed;
      if (null == h)
        return;
      var y1 = h.Up;
      var y2 = h.Next as WorkTimed;
      if (y1.Ticks - y2.Ticks < 0) {
        var t = y1;
        y1 = y2;
        y2 = t;
      }
      if (y1 == h) {
        TopTimed = null;
        return;
      }
      WorkTimed x;
      var h3 = y1;
      y1 = y1.Up;
      h3.Up = h3;
      while (true) {
        if (y1.Ticks - y2.Ticks < 0) {
          var t = y1;
          y1 = y2;
          y2 = t;
        }
        if (y1 == h) {
          TopTimed = h3;
          return;
        }
        x = y1;
        y1 = y1.Up;
        x.Up = x.Next as WorkTimed;
        x.Next = h3.Up;
        h3.Up = x;
        h3 = x;
      }
    }
  }
}
