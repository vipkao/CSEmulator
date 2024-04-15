using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class UnityCharacterController
        : Components.ICharacterController
    {
        readonly CharacterController parent;
        public UnityCharacterController(
            CharacterController parent
        )
        {
            this.parent = parent;
        }

        public bool isGrounded => parent.isGrounded;

        public void Move(Vector3 motion)
        {
            parent.Move(motion);
        }
    }
}
