using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Archive.Utils
{
    public class ActionExecutionElement
    {
        public IEnumerable<Action> Actions { get; set; }

        public ActionExecutionElement Next { get; set; }

        public ActionExecutionElement(IEnumerable<Action> actions)
        {
            Actions = actions;
        }
    }

    public class ParallelOrderExecutor
    {
        private readonly List<Thread> _threads = new List<Thread>();

        private ActionExecutionElement _head;

        public static ParallelOrderExecutor Execute()
        {
            return new ParallelOrderExecutor();
        }

        public static ParallelOrderExecutor Do(Action action)
        {
            return Execute().ContinueWith(action);
        }

        public static ParallelOrderExecutor Do(IEnumerable<Action> actions)
        {
            return Execute().ContinueWith(actions);
        }

        public ParallelOrderExecutor ContinueWith(IEnumerable<Action> actions)
        {
            var newElement = new ActionExecutionElement(actions);

            var nextElement = _head;
            if (_head != null)
            {
                while (nextElement.Next != null)
                {
                    nextElement = nextElement.Next;
                }

                nextElement.Next = newElement;
            }
            else
            {
                _head = newElement;
            }

            return this;
        }

        public ParallelOrderExecutor ContinueWithParallel(int numberOfThreads, Action action)
        {
            var actions = new List<Action>();
            for (var i = 0; i < numberOfThreads; i++)
            {
                actions.Add(action);
            }

            ContinueWith(actions);
            return this;
        }

        public ParallelOrderExecutor ContinueWith(Action action)
        {
            ContinueWith(new[] {action});
            return this;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var nextElement = _head;
            while (nextElement != null)
            {
                // check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    foreach (var thread in _threads)
                    {
                        if (thread.IsAlive)
                            thread.Join();
                    }
                }

                // iteration start
                var countdownEvent = new CountdownEvent(nextElement.Actions.Count());

                foreach (var action in nextElement.Actions)
                {
                    var thread = new Thread(() =>
                    {
                        action();

                        // signal thread has finished execution
                        countdownEvent.Signal();
                    }) {IsBackground = true};
                    _threads.Add(thread);

                    thread.Start();
                }

                // wait while all actions in iterations finish
                countdownEvent.Wait(cancellationToken);

                // iteration end
                nextElement = nextElement.Next;
            }
        }
    }
}