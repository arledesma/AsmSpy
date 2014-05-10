using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AsmSpy
{
    internal class Spy
    {
        private readonly Options _options;
        public Spy(Options options)
        {
            _options = options;
        }

        public void Analyse()
        {
            var directoryInfo = GetDirectoryInfo(_options.Path);
            var assemblyFiles = GetFiles(directoryInfo, _options.SubDirectories);
            AnalyseAssemblies(assemblyFiles, _options.SkipSystem, _options.OnlyConflicts);
        }

        private static List<FileInfo> GetFiles(DirectoryInfo directoryInfo, bool subDirectories)
        {
            Console.WriteLine("Check assemblies in:");
            Console.WriteLine(directoryInfo.FullName);
            Console.WriteLine("");

            var option = subDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var assemblyFiles = directoryInfo.GetFilesByExtensions(option, "*.dll", "*.exe")
                .ToList();

            if(assemblyFiles.Any())
                return assemblyFiles;

            var error = String.Format("No dll files found in directory: '{0}'",
                directoryInfo.FullName);
            throw new FileNotFoundException(error, directoryInfo.FullName);
        }

        private static DirectoryInfo GetDirectoryInfo(string path)
        {
            if(Directory.Exists(path))
                return new DirectoryInfo(path);

            PrintDirectoryNotFound(path);
            throw new FileNotFoundException();
        }

        public static void AnalyseAssemblies(List<FileInfo> assemblyFiles, bool skipSystem, bool onlyConflicts)
        {
            var assemblies = new ConcurrentDictionary<string, IList<ReferencedAssembly>>();
            Parallel.ForEach(assemblyFiles, fileInfo =>
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Failed to load assembly '{0}': {1}", fileInfo.FullName, ex.Message);
                    return;
                }

                foreach(var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    if(!assemblies.ContainsKey(referencedAssembly.Name))
                    {
                        assemblies.TryAdd(referencedAssembly.Name, new List<ReferencedAssembly>());
                    }
                    assemblies[referencedAssembly.Name]
                        .Add(new ReferencedAssembly(referencedAssembly.Version, assembly));
                }
            });

            if(onlyConflicts)
                Console.WriteLine("Detailing only conflicting assembly references.");

            foreach(var assembly in assemblies.OrderBy(x => x.Key))
            {
                if(skipSystem && (assembly.Key.StartsWith("System") || assembly.Key.StartsWith("mscorlib")))
                    continue;

                if(onlyConflicts && (!onlyConflicts || assembly.Value.GroupBy(x => x.VersionReferenced).Count() == 1))
                    continue;

                var versionsList = new List<string>();
                var referencedAssemblies = Enumerable.ToList(assembly.Value.Select(GetReferencedAssemblyPair));

                foreach(var referencedAssembly in referencedAssemblies)
                {
                    if(!versionsList.Contains(referencedAssembly.Key))
                    {
                        versionsList.Add(referencedAssembly.Key);
                    }
                }

                PrintResults(referencedAssemblies, versionsList, assembly.Key);
            }

            if(Debugger.IsAttached)
                Debugger.Break();
        }

        private static void PrintResults(IEnumerable<KeyValuePair<string, string>> referencedAssemblies, IList<string> versionsList, string assemblyName)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Reference: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("{0}", assemblyName);

            foreach(var referencedAssembly in referencedAssemblies)
            {
                var versionColor = ConsoleColors[versionsList.IndexOf(referencedAssembly.Key) % ConsoleColors.Length];

                Console.ForegroundColor = versionColor;
                Console.Write("   {0}", referencedAssembly.Key);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" by ");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("{0}", referencedAssembly.Value);
            }

            Console.WriteLine();
        }

        private static KeyValuePair<string, string> GetReferencedAssemblyPair(ReferencedAssembly assembly)
        {
            return new KeyValuePair<string, string>(assembly.VersionReferenced.ToString(), assembly.ReferencedBy.GetName().Name);
        }

        private static void PrintDirectoryNotFound(string directoryPath)
        {
            Console.WriteLine("Directory: '" + directoryPath + "' does not exist.");
        }

        private static readonly ConsoleColor[] ConsoleColors =
        {
            ConsoleColor.Green,
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
        };
    }
}