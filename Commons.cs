using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator
{
    public static class Commons
    {
        public static float STANDARD_GRAVITY = -9.81f;

        static System.Text.RegularExpressions.Regex uuidPattern = new System.Text.RegularExpressions.Regex(
            "^([0-9a-fA-F]{8})-([0-9a-fA-F]{4})-([0-9a-fA-F]{4})-([0-9a-fA-F]{4})-([0-9a-fA-F]{12})$"
        );
        public static bool IsUUID(string str)
        {
            return uuidPattern.IsMatch(str);
        }

        public static int BuildLayerMask(params int[] maskBits)
        {
            int mask = 0;

            foreach (var bit in maskBits)
            {
                mask = mask | (1 << bit);
            }

            return mask;
        }

        public static string GetFullPath(this UnityEngine.GameObject gameObject)
        {
            return GetFullPath(gameObject.transform);
        }
        static string GetFullPath(UnityEngine.Transform transform)
        {
            string path = transform.name;
            var parent = transform.parent;
            while (parent)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }

        public static T AddComponent<T>(UnityEngine.GameObject gameObject)
            where T : UnityEngine.MonoBehaviour
        {
            var c = gameObject.GetComponent<T>();
            if (c == null)
            {
                return gameObject.AddComponent<T>();
            }
            else
            {
                return c;
            }
        }

        public static string ObjectArrayToString(
            object[] objectArray
        )
        {
            return "[" + String.Join(",", objectArray.Select(o =>
            {
                if (o is string str)
                    return "\"" + str + "\"";
                return o.ToString();
            })) + "]";
        }

        public static string ExpandoObjectToString(
            System.Dynamic.ExpandoObject eo,
            string openb = "{\n", string closeb = "}\n",
            string indent = " ", string separator = ",\n",
            int depth = 0, string _pref = "", string _suff = ""
        )
        {
            var pref = _pref + indent + openb;
            var suff = indent + closeb + _suff;
            depth++;
            var ind = String.Concat(Enumerable.Repeat(indent, depth).ToArray());
            var body = pref;
            foreach (var kv in eo)
            {
                body += ind + kv.Key + ":";
                if (kv.Value is System.Dynamic.ExpandoObject _eo)
                {
                    body += ExpandoObjectToString(_eo, openb, closeb, indent, separator, depth, _pref, _suff);
                }
                else if (kv.Value is object[] oa)
                {
                    body += ObjectArrayToString(oa);
                }
                else if (kv.Value is string str)
                {
                    body += "\"" + str + "\"";
                }
                else
                {
                    body += kv.Value.ToString();
                }
                body += separator;
            }
            body += suff;

            return body;
        }

        public static UnityEngine.Vector2 Clone(this UnityEngine.Vector2 v)
        {
            return new UnityEngine.Vector2(v.x, v.y);
        }

        public static UnityEngine.Vector3 Clone(this UnityEngine.Vector3 v)
        {
            return new UnityEngine.Vector3(v.x, v.y, v.z);
        }

        public static UnityEngine.Quaternion Clone(this UnityEngine.Quaternion q)
        {
            return new UnityEngine.Quaternion(q.x, q.y, q.z, q.w);
        }

    }
}
