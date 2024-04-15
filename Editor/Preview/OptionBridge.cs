using Assets.KaomoLab.CSEmulator.Editor.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class OptionBridge
        : Engine.IRunnerOptions
    {
        public bool isDebug => options.debug;

        readonly EmulatorOptions options;

        public OptionBridge(
            EmulatorOptions options
        )
        {
            this.options = options;
        }
    }
}
