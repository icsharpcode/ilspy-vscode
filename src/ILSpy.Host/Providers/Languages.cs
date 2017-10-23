// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MsilDecompiler.MsilSpy;

namespace MsilDecompiler.Host.Providers
{
    public static class Languages
    {
        public static Language CurrentLanguage { get; private set; }
        public static Language CSharp { get; } = new CSharpLanguage();

        public static Language[] AllLanguages { get; } = new[] { Languages.CSharp };

        public static void SetLanguageByName(string name)
        {
            switch (name)
            {
                case Names.CSharp:
                    CurrentLanguage = CSharp;
                    break;
                default:
                    throw new ArgumentException(nameof(name));
            }
        }

        public static class Names
        {
            public const string CSharp = "C#";
            public const string VisualBasic = "VB";
            public const string IL = "IL";
        }
    }
}
