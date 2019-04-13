using DSoft.VersionChanger.Enums;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Xml;

namespace DSoft.VersionChanger.Data
{
	public class AndroidAppVersion : AppVersion
	{
		public override string FilePath
		{
			get;
			set;
		}

		public override string VersionOne
		{
			get
			{
				return this.GetVersion("android:versionCode");
			}
			set
			{
				this.mVersionOne = value;
			}
		}

		public override string VersionTwo
		{
			get
			{
				return this.GetVersion("android:versionName");
			}
			set
			{
				this.mVersionTwo = value;
			}
		}

		public AndroidAppVersion()
		{
			base.ApplicationType = AppType.Android;
		}

		public string GetVersion(string Key)
		{
			string str;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(this.FilePath);
			IEnumerator enumerator = xmlDocument.ChildNodes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					XmlNode current = (XmlNode)enumerator.Current;
					if (!current.Name.ToLower().Equals("manifest"))
					{
						continue;
					}
					XmlAttribute itemOf = current.Attributes[Key];
					str = (itemOf != null ? itemOf.Value.ToString() : string.Empty);
					return str;
				}

				return string.Empty;
			}
			finally
			{
				IDisposable disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}

		}

		public static string ToBuild(Version CurrentVersion)
		{
			int[] major = new int[] { CurrentVersion.Major, CurrentVersion.Minor, CurrentVersion.Build, CurrentVersion.Revision };
			string empty = string.Empty;
			int[] numArray = major;
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				int num = numArray[i];
				if (num > 0)
				{
					empty = string.Concat(empty, num.ToString("D2"));
				}
			}
			if (empty.StartsWith("0"))
			{
				empty = empty.Substring(1);
			}
			return empty;
		}

		public override void Update()
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(this.FilePath);
			bool flag = false;
			foreach (XmlNode childNode in xmlDocument.ChildNodes)
			{
				if (!childNode.Name.ToLower().Equals("manifest"))
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(this.mVersionOne))
				{
					childNode.Attributes["android:versionCode"].Value = this.mVersionOne;
					flag = true;
				}
				if (string.IsNullOrWhiteSpace(this.mVersionTwo))
				{
					continue;
				}
				childNode.Attributes["android:versionName"].Value = this.mVersionTwo;
				flag = true;
			}
			if (flag)
			{
				xmlDocument.Save(this.FilePath);
			}
		}
	}
}