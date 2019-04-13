using DSoft.VersionChanger.Enums;
using System;

namespace DSoft.VersionChanger.Data
{
	public class iOSAppVersion : CocoaAppVersion
	{
		public iOSAppVersion()
		{
			base.ApplicationType = AppType.iOS;
		}
	}
}