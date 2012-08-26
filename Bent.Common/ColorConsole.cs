using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common
{
    public static class ColorConsole
    {
        public static void Temp(ConsoleColor foregroundColor, Action action)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;

            action();

            Console.ForegroundColor = original;
        }
    }
}
