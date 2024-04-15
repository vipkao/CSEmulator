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
        : Components.IVelocityYHolder
    {
        readonly DesktopPlayerController controller;

        readonly FieldInfo velocityY;

        float IVelocityYHolder.value
        {
            get => (float)velocityY.GetValue(controller);
            set => velocityY.SetValue(controller, value);
        }

        public DesktopPlayerControllerReflector(
            DesktopPlayerController controller
        )
        {
            this.controller = controller;
            velocityY = typeof(DesktopPlayerController)
                .GetField("velocityY", BindingFlags.NonPublic | BindingFlags.Instance);

        }
    }
}
