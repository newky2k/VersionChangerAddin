using DSoft.VersionChanger.Enums;
using System;

namespace DSoft.VersionChanger.Data
{
	public class MacAppVersion : CocoaAppVersion
	{
		public MacAppVersion()
		{
			base.ApplicationType = AppType.Mac;
		}
	}
}