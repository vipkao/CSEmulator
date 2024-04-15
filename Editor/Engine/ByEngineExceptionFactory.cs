using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class ByEngineExceptionFactory
        : IItemExceptionFactory
    {
        readonly Jint.Engine engine;
        readonly Jint.Native.Error.ErrorConstructor clusterScriptErrorConstructor;

        public ByEngineExceptionFactory(
            Jint.Engine engine
        )
        {
            this.engine = engine;

            var prototypeObject = typeof(Jint.Native.Error.ErrorConstructor)
                .GetProperty("PrototypeObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(engine.Realm.Intrinsics.Error);
            Func<Jint.Runtime.Intrinsics, Jint.Native.Object.ObjectInstance> intrinsicDefaultProto = (intrinsics) =>
            {
                var ret = (Jint.Native.Object.ObjectInstance)typeof(Jint.Native.Error.ErrorConstructor)
                    .GetField("PrototypeObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(clusterScriptErrorConstructor);
                return ret;
            };
            clusterScriptErrorConstructor = (Jint.Native.Error.ErrorConstructor)Activator.CreateInstance(
                typeof(Jint.Native.Error.ErrorConstructor),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[]
                {
                    engine,
                    engine.Realm,
                    engine.Realm.Intrinsics.Error,
                    prototypeObject,
                    new Jint.Native.JsString("ClusterScriptError"),
                    intrinsicDefaultProto
                },
                null
            );
        }

        public Exception CreateDistanceLimitExceeded(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[DistanceLimitExceeded]" + message)
            {
                distanceLimitExceeded = true
            };
        }

        public Exception CreateRateLimitExceeded(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[RateLimitExceeded]" + message)
            {
                rateLimitExceeded = true
            };
        }

        public Exception CreateExecutionNotAllowed(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[ExecutionNotAllowed]" + message)
            {
                executionNotAllowed = true
            };

        }

        public Exception CreateGeneral(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, message);
        }
    }
}
