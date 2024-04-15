﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class PlayerHandle
    {
        public string id => playerController.id;

        public IPlayerController playerController { get; private set; }
        //ownerの切り替えにはnewを強制しておきたいためprivate
        readonly Components.CSEmulatorItemHandler csOwnerItemHandler;

        //csOwnerItemHandlerとはこのハンドルがいるスクリプト空間($)のこと。
        public PlayerHandle(
            IPlayerController playerController,
            Components.CSEmulatorItemHandler csOwnerItemHandler
        )
        {
            this.playerController = playerController;
            this.csOwnerItemHandler = csOwnerItemHandler;
        }

        public void addVelocity(EmulateVector3 velocity)
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.AddVelocity(velocity._ToUnityEngine());
        }

        public bool exists()
        {
            return playerController.exists;
        }

        public EmulateVector3 getHumanoidBonePosition(HumanoidBone bone)
        {
            if (!playerController.exists) return null;

            CheckOwnerDistanceLimit();

            var v = playerController.animator.GetBoneTransform((HumanBodyBones)bone);
            return new EmulateVector3(v.position);
        }

        public EmulateQuaternion getHumanoidBoneRotation(HumanoidBone bone)
        {
            if (!playerController.exists) return null;

            CheckOwnerDistanceLimit();

            var v = playerController.animator.GetBoneTransform((HumanBodyBones)bone);
            return new EmulateQuaternion(v.rotation);
        }

        public EmulateVector3 getPosition()
        {
            if (!playerController.exists) return null;

            CheckOwnerDistanceLimit();

            var v = playerController.GetPosition();
            return new EmulateVector3(v);
        }

        public EmulateQuaternion getRotation()
        {
            if (!playerController.exists) return null;

            CheckOwnerDistanceLimit();

            var q = playerController.GetRotation();
            return new EmulateQuaternion(q);
        }

        public void resetPlayerEffects()
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.moveSpeedRate = 1;
            playerController.jumpSpeedRate = 1;
            playerController.gravity = CSEmulator.Commons.STANDARD_GRAVITY;
        }

        public void respawn()
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.Respawn();
        }


        public void setGravity(float gravity)
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.gravity = gravity;
        }

        //開発用
        public HumanoidPose __getHumanoidPose()
        {
            var humanPose = playerController.GetHumanPose();

            var muscles = new Muscles();
            for(var i = 0; i < humanPose.muscles.Length; i++)
            {
                muscles.Set(i, humanPose.muscles[i]);
            }

            var ret = new HumanoidPose(
                new EmulateVector3(humanPose.bodyPosition),
                new EmulateQuaternion(humanPose.bodyRotation),
                muscles
            );

            return ret;
        }

        public void setHumanoidPose(
            HumanoidPose pose
        )
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            if(pose == null)
            {
                playerController.SetHumanPosition(null);
                playerController.SetHumanRotation(null);
                playerController.InvalidateHumanMuscles();
                return;
            }

            playerController.SetHumanPosition(
                pose.centerPosition == null ? null : pose.centerPosition._ToUnityEngine()
            );
            playerController.SetHumanRotation(
                pose.centerRotation == null ? null : pose.centerRotation._ToUnityEngine()
            );

            if (pose.muscles == null)
            {
                playerController.InvalidateHumanMuscles();
            }
            else
            {
                playerController.SetHumanMuscles(
                    pose.muscles.muscles,
                    pose.muscles.changed
                );
            }
        }

        public void setJumpSpeedRate(float jumpSpeedRate)
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.jumpSpeedRate = jumpSpeedRate;
        }

        public void setMoveSpeedRate(float moveSpeedRate)
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.moveSpeedRate = moveSpeedRate;
        }

        public void setPosition(
            EmulateVector3 position
        )
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.SetPosition(position._ToUnityEngine());
        }

        public void setRotation(
            EmulateQuaternion rotation
        )
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            //Ｙ軸回転(鉛直軸)のみ
            var r = rotation._ToUnityEngine().eulerAngles;
            var y = Quaternion.Euler(0, r.y, 0);
            playerController.SetRotation(y);
        }

        public override string ToString()
        {
            return string.Format("[PlayerHandle][{0}][{1}]", playerController.vrm.name, id);
        }

        void CheckOwnerDistanceLimit()
        {
            var p1 = playerController.GetPosition();
            var p2 = csOwnerItemHandler.gameObject.transform.position;
            var d = UnityEngine.Vector3.Distance(p1, p2);
            //30メートル以内はOK
            if (d <= 30f) return;

            throw new ClusterScriptError(String.Format("distanceLimitExceeded[{0}]>>>[Player]", csOwnerItemHandler)) { distanceLimitExceeded = true };
        }
        void CheckOwnerOperationLimit()
        {
            var result = csOwnerItemHandler.TryPlayerOperate();
            if (result) return;

            throw new ClusterScriptError(String.Format("rateLimitExceeded[{0}]>>>[Player]", csOwnerItemHandler)) { rateLimitExceeded = true };
        }
    }
}
