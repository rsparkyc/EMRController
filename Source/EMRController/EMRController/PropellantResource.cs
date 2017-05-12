using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController
{
	class PropellantResource
	{
		public Propellant Propellant { get; set; }
		public PartResourceDefinition Resource { get; set; }

		public int Id {
			get {
				return Propellant.id;
			}
		}

		public float Ratio {
			get {
				return Propellant.ratio;
			}
			set {
				Propellant.ratio = value;
			}
		}

		public float Density {
			get {
				return Resource.density;
			}
		}

		public float PropellantMassFlow {
			get {
				return Density * Ratio;
			}
		}

		public object Name {
			get {
				return Propellant.name;
			}
		}

		public PropellantResource(Propellant propellant, PartResourceDefinition resource)
		{
			if (propellant.id != resource.id) {
				string errorMessage = "Propellant and Resource do not have the same id.";
				EMRUtils.Log("ERROR: ", errorMessage);
				throw new ArgumentException(errorMessage);
			}

			// We're going to make a clone of the propellant, since we want to leave these "stock" rations alone
			Propellant = new Propellant() {
				id = propellant.id,
				name = propellant.name,
				ratio = propellant.ratio
			};

			Resource = resource;
		}
	}
}
