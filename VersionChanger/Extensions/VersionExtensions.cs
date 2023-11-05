using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Extensions
{
    public static class VersionExtensions
    {
        public static Version AsCleanVersion(this Version version)
        {
            var str = version.AsCleanString();

            return Version.Parse(str);
        }

        public static string AsCleanString(this Version version)
        {
            var strVersion = "1.0";

            if (version.Major >= 0)
            {
                strVersion = $"{version.Major}";
            }

            if (version.Minor >= 0)
            {
                strVersion = $"{strVersion}.{version.Minor}";
            }
            else
            {
                strVersion = $"{strVersion}.0";
            }

            if (version.Build >= 0)
            {
                strVersion = $"{strVersion}.{version.Build}";
            }
            else
            {
                strVersion = $"{strVersion}.0";
            }

            if (version.Revision >= 0)
            {
                strVersion = $"{strVersion}.{version.Revision}";
            }
            else
            {
                strVersion = $"{strVersion}.0";
            }


            return strVersion;
        }
    }
}
