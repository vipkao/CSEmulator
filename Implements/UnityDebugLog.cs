using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Implements
{
    public class UnityDebugLog
        : ILogger
    {
        public void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }
        public void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        public void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
