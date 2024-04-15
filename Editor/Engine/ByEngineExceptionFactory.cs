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
        public ByEngineExceptionFactory(
            Jint.Engine engine
        )
        {
            this.engine = engine;
        }

        public Exception CreateDistanceLimitExceeded(string message)
        {
            return new ClusterScriptError(engine.Realm.Intrinsics.Error, "[DistanceLimitExceeded]" + message)
            {
                distanceLimitExceeded = true
            };
        }

        public Exception CreateRateLimitExceeded(string message)
        {
            return new ClusterScriptError(engine.Realm.Intrinsics.Error, "[RateLimitExceeded]" + message)
            {
                rateLimitExceeded = true
            };
        }

        public Exception CreateExecutionNotAllowed(string message)
        {
            return new ClusterScriptError(engine.Realm.Intrinsics.Error, "[ExecutionNotAllowed]" + message)
            {
                executionNotAllowed = true
            };

        }

        public Exception CreateGeneral(string message)
        {
            return new ClusterScriptError(engine.Realm.Intrinsics.Error, message);
        }
    }
}
