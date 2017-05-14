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

		[KSPField(isPersistant = true, guiName = "Percentage at Final EMR", guiActive = false, guiActiveEditor = false, guiUnits = "%"),
			UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.Editor)]
		public float emrSplitPercentage;

		[KSPField(isPersistant = true, guiName = "Boiloff Reserve Percentage", guiActive = false, guiActiveEditor = true, guiUnits = "%"),
			UI_FloatRange(minValue = -50, maxValue = 50, stepIncrement = 1, scene = UI_Scene.Editor)]
		public float fuelReservePercentage; 

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Reserve")]
		public string fuelReserveText;

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
			// Don't think I really need to do any of this
			/*
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
			*/
		}

		private PropellantResources propellantResources;
		private void SetNeededFuel()
		{

			if (propellantResources == null) {
				propellantResources = new PropellantResources(engineModule);
			}
			//EMRUtils.Log("Oxidizer: ", propellantResources.Oxidizer.Name);
			foreach (var fuel in propellantResources.Fuels) {
				//EMRUtils.Log("Fuel: ", fuel.Name);
			}

			SetNewRatios(propellantResources, startingEMR, finalEMR, emrSplitPercentage);

		}

		private void SetNewRatios(PropellantResources propellantResources, float startingEMR, float finalEMR, float emrSplitPercentage)
		{
			Dictionary<int, float> startRatios = GetRatiosForEMR(propellantResources, startingEMR);
			Dictionary<int, float> endRatios = GetRatiosForEMR(propellantResources, finalEMR);

			foreach (var prop in engineModule.propellants) {
				var ratioDiff = endRatios[prop.id] - startRatios[prop.id];
				//EMRUtils.Log("Ratio Diff for ", prop.name, ": ", ratioDiff);
				prop.ratio = startRatios[prop.id] + ((emrSplitPercentage / 100) * ratioDiff);
				//EMRUtils.Log("New ratio: ", prop.ratio);
				if (propellantResources.Oxidizer.Id == prop.id && fuelReservePercentage > 0) { 
					//EMRUtils.Log("Adujusting oxidizer capacity to account for boiloff");
					prop.ratio = prop.ratio * ((100 - fuelReservePercentage) / 100);
				}
				if (propellantResources.Fuels[0].Id == prop.id && fuelReservePercentage < 0) { 
					//EMRUtils.Log("Adujusting fuel capacity to account for boiloff");
					prop.ratio = prop.ratio * ((100 + fuelReservePercentage) / 100);
				}
			}

		}

		Dictionary<int, float> GetRatiosForEMR(PropellantResources propellantResources, float EMR)
		{
			// right now, the ratio is a volume ratio, so we need to convert that to a mass ratio

			// finalEMR = oxidizer mass flow rate
			// 1 = fuel mass flow rate

			// let's sum up all the mass flows for our fuels
			var fuelMassFlow = propellantResources.Fuels.Sum(fuel => fuel.PropellantMassFlow);

			// oxidizer mass flow will be that times the EMR
			var oxidizerMassFlow = fuelMassFlow * EMR;

			// dividing that by density should give us the ratios tha we want
			var oxidierRatio = oxidizerMassFlow / propellantResources.Oxidizer.Density;

			//TODO: handle more than one fuel
			var fuelRatio = fuelMassFlow / propellantResources.Fuels[0].Density;

			Dictionary<int, float> ratios = new Dictionary<int, float>();
			ratios.Add(propellantResources.Oxidizer.Id, oxidierRatio);
			ratios.Add(propellantResources.Fuels[0].Id, fuelRatio);
			return ratios;
		}

		private void BindCallbacks()
		{
			string[] editorNames = new string[] { "startingEMR", "finalEMR", "emrSplitPercentage", "fuelReservePercentage" };
			foreach (var editorName in editorNames) {
				Fields[editorName].uiControlEditor.onFieldChanged += UIChanged;
			}
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
			UpdateAllParts();
		}

		private void UpdateAllParts()
		{
			List<Part> parts;
			if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch.ship != null)
				parts = EditorLogic.fetch.ship.parts;
			else if (HighLogic.LoadedSceneIsFlight && vessel != null)
				parts = vessel.parts;
			else parts = new List<Part>();
			for (int i = parts.Count - 1; i >= 0; --i)
				parts[i].SendMessage("UpdateUsedBy", SendMessageOptions.DontRequireReceiver);
		}

		private void UpdateIspAndThrustDisplay()
		{
			startingEMRText = BuildIspAndThrustString(GenerateMixtureConfigNodeForRatio(startingEMR));
			finalEMRText = BuildIspAndThrustString(GenerateMixtureConfigNodeForRatio(finalEMR));
			fuelReserveText = BuildFuelReserveText(fuelReservePercentage);
		}

		private string BuildIspAndThrustString(MixtureConfigNode node)
		{
			return node.isp + "s   Thrust: " + MathUtils.ToStringSI(node.thrust, 2, 0, "N");
		}

		private string BuildFuelReserveText(float fuelReservePercentage)
		{
			if (fuelReservePercentage == 0) {
				return "balanced";
			}
			string richProp = fuelReservePercentage > 0 ? "fuel" : "oxidizer";
			return Math.Abs(fuelReservePercentage).ToString() + "% " + richProp;
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
			Fields["startingEMRText"].guiActiveEditor = emrEnabled;
			Fields["finalEMRText"].guiActiveEditor = emrEnabled;
			Fields["emrSplitPercentage"].guiActiveEditor = emrEnabled;
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
