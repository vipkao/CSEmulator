using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class OnUpdateBridge
        : IDisposable, EmulateClasses.IUpdateListenerBinder
    {
        long prevTicks;

        Dictionary<string, (Action<double>, string)> UpdateCallbacks = new Dictionary<string, (Action<double>, string)>();
        Dictionary<string, (Action<double>, string)> LateUpdateCallbacks = new Dictionary<string, (Action<double>, string)>();


        public Action InvokeUpdate { get; private set; }

        public OnUpdateBridge()
        {
            InvokeUpdate = () =>
            {
                //prevTicksを初期化したらUpdateを通常処理に切り替える
                prevTicks = DateTime.Now.Ticks;
                InvokeUpdate = () =>
                {
                    var nowTicks = DateTime.Now.Ticks;
                    var deltaTime = (double)(nowTicks - prevTicks) / 10_000_000d;
                    //ループ中に登録が解除されるので、念のためにToArrayしておく
                    foreach(var (Callback, source) in UpdateCallbacks.Values.ToArray())
                    {
                        try
                        {
                            Callback(deltaTime);
                        }
                        catch(Exception e)
                        {
                            Commons.ExceptionLogger(e, source);
                        }
                    }
                    foreach (var (Callback, source) in LateUpdateCallbacks.Values.ToArray())
                    {
                        try
                        {
                            Callback(deltaTime);
                        }
                        catch (Exception e)
                        {
                            Commons.ExceptionLogger(e, source);
                        }
                    }
                    prevTicks = nowTicks;
                };
            };
        }

        public void SetUpdateCallback(string key, string source, Action<double> Callback)
        {
            UpdateCallbacks[key] = (Callback, source);
        }

        public void DeleteUpdateCallback(string key)
        {
            if (!UpdateCallbacks.ContainsKey(key)) return;
            UpdateCallbacks.Remove(key);
        }

        public void SetLateUpdateCallback(string key, string source, Action<double> Callback)
        {
            LateUpdateCallbacks[key] = (Callback, source);
        }

        public void DeleteLateUpdateCallback(string key)
        {
            if (!LateUpdateCallbacks.ContainsKey(key)) return;
            LateUpdateCallbacks.Remove(key);
        }

        public void Dispose()
        {
            UpdateCallbacks.Clear();
            LateUpdateCallbacks.Clear();
        }
    }
}
