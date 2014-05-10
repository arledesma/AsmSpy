using System;
using System.Collections.Generic;

namespace AsmSpy
{
    public class AssemblyResult
    {
        public IEnumerable<KeyValuePair<string, string>> ReferencedAssemblies { get; set; }
        public IList<string> VersionsList { get; set; }
        public String AssemblyName { get; set; }
    }
}