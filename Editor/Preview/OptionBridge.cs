using Assets.KaomoLab.CSEmulator.Components;
using Assets.KaomoLab.CSEmulator.Editor.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class OptionBridge
        : Engine.IRunnerOptions, Components.IPerspectiveChangeNotifier
    {
        public bool isDebug => raw.debug;

        public EmulatorOptions raw { get; private set; }

        public OptionBridge(
            EmulatorOptions options
        )
        {
            this.raw = options;
            options.OnChangedPerspective += Options_OnChangedPerspective;
        }

        private void Options_OnChangedPerspective()
        {
            foreach (var l in perspectiveChangeListeners)
            {
                l.Invoke(raw.perspective);
            }
        }

        readonly List<Handler<bool>> perspectiveChangeListeners = new List<Handler<bool>>();
        event Handler<bool> IPerspectiveChangeNotifier.OnChanged
        {
            add => perspectiveChangeListeners.Add(value);
            remove => perspectiveChangeListeners.Remove(value);
        }

        void IPerspectiveChangeNotifier.RequestNotify()
        {
            Options_OnChangedPerspective();
        }
    }
}
