using DSoft.VersionChanger.Enums;
using System;
using System.Runtime.CompilerServices;

namespace DSoft.VersionChanger.Data
{
	public abstract class AppVersion
	{
		protected string mVersionOne;

		protected string mVersionTwo;

		public AppType ApplicationType
		{
			get;
			set;
		}

		public abstract string FilePath
		{
			get;
			set;
		}

		public abstract string VersionOne
		{
			get;
			set;
		}

		public abstract string VersionTwo
		{
			get;
			set;
		}

		protected AppVersion()
		{
		}

		public abstract void Update();
	}
}