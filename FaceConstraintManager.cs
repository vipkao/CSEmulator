using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator
{
    public class FaceConstraintManager
    {
        bool isGrab = false;
        bool isFirstPersonPerspective = false;
        public bool isConstraintForward { get; private set; } = false;

        readonly Action<bool> FaceConstraintChangedCallback;

        public FaceConstraintManager(
            Action<bool> FaceConstraintChangedCallback
        )
        {
            this.FaceConstraintChangedCallback = FaceConstraintChangedCallback;
        }

        public void ChangeGrabbing(bool isGrab)
        {
            this.isGrab = isGrab;
            CheckFaceConstraint();
        }
        public void ChangePerspective(bool isFirstPerson)
        {
            this.isFirstPersonPerspective = isFirstPerson;
            CheckFaceConstraint();
        }
        void CheckFaceConstraint()
        {
            var nowConstraintForward = isGrab || isFirstPersonPerspective;

            if (isConstraintForward == nowConstraintForward) return;

            isConstraintForward = nowConstraintForward;
            FaceConstraintChangedCallback(nowConstraintForward);
        }

    }
}
