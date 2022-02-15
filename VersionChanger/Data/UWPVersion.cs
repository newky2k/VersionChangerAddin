using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DSoft.VersionChanger.Data
{
	public class UWPVersion : AppVersion
	{
		public override string FilePath { get; set; }

		public override string VersionOne { get; set; }

		public override string VersionTwo { get; set; }

		public override void Update()
		{
			try
			{
				var xmlDoc = new XmlDocument();
				xmlDoc.Load(FilePath);

				XmlNode node = xmlDoc.DocumentElement.ChildNodes.OfType<XmlNode>().FirstOrDefault(x => x.Name.Equals("Identity", StringComparison.OrdinalIgnoreCase));

				if (node != null)
				{
					var versionAttr = node.Attributes.OfType<XmlAttribute>().FirstOrDefault(x => x.Name.Equals("Version", StringComparison.OrdinalIgnoreCase));

					if (versionAttr != null)
					{
						versionAttr.Value = VersionOne;

						xmlDoc.Save(FilePath);
					}
				}
			}
			catch (Exception)
			{

				
			}


		}
	}
}
