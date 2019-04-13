using DSoft.VersionChanger.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace DSoft.VersionChanger.Data
{
	public static class Plist
	{
		private static List<int> offsetTable;

		private static List<byte> objectTable;

		private static int refCount;

		private static int objRefSize;

		private static int offsetByteSize;

		private static long offsetTableOffset;

		static Plist()
		{
			Plist.offsetTable = new List<int>();
			Plist.objectTable = new List<byte>();
		}

		private static void compose(object value, XmlWriter writer)
		{
			if (value == null || value is string)
			{
				writer.WriteElementString("string", value as string);
				return;
			}
			if (value is int || value is long)
			{
				int num = (int)value;
				writer.WriteElementString("integer", num.ToString(NumberFormatInfo.InvariantInfo));
				return;
			}
			if (value is Dictionary<string, object> || value.GetType().ToString().StartsWith("System.Collections.Generic.Dictionary`2[System.String"))
			{
				Dictionary<string, object> strs = value as Dictionary<string, object>;
				if (strs == null)
				{
					strs = new Dictionary<string, object>();
					IDictionary dictionaries = (IDictionary)value;
					foreach (object key in dictionaries.Keys)
					{
						strs.Add(key.ToString(), dictionaries[key]);
					}
				}
				Plist.writeDictionaryValues(strs, writer);
				return;
			}
			if (value is List<object>)
			{
				Plist.composeArray((List<object>)value, writer);
				return;
			}
			if (value is byte[])
			{
				writer.WriteElementString("data", Convert.ToBase64String((byte[])value));
				return;
			}
			if (value is float || value is double)
			{
				double num1 = (double)value;
				writer.WriteElementString("real", num1.ToString(NumberFormatInfo.InvariantInfo));
				return;
			}
			if (value is DateTime)
			{
				writer.WriteElementString("date", XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Utc));
				return;
			}
			if (!(value is bool))
			{
				throw new Exception(string.Format("Value type '{0}' is unhandled", value.GetType().ToString()));
			}
			writer.WriteElementString(value.ToString().ToLower(), "");
		}

		private static void composeArray(List<object> value, XmlWriter writer)
		{
			writer.WriteStartElement("array");
			foreach (object obj in value)
			{
				Plist.compose(obj, writer);
			}
			writer.WriteEndElement();
		}

		private static byte[] composeBinary(object obj)
		{
			string str = obj.GetType().ToString();
			if (str == "System.Collections.Generic.Dictionary`2[System.String,System.Object]")
			{
				return Plist.writeBinaryDictionary((Dictionary<string, object>)obj);
			}
			if (str == "System.Collections.Generic.List`1[System.Object]")
			{
				return Plist.composeBinaryArray((List<object>)obj);
			}
			if (str == "System.Byte[]")
			{
				return Plist.writeBinaryByteArray((byte[])obj);
			}
			if (str == "System.Double")
			{
				return Plist.writeBinaryDouble((double)obj);
			}
			if (str == "System.Int32")
			{
				return Plist.writeBinaryInteger((int)obj, true);
			}
			if (str == "System.String")
			{
				return Plist.writeBinaryString((string)obj, true);
			}
			if (str == "System.DateTime")
			{
				return Plist.writeBinaryDate((DateTime)obj);
			}
			if (str != "System.Boolean")
			{
				return new byte[0];
			}
			return Plist.writeBinaryBool((bool)obj);
		}

		private static byte[] composeBinaryArray(List<object> objects)
		{
			List<byte> nums = new List<byte>();
			List<byte> nums1 = new List<byte>();
			List<int> nums2 = new List<int>();
			for (int i = objects.Count - 1; i >= 0; i--)
			{
				Plist.composeBinary(objects[i]);
				Plist.offsetTable.Add(Plist.objectTable.Count);
				nums2.Add(Plist.refCount);
				Plist.refCount--;
			}
			if (objects.Count >= 15)
			{
				nums1.Add(175);
				nums1.AddRange(Plist.writeBinaryInteger(objects.Count, false));
			}
			else
			{
				nums1.Add(Convert.ToByte(160 | Convert.ToByte(objects.Count)));
			}
			foreach (int num in nums2)
			{
				byte[] numArray = Plist.RegulateNullBytes(BitConverter.GetBytes(num), Plist.objRefSize);
				Array.Reverse(numArray);
				nums.InsertRange(0, numArray);
			}
			nums.InsertRange(0, nums1);
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static int countObject(object value)
		{
			int count = 0;
			string str = value.GetType().ToString();
			if (str == "System.Collections.Generic.Dictionary`2[System.String,System.Object]")
			{
				Dictionary<string, object> strs = (Dictionary<string, object>)value;
				foreach (string key in strs.Keys)
				{
					count += Plist.countObject(strs[key]);
				}
				count += strs.Keys.Count;
				count++;
			}
			else if (str == "System.Collections.Generic.List`1[System.Object]")
			{
				foreach (object obj in (List<object>)value)
				{
					count += Plist.countObject(obj);
				}
				count++;
			}
			else
			{
				count++;
			}
			return count;
		}

		private static int getCount(int bytePosition, out int newBytePosition)
		{
			int num;
			byte num1 = Convert.ToByte(Plist.objectTable[bytePosition] & 15);
			if (num1 >= 15)
			{
				num = (int)Plist.parseBinaryInt(bytePosition + 1, out newBytePosition);
			}
			else
			{
				num = num1;
				newBytePosition = bytePosition + 1;
			}
			return num;
		}

		public static plistType getPlistType(Stream stream)
		{
			byte[] numArray = new byte[8];
			stream.Read(numArray, 0, 8);
			if (BitConverter.ToInt64(numArray, 0) == 3472403351741427810L)
			{
				return plistType.Binary;
			}
			return plistType.Xml;
		}

		private static object parse(XmlNode node)
		{
			string name = node.Name;
			if (name == "dict")
			{
				return Plist.parseDictionary(node);
			}
			if (name == "array")
			{
				return Plist.parseArray(node);
			}
			if (name == "string")
			{
				return node.InnerText;
			}
			if (name == "integer")
			{
				return Convert.ToInt32(node.InnerText, NumberFormatInfo.InvariantInfo);
			}
			if (name == "real")
			{
				return Convert.ToDouble(node.InnerText, NumberFormatInfo.InvariantInfo);
			}
			if (name == "false")
			{
				return false;
			}
			if (name == "true")
			{
				return true;
			}
			if (name == "null")
			{
				return null;
			}
			if (name == "date")
			{
				return XmlConvert.ToDateTime(node.InnerText, XmlDateTimeSerializationMode.Utc);
			}
			if (name != "data")
			{
				throw new ApplicationException(string.Format("Plist Node `{0}' is not supported", node.Name));
			}
			return Convert.FromBase64String(node.InnerText);
		}

		private static List<object> parseArray(XmlNode node)
		{
			List<object> objs = new List<object>();
			foreach (object childNode in node.ChildNodes)
			{
				object obj = Plist.parse((XmlNode)childNode);
				if (obj == null)
				{
					continue;
				}
				objs.Add(obj);
			}
			return objs;
		}

		private static object parseBinary(int objRef)
		{
			int item = Plist.objectTable[Plist.offsetTable[objRef]] & 240;
			if (item <= 48)
			{
				if (item > 16)
				{
					if (item == 32)
					{
						return Plist.parseBinaryReal(Plist.offsetTable[objRef]);
					}
					if (item == 48)
					{
						return Plist.parseBinaryDate(Plist.offsetTable[objRef]);
					}
				}
				else
				{
					if (item == 0)
					{
						if (Plist.objectTable[Plist.offsetTable[objRef]] == 0)
						{
							return null;
						}
						return (Plist.objectTable[Plist.offsetTable[objRef]] == 9 ? true : false);
					}
					if (item == 16)
					{
						return Plist.parseBinaryInt(Plist.offsetTable[objRef]);
					}
				}
			}
			else if (item > 80)
			{
				if (item == 96)
				{
					return Plist.parseBinaryUnicodeString(Plist.offsetTable[objRef]);
				}
				if (item == 160)
				{
					return Plist.parseBinaryArray(objRef);
				}
				if (item == 208)
				{
					return Plist.parseBinaryDictionary(objRef);
				}
			}
			else
			{
				if (item == 64)
				{
					return Plist.parseBinaryByteArray(Plist.offsetTable[objRef]);
				}
				if (item == 80)
				{
					return Plist.parseBinaryAsciiString(Plist.offsetTable[objRef]);
				}
			}
			throw new Exception("This type is not supported");
		}

		private static object parseBinaryArray(int objRef)
		{
			int num;
			List<object> objs = new List<object>();
			List<int> nums = new List<int>();
			int count = 0;
			count = Plist.getCount(Plist.offsetTable[objRef], out num);
			num = (count >= 15 ? Plist.offsetTable[objRef] + 2 + (int)Plist.RegulateNullBytes(BitConverter.GetBytes(count), 1).Length : Plist.offsetTable[objRef] + 1);
			for (int i = num; i < num + count * Plist.objRefSize; i += Plist.objRefSize)
			{
				byte[] array = Plist.objectTable.GetRange(i, Plist.objRefSize).ToArray();
				Array.Reverse(array);
				nums.Add(BitConverter.ToInt32(Plist.RegulateNullBytes(array, 4), 0));
			}
			for (int j = 0; j < count; j++)
			{
				objs.Add(Plist.parseBinary(nums[j]));
			}
			return objs;
		}

		private static object parseBinaryAsciiString(int headerPosition)
		{
			int num;
			int count = Plist.getCount(headerPosition, out num);
			List<byte> range = Plist.objectTable.GetRange(num, count);
			if (range.Count <= 0)
			{
				return string.Empty;
			}
			return Encoding.ASCII.GetString(range.ToArray());
		}

		private static object parseBinaryByteArray(int headerPosition)
		{
			int num;
			int count = Plist.getCount(headerPosition, out num);
			return Plist.objectTable.GetRange(num, count).ToArray();
		}

		public static object parseBinaryDate(int headerPosition)
		{
			byte[] array = Plist.objectTable.GetRange(headerPosition + 1, 8).ToArray();
			Array.Reverse((Array)array);
			return PlistDateConverter.ConvertFromAppleTimeStamp(BitConverter.ToDouble(array, 0));
		}

		private static object parseBinaryDictionary(int objRef)
		{
			int num;
			Dictionary<string, object> strs = new Dictionary<string, object>();
			List<int> nums = new List<int>();
			int count = 0;
			count = Plist.getCount(Plist.offsetTable[objRef], out num);
			num = (count >= 15 ? Plist.offsetTable[objRef] + 2 + (int)Plist.RegulateNullBytes(BitConverter.GetBytes(count), 1).Length : Plist.offsetTable[objRef] + 1);
			for (int i = num; i < num + count * 2 * Plist.objRefSize; i += Plist.objRefSize)
			{
				byte[] array = Plist.objectTable.GetRange(i, Plist.objRefSize).ToArray();
				Array.Reverse(array);
				nums.Add(BitConverter.ToInt32(Plist.RegulateNullBytes(array, 4), 0));
			}
			for (int j = 0; j < count; j++)
			{
				strs.Add((string)Plist.parseBinary(nums[j]), Plist.parseBinary(nums[j + count]));
			}
			return strs;
		}

		private static object parseBinaryInt(int headerPosition)
		{
			int num;
			return Plist.parseBinaryInt(headerPosition, out num);
		}

		private static object parseBinaryInt(int headerPosition, out int newHeaderPosition)
		{
			byte item = Plist.objectTable[headerPosition];
			int num = (int)Math.Pow(2, (double)(item & 15));
			byte[] array = Plist.objectTable.GetRange(headerPosition + 1, num).ToArray();
			Array.Reverse((Array)array);
			newHeaderPosition = headerPosition + num + 1;
			return BitConverter.ToInt32(Plist.RegulateNullBytes(array, 4), 0);
		}

		private static object parseBinaryReal(int headerPosition)
		{
			byte item = Plist.objectTable[headerPosition];
			int num = (int)Math.Pow(2, (double)(item & 15));
			byte[] array = Plist.objectTable.GetRange(headerPosition + 1, num).ToArray();
			Array.Reverse((Array)array);
			return BitConverter.ToDouble(Plist.RegulateNullBytes(array, 8), 0);
		}

		private static object parseBinaryUnicodeString(int headerPosition)
		{
			int num;
			int count = Plist.getCount(headerPosition, out num);
			count *= 2;
			byte[] numArray = new byte[count];
			for (int i = 0; i < count; i += 2)
			{
				byte item = Plist.objectTable.GetRange(num + i, 1)[0];
				byte item1 = Plist.objectTable.GetRange(num + i + 1, 1)[0];
				if (!BitConverter.IsLittleEndian)
				{
					numArray[i] = item;
					numArray[i + 1] = item1;
				}
				else
				{
					numArray[i] = item1;
					numArray[i + 1] = item;
				}
			}
			return Encoding.Unicode.GetString(numArray);
		}

		private static Dictionary<string, object> parseDictionary(XmlNode node)
		{
			XmlNodeList childNodes = node.ChildNodes;
			if (childNodes.Count % 2 != 0)
			{
				throw new DataMisalignedException("Dictionary elements must have an even number of child nodes");
			}
			Dictionary<string, object> strs = new Dictionary<string, object>();
			for (int i = 0; i < childNodes.Count; i += 2)
			{
				XmlNode itemOf = childNodes[i];
				XmlNode xmlNodes = childNodes[i + 1];
				if (itemOf.Name != "key")
				{
					throw new ApplicationException("expected a key node");
				}
				object obj = Plist.parse(xmlNodes);
				if (obj != null)
				{
					strs.Add(itemOf.InnerText, obj);
				}
			}
			return strs;
		}

		private static void parseOffsetTable(List<byte> offsetTableBytes)
		{
			for (int i = 0; i < offsetTableBytes.Count; i += Plist.offsetByteSize)
			{
				byte[] array = offsetTableBytes.GetRange(i, Plist.offsetByteSize).ToArray();
				Array.Reverse(array);
				Plist.offsetTable.Add(BitConverter.ToInt32(Plist.RegulateNullBytes(array, 4), 0));
			}
		}

		private static void parseTrailer(List<byte> trailer)
		{
			Plist.offsetByteSize = BitConverter.ToInt32(Plist.RegulateNullBytes(trailer.GetRange(6, 1).ToArray(), 4), 0);
			Plist.objRefSize = BitConverter.ToInt32(Plist.RegulateNullBytes(trailer.GetRange(7, 1).ToArray(), 4), 0);
			byte[] array = trailer.GetRange(12, 4).ToArray();
			Array.Reverse((Array)array);
			Plist.refCount = BitConverter.ToInt32(array, 0);
			byte[] numArray = trailer.GetRange(24, 8).ToArray();
			Array.Reverse((Array)numArray);
			Plist.offsetTableOffset = BitConverter.ToInt64(numArray, 0);
		}

		private static object readBinary(byte[] data)
		{
			Plist.offsetTable.Clear();
			List<byte> nums = new List<byte>();
			Plist.objectTable.Clear();
			Plist.refCount = 0;
			Plist.objRefSize = 0;
			Plist.offsetByteSize = 0;
			Plist.offsetTableOffset = (long)0;
			List<byte> nums1 = new List<byte>(data);
			Plist.parseTrailer(nums1.GetRange(nums1.Count - 32, 32));
			Plist.objectTable = nums1.GetRange(0, (int)Plist.offsetTableOffset);
			Plist.parseOffsetTable(nums1.GetRange((int)Plist.offsetTableOffset, nums1.Count - (int)Plist.offsetTableOffset - 32));
			return Plist.parseBinary(0);
		}

		public static object readPlist(string path)
		{
			object obj;
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				obj = Plist.readPlist(fileStream, plistType.Auto);
			}
			return obj;
		}

		public static object readPlist(byte[] data)
		{
			return Plist.readPlist(new MemoryStream(data), plistType.Auto);
		}

		public static object readPlist(Stream stream, plistType type)
		{
			object obj;
			if (type == plistType.Auto)
			{
				type = Plist.getPlistType(stream);
				stream.Seek((long)0, SeekOrigin.Begin);
			}
			if (type != plistType.Binary)
			{
				XmlDocument xmlDocument = new XmlDocument()
				{
					XmlResolver = null
				};
				xmlDocument.Load(stream);
				return Plist.readXml(xmlDocument);
			}
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				obj = Plist.readBinary(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));
			}
			return obj;
		}

		public static object readPlistSource(string source)
		{
			return Plist.readPlist(Encoding.UTF8.GetBytes(source));
		}

		private static object readXml(XmlDocument xml)
		{
			return Plist.parse(xml.DocumentElement.ChildNodes[0]);
		}

		private static byte[] RegulateNullBytes(byte[] value)
		{
			return Plist.RegulateNullBytes(value, 1);
		}

		private static byte[] RegulateNullBytes(byte[] value, int minBytes)
		{
			Array.Reverse(value);
			List<byte> nums = new List<byte>(value);
			for (int i = 0; i < nums.Count && nums[i] == 0 && nums.Count > minBytes; i++)
			{
				nums.Remove(nums[i]);
				i--;
			}
			if (nums.Count < minBytes)
			{
				int num = minBytes - nums.Count;
				for (int j = 0; j < num; j++)
				{
					nums.Insert(0, 0);
				}
			}
			value = nums.ToArray();
			Array.Reverse(value);
			return value;
		}

		public static void writeBinary(object value, string path)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(path, FileMode.Create)))
			{
				binaryWriter.Write(Plist.writeBinary(value));
			}
		}

		public static void writeBinary(object value, Stream stream)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(stream))
			{
				binaryWriter.Write(Plist.writeBinary(value));
			}
		}

		public static byte[] writeBinary(object value)
		{
			Plist.offsetTable.Clear();
			Plist.objectTable.Clear();
			Plist.refCount = 0;
			Plist.objRefSize = 0;
			Plist.offsetByteSize = 0;
			Plist.offsetTableOffset = (long)0;
			int num = Plist.countObject(value) - 1;
			Plist.refCount = num;
			Plist.objRefSize = (int)Plist.RegulateNullBytes(BitConverter.GetBytes(Plist.refCount)).Length;
			Plist.composeBinary(value);
			Plist.writeBinaryString("bplist00", false);
			Plist.offsetTableOffset = (long)Plist.objectTable.Count;
			Plist.offsetTable.Add(Plist.objectTable.Count - 8);
			Plist.offsetByteSize = (int)Plist.RegulateNullBytes(BitConverter.GetBytes(Plist.offsetTable[Plist.offsetTable.Count - 1])).Length;
			List<byte> nums = new List<byte>();
			Plist.offsetTable.Reverse();
			for (int i = 0; i < Plist.offsetTable.Count; i++)
			{
				Plist.offsetTable[i] = Plist.objectTable.Count - Plist.offsetTable[i];
				byte[] numArray = Plist.RegulateNullBytes(BitConverter.GetBytes(Plist.offsetTable[i]), Plist.offsetByteSize);
				Array.Reverse(numArray);
				nums.AddRange(numArray);
			}
			Plist.objectTable.AddRange(nums);
			Plist.objectTable.AddRange(new byte[6]);
			Plist.objectTable.Add(Convert.ToByte(Plist.offsetByteSize));
			Plist.objectTable.Add(Convert.ToByte(Plist.objRefSize));
			byte[] bytes = BitConverter.GetBytes((long)num + (long)1);
			Array.Reverse(bytes);
			Plist.objectTable.AddRange(bytes);
			Plist.objectTable.AddRange(BitConverter.GetBytes((long)0));
			bytes = BitConverter.GetBytes(Plist.offsetTableOffset);
			Array.Reverse(bytes);
			Plist.objectTable.AddRange(bytes);
			return Plist.objectTable.ToArray();
		}

		public static byte[] writeBinaryBool(bool obj)
		{
			List<byte> nums = new List<byte>(new byte[] { obj ? (byte)9 : (byte)8 });
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static byte[] writeBinaryByteArray(byte[] value)
		{
			List<byte> nums = new List<byte>(value);
			List<byte> nums1 = new List<byte>();
			if ((int)value.Length >= 15)
			{
				nums1.Add(79);
				nums1.AddRange(Plist.writeBinaryInteger(nums.Count, false));
			}
			else
			{
				nums1.Add(Convert.ToByte(64 | Convert.ToByte((int)value.Length)));
			}
			nums.InsertRange(0, nums1);
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		public static byte[] writeBinaryDate(DateTime obj)
		{
			List<byte> nums = new List<byte>(Plist.RegulateNullBytes(BitConverter.GetBytes(PlistDateConverter.ConvertToAppleTimeStamp(obj)), 8));
			nums.Reverse();
			nums.Insert(0, 51);
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static byte[] writeBinaryDictionary(Dictionary<string, object> dictionary)
		{
			List<byte> nums = new List<byte>();
			List<byte> nums1 = new List<byte>();
			List<int> nums2 = new List<int>();
			for (int i = dictionary.Count - 1; i >= 0; i--)
			{
				object[] objArray = new object[dictionary.Count];
				dictionary.Values.CopyTo(objArray, 0);
				Plist.composeBinary(objArray[i]);
				Plist.offsetTable.Add(Plist.objectTable.Count);
				nums2.Add(Plist.refCount);
				Plist.refCount--;
			}
			for (int j = dictionary.Count - 1; j >= 0; j--)
			{
				string[] strArrays = new string[dictionary.Count];
				dictionary.Keys.CopyTo(strArrays, 0);
				Plist.composeBinary(strArrays[j]);
				Plist.offsetTable.Add(Plist.objectTable.Count);
				nums2.Add(Plist.refCount);
				Plist.refCount--;
			}
			if (dictionary.Count >= 15)
			{
				nums1.Add(223);
				nums1.AddRange(Plist.writeBinaryInteger(dictionary.Count, false));
			}
			else
			{
				nums1.Add(Convert.ToByte(208 | Convert.ToByte(dictionary.Count)));
			}
			foreach (int num in nums2)
			{
				byte[] numArray = Plist.RegulateNullBytes(BitConverter.GetBytes(num), Plist.objRefSize);
				Array.Reverse(numArray);
				nums.InsertRange(0, numArray);
			}
			nums.InsertRange(0, nums1);
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static byte[] writeBinaryDouble(double value)
		{
			List<byte> nums = new List<byte>(Plist.RegulateNullBytes(BitConverter.GetBytes(value), 4));
			while ((double)nums.Count != Math.Pow(2, Math.Log((double)nums.Count) / Math.Log(2)))
			{
				nums.Add(0);
			}
			int num = 32 | (int)(Math.Log((double)nums.Count) / Math.Log(2));
			nums.Reverse();
			nums.Insert(0, Convert.ToByte(num));
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static byte[] writeBinaryInteger(int value, bool write)
		{
			List<byte> nums = new List<byte>(BitConverter.GetBytes((long)value));
			nums = new List<byte>(Plist.RegulateNullBytes(nums.ToArray()));
			while ((double)nums.Count != Math.Pow(2, Math.Log((double)nums.Count) / Math.Log(2)))
			{
				nums.Add(0);
			}
			int num = 16 | (int)(Math.Log((double)nums.Count) / Math.Log(2));
			nums.Reverse();
			nums.Insert(0, Convert.ToByte(num));
			if (write)
			{
				Plist.objectTable.InsertRange(0, nums);
			}
			return nums.ToArray();
		}

		private static byte[] writeBinaryString(string value, bool head)
		{
			List<byte> nums = new List<byte>();
			List<byte> nums1 = new List<byte>();
			char[] charArray = value.ToCharArray();
			for (int i = 0; i < (int)charArray.Length; i++)
			{
				nums.Add(Convert.ToByte(charArray[i]));
			}
			if (head)
			{
				if (value.Length >= 15)
				{
					nums1.Add(95);
					nums1.AddRange(Plist.writeBinaryInteger(nums.Count, false));
				}
				else
				{
					nums1.Add(Convert.ToByte(80 | Convert.ToByte(value.Length)));
				}
			}
			nums.InsertRange(0, nums1);
			Plist.objectTable.InsertRange(0, nums);
			return nums.ToArray();
		}

		private static void writeDictionaryValues(Dictionary<string, object> dictionary, XmlWriter writer)
		{
			writer.WriteStartElement("dict");
			foreach (string key in dictionary.Keys)
			{
				object item = dictionary[key];
				writer.WriteElementString("key", key);
				Plist.compose(item, writer);
			}
			writer.WriteEndElement();
		}

		public static void writeXml(object value, string path)
		{
			using (StreamWriter streamWriter = new StreamWriter(path))
			{
				streamWriter.Write(Plist.writeXml(value));
			}
		}

		public static void writeXml(object value, Stream stream)
		{
			using (StreamWriter streamWriter = new StreamWriter(stream))
			{
				streamWriter.Write(Plist.writeXml(value));
			}
		}

		public static string writeXml(object value)
		{
			string str;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				XmlWriterSettings xmlWriterSetting = new XmlWriterSettings()
				{
					Encoding = new UTF8Encoding(false),
					ConformanceLevel = ConformanceLevel.Document,
					Indent = true
				};
				using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSetting))
				{
					xmlWriter.WriteStartDocument();
					xmlWriter.WriteDocType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
					xmlWriter.WriteStartElement("plist");
					xmlWriter.WriteAttributeString("version", "1.0");
					Plist.compose(value, xmlWriter);
					xmlWriter.WriteEndElement();
					xmlWriter.WriteEndDocument();
					xmlWriter.Flush();
					xmlWriter.Close();
					str = Encoding.UTF8.GetString(memoryStream.ToArray());
				}
			}
			return str;
		}
	}
}