using System;
using System.Diagnostics;
using System.Globalization;
using CommandLine;

namespace AsmSpy
{
    public class Program
    {
        static void Main(string[] args)
        {
            var options = GetOptions(args);
            var spy = new AsmSpy(options.All, options.Path, options.SkipSystem, options.SubDirectories, Console.Out);
            spy.Analyse();

            if(Debugger.IsAttached)
                Debugger.Break();
        }

        private static Options GetOptions(string[] args)
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

            return options;
        }
    }
}
