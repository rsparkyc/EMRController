using EMRController.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController.Config
{
	public class MixtureConfigNodeProcessor
	{

		private Dictionary<string, MixtureConfigNodePair> allNodes = new Dictionary<string, MixtureConfigNodePair>();
		public byte[] Serialized { get; internal set; }

		public MixtureConfigNodeProcessor(ConfigNode node)
		{
			List<MixtureConfigNode> nodes = new List<MixtureConfigNode>();

			foreach (ConfigNode tNode in node.GetNodes("MIXTURE")) {
				MixtureConfigNode configNode = new MixtureConfigNode();
				configNode.Load(tNode);
				nodes.Add(configNode);
			}

			BuildFromNodes(nodes);
		}

		public MixtureConfigNodeProcessor (byte[] mixtureConfigNodesSerialized)
		{
			List<string> deserialized = ObjectSerializer.Deserialize<List<string>>(mixtureConfigNodesSerialized);
			IEnumerable<MixtureConfigNode> nodes = deserialized.Select(serializedItem => new MixtureConfigNode(serializedItem));
			BuildFromNodes(nodes);
		}

		private void BuildFromNodes(IEnumerable<MixtureConfigNode> nodes) { 
			IEnumerable<string> names = nodes.Select(item => item.configName).Distinct();
			foreach (string name in names) {
				List<MixtureConfigNode> nodesForName = nodes.Where(item => item.configName == name).ToList();
				if (nodesForName.Count != 2) {
					EMRUtils.Log("ERROR: expected two nodes per config name");
					throw new ArgumentException("It is expected to have two nodes per config name", "node");
				}
				MixtureConfigNodePair pair; 
				if (nodesForName[0].ratio < nodesForName[1].ratio) {
					pair = new MixtureConfigNodePair(nodesForName[0], nodesForName[1]);
				}
				else {
					pair = new MixtureConfigNodePair(nodesForName[1], nodesForName[0]);
				}
				allNodes.Add(name, pair);
			}

			List<String> configList = nodes.Select(item => item.ToString()).ToList();
			Serialized = ObjectSerializer.Serialize(configList);
		}

		public MixtureConfigNodePair GetForConfigName(string currentConfigName)
		{
			if (allNodes.ContainsKey(currentConfigName)) {
				return allNodes[currentConfigName];
			}
			return null;
		}

	}
}
