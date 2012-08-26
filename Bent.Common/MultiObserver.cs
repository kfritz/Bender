using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bent.Common
{
    public sealed class MultiObserver<T> : IObserver<T>
    {
        private ReaderWriterLockSlim rw = new ReaderWriterLockSlim();
        private HashSet<IObserver<T>> observers = new HashSet<IObserver<T>>();

        public IDisposable Add(IObserver<T> observer)
        {
            rw.EnterWriteLock();
            try
            {
                this.observers.Add(observer);
            }
            finally
            {
                rw.ExitWriteLock();
            }

            return new Unsubscriber(() =>
                {
                    rw.EnterWriteLock();
                    try
                    {
                        this.observers.Remove(observer);
                    }
                    finally
                    {
                        rw.EnterWriteLock();
                    }
                });
        }

        public void OnCompleted()
        {
            this.ForEachObserver(i => i.OnCompleted());
        }

        public void OnError(Exception error)
        {
            this.ForEachObserver(i => i.OnError(error));
        }

        public void OnNext(T value)
        {
            this.ForEachObserver(i => i.OnNext(value));
        }

        private void ForEachObserver(Action<IObserver<T>> action)
        {
            var exceptions = new List<Exception>();
            
            rw.EnterReadLock();
            try
            {
                foreach (var o in this.observers)
                {
                    try
                    {
                        action(o);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
            }
            finally
            {
                rw.ExitReadLock();
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action unsunscribe;

            public Unsubscriber(Action unsubcribe)
            {
                this.unsunscribe = unsubcribe;
            }

            public void Dispose()
            {
                this.unsunscribe();
            }
        }        
    }
}
