using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Helpers
{
	internal class VersionHelper
	{
		public static string CalculateVersion(Version version, string versionSuffix = null, bool includeZeroRevision = false)
		{
			var newFileVersionValue = (version.Revision == -1) ? ((includeZeroRevision) ? $"{version.ToString(3)}.0" : version.ToString(3)) : version.ToString();

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
