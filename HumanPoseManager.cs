using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator
{
    public class HumanPoseManager
    {
        const int NUMBER_OF_MUSCLES = 95;

        readonly HumanPoseHandler poseHandler;

        Vector3? position = null;
        Quaternion? rotation = null;
        bool invalidMuscles = true;
        float?[] muscles = new float[NUMBER_OF_MUSCLES].Select(_ => (float?)null).ToArray();

        public HumanPoseManager(
            Animator animator
        )
        {
            poseHandler = new HumanPoseHandler(
                animator.avatar,
                animator.transform
            );
        }

        public void SetPosition(
            Vector3? position
        )
        {
            this.position = position;
        }

        public void SetRotation(
            Quaternion? rotation
        )
        {
            this.rotation = rotation;
        }

        public void SetMuscles(
            float[] muscles,
            bool[] hasMascles
        )
        {
            for (var i = 0; i < NUMBER_OF_MUSCLES; i++)
            {
                //hasMasclesではない時は、前回のは残らないようなのでnull
                this.muscles[i] = hasMascles[i] ? muscles[i] : null;
            }
            invalidMuscles = false;
        }
        public void InvalidateMuscles()
        {
            invalidMuscles = true;
            muscles = new float[NUMBER_OF_MUSCLES].Select(_ => (float?)null).ToArray();
        }

        public void Apply()
        {
            if (!position.HasValue && !rotation.HasValue && invalidMuscles)
                return;


            //初期状態ではこの段階でhumanPoseにはT-Poseのmusclesが入っている。
            //musclesを全て0にすると、手を前に出した中腰ポーズになる。
            var humanPose = GetHumanPose();

            if (!invalidMuscles)
            {
                for (var i = 0; i < NUMBER_OF_MUSCLES; i++)
                {
                    if (!muscles[i].HasValue) continue;
                    humanPose.muscles[i] = muscles[i].Value;
                }
                //musclesは残り続けるのでリセットしない
            }

            if (position != null) humanPose.bodyPosition = position.Value.Clone();
            if (rotation != null) humanPose.bodyRotation = rotation.Value.Clone();
            position = null;
            rotation = null;

            poseHandler.SetHumanPose(ref humanPose);
        }

        public HumanPose GetHumanPose()
        {
            var humanPose = new HumanPose();
            poseHandler.GetHumanPose(ref humanPose);
            return humanPose;
        }
    }
}
