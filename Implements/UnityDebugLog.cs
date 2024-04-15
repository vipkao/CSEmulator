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
        public void Write(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
