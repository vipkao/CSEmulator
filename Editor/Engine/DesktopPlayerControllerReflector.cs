using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Assets.KaomoLab.CSEmulator.Components;
using ClusterVR.CreatorKit.Preview.PlayerController;


namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    //Reflection以上のことをさせてはいけない。
    public class DesktopPlayerControllerReflector
        : Components.IVelocityYHolder,
        Components.IBaseMoveSpeedHolder
    {
        readonly DesktopPlayerController controller;

        readonly FieldInfo velocityY;
        readonly FieldInfo baseMoveSpeed;

        float IVelocityYHolder.value
        {
            get => (float)velocityY.GetValue(controller);
            set => velocityY.SetValue(controller, value);
        }
        float IBaseMoveSpeedHolder.value {
            get => (float)baseMoveSpeed.GetValue(controller);
            set => baseMoveSpeed.SetValue(controller, value);
        }

        public DesktopPlayerControllerReflector(
            DesktopPlayerController controller
        )
        {
            this.controller = controller;
            velocityY = typeof(DesktopPlayerController)
                .GetField("velocityY", BindingFlags.NonPublic | BindingFlags.Instance);
            baseMoveSpeed = typeof(DesktopPlayerController)
                .GetField("baseMoveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

        }
    }
}
