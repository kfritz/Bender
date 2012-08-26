using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Apis.WolframAlpha
{
    internal static class Reference
    {
        private static IDictionary<Format, string> formatToString = new Dictionary<Format, string>
        {
            {Format.Html, "html"},
            {Format.Image, "image"},
            {Format.MathematicaCells, "cell"},
            {Format.MathematicaInput, "minput"},
            {Format.PlainText, "plaintext"},
            {Format.Sound, "sound"}
        };

        public static string GetFormatString(Format format)
        {
            return formatToString[format];
        }
    }
}
