What does this mod do?
----------------------------------
This is a mod that adjust propellent utilization on engines, as was done in real life on the 
J-2 rocket engine. EMR stands for Engine Mixture Ratio

## How do I use it
ERMController works in two modes: "open", and "closed" loop modes.  Open loop takes the
Oxidizer:Fuel mixture ratio you want to use, and runs the engine at that mixture.  Closed loop
takes a look at your remaining fuel, and continually adjusts your mixture ratio to exhaust
both fuel and oxidizer at the same time.

We'll cover how to

 1. Set up an EMRController configured engine in the VAB, and
 2. Use that engine in flight

### VAB
In the VAB, the first step is to click the `Enable EMR` button.  By doing so, you'll get a wide range 
of options to configure:
![EMR VAB](https://cdn.pbrd.co/images/aLvhhUTyE.png)

`Starting EMR`: This is the mixture ratio the engine will (by default) start at

`Final EMR`: this is the mixture ratio the engines will (ideally) end at

> Note: Both Starting and Final EMR have vacuum ISP and Thrust displays visible

`Percentage at Final EMR`: This is the percentage of fuel you plan to use at your Final EMR
(This is used to determine how much fuel we should put in our tanks)

`Boiloff Reserve Percentage`: If you're using a mod that simulates propellent boiloff, 
this slider allows us to add up to 50% more fuel or oxidizer, whichever you want to have more of.
Drag right for fuel (the more common choice), or right for oxidizer.

Now when you add your fuel (assuming you're using RealFuels), the fuel tank should properly select the
right mixture for you.

### In Flight
![EMR In Flight](https://cdn.pbrd.co/images/aM2yq3O7x.png)

When operating in Open Loop, the following options are available:

`Current EMR`: The currently active EMR (again, with Vac ISP and Thrust values below)
`Reserve`: Assuming you stayed at the current EMR, you would be left with this much fuel/oxidizer
`Change to Closed Loop (X.X:1)`: clicking this will switch you to Closed Loop mode,
which would change your EMR to X.X:1

Clicking on that, the UI changes.  The slider for `Current EMR` gets replaced by text that updates, 
and generally, `Reserve` will show "None".  An exception to this is when Closed Loop mode is 
operating at the configured EMR limits

## Configuration 
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


