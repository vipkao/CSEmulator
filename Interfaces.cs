using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator
{
    //CCKのIMoveInputControllerみたいにしたかったけど若干時すでに遅し
    public interface IRawInput
    {
        Func<bool> IsForwardKey { get; }
        Func<bool> IsBackKey { get; }
        Func<bool> IsRightKey { get; }
        Func<bool> IsLeftKey { get; }

        Func<bool> IsRightHandKeyDown { get; }
        Func<bool> IsRightHandKeyUp { get; }
        Func<bool> IsLeftHandKeyDown { get; }
        Func<bool> IsLeftHandKeyUp { get; }
    }
    public interface IProgramStatus
    {
        string GetLineInfo();
    }
    public interface IItemExceptionFactory
    {
        Exception CreateDistanceLimitExceeded(string message);
        Exception CreateRateLimitExceeded(string message);
        Exception CreateExecutionNotAllowed(string message);
        Exception CreateGeneral(string message);
    }
    public interface ITicker
    {
        long Ticks();
    }
}
