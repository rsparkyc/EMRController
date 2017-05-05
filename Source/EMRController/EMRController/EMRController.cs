using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController
{
	public class EMRController : PartModule
	{
		[KSPField]
		public float minEMR = 4;

		[KSPField]
		public float maxEMR = 6;

		[KSPField(isPersistant = true, guiName = "Starting EMR", guiActive = false, guiActiveEditor = false, guiUnits = ":1"),
			UI_FloatEdit(incrementSmall = 0.1f, incrementLarge = 1.0f, incrementSlide = 0.01f, sigFigs = 2, unit = ":1")]
		public float startingEMR;

		[KSPField(isPersistant = true, guiName = "Final EMR", guiActive = false, guiActiveEditor = false, guiUnits = ":1"),
			UI_FloatEdit(incrementSmall = 0.1f, incrementLarge = 1.0f, incrementSlide = 0.01f, sigFigs = 2, unit = ":1")]
		public float finalEMR;

		public void Start()
		{
			UI_FloatEdit startFloatEdit = (UI_FloatEdit)Fields["startingEMR"].uiControlEditor;
			UI_FloatEdit finalFloatEdit = (UI_FloatEdit)Fields["finalEMR"].uiControlEditor;
			startFloatEdit.minValue = minEMR;
			startFloatEdit.maxValue = maxEMR;
			finalFloatEdit.minValue = minEMR;
			finalFloatEdit.maxValue = maxEMR;

			SetActionsAndGui();
		}

		private void SetActionsAndGui()
		{
			Events["ToggleEMR"].guiName = (emrEnabled ? "Disable" : "Enable") + " EMR Controller";
			Fields["startingEMR"].guiActiveEditor = emrEnabled;
			Fields["finalEMR"].guiActiveEditor = emrEnabled;
		}

		[KSPField]
		public bool emrEnabled = false;
		[KSPEvent(guiActive = true, guiActiveEditor = true)]
		public void ToggleEMR()
		{
			emrEnabled = !emrEnabled;
			SetActionsAndGui();
		}
	}
}
