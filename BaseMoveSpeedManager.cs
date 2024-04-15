using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator
{
    public class BaseMoveSpeedManager
    {
        enum Speed
        {
            Walk,
            Run,
            Dash
        }
        Speed speed;

        readonly float baseSpeed;
        readonly float dashRatio;
        readonly float runRatio;
        readonly float walkRatio;

        readonly IRawInput rawInput;

        bool dashKeyPressed = false;
        bool walkKeyPressed = false;


        public BaseMoveSpeedManager(
            float baseSpeed,
            float dashRatio,
            float runRatio,
            float walkRatio,
            IRawInput rawInput
        )
        {
            this.rawInput = rawInput;
            this.baseSpeed = baseSpeed;
            this.dashRatio = dashRatio;
            this.runRatio = runRatio;
            this.walkRatio = walkRatio;
            this.speed = Speed.Run;
        }

        public void Update(
            Action<float> OnSpeedChanged
        )
        {
            //後に押された操作が優先される
            var nextSpeed = speed;
            var isKeyUp = false;

            CheckChange(
                rawInput.IsDashKey, ref dashKeyPressed,
                () => nextSpeed = Speed.Dash,
                () => isKeyUp = true
            );

            CheckChange(
                rawInput.IsWalkKey, ref walkKeyPressed,
                () => nextSpeed = Speed.Walk,
                () => isKeyUp = true
            );

            if (isKeyUp)
            {
                nextSpeed = Speed.Run;
                if (dashKeyPressed) nextSpeed = Speed.Dash;
                if (walkKeyPressed) nextSpeed = Speed.Walk;
            }

            if (nextSpeed == speed) return;

            speed = nextSpeed;
            var ratio = speed switch
            {
                Speed.Walk => walkRatio,
                Speed.Run => runRatio,
                Speed.Dash => dashRatio,
                _ => throw new Exception("開発者のミス。開発者に連絡してください。")
            };
            OnSpeedChanged(baseSpeed * ratio);
        }

        void CheckChange(Func<bool> IsKey, ref bool pressed, Action OnDown, Action OnUp)
        {
            if (IsKey())
            {
                if (!pressed)
                {
                    pressed = true;
                    OnDown();
                }
            }
            else
            {
                if (pressed)
                {
                    pressed = false;
                    OnUp();
                }
            }
        }

    }
}
