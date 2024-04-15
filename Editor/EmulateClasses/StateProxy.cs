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

        public static int CalcSendableSize(
            object value, int arrayAddition, int size = 0
        )
        {
            var add = 0;
            if (value == null)
            {
                add = 0;
            }
            else if (value is Jint.Native.JsValue jsv && jsv == Jint.Native.JsValue.Undefined)
            {
                throw new Exception("undefinedはSendableではありません。nullを指定してください。");
            }
            else if (value is bool boolValue)
            {
                add = 2;
            }
            else if (value is int intValue)
            {
                add = 9; //たぶん
            }
            else if (value is float floatValue)
            {
                add = 9; //たぶん
            }
            else if (value is double doubleValue)
            {
                add = 9; //数字は基本これで入ってくる
            }
            else if (value is string stringValue)
            {
                //"a":1、"あ":3
                var count = Encoding.UTF8.GetByteCount(stringValue);
                add = 2 + count;
            }
            else if (value.GetType().IsArray)
            {
                var objects = (object[])value;
                add = 2 + objects.Select(o => CalcSendableSize(o, 2, size)).Sum();
            }
            else if (value is ISendableSize sendableSize)
            {
                add = sendableSize.GetSize();
            }
            else if (value is System.Dynamic.ExpandoObject eo)
            {
                add = 2;
                foreach (var kv in eo.ToArray())
                {
                    add += Encoding.UTF8.GetByteCount(kv.Key);
                    add += 6 + CalcSendableSize(kv.Value, 0, size);
                }
            }

            //階層が深くなると加算される
            add += arrayAddition;

            //なんだか分からないけど130に入った瞬間に+1される
            if (size < 130 && size + add >= 130) add++;

            return size + add;
        }

        public static object SanitizeSingleValue(
            object value,
            Func<ItemHandle, ItemHandle> SanitizeItemHandle = null,
            Func<PlayerHandle, PlayerHandle> SanitizeItemPlayerHandle = null
        )
        {
            if (value == null)
            {
                return Jint.Native.JsValue.Undefined;
            }
            else if (value is Jint.Native.JsValue jsv && jsv == Jint.Native.JsValue.Undefined)
            {
                return jsv;
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
                //CSETODO 実際は少し違う
                return new object();
            }
            else if (value is DateTime)
            {
                return new object();
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
                if(SanitizeItemHandle == null)
                    return itemHandle;
                return SanitizeItemHandle(itemHandle);
            }
            else if (value is PlayerHandle playerHandle)
            {
                if (SanitizeItemPlayerHandle == null)
                    return playerHandle;
                return SanitizeItemPlayerHandle(playerHandle);
            }
            else if (value is System.Dynamic.ExpandoObject eo)
            {
                var newEo = new System.Dynamic.ExpandoObject();
                foreach (var kv in eo.ToArray())
                {
                    ((IDictionary<string, object>)newEo)[kv.Key] = SanitizeSingleValue(kv.Value, SanitizeItemHandle);
                }
                return newEo;
            }

            //CSETODO 実際は少し違う
            return new object();
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            return o;
        }
        public override string ToString()
        {
            return String.Format("[StateProxy]");
        }

    }
}
