using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Serialize(this Dictionary<string, bool> dictionary, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(dictionary.Count);
            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
            writer.Flush();
        }

        public static Dictionary<string, bool> Deserialize(this Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            var dictionary = new Dictionary<string, bool>(count);
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                var value = reader.ReadBoolean();
                dictionary.Add(key, value);
            }
            return dictionary;
        }
    }
}
