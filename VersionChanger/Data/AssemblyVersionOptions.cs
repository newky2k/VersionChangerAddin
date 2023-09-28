using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Data
{
    public  class AssemblyVersionOptions
    {
        public bool UpdateAssemblyVersion { get; set; } = true;

        public bool UpdateAssemblyVersionPrefix { get; set; } = true;

        public bool UpdateVersion { get; set; } = true;

        public bool UpdatePackageVersion { get; set; } = true;

        public bool UpdateFileVersion { get; set; } = true;

        public bool UpdateInformationalVersion { get; set; } = true;

        public bool EnableRevision { get; set; } = true;

        public AssemblyVersionOptions()
        {
           
        }

        /// <summary>
        /// Create default instance
        /// </summary>
        public static AssemblyVersionOptions Default => new AssemblyVersionOptions();

        public string GetVersionString(Version version)
        {
            return (EnableRevision) ? version.ToString(3) : version.ToString(3);
        }

        public string CalculateVersion(Version version, string versionSuffix = null, bool includeZeroRevision = false)
        {
            var newFileVersionValue = (EnableRevision) ? version.ToString() : ((includeZeroRevision) ? $"{version.ToString(3)}.0" : version.ToString(3));

            if (string.IsNullOrEmpty(versionSuffix) == false)
            {
                if (versionSuffix.StartsWith("-") || versionSuffix.StartsWith("+"))
                {
                    // overriding for semver
                    newFileVersionValue += $"{versionSuffix}";
                }
                else
                {
                    // overriding for semver
                    newFileVersionValue += $"-{versionSuffix}";
                }

            }

            return newFileVersionValue;
        }
    }
}
