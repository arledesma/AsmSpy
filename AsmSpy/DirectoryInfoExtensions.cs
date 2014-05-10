using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AsmSpy
{
    internal static class DirectoryInfoExtensions
    {
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
        {
            return GetFilesByExtensions(dir, SearchOption.TopDirectoryOnly, extensions);
        }

        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, SearchOption searchOption, params string[] extensions)
        {
            if(extensions == null)
                throw new ArgumentNullException("extensions");

            return extensions.Aggregate(Enumerable.Empty<FileInfo>(), (current, ext) => current.Concat(dir.GetFiles(ext, searchOption)));
        }
    }
}
