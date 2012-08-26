using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bent.Common.Exceptions;

namespace Bent.Common.IO
{
    public sealed class ConsoleStreamObserver : IObserver<ObservableStreamEvent>
    {
        private readonly Encoding encoding;
        private readonly ConsoleColor readColor;
        private readonly ConsoleColor writeColor;

        public ConsoleStreamObserver(Encoding encoding, ConsoleColor readColor, ConsoleColor writeColor)
        {
            this.encoding = encoding;
            this.readColor = readColor;
            this.writeColor = writeColor;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(ObservableStreamEvent value)
        {
            ColorConsole.Temp(GetConsoleColor(value.Operation),
                () => Console.Write(this.encoding.GetString(value.Data.ToArray())));
        }

        private ConsoleColor GetConsoleColor(StreamOperation operation)
        {
            switch (operation)
            {
                case StreamOperation.Read:
                    return this.readColor;
                case StreamOperation.Write:
                    return this.writeColor;
                default:
                    throw new UnhandledEnumException(operation);
            }
        }
    }
}
