using EMRController.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController.Config
{
	public class MixtureConfigNodePair
	{
		public MixtureConfigNode Max { get; internal set; }
		public MixtureConfigNode Min { get; internal set; }

		public bool Disabled { get; internal set; }

		public string ConfigName { get {
				return Max.configName;
			}
		}

		public MixtureConfigNodePair(MixtureConfigNode min, MixtureConfigNode max)
		{
			Min = min;
			Max = max;

			if (min == null || max == null) {
				EMRUtils.Log("One or both of the MixtureConfigNodes were null, disabling");
				Disabled = true;
			}
		}

		internal static MixtureConfigNodePair NotConfigured()
		{
			return new MixtureConfigNodePair(null, null);
		}
	}
}
