using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.Exceptions
{
    public class UnhandledEnumException : Exception
    {
        private const string formatMessage = "Enumeration value {0} of type {1} was unhandled.";

        public UnhandledEnumException(Enum enumValue)
            : base(ExceptionMessage(enumValue)) { }

        private static string ExceptionMessage(Enum enumValue)
        {
            return String.Format(formatMessage, enumValue, enumValue.GetType().Name);
        }
    }
}
