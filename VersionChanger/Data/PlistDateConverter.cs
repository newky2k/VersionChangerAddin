using System;

namespace DSoft.VersionChanger.Data
{
	public static class PlistDateConverter
	{
		public static long timeDifference;

		static PlistDateConverter()
		{
			PlistDateConverter.timeDifference = (long)978307200;
		}

		public static DateTime ConvertFromAppleTimeStamp(double timestamp)
		{
			DateTime dateTime = new DateTime(2001, 1, 1, 0, 0, 0, 0);
			return dateTime.AddSeconds(timestamp);
		}

		public static double ConvertToAppleTimeStamp(DateTime date)
		{
			DateTime dateTime = new DateTime(2001, 1, 1, 0, 0, 0, 0);
			return Math.Floor((date - dateTime).TotalSeconds);
		}

		public static long GetAppleTime(long unixTime)
		{
			return unixTime - PlistDateConverter.timeDifference;
		}

		public static long GetUnixTime(long appleTime)
		{
			return appleTime + PlistDateConverter.timeDifference;
		}
	}
}