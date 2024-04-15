using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class StateProxy
    {
        readonly Dictionary<string, object> state;

        public StateProxy(
        )
        {
            state = new Dictionary<string, object>();
        }

        public object this[string index]
        {
            get
            {
                if (!state.ContainsKey(index))
                    return Jint.Native.JsValue.Undefined;

                var ret = state[index];

                ret = SanitizeSingleValue(ret);

                return ret;
            }
            set
            {
                state[index] = SanitizeSingleValue(value);
            }
        }

        public static object SanitizeSingleValue(object value)
        {
            if (value == null)
            {
                return Jint.Native.JsValue.Undefined;
            }
            else if (value is bool boolValue)
            {
                return boolValue;
            }
            else if (value is int intValue)
            {
                return intValue;
            }
            else if (value is float floatValue)
            {
                return floatValue;
            }
            else if (value is double doubleValue)
            {
                return doubleValue;
            }
            else if (value is string stringValue)
            {
                return stringValue;
            }
            else if (value.GetType().IsArray)
            {
                var objects = (object[])value;
                return objects.Select(o => SanitizeSingleValue(o)).ToArray();
            }
            else if (value is Delegate)
            {
                UnityEngine.Debug.LogWarning("$.state dose not accept Function.");
                throw new ClusterScriptError("$.state dose not accept Function.");
            }
            else if (value is DateTime)
            {
                //DateTimeつまりsignalの値を入れようとした場合は無視される
                UnityEngine.Debug.LogWarning("$.state dose not accept *signal* value. use *double* value.");
                return Jint.Native.JsValue.Undefined;
            }
            else if (value is EmulateVector2 vector2)
            {
                return vector2.clone();
            }
            else if (value is EmulateVector3 vector3)
            {
                return vector3.clone();
            }
            else if (value is EmulateQuaternion quaternion)
            {
                return quaternion.clone();
            }
            else if (value is ItemHandle itemHandle)
            {
                return itemHandle;
            }
            else if (value is PlayerHandle playerHandle)
            {
                return playerHandle;
            }
            else if (value is System.Dynamic.ExpandoObject eo)
            {
                return SanitizeExpandoObject(eo);
            }
            else
            {
                var message = String.Format("$.state dose not accept [{0}].", value.GetType().Name);
                UnityEngine.Debug.LogWarning(message);
                throw new ClusterScriptError(message);
            }
        }
        public static System.Dynamic.ExpandoObject SanitizeExpandoObject(System.Dynamic.ExpandoObject value)
        {
            foreach (var kv in value)
            {
                SanitizeSingleValue(kv.Value);
            }
            return value;
        }

    }
}
