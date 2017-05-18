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

		#region In Flight 
		[KSPField]
		public bool emrInClosedLoop = false;

		[KSPEvent(guiActive = true, guiActiveEditor = false)]
		public void ChangeEMRMode()
		{
			emrInClosedLoop = !emrInClosedLoop;
			UpdateInFlightEMRParams();
		}

		[KSPField(isPersistant = true, guiName = "Current EMR", guiActiveEditor = false, guiUnits = ":1"),
			UI_FloatEdit(incrementSmall = 0.1f, incrementLarge = 1.0f, incrementSlide = 0.01f, sigFigs = 2, unit = ":1", scene = UI_Scene.Flight)]
		public float currentEMR;

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "EMR Ratio")]
		public string closedLoopEMRText;

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "ISP")]
		public string currentEMRText;

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Reserve")]
		public string currentReserveText;

		private void UpdateInFlightEMRParams()
		{
			EMRUtils.Log("Updating In Flight EMR Params");
			Fields["currentEMR"].guiActive = !emrInClosedLoop;
			Fields["closedLoopEMRText"].guiActive = emrInClosedLoop;

			UI_FloatEdit currentEMREditor = (UI_FloatEdit)Fields["currentEMR"].uiControlFlight;
			MixtureConfigNode minNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Min()];
			MixtureConfigNode maxNode = mixtureConfigNodes[mixtureConfigNodes.Keys.Max()];
			currentEMREditor.minValue = minNode.ratio;
			currentEMREditor.maxValue = maxNode.ratio;
			EMRUtils.Log("Done Updating In Flight EMR Params");
		}

		private void BindInFlightCallbacks()
		{
			EMRUtils.Log("Binding In Flight Callbacks");
			string[] editorNames = new string[] { "currentEMR" };
			foreach (var editorName in editorNames) {
				Fields[editorName].uiControlEditor.onFieldChanged += InFlightUIChanged;
			}
			EMRUtils.Log("Done Binding In Flight Callbacks");
		}

		private void InFlightUIChanged(BaseField baseField, object obj)
		{
			//UpdateIspAndThrustDisplay();
			//SetNeededFuel();
			//UpdateAllParts();
		}
		#endregion

		#region Editor 
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

		[KSPField(isPersistant = true)]
		public bool emrEnabled;

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

			DeserializeNodes();

			if (HighLogic.LoadedSceneIsFlight) {
				BindInFlightCallbacks();
				UpdateInFlightEMRParams();
			}

			BindCallbacks();

			UpdateIspAndThrustDisplay();

			if (engineModule == null) {
				engineModule = part.FindModuleImplementing<ModuleEngines>();
			}
			if (engineModule == null) {
				EMRUtils.Log("ERROR! could not find ModuleEngines");
			}
			base.OnStart(state);
		}

		private PropellantResources propellantResources;
		private void SetNeededFuel()
		{
			if (propellantResources == null) {
				propellantResources = new PropellantResources(engineModule);
			}

			SetNewRatios(propellantResources, startingEMR, finalEMR, emrSplitPercentage);
		}

		private void SetNewRatios(PropellantResources propellantResources, float startingEMR, float finalEMR, float emrSplitPercentage)
		{
			Dictionary<int, float> startRatios = GetRatiosForEMR(propellantResources, startingEMR);
			Dictionary<int, float> endRatios = GetRatiosForEMR(propellantResources, finalEMR);

			foreach (var prop in engineModule.propellants) {
				if (endRatios.ContainsKey(prop.id) && startRatios.ContainsKey(prop.id)) {
					var ratioDiff = endRatios[prop.id] - startRatios[prop.id];
					//EMRUtils.Log("Ratio Diff for ", prop.name, ": ", ratioDiff);
					prop.ratio = startRatios[prop.id] + ((emrSplitPercentage / 100) * ratioDiff);
				}
				else {
					prop.ratio = propellantResources.GetById(prop.id).Ratio;
				}
				//EMRUtils.Log("New ratio: ", prop.ratio);
				if (propellantResources.Oxidizer.Id == prop.id && fuelReservePercentage > 0) {
					//EMRUtils.Log("Adjusting oxidizer capacity to account for boiloff");
					prop.ratio = prop.ratio * ((100 - fuelReservePercentage) / 100);
				}
				if (propellantResources.Oxidizer.Id != prop.id && fuelReservePercentage < 0) {
					//EMRUtils.Log("Adjusting fuel capacity to account for boiloff");
					prop.ratio = prop.ratio * ((100 + fuelReservePercentage) / 100);
				}
			}

		}

		Dictionary<int, float> GetRatiosForEMR(PropellantResources propellantResources, float EMR)
		{
			// right now, the ratio is a volume ratio, so we need to convert that to a mass ratio

			// EMR = oxidizer mass flow rate
			// 1 = fuel mass flow rate

			// let's sum up all the mass flows for our fuels
			var fuelMassFlow = propellantResources.Fuels.Sum(fuel => fuel.PropellantMassFlow);

			// oxidizer mass flow will be that times the EMR
			var oxidizerMassFlow = fuelMassFlow * EMR;

			// dividing that by density should give us the ratios tha we want
			var oxidierRatio = oxidizerMassFlow / propellantResources.Oxidizer.Density;

			Dictionary<int, float> ratios = new Dictionary<int, float>();
			ratios.Add(propellantResources.Oxidizer.Id, oxidierRatio);
			return ratios;
		}

		private void BindCallbacks()
		{
			string[] editorNames = new string[] { "startingEMR", "finalEMR", "emrSplitPercentage", "fuelReservePercentage" };
			foreach (var editorName in editorNames) {
				Fields[editorName].uiControlEditor.onFieldChanged += UIChanged;
			}
			EMRUtils.Log("Bound Callbacks");
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
			return node.atmosphereCurve.Evaluate(0) + "s   Thrust: " + MathUtils.ToStringSI(node.thrust, 2, 0, "N");
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
				atmosphereCurve = FloatCurveTransformer.GenerateForPercentage(minNode.atmosphereCurve, maxNode.atmosphereCurve, ratioPercentage),
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

			List<String> configList = mixtureConfigNodes.Values.Select(item => item.ToString()).ToList();
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

			if (startingEMR < minNode.ratio || startingEMR > maxNode.ratio) {
				startingEMR = maxNode.ratio;
			}
			if (finalEMR < minNode.ratio || finalEMR > maxNode.ratio) {
				finalEMR = minNode.ratio;
			}
		}

		private void SetActionsAndGui()
		{
			Events["ToggleEMR"].guiName = (emrEnabled ? "Disable" : "Enable") + " EMR Controller";
			string[] fieldsToShow = new string[] {
				"startingEMR", "finalEMR", "startingEMRText", "finalEMRText",
				"emrSplitPercentage", "fuelReservePercentage", "fuelReserveText"
			};
			foreach (string field in fieldsToShow) {
				Fields[field].guiActiveEditor = emrEnabled;
			}
		}

		private void DeserializeNodes()
		{
			if (mixtureConfigNodes == null && mixtureConfigNodesSerialized != null) {
				EMRUtils.Log("ConfigNode Deserialization Needed");
				mixtureConfigNodes = new Dictionary<float, MixtureConfigNode>();
				List<string> deserialized = 
					ObjectSerializer.Deserialize<List<string>>(mixtureConfigNodesSerialized);
				foreach (var serializedItem in deserialized) {
					MixtureConfigNode node = new MixtureConfigNode(serializedItem);
					EMRUtils.Log("Deserialized ratio: ", node.ratio, " (", node.atmosphereCurve.Evaluate(0), "/", node.atmosphereCurve.Evaluate(1), ")");
					mixtureConfigNodes.Add(node.ratio, node);
				}
				EMRUtils.Log("Deserialized ", mixtureConfigNodes.Count, " configs");

				SetEditorFields();
				SetActionsAndGui();
			}
		}
		#endregion

	}

}
