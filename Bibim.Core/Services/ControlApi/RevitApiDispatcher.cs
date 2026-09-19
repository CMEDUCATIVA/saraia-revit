// Copyright (c) 2026 SquareZero Inc. — Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace Bibim.Core
{
    /// <summary>
    /// Generic main-thread invoker for the local control API.
    ///
    /// HttpListener callbacks run on background thread-pool threads, but the Revit
    /// API must be touched from the main thread inside a valid API context. This
    /// IExternalEventHandler drains a queue of <see cref="Func{UIApplication, Object}"/>
    /// work items when Revit raises the event, returning each result through a
    /// TaskCompletionSource so the calling background thread can await it.
    ///
    /// Code execution (dry-run / commit with transactions) keeps using the existing
    /// <see cref="BibimExecutionHandler"/>; this dispatcher is for read-only queries
    /// and image export where compiling a throwaway assembly would be overkill.
    /// </summary>
    public class RevitApiDispatcher : IExternalEventHandler
    {
        private sealed class WorkItem
        {
            public Func<UIApplication, object> Work;
            public TaskCompletionSource<object> Callback;
        }

        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private ExternalEvent _event;

        /// <summary>
        /// Wire up the ExternalEvent created for this handler. Must be called once,
        /// from a valid Revit context (e.g. IExternalApplication.OnStartup), before
        /// any <see cref="InvokeAsync"/> call.
        /// </summary>
        public void SetEvent(ExternalEvent externalEvent) => _event = externalEvent;

        /// <summary>
        /// Queue work to run on the Revit main thread and await its result.
        /// Safe to call from any thread.
        /// </summary>
        public Task<object> InvokeAsync(Func<UIApplication, object> work)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));

            var item = new WorkItem
            {
                Work = work,
                Callback = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            _queue.Enqueue(item);
            _event?.Raise();
            return item.Callback.Task;
        }

        public void Execute(UIApplication app)
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    item.Callback.TrySetResult(item.Work(app));
                }
                catch (Exception ex)
                {
                    item.Callback.TrySetException(ex);
                }
            }
        }

        public string GetName() => "Bibim Control API Dispatcher";
    }
}
