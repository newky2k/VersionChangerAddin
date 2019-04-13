using DSoft.VersionChanger.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DSoft.VersionChanger.Data
{
	public class CocoaAppVersion : AppVersion
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
				return this.GetVersion("CFBundleShortVersionString");
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
				return this.GetVersion("CFBundleVersion");
			}
			set
			{
				this.mVersionTwo = value;
			}
		}

		public CocoaAppVersion()
		{
			base.ApplicationType = AppType.Cocoa;
		}

		public string GetVersion(string Key)
		{
			string str;
			try
			{
				str = ((Dictionary<string, object>)Plist.readPlist(this.FilePath))[Key].ToString();
			}
			catch
			{
				str = string.Empty;
			}
			return str;
		}

		public static string ToShortVersion(Version CurrentVersion)
		{
			string str = CurrentVersion.Revision.ToString("D2");
			string str1 = CurrentVersion.ToString();
			if ((int)str1.Split(new char[] { '.' }).Length > 3)
			{
				int num = str1.LastIndexOf('.');
				str1 = str1.Remove(num);
				str1 = str1.Insert(num, str);
			}
			return str1;
		}

		public override void Update()
		{
			try
			{
				Dictionary<string, object> strs = (Dictionary<string, object>)Plist.readPlist(this.FilePath);
				bool flag = false;
				if (!string.IsNullOrWhiteSpace(this.mVersionOne))
				{
					strs["CFBundleShortVersionString"] = this.mVersionOne;
					flag = true;
				}
				if (!string.IsNullOrWhiteSpace(this.mVersionTwo))
				{
					strs["CFBundleVersion"] = this.mVersionTwo;
					flag = true;
				}
				if (flag)
				{
					Plist.writeXml(strs, this.FilePath);
				}
			}
			catch
			{
			}
		}
	}
}