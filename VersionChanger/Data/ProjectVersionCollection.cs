using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace DSoft.VersionChanger.Data
{
	public class ProjectVersionCollection : ObservableCollection<ProjectVersion>
	{
		public bool HasAndroid
		{
			get;
			set;
		}

		public bool HasIosMac
		{
			get;
			set;
		}

		public Version HighestFileVersion
		{
			get
			{
				Version fileVersion = null;
				foreach (ProjectVersion item in base.Items)
				{
					if (fileVersion != null)
					{
						if (item.FileVersion <= fileVersion)
						{
							continue;
						}
						fileVersion = item.FileVersion;
					}
					else
					{
						fileVersion = item.FileVersion;
					}
				}
				return fileVersion;
			}
		}

		public Version HighestVersion
		{
			get
			{
                Version version = null;

				foreach (ProjectVersion item in base.Items)
				{
					if (version != null)
					{
						if (item.ActiveVersion <= version)
						{
							continue;
						}
						version = item.ActiveVersion;
					}
					else
					{
						version = item.ActiveVersion;
					}
				}
				return version;
			}
		}

		public ProjectVersionCollection()
		{
		}
	}
}