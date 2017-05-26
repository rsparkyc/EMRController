This is a mod that adjust propellent utilization on engines, as was done in 
real life on the J-2 rocket engine.  

Each configuration for an EMRController module takes 2 MIXTURE configs
(an upper and lower limit.)  There are some key parameters on a MIXTURE 
config as follows:

  configName: if using ModuleEngineConfigs, you can specify the config this
              MIXTURE should be applied to

  ratio:      the oxidizer:fuel ratio that this mixture uses.  
              Note: this is by mass (and not volume, as is what KSP normally uses)

  maxThrust:  The maximum thrust this engine produces at this ratio

  minThrust:  The minimum thrust this engine produces at this ratio
              This is optional, and defaults to 0

  atmosphericCurve:  a standard FloatCurve for the ISP at this MIXTURE

A sample MM config:

	@PART[liquidEngine2]:FOR[EMRController]
	{
		MODULE
		{
			name = EMRController

			MIXTURE
			{
				configName = something
				ratio = 1
				maxThrust = 200
				minThrust = 0

				atmosphereCurve
				{
					key = 0 325
					key = 1 260
					key = 6 0.001
				}
			}
			MIXTURE
			{
				configName = something
				ratio = 1.5
				maxThrust = 300
				minThrust = 0

				atmosphereCurve
				{
					key = 0 300
					key = 1 200
					key = 6 0.001
				}
			}
		}
	}

