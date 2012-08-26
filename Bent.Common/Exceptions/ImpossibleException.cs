using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bent.Common.Extensions;

namespace Bent.Common.Exceptions
{
    public class ImpossibleException : Exception
    {
        public ImpossibleException(string message)
            : base(ExceptionMessage(message)) { }

        private static string ExceptionMessage(string message)
        {
            return "A situation occured that should have been impossible. {0}".FormatWith(message);
        }
    }
}
