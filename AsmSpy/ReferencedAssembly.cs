using System;
using System.Reflection;

namespace AsmSpy
{
    internal class ReferencedAssembly
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