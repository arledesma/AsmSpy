using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

namespace AsmSpy
{
    public class Program
    {
        static readonly ConsoleColor[] ConsoleColors = new ConsoleColor[]
            {
                ConsoleColor.Green,
                ConsoleColor.Red,
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
                ConsoleColor.Cyan,
                ConsoleColor.Magenta,
            };
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

            var directoryInfo = GetDirectoryInfo(options.Path);
            var assemblyFiles = GetFiles(directoryInfo, options.SubDirectories);
            AnalyseAssemblies(assemblyFiles, options);
        }

        private static List<FileInfo> GetFiles(DirectoryInfo directoryInfo, bool subDirectories)
        {
            Console.WriteLine("Check assemblies in:");
            Console.WriteLine(directoryInfo.FullName);
            Console.WriteLine("");

            var option = subDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var assemblyFiles = directoryInfo.GetFilesByExtensions(option, "*.dll", "*.exe")
                                             .ToList();

            if (assemblyFiles.Any())
                return assemblyFiles;

            var error = String.Format("No dll files found in directory: '{0}'",
                directoryInfo.FullName);
            throw new FileNotFoundException(error, directoryInfo.FullName);
        }

        private static DirectoryInfo GetDirectoryInfo(string path)
        {
            if (Directory.Exists(path))
                return new DirectoryInfo(path);

            PrintDirectoryNotFound(path);
            throw new FileNotFoundException();
        }

        public static void AnalyseAssemblies(List<FileInfo> assemblyFiles, Options options)
        {
            var assemblies = new ConcurrentDictionary<string, IList<ReferencedAssembly>>();
            Parallel.ForEach(assemblyFiles, fileInfo =>
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load assembly '{0}': {1}", fileInfo.FullName, ex.Message);
                    return;
                }

                foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    if (!assemblies.ContainsKey(referencedAssembly.Name))
                    {
                        assemblies.TryAdd(referencedAssembly.Name, new List<ReferencedAssembly>());
                    }
                    assemblies[referencedAssembly.Name]
                        .Add(new ReferencedAssembly(referencedAssembly.Version, assembly));
                }
            });

            if (options.OnlyConflicts)
                Console.WriteLine("Detailing only conflicting assembly references.");

            foreach (var assembly in assemblies.OrderBy(x=>x.Key))
            {
                if (options.SkipSystem && (assembly.Key.StartsWith("System") || assembly.Key.StartsWith("mscorlib"))) continue;

                if (options.OnlyConflicts && (!options.OnlyConflicts || assembly.Value.GroupBy(x => x.VersionReferenced).Count() == 1))
                    continue;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Reference: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("{0}", assembly.Key);

                var versionsList = new List<string>();
                var referencedAssemblies = assembly.Value.Select(GetReferencedAssemblyPair).ToList();

                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (!versionsList.Contains(referencedAssembly.Key))
                    {
                        versionsList.Add(referencedAssembly.Key);
                    }
                }

                foreach (var referencedAssembly in referencedAssemblies)
                {
                    var versionColor = ConsoleColors[versionsList.IndexOf(referencedAssembly.Key)%ConsoleColors.Length];

                    Console.ForegroundColor = versionColor;
                    Console.Write("   {0}", referencedAssembly.Key);
                        
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" by ");
                        
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("{0}", referencedAssembly.Value);
                }

                Console.WriteLine();
            }

            if (Debugger.IsAttached)
                Debugger.Break();
        }

        private static KeyValuePair<string, string> GetReferencedAssemblyPair(ReferencedAssembly assembly)
        {
            return new KeyValuePair<string, string>(assembly.VersionReferenced.ToString(), assembly.ReferencedBy.GetName().Name);
        }

        private static void PrintDirectoryNotFound(string directoryPath)
        {
            Console.WriteLine("Directory: '" + directoryPath + "' does not exist.");
        }
    }

    public class ReferencedAssembly
    {
        public Version VersionReferenced { get; private set; }
        public Assembly ReferencedBy { get; private set; }

        public ReferencedAssembly(Version versionReferenced, Assembly referencedBy)
        {
            VersionReferenced = versionReferenced;
            ReferencedBy = referencedBy;
        }
    }
}
