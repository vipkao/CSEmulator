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
        : Engine.IRunnerOptions,
        Components.IPerspectiveChangeNotifier,
        EmulateClasses.IPlayerMeta
    {
        public bool isDebug => raw.debug;
        public IExternalCallerOptions externalCallerOptions { get; private set; }

        public EmulatorOptions raw { get; private set; }

        public string userIdfc => raw.userIdfc;
        public string userId => raw.userId;
        public string userDisplayName => raw.userName;

        public bool exists => raw.exists;

        public class ExternalCallerOptions : IExternalCallerOptions
        {
            public event Handler OnChangeLimit = delegate { };
            readonly EmulatorOptions options;
            public ExternalCallerOptions(EmulatorOptions options)
            {
                this.options = options;
                this.options.OnChangedExternalCallLimit += () => {
                    OnChangeLimit.Invoke();
                };
            }
            public string url => options.callExternalUrl;
            public EmulateClasses.CallExternalRateLimit rateLimit => options.limitExternalCall;
        }

        public OptionBridge(
            EmulatorOptions options
        )
        {
            this.raw = options;
            this.externalCallerOptions = new ExternalCallerOptions(options);
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
