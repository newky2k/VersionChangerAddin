using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

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

		public Dictionary<string, bool> StateDictionary
		{
			get
			{
				var dict = new Dictionary<string, bool>();

                foreach (var item in Items)
                {
					dict.Add(item.Name, item.Update);
                }

                return dict;
			}
		}

		public ProjectVersionCollection()
		{
		}

        internal void UpdateState(Dictionary<string, bool> dict)
        {
			foreach (var item in Items)
			{
				if (dict.ContainsKey(item.Name))
				{
					item.Update = dict[item.Name];
				}
			}
        }
    }
}