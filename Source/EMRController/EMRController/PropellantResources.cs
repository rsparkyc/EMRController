using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController
{
	class PropellantResources : List<PropellantResource>
	{
		// We're going to make an assumption here and assume that the oxidizer is the heaver propellent 
		private PropellantResource _oxidizer;
		public PropellantResource Oxidizer {
			get {
				if (_oxidizer == null) {
					//EMRUtils.Log("Oxidizer detection needed");
					// I tried doing the following, but was getting exceptions throw
					//_oxidizer = this.MaxAt(prop => prop.PropellantMassFlow);

					//Instead, I'll just find the max and do a find on use that.
					var maxMassFlow = this.Max(prop => prop.PropellantMassFlow);
					var oxidizerCandidates = this.FindAll(prop => prop.PropellantMassFlow == maxMassFlow);
					if (oxidizerCandidates.Count == 1) {
						_oxidizer = oxidizerCandidates[0];
						//EMRUtils.Log("Oxidizer detected as ", _oxidizer.Name, " (with a mass flow of ", _oxidizer.PropellantMassFlow, ")");
					}
					else {
						//Multiple candidates found, looking for this first one with "ox" in the name
						_oxidizer = oxidizerCandidates.Find(prop => prop.Name.ToLower().Contains("ox"));
						//EMRUtils.Log("Multiple Oxidizer candidates found, using ", _oxidizer.Name);
					}
				}
				return _oxidizer;
			}
		}

		private List<PropellantResource> _fuels;
		public List<PropellantResource> Fuels {
			get {
				if (_fuels == null || _fuels.Count == 0) {
					// You may think that we would just get the minimum, but if we use more than 2 propellants
					// (an oxidizer, and multiple fuel components) we need to select all of them
					_fuels = this.Where(item => item != Oxidizer).ToList();
				}
				return _fuels;
			}
		}

		public float RatioTotals {
			get {
				return this.Sum(prop => prop.Ratio);
			}
		}

		public double AverageFuelDensity {
			get {
				return Fuels.Sum(fuel => fuel.Ratio * fuel.Density) / (RatioTotals - Oxidizer.Ratio);
			}
		}

		/// <summary>
		/// This function will match a propellant with a resource, so we can get things like Ratio and Density in one object
		/// It is required that each propellant have a matching resource
		/// </summary>
		/// <param name="propellants">A list of propellants</param>
		/// <param name="resources">A list of resources</param>
		/// <returns>A dictionary lookup, with the id as the key</returns>
		public PropellantResources(IEnumerable<Propellant> propellants, IEnumerable<PartResourceDefinition> resources)
		{
			Build(propellants, resources);
		}

		public PropellantResources(ModuleEngines engineModule)
		{
			Build(engineModule.propellants, engineModule.GetConsumedResources());
		}

		private void Build(IEnumerable<Propellant> propellants, IEnumerable<PartResourceDefinition> resources)
		{
			foreach (var prop in propellants) {
				var resource = resources.First(res => res.id == prop.id);
				Add(new PropellantResource(prop, resource));
			}
			//EMRUtils.Log("Built PropellantResources with ", Count, " fuels");
		}

		Dictionary<int, PropellantResource> resourceCache = new Dictionary<int, PropellantResource>();

		public PropellantResource GetById(int id)
		{
			if (!resourceCache.ContainsKey(id)) {
				resourceCache.Add(id, Find(prop => prop.Id == id));
			}
			return resourceCache[id];
		}
	}
}
