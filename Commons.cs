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
    }
}
