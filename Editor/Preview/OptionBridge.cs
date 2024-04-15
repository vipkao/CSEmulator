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
        public IUrlHolder urlHolder { get; private set; }

        public EmulatorOptions raw { get; private set; }

        public string userId => raw.userId;

        public string userDisplayName => raw.userName;

        public bool exists => raw.exists;

        public class UrlHolder : IUrlHolder
        {
            readonly EmulatorOptions options;
            public UrlHolder(EmulatorOptions options) => this.options = options;
            public string url => options.callExternalUrl;
        }

        public OptionBridge(
            EmulatorOptions options
        )
        {
            this.raw = options;
            this.urlHolder = new UrlHolder(options);
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
