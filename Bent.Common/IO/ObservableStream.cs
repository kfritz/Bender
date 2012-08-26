using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.IO
{
    public sealed class ObservableStream : WrapperStreamBase, IObservable<ObservableStreamEvent>
    {
        private readonly List<IObserver<ObservableStreamEvent>> observers = new List<IObserver<ObservableStreamEvent>>();

        public ObservableStream(Stream stream)
            : base(stream) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var ret =  base.Read(buffer, offset, count);

            this.NotifySubscribers(new ObservableStreamEvent(StreamOperation.Read, buffer.Skip(offset).Take(ret).ToList()));

            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            this.NotifySubscribers(new ObservableStreamEvent(StreamOperation.Write, buffer.Skip(offset).Take(count).ToList()));
        }

        public IDisposable Subscribe(IObserver<ObservableStreamEvent> observer)
        {
            this.observers.Add(observer);

            return new Unsubscriber(observer, this.observers);
        }

        private void NotifySubscribers(ObservableStreamEvent streamEvent)
        {            
            foreach (var observer in this.observers)
            {
                observer.OnNext(streamEvent);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly IObserver<ObservableStreamEvent> observer;
            private readonly List<IObserver<ObservableStreamEvent>> observers;

            public Unsubscriber(IObserver<ObservableStreamEvent> observer, List<IObserver<ObservableStreamEvent>> observers)
            {
                this.observer = observer;
                this.observers = observers;
            }

            public void Dispose()
            {
                lock (this.observers)
                {
                    this.observers.Remove(this.observer);
                }
            }
        }
    }
}
