using KSPAPIExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EMRController
{
	public class EMRController : PartModule
	{
		[KSPField(isPersistant = true, guiName = "Starting EMR", guiActive = false, guiActiveEditor = false, guiUnits = ":1"),
			UI_FloatEdit(incrementSmall = 0.1f, incrementLarge = 1.0f, incrementSlide = 0.01f, sigFigs = 2, unit = ":1")]
		public float startingEMR;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "ISP")]
		public string startingEMRText;

		[KSPField(isPersistant = true, guiName = "Final EMR", guiActive = false, guiActiveEditor = false, guiUnits = ":1"),
			UI_FloatEdit(incrementSmall = 0.1f, incrementLarge = 1.0f, incrementSlide = 0.01f, sigFigs = 2, unit = ":1")]
		public float finalEMR;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "ISP")]
		public string finalEMRText;

		[KSPField]
		public bool emrEnabled = false;

		[KSPEvent(guiActive = true, guiActiveEditor = true)]
		public void ToggleEMR()
		{
			emrEnabled = !emrEnabled;
			SetActionsAndGui();
		}

		[SerializeField]
		public byte[] mixtureConfigNodesSerialized;

		private Dictionary<float, MixtureConfigNode> mixtureConfigNodes;

		ModuleEngines engineModule = null;
		public override void OnStart(StartState state)
		{
			EMRUtils.Log("OnStart called");
			BindCallbacks();

			DeserializeNodes();

			if (engineModule == null) {
				engineModule = part.FindModuleImplementing<ModuleEngines>();
			}
			if (engineModule == null) {
				EMRUtils.Log("ERROR! could not find ModuleEngines");
			}
			base.OnStart(state);
		}

		private void TESTINGCheckFuelLevels()
		{
			if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch.ship.parts.Count == 1 && EditorLogic.fetch.ship.parts[0] == part) {
				List<PartResourceDefinition> consumedResources = engineModule.GetConsumedResources();
				List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);

				if (HighLogic.LoadedSceneIsEditor) {
					PartSet.BuildPartSets(parts, null);
				}


				foreach (PartResourceDefinition resource in consumedResources) {
					double amount;
					double maxAmount;


					//EMRUtils.Log("Looking up Resource: ", resource.name);
					part.GetConnectedResourceTotals(resource.id, out amount, out maxAmount);
					//EMRUtils.Log("Remaining ", resource.name, ": ", amount);
				}
			}
		}

		private PropellantResources propellantResources;
		private void SetNeededFuel()
		{

			if (propellantResources == null) {
				propellantResources = new PropellantResources(engineModule);
			}
			EMRUtils.Log("Oxidizer: ", propellantResources.Oxidizer.Name);
			foreach (var fuel in propellantResources.Fuels) {
				EMRUtils.Log("Fuel: ", fuel.Name);
			}

		}


		private void BindCallbacks()
		{
			UI_FloatEdit startFloatEdit = (UI_FloatEdit)Fields["startingEMR"].uiControlEditor;
			UI_FloatEdit endFloatEdit = (UI_FloatEdit)Fields["finalEMR"].uiControlEditor;

			startFloatEdit.onFieldChanged += UIChanged;
			endFloatEdit.onFieldChanged += UIChanged;

			part.OnEditorAttach += PartAttached;
		}

		private void PartAttached()
		{
			//EMRUtils.Log("I'm attached!");
		}

		private void UIChanged(BaseField baseField, object obj)
		{
			UpdateIspAndThrustDisplay();
			SetNeededFuel();
		}

		private void UpdateIspAndThrustDisplay()
		{
			startingEMRText = BuildIspAndThrustString(GenerateMixtureConfigNodeForRatio(startingEMR));
			finalEMRText = BuildIspAndThrustString(GenerateMixtureConfigNodeForRatio(finalEMR));
		}

		private string BuildIspAndThrustString(MixtureConfigNode node)
		{
			return node.isp + "s   Thrust: " + MathUtils.ToStringSI(node.thrust, 2, 0, "N");
		}

		private MixtureConfigNode GenerateMixtureConfigNodeForRatio(float ratio)
		{
			MixtureConfigNode minNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Min()];
			MixtureConfigNode maxNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Max()];

			float fullRatioDiff = maxNode.ratio - minNode.ratio;
			float currentRatioDiff = ratio - minNode.ratio;
			float ratioPercentage = currentRatioDiff / fullRatioDiff;

			return new MixtureConfigNode() {
				ratio = ratio,
				isp = Mathf.RoundToInt(ratioPercentage * (maxNode.isp - minNode.isp)) + minNode.isp,
				thrust = (ratioPercentage * (maxNode.thrust - minNode.thrust)) + minNode.thrust
			};
		}

		public override void OnLoad(ConfigNode node)
		{
			EMRUtils.Log("OnLoad called");
			if (GameSceneFilter.AnyInitializing.IsLoaded()) {
				EMRUtils.Log("Loaded");
				LoadMixtureConfigNodes(node);
			}
			base.OnLoad(node);
		}

		private void LoadMixtureConfigNodes(ConfigNode node)
		{
			mixtureConfigNodes = new Dictionary<float, MixtureConfigNode>();
			foreach (ConfigNode tNode in node.GetNodes("MIXTURE")) {
				MixtureConfigNode configNode = new MixtureConfigNode();
				configNode.Load(tNode);
				mixtureConfigNodes.Add(configNode.ratio, configNode);
				EMRUtils.Log("Loaded ratio: " + configNode.ratio);
			}

			List<MixtureConfigNode> configList = mixtureConfigNodes.Values.ToList();
			mixtureConfigNodesSerialized = ObjectSerializer.Serialize(configList);
			EMRUtils.Log("Serialized ratios");
		}

		private void SetEditorFields()
		{
			EMRUtils.Log("Setting editor fields");
			MixtureConfigNode minNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Min()];
			EMRUtils.Log("Minimum EMR: ", minNode.ratio);
			MixtureConfigNode maxNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Max()];
			EMRUtils.Log("Maximum EMR: ", maxNode.ratio);

			UI_FloatEdit startFloatEdit = (UI_FloatEdit)Fields["startingEMR"].uiControlEditor;
			UI_FloatEdit finalFloatEdit = (UI_FloatEdit)Fields["finalEMR"].uiControlEditor;
			startFloatEdit.minValue = minNode.ratio;
			startFloatEdit.maxValue = maxNode.ratio;
			finalFloatEdit.minValue = minNode.ratio;
			finalFloatEdit.maxValue = maxNode.ratio;

			startingEMR = maxNode.ratio;
		}

		private void SetActionsAndGui()
		{
			Events["ToggleEMR"].guiName = (emrEnabled ? "Disable" : "Enable") + " EMR Controller";
			Fields["startingEMR"].guiActiveEditor = emrEnabled;
			Fields["finalEMR"].guiActiveEditor = emrEnabled;
		}

		private void DeserializeNodes()
		{
			if (mixtureConfigNodes == null && mixtureConfigNodesSerialized != null) {
				EMRUtils.Log("ConfigNode Deserialization Needed");
				mixtureConfigNodes = new Dictionary<float, MixtureConfigNode>();
				List<MixtureConfigNode> configList =
					ObjectSerializer.Deserialize<List<MixtureConfigNode>>(mixtureConfigNodesSerialized);
				foreach (var item in configList) {
					EMRUtils.Log("Deserialized ratio: ", item.ratio);
					mixtureConfigNodes.Add(item.ratio, item);
				}
				EMRUtils.Log("Deserialized ", mixtureConfigNodes.Count, " configs");

				SetEditorFields();
				SetActionsAndGui();
			}
		}

		public void FixedUpdate()
		{
			TESTINGCheckFuelLevels();
		}


	}

}
