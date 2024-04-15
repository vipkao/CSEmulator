using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator
{
    public class KeyWalkManager
    {
        readonly Func<bool> IsForwardKeyDown;
        readonly Func<bool> IsForwardKeyUp;
        readonly Func<bool> IsBackKeyDown;
        readonly Func<bool> IsBackKeyUp;
        readonly Func<bool> IsRightKeyDown;
        readonly Func<bool> IsRightKeyUp;
        readonly Func<bool> IsLeftKeyDown;
        readonly Func<bool> IsLeftKeyUp;

        int forwardForce = 0;
        int forwardDirection = 0;
        int rightForce = 0;
        int rightDirection = 0;
        int walkingSpeed = 0;

        public KeyWalkManager(
            Func<bool> IsForwardKeyDown, Func<bool> IsForwardKeyUp,
            Func<bool> IsBackKeyDown, Func<bool> IsBackKeyUp,
            Func<bool> IsRightKeyDown, Func<bool> IsRightKeyUp,
            Func<bool> IsLeftKeyDown, Func<bool> IsLeftKeyUp
        )
        {
            this.IsForwardKeyDown = IsForwardKeyDown;
            this.IsForwardKeyUp = IsForwardKeyUp;
            this.IsBackKeyDown = IsBackKeyDown;
            this.IsBackKeyUp = IsBackKeyUp;
            this.IsRightKeyDown = IsRightKeyDown;
            this.IsRightKeyUp = IsRightKeyUp;
            this.IsLeftKeyDown = IsLeftKeyDown;
            this.IsLeftKeyUp = IsLeftKeyUp;
        }

        public void Update(
            Action<int> OnWalkingChanged,
            Action<int> OnForwardDirectionChanged,
            Action<int> OnRightDirectionChanged
        )
        {
            var changed = false;
            ApplyWalkForce(IsForwardKeyDown, IsForwardKeyUp, 1, ref forwardForce, ref changed);
            ApplyWalkForce(IsBackKeyDown, IsBackKeyUp, -1, ref forwardForce, ref changed);
            ApplyWalkForce(IsRightKeyDown, IsRightKeyUp, 1, ref rightForce, ref changed);
            ApplyWalkForce(IsLeftKeyDown, IsLeftKeyUp, -1, ref rightForce, ref changed);
            if (changed)
            {
                var toWalkingSpeed = forwardForce == 0 && rightForce == 0 ? 0 : 1;
                if(walkingSpeed != toWalkingSpeed)
                {
                    walkingSpeed = toWalkingSpeed;
                    OnWalkingChanged(walkingSpeed);
                }

                var toForwardDirection = CalcDirection(forwardDirection, forwardForce, rightForce);
                if (forwardDirection != toForwardDirection)
                {
                    forwardDirection = toForwardDirection;
                    OnForwardDirectionChanged(forwardDirection);
                }

                var toRightDirection = CalcDirection(rightDirection, rightForce, forwardForce);
                if (rightDirection != toRightDirection)
                {
                    rightDirection = toRightDirection;
                    OnRightDirectionChanged(rightDirection);
                }
            }
        }

        void ApplyWalkForce(
            Func<bool> IsKeyDown, Func<bool> IsKeyUp, int direction, ref int force, ref bool changed)
        {
            if (IsKeyDown())
            {
                force += direction;
                changed = true;
            }
            if (IsKeyUp())
            {
                force -= direction;
                changed = true;
            }
        }

        int CalcDirection(int targetDirection, int targetForce, int otherForce)
        {
            if (targetForce != 0) return targetForce;
            if (otherForce == 0) return targetDirection;
            return 0;
        }
    }
}
