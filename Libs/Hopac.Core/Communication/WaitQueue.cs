﻿// Copyright (C) by Housemarque, Inc.

namespace Hopac.Core {
  using Microsoft.FSharp.Core;
  using System;
  using System.Threading;
  using System.Runtime.CompilerServices;

  internal sealed class Giver<T> {
    internal Giver<T> Next;
    internal Pick Pick;
    internal Cont<Unit> Cont;
    internal T Value;
    internal int Me;
  }

  internal sealed class Taker<T> : Cont<T> {
    internal Cont<T> Cont;
    internal Pick Pick;
    internal int Me;

    internal override Pick GetPick(ref int me) {
      me = Me;
      return Pick;
    }

    internal override void DoHandle(ref Worker wr, Exception e) {
      this.Cont.DoHandle(ref wr, e);
    }

    internal override void DoWork(ref Worker wr) {
      this.Cont.DoCont(ref wr, this.Value);
    }

    internal override void DoCont(ref Worker wr, T value) {
      this.Cont.DoCont(ref wr, value);
    }
  }

  internal static class WaitQueue {
    [MethodImpl(AggressiveInlining.Flag)]
    internal static void RemoveRange(ref Work queue, Work last) {
      var tail = queue;
      if (tail == last)
        queue = null;
      else
        tail.Next = last.Next;
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void ReplaceRange<T>(ref Giver<T> queue, Giver<T> last, Giver<T> cache) {
      var tail = queue;
      if (tail == last) {
        queue = cache;
      } else {
        ReplaceRangeInclusive(tail, last.Next, cache);
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void ReplaceRange<T>(ref Cont<T> queue, Cont<T> last, Cont<T> cache) {
      var tail = queue;
      if (tail == last) {
        queue = cache;
      } else {
        ReplaceRangeInclusive(tail, last.Next, cache);
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void ReplaceRangeInclusive<T>(Giver<T> tail, Giver<T> next, Giver<T> cache) {
      if (null == cache) {
        tail.Next = next;
      } else {
        tail.Next = cache.Next;
        cache.Next = next;
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void ReplaceRangeInclusive(Work tail, Work next, Work cache) {
      if (null == cache) {
        tail.Next = next;
      } else {
        tail.Next = cache.Next;
        cache.Next = next;
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void Enqueue<T>(ref Giver<T> queue, Giver<T> elem) {
      var tail = queue;
      if (null == tail) {
        elem.Next = elem;
        queue = elem;
      } else {
        elem.Next = tail.Next;
        tail.Next = elem;
      }
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void Enqueue<T>(ref Cont<T> queue, Cont<T> elem) {
      var tail = queue;
      if (null == tail) {
        elem.Next = elem;
        queue = elem;
      } else {
        elem.Next = tail.Next;
        tail.Next = elem;
      }
    }

    /// <summary>Note that this specifically tries to reuse a giver from the
    /// queue.  This reduces the chance of space leaks.</summary>
    [MethodImpl(AggressiveInlining.Flag)]
    internal static void AddGiver<T>(ref Giver<T> queue, T x, int me, Pick pk, Cont<Unit> uK) {
      var last = queue;
      Giver<T> elem;
      if (null != last)
        goto MaybeReuse;
      elem = new Giver<T>();
      queue = elem;
      elem.Next = elem;
      goto Init;

    MaybeReuse:
      elem = last;// as Giver<T>;
      var pkGiver = elem.Pick;
      if (null != pkGiver && pkGiver.State > 0)
        goto Init;
      elem = new Giver<T>();
      queue = elem;
      elem.Next = last.Next;
      last.Next = elem;

    Init:
      elem.Value = x;
      elem.Me = me;
      elem.Pick = pk;
      elem.Cont = uK;
    }

    /// <summary>Note that this specifically tries to reuse a giver from the
    /// queue.  This reduces the chance of space leaks.</summary>
    [MethodImpl(AggressiveInlining.Flag)]
    internal static void AddGiver<T>(ref Giver<T> queue, T x, Cont<Unit> uK) {
      var last = queue;
      Giver<T> elem;
      if (null != last)
        goto MaybeReuse;
      elem = new Giver<T>();
      queue = elem;
      elem.Next = elem;
      elem.Value = x;
      elem.Cont = uK;
      return;

    MaybeReuse:
      elem = last;// as Giver<T>;
      var pkGiver = elem.Pick;
      if (null != pkGiver && pkGiver.State > 0)
        goto Reuse;
      elem = new Giver<T>();
      queue = elem;
      elem.Next = last.Next;
      last.Next = elem;
      elem.Value = x;
      elem.Cont = uK;
      return;

    Reuse:
      elem.Pick = null;
      elem.Value = x;
      elem.Cont = uK;
    }

    /// <summary>Note that this specifically tries to reuse a taker from the
    /// queue.  This reduces the chance of space leaks.</summary>
    [MethodImpl(AggressiveInlining.Flag)]
    internal static void AddTaker<T>(ref Cont<T> queue, int me, Pick pk, Cont<T> xK) {
      var last = queue;
      Taker<T> elem;
      if (null != last)
        goto MaybeReuse;
      elem = new Taker<T>();
      queue = elem;
      elem.Next = elem;
      goto Init;

    MaybeReuse:
      elem = last as Taker<T>;
      if (null == elem)
        goto New;
      var pkTaker = elem.Pick;
      if (/*null != pkTaker &&*/ pkTaker.State > 0)
        goto Init;
    New:
      elem = new Taker<T>();
      queue = elem;
      elem.Next = last.Next;
      last.Next = elem;

    Init:
      elem.Me = me;
      elem.Pick = pk;
      elem.Cont = xK;
    }

    [MethodImpl(AggressiveInlining.Flag)]
    internal static void AddTaker<T>(ref Cont<T> queue, Cont<T> xK) {
      var last = queue;
      if (null == last) {
        queue = xK;
        xK.Next = xK;
      } else {
        var next = last.Next;
        last.Next = xK;
        queue = xK;
        xK.Next = next;
      }
    }
  }
}
