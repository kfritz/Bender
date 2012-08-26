using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bent.Common.Extensions;

namespace Bend
{
    public sealed class Jid
    {
        private const string formatBare = "{0}@{1}";
        private const string formatFull = "{0}@{1}/{2}";

        public string Local { get; private set; }
        public string Domain { get; private set; }
        public string Resource { get; private set; }

        public Jid Bare { get; private set; }

        private readonly string toString;

        public Jid(string jid)
            : this(SplitJid(jid)) { }

        public Jid(string local, string domain)
            : this(local, domain, null) { }

        public Jid(string local, string domain, string resource)
            : this(Tuple.Create(local, domain, resource)) {}

        private Jid(Tuple<string, string, string> splitJid)
        {
            if (splitJid.Item2.IsNull())
            {
                throw new ArgumentNullException("domain");
            }

            this.Local = splitJid.Item1;
            this.Domain = splitJid.Item2;
            this.Resource = splitJid.Item3;

            this.Bare = this.Resource.IsNull() ? this : new Jid(this.Local, this.Domain);
            this.toString = this.Local.IsNull() ? this.Domain :
                (this.Resource.IsNull() ? formatBare.FormatWith(this.Local, this.Domain) :
                    formatFull.FormatWith(this.Local, this.Domain, this.Resource));
        }

        public override string ToString()
        {
            return this.toString;
        }

        private static Tuple<string, string, string> SplitJid(string jid)
        {
            string local = null;
            string domain = null;
            string resource = null;

            var atIndex = jid.IndexOf('@');
            var slashIndex = jid.IndexOf('/');

            int domainStartIndex;
            int domainEndIndex;

            if (atIndex >= 0 && slashIndex >= 0 && slashIndex < atIndex)
            {
                throw new Exception(); // TODO: More sepcific exception
            }

            if (atIndex == 0 || atIndex == jid.Length - 1)
            {
                throw new Exception(); // TODO: More specific exception
            }
            else if (atIndex > 0)
            {
                local = jid.Substring(0, atIndex);
                domainStartIndex = atIndex + 1;
            }
            else
            {
                domainStartIndex = 0;
            }

            if (slashIndex == 0 || slashIndex == jid.Length - 1)
            {
                throw new Exception(); // TODO: More specific exception
            }
            else if(slashIndex > 0)
            {
                resource = jid.Substring(slashIndex + 1);
                domainEndIndex = slashIndex - 1;
            }
            else
            {
                domainEndIndex = jid.Length - 1;
            }

            if (domainStartIndex <= domainEndIndex)
            {
                domain = jid.Substring(domainStartIndex, domainEndIndex - domainStartIndex + 1);
            }
            else
            {
                throw new Exception(); // TODO: More specific exception
            }

            return Tuple.Create(local, domain, resource);
        }
    }
}
