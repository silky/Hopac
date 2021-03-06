﻿// Copyright (C) by Housemarque, Inc.

namespace Hopac {
  using Microsoft.FSharp.Core;
  using Hopac.Core;
  using System.Runtime.CompilerServices;
  using System;

  /// <summary>A parallel processing job.</summary>
  public abstract class Job<T> {
    /// Internal implementation detail.
    internal abstract void DoJob(ref Worker wr, Cont<T> aK);
  }

  namespace Core {
    /// Internal implementation detail.
    internal sealed class JobWork<T> : Work {
      internal readonly Job<T> tJ;
      internal Cont<T> tK;

      /// Internal implementation detail.
      internal override void DoHandle(ref Worker wr, Exception e) {
        this.tK.DoHandle(ref wr, e);
      }

      /// Internal implementation detail.
      [MethodImpl(AggressiveInlining.Flag)]
      internal JobWork(Job<T> tJ) {
        this.tJ = tJ;
      }

      /// Internal implementation detail.
      [MethodImpl(AggressiveInlining.Flag)]
      internal JobWork(Job<T> tJ, Cont<T> tK) {
        this.tJ = tJ;
        this.tK = tK;
      }

      /// Internal implementation detail.
      internal override void DoWork(ref Worker wr) {
        this.tJ.DoJob(ref wr, this.tK);
      }
    }
  }
}
