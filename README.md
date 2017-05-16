This is a mod that adjust propellent utilization on engines

A sample MM config:

	@PART[liquidEngine2]:FOR[EMRController]
	{
		MODULE
		{
			name = EMRController

			MIXTURE
			{
				ratio = 1
				thrust = 200

				atmosphereCurve
				{
					key = 0 325
					key = 1 260
					key = 6 0.001
				}
			}
			MIXTURE
			{
				ratio = 1.5
				thrust = 300

				atmosphereCurve
				{
					key = 0 300
					key = 1 200
					key = 6 0.001
				}
			}
		}
	}

