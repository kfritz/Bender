using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.IO
{
    public sealed class ObservableStreamEvent
    {
        public StreamOperation Operation { get; private set; }
        public IEnumerable<byte> Data { get; private set; }

        public ObservableStreamEvent(StreamOperation operation, IEnumerable<byte> data)
        {
            this.Operation = operation;
            this.Data = data;
        }
    }
}
