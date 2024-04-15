using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class SetHumanoidPoseOption
    {
        public double timeoutSeconds = double.PositiveInfinity;
        public double timeoutTransitionSeconds = 0;
        public double transitionSeconds = 0;

        public override string ToString()
        {
            return String.Format("timeoutSeconds:{0} timeoutTransitionSeconds:{1} transitionSeconds:{2}", timeoutSeconds, timeoutTransitionSeconds, transitionSeconds);
        }
    }
}
