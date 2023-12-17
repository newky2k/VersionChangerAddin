using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace DSoft.VersionChanger.Data
{
	public class ProjectVersionCollection : ObservableCollection<ProjectVersion>
	{
		public event EventHandler<bool> SelectionStateChanged = delegate { };

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

        internal bool UpdateState(Dictionary<string, bool> dict)
        {
			var result = false;

			foreach (var item in Items)
			{
				if (dict.ContainsKey(item.Name))
				{
					item.Update = dict[item.Name];
				}
				else
				{
					result = true;
				}
			}
			
			return result;
        }

		public void WireUpEvents()
		{
            foreach (INotifyPropertyChanged item in Items)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ProjectVersion.Update)))
            {
				SelectionStateChanged(sender, true);
            }
        }
    }
}