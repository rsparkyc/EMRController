﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController
{
	[Serializable]
	class MixtureConfigNode:IConfigNode
	{

		[Persistent]
		public int isp;

		[Persistent]
		public float ratio; // This is the Oxidizer:Fuel ratio (where the Fuel part is always 1) by mass
							// A lower number here means less oxygen, resulting in a less powerful burn,
							// (and less thrust), but since there is more unburnt fuel in the exhaust gas,
							// the mass flow rate increases, increasing ISP

		[Persistent]
		public float thrust;

		public void Load(ConfigNode node)
		{
			ConfigNode.LoadObjectFromConfig(this, node);
			isp = int.Parse(node.GetValue("isp"));
			ratio = float.Parse(node.GetValue("ratio"));
			thrust = float.Parse(node.GetValue("thrust"));
		}

		public void Save(ConfigNode node)
		{
			ConfigNode.CreateConfigFromObject(this, node);
		}
	}
}
