using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor
{
    public class ClusterScriptError : Jint.Runtime.JavaScriptException
    {
        public bool distanceLimitExceeded;
        public bool executionNotAllowed;
        public bool rateLimitExceeded;
        public bool requestSizeLimitExceeded;
        public string message => this.Message;
        public string name => GetType().Name;
        public string stack => base.JavaScriptStackTrace;

        public ClusterScriptError(
            Jint.Native.Error.ErrorConstructor errorConstructor,
            string message
        )
            : base(errorConstructor, message)
        {
        }
    }
}
