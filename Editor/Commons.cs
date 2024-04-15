using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor
{
    public static class Commons
    {
        public static void ExceptionLogger(Exception e, string source)
        {
            switch (e)
            {
                //CSETODO ここに入らないのでなんとかする。
                case ClusterScriptError cse:
                    UnityEngine.Debug.LogError(String.Format(
                        "[{3}]{0} {2}\n{1}",
                        cse.message,
                        cse.JavaScriptStackTrace,
                        cse.Location,
                        source
                    ));
                    break;
                case Jint.Runtime.JavaScriptException jse:
                    UnityEngine.Debug.LogError(String.Format(
                        "[{3}]{0} {2}\n{1}",
                        jse.Message,
                        jse.JavaScriptStackTrace,
                        jse.Location,
                        source
                    ));
                    break;
                default:
                    UnityEngine.Debug.LogException(e);
                    break;
            }
        }
    }
}
