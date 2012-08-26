using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common
{
    public struct Automatic<T>
    {
        private readonly T value;
        private readonly bool hasValue;

        public bool HasValue
        {
            get { return this.hasValue; }
        }

        public T Value
        {
            get
            {
                if (!this.hasValue)
                    throw new Exception("Value must be determined automatically by caller.");

                return this.value;
            }
        }

        public Automatic(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public T ValueOr(T automaticValue)
        {
            return this.hasValue ? this.value : automaticValue;
        }

        public static implicit operator Automatic<T>(T value)
        {
            return new Automatic<T>(value);
        }
    }
}
