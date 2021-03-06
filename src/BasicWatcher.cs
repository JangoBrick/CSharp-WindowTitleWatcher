﻿using System;
using System.Diagnostics;
using System.Threading;
using WindowTitleWatcher.Internal;
using WindowTitleWatcher.Util;

namespace WindowTitleWatcher
{
    /// <summary>
    /// State and title watcher for a specific window handle, i.e. the most
    /// basic type.
    /// </summary>
    public class BasicWatcher : Watcher, IDisposable
    {
        /// <summary>
        /// Returns whether the window has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get;
            protected set;
        }

        public event EventHandler Disposed;

        private readonly WindowPoller poller;
        private WindowPoller.Results lastPoll;
        private bool isRunning = true;

        /// <summary>
        /// Watches the process's main window in the background (keepAlive = false).
        /// </summary>
        /// <param name="proc">The process.</param>
        public BasicWatcher(Process proc)
            : this(proc.MainWindowHandle)
        {
        }

        /// <summary>
        /// Watches the given window in the background (keepAlive = false).
        /// </summary>
        /// <param name="window">The window.</param>
        public BasicWatcher(WindowInfo window)
            : this(window.Handle, false)
        {
        }

        /// <summary>
        /// Watches the window with the given handle in the background
        /// (keepAlive = false).
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        public BasicWatcher(IntPtr windowHandle)
            : this(windowHandle, false)
        {
        }

        /// <summary>
        /// Watches the window with the given handle, optionally keeping this
        /// process active until the remote process closes or this watcher is
        /// disposed.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="keepAlive">Whether to keep this process alive.</param>
        public BasicWatcher(IntPtr windowHandle, bool keepAlive)
        {
            this.poller = new WindowPoller(windowHandle);

            Update();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = !keepAlive;

                while (isRunning)
                {
                    Thread.Sleep(10);
                    Update();
                }
            }).Start();
        }

        /// <summary>
        /// Disposes this watcher (stops any threads used and stops reporting).
        /// </summary>
        public void Dispose()
        {
            isRunning = false;
        }

        private void Update()
        {
            WindowPoller.Results prev = lastPoll;
            WindowPoller.Results results = poller.Poll();
            lastPoll = results;

            IsVisible = results.IsVisible;
            Title = results.Title;
            IsDisposed = results.IsDisposed;

            if (prev != null && results.IsVisible != prev.IsVisible)
            {
                RaiseVisibilityChanged(EventArgs.Empty);
            }

            if (results.IsDisposed)
            {
                RaiseDisposed(EventArgs.Empty);
                isRunning = false;
                return;
            }

            if (prev != null && results.Title != prev.Title)
            {
                RaiseTitleChanged(new TitleEventArgs(prev.Title, results.Title));
            }
        }

        protected void RaiseDisposed(EventArgs e)
        {
            Disposed?.Invoke(this, e);
        }
    }
}
