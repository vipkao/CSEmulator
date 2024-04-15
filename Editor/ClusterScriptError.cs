using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor
{
    //CSETODO JS内でキャッチできるようになったけども色々雑なのでなんとかする。
    public class ClusterScriptError : Jint.Runtime.JavaScriptException
    {
        public bool distanceLimitExceeded;
        public bool executionNotAllowed;
        public string message;
        public string name;
        public bool rateLimitExceeded;
        public bool requestSizeLimitExceeded;
        public string stack;

        public ClusterScriptError(string message)
            : base(message)
        {
            this.message = message;
            stack = base.StackTrace;
            name = base.GetType().Name;
        }
    }
}
