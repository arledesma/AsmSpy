using System;
using System.Globalization;
using CommandLine;

namespace AsmSpy
{
    public class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(x =>
            {
                x.HelpWriter = Console.Error;
                x.CaseSensitive = false;
                x.IgnoreUnknownArguments = false;
                x.MutuallyExclusive = false;
                x.ParsingCulture = CultureInfo.CurrentCulture;
            });

            var options = new Options();
            if (!parser.ParseArguments(args, options))
            {
                Environment.Exit(Parser.DefaultExitCodeFail);
            }

            var spy = new Spy(options);
            spy.Analyse();
        }
    }
}
