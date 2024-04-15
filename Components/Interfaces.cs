using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    public interface IVelocityYHolder
    {
        public float value { get; set; }
    }
    public interface IBaseMoveSpeedHolder
    {
        public float value { get; set; }
    }
    public interface IPlayerRotateHolder
    {
        public Transform rotateTransform { get; }
    }
    public interface IPerspectiveChangeNotifier
    {
        event Handler<bool> OnChanged;
        void RequestNotify();
    }
    public interface ICharacterController
    {
        public bool isGrounded { get; }
        public void Move(UnityEngine.Vector3 motion);
    }
}
