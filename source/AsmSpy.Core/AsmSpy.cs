using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmSpy.Core;

namespace AsmSpy
{
    public class AsmSpy
    {
        private readonly String _path;
        private readonly bool _all;
        private readonly bool _skipSystem;
        private readonly bool _subDirectories;
        private readonly ConcurrentDictionary<string, IList<ReferencedAssembly>> _assemblies = new ConcurrentDictionary<string, IList<ReferencedAssembly>>();
        private readonly IList<AssemblyResult> _assemblyResults = new List<AssemblyResult>();
        private readonly TextWriter _writer;

        public AsmSpy()
        {
            _writer = TextWriter.Null;
        }

        public AsmSpy(TextWriter writer)
        {
            _writer = writer;
        }

        public AsmSpy(string path)
        {
            _path = path;
            _writer = TextWriter.Null;
        }

        public AsmSpy(string path, TextWriter writer)
        {
            _path = path;
            _writer = writer;
        }

        public AsmSpy(bool all, string path, bool skipSystem, bool subDirectories)
        {
            _all = all;
            _path = path;
            _skipSystem = skipSystem;
            _subDirectories = subDirectories;
            _writer = TextWriter.Null;
        }

        public AsmSpy(bool all, string path, bool skipSystem, bool subDirectories, TextWriter writer)
        {
            _all = all;
            _path = path;
            _skipSystem = skipSystem;
            _subDirectories = subDirectories;
            _writer = writer;
        }

        public IList<AssemblyResult> Analyse()
        {
            if(String.IsNullOrWhiteSpace(_path))
                throw new ArgumentOutOfRangeException(_path, "Must instantiate with path in order to call Analyse()");

            var directoryInfo = GetDirectoryInfo();
            var assemblyFiles = GetFiles(directoryInfo);
            return AnalyseAssemblies(assemblyFiles);
        }

        private List<FileInfo> GetFiles(DirectoryInfo directoryInfo)
        {
            
            _writer.WriteLine("Check assemblies in:");
            _writer.WriteLine(directoryInfo.FullName);
            _writer.WriteLine("");

            var option = _subDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var assemblyFiles = directoryInfo.GetFilesByExtensions(option, "*.dll", "*.exe").ToList();

            if(assemblyFiles.Any())
                return assemblyFiles;

            var error = String.Format("No dll files found in directory: '{0}'",
                directoryInfo.FullName);
            throw new FileNotFoundException(error, directoryInfo.FullName);
        }

        private DirectoryInfo GetDirectoryInfo()
        {
            if(Directory.Exists(_path))
                return new DirectoryInfo(_path);

            _writer.WriteLine("Directory: '{0}' does not exist.", _path);
            throw new FileNotFoundException();
        }

        public IList<AssemblyResult> AnalyseAssemblies(List<FileInfo> assemblyFiles)
        {
            Parallel.ForEach(assemblyFiles, fileInfo =>
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                }
                catch(Exception ex)
                {
                    _writer.WriteLine("Failed to load assembly '{0}': {1}", fileInfo.FullName, ex.Message);
                    return;
                }

                foreach(var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    if(!_assemblies.ContainsKey(referencedAssembly.Name))
                    {
                        _assemblies.TryAdd(referencedAssembly.Name, new List<ReferencedAssembly>());
                    }
                    _assemblies[referencedAssembly.Name]
                        .Add(new ReferencedAssembly(referencedAssembly.Version, assembly));
                }
            });

            return GetResults();
        }

        private IList<AssemblyResult> GetResults()
        {
            if (!_all)
            {
                _writer.WriteLine("Detailing only conflicting assembly references.");
            }

            foreach (var assembly in _assemblies)
            {
                if (_skipSystem && (assembly.Key.StartsWith("System") || assembly.Key.StartsWith("mscorlib")))
                    continue;

                if (!_all && (assembly.Value.GroupBy(x => x.VersionReferenced).Count() == 1))
                    continue;

                var versionsList = new List<string>();
                var referencedAssemblies = assembly.Value.Select(GetReferencedAssemblyPair).ToList();

                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (!versionsList.Contains(referencedAssembly.Key))
                    {
                        versionsList.Add(referencedAssembly.Key);
                    }
                }

                _assemblyResults.Add(new AssemblyResult
                {
                    ReferencedAssemblies = referencedAssemblies,
                    VersionsList = versionsList,
                    AssemblyName = assembly.Key
                });
            }

            return _assemblyResults;
        }

        public void PrintResults()
        {
            foreach (var assemblyResult in _assemblyResults.OrderBy(x=>x.AssemblyName))
            {
                PrintResultsForAssembly(assemblyResult);
            }
        }
        private void PrintResultsForAssembly(AssemblyResult result)
        {
            Console.ForegroundColor = ConsoleColor.White;
            _writer.Write("Reference: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            _writer.WriteLine("{0}", result.AssemblyName);

            foreach(var referencedAssembly in result.ReferencedAssemblies)
            {
                var versionColor = ConsoleColors[result.VersionsList.IndexOf(referencedAssembly.Key) % ConsoleColors.Length];

                Console.ForegroundColor = versionColor;
                _writer.Write("   {0}", referencedAssembly.Key);

                Console.ForegroundColor = ConsoleColor.White;
                _writer.Write(" by ");

                Console.ForegroundColor = ConsoleColor.Gray;
                _writer.WriteLine("{0}", referencedAssembly.Value);
            }

            _writer.WriteLine();
        }

        private static KeyValuePair<string, string> GetReferencedAssemblyPair(ReferencedAssembly assembly)
        {
            return new KeyValuePair<string, string>(assembly.VersionReferenced.ToString(), assembly.ReferencedBy.GetName().Name);
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