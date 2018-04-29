using EMRController.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController.Config
{
	[Serializable]
	public class MixtureConfigNode : IConfigNode
	{
		private const char DELIM = '|';

		[Persistent]
		public FloatCurve atmosphereCurve;

		[Persistent]
		public string configName;


		[Persistent]
		public float ratio; // This is the Oxidizer:Fuel ratio (where the Fuel part is always 1) by mass
							// A lower number here means less oxygen, resulting in a less powerful burn,
							// (and less thrust), but since there is more unburnt fuel in the exhaust gas,
							// the mass flow rate increases, increasing ISP

		[Persistent]
		public float maxThrust;

		[Persistent]
		public float minThrust;

		public MixtureConfigNode()
		{

		}

		public MixtureConfigNode(string serialized)
		{
			EMRUtils.Log("Creating MixtureConfigNode from: ", serialized);
			var parts = serialized.Split(DELIM);
			configName = parts[0];
			ratio = float.Parse(parts[1]);
			minThrust = float.Parse(parts[2]);
			maxThrust = float.Parse(parts[3]);
			atmosphereCurve = new FloatCurve();
			for (int i = 4; i < parts.Length; i += 4) {
				atmosphereCurve.Add(
					float.Parse(parts[i]),
					float.Parse(parts[i + 1]),
					float.Parse(parts[i + 2]),
					float.Parse(parts[i + 3]));
			}
		}

		public void Load(ConfigNode node)
		{
			ConfigNode.LoadObjectFromConfig(this, node);
			if (node.HasValue("configName")) {
				configName = node.GetValue("configName");
			}
			else {
				configName = "";
			}
			ratio = float.Parse(node.GetValue("ratio"));
			if (node.HasValue("minThrust")) {
				minThrust = float.Parse(node.GetValue("minThrust"));
			}
			else {
				minThrust = 0;
			}
			maxThrust = float.Parse(node.GetValue("maxThrust"));
			atmosphereCurve = new FloatCurve();
			ConfigNode atmosCurveNode = node.GetNode("atmosphereCurve");
			if (atmosCurveNode != null) {
				atmosphereCurve.Load(atmosCurveNode);
			}
		}

		public void Save(ConfigNode node)
		{
			ConfigNode.CreateConfigFromObject(this, node);
		}

		public override string ToString()
		{
			StringBuilder sBuilder = StringBuilderCache.Acquire();
			sBuilder.Append(configName).Append(DELIM)
			.Append(ratio).Append(DELIM)
			.Append(minThrust).Append(DELIM)
			.Append(maxThrust).Append(DELIM);
			if (atmosphereCurve != null) {
				foreach (var key in atmosphereCurve.Curve.keys) {
					sBuilder.Append(key.time).Append(DELIM);
					sBuilder.Append(key.value).Append(DELIM);
					sBuilder.Append(key.inTangent).Append(DELIM);
					sBuilder.Append(key.outTangent).Append(DELIM);
				}
			}
			string resultString = sBuilder.ToStringAndRelease().TrimEnd(DELIM);
			EMRUtils.Log("Serialized: ", resultString);
			return resultString;
		}

	}
}
