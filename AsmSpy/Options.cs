using System;
using CommandLine;
using CommandLine.Text;

namespace AsmSpy
{
    public class Options
    {
        [Option('p', HelpText = "Directory to scan.", Required = true)]
        public String Path { get; set; }

        [Option('c', DefaultValue = false, HelpText = "Only includes files with conflicts.", Required = false)]
        public bool OnlyConflicts { get; set; }

        [Option('s', DefaultValue = false, HelpText = "Do not include system files with conflicts.", Required = false)]
        public bool SkipSystem { get; set; }

        [Option('d', DefaultValue = false, HelpText = "Recurse into subdirectories in order to find additional files.", Required = false)]
        public bool SubDirectories { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}