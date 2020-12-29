﻿using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Coop.Mod
{

    public interface IGameLoopRunner : IUpdateable
    {
        new void Update(TimeSpan frameTime);
        void RunOnMainThread(Action action, bool bBlocking = true);
        void SetGameLoopThread();
    }

    internal class GameLoopRunner : IGameLoopRunner
    {
        private static readonly Lazy<GameLoopRunner> m_Instance =
            new Lazy<GameLoopRunner>(() => new GameLoopRunner());

        private readonly List<(Action, EventWaitHandle)> m_Queue =
            new List<(Action, EventWaitHandle)>();

        private readonly object m_QueueLock = new object();
        private int m_GameLoopThreadId;

        public GameLoopRunner()
        {
        }

        public static GameLoopRunner Instance => m_Instance.Value;

        public void Update(TimeSpan frameTime)
        {
            if (Thread.CurrentThread.ManagedThreadId != Instance.m_GameLoopThreadId)
            {
                throw new ArgumentException("Wrong thread!");
            }

            List<(Action, EventWaitHandle)> toBeRun = new List<(Action, EventWaitHandle)>();
            lock (m_Queue)
            {
                toBeRun.AddRange(m_Queue);
                m_Queue.Clear();
            }

            foreach ((Action, EventWaitHandle) task in toBeRun)
            {
                task.Item1.Invoke();
                task.Item2?.Set();
            }
        }

        public void RunOnMainThread(Action action, bool bBlocking = true)
        {
            if (Thread.CurrentThread.ManagedThreadId == Instance.m_GameLoopThreadId)
            {
                action();
            }
            else
            {
                EventWaitHandle ewh = bBlocking ?
                    new EventWaitHandle(false, EventResetMode.ManualReset) :
                    null;
                lock (Instance.m_QueueLock)
                {
                    Instance.m_Queue.Add((action, ewh));
                }

                ewh?.WaitOne();
            }
        }

        public void SetGameLoopThread()
        {
            m_GameLoopThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }
}
