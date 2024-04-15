using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public interface ILoggerFactory
    {
        ILogger Create(IProgramStatus programStatus);
    }

    public interface IRunnerOptions
    {
        bool isDebug { get; }
        IExternalCallerOptions externalCallerOptions { get; }
    }

    public interface IExternalCallerOptions
    {
        event Handler OnChangeLimit;
        string url { get; }
        EmulateClasses.CallExternalRateLimit rateLimit { get; }
    }
}
