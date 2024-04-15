using System;
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

        readonly IPlayerController playerController;
        readonly HumanPoseHandler poseHandler;

        public PlayerHandle(
            IPlayerController playerController
        )
        {
            this.playerController = playerController;
            poseHandler = new HumanPoseHandler(
                playerController.animator.avatar,
                playerController.animator.transform
            );

        }

        public void addVelocity(EmulateVector3 velocity)
        {
            //CSETODO PreviewOnly下のDesktopPlayerControllerを丸ごと入れ替えるしかない？
            UnityEngine.Debug.Log("ごめんなさい。addVelocityは対応中です。");
        }

        public bool exists()
        {
            return playerController.exists;
        }

        public EmulateVector3 getHumanoidBonePosition(HumanoidBone bone)
        {
            if (!playerController.exists) return null;

            //CSETODO 距離チェックどうする？

            var v = playerController.animator.GetBoneTransform((HumanBodyBones)bone);
            return new EmulateVector3(v.position);
        }

        public EmulateQuaternion getHumanoidBoneRotation(HumanoidBone bone)
        {
            if (!playerController.exists) return null;

            //CSETODO 距離チェックどうする？

            var v = playerController.animator.GetBoneTransform((HumanBodyBones)bone);
            return new EmulateQuaternion(v.rotation);
        }

        public EmulateVector3 getPosition()
        {
            if (!playerController.exists) return null;

            //CSETODO 距離チェックどうする？

            var v = playerController.getPosition();
            return new EmulateVector3(v);
        }

        public EmulateQuaternion getRotation()
        {
            if (!playerController.exists) return null;

            //CSETODO 距離チェックどうする？

            var q = playerController.getRotation();
            return new EmulateQuaternion(q);
        }

        public void resetPlayerEffects()
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？
            playerController.moveSpeedRate = 1;
            playerController.jumpSpeedRate = 1;
            playerController.gravity = CSEmulator.Commons.STANDARD_GRAVITY;
        }

        public void respawn()
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？
            playerController.Respawn();
        }


        public void setGravity(float gravity)
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？
            playerController.gravity = gravity;
        }

        public void setHumanoidPose(
            HumanoidPose pose
        )
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？

            var humanPose = new HumanPose();

            if (pose == null || (pose.centerPosition == null && pose.centerRotation == null && pose.muscles == null))
            {
                //ポーズリセット
                humanPose.bodyPosition = new Vector3(0, 1, 0);
                humanPose.bodyRotation = new Quaternion(0, 0, 0, 1);
                humanPose.muscles = Muscles.TPOSE.ToArray();
                poseHandler.SetHumanPose(ref humanPose);
                return;
            }

            //初期状態ではこの段階でhumanPoseにはT-Poseのmusclesが入っている。
            //musclesを全て0にすると、手を前に出した中腰ポーズになる。
            poseHandler.GetHumanPose(ref humanPose);

            //優先順位は動きを付けたら考慮
            if (pose.muscles != null)
            {
                for (var i = 0; i < pose.muscles.changed.Length; i++)
                {
                    if (!pose.muscles.changed[i]) continue;
                    humanPose.muscles[i] = pose.muscles.muscles[i];
                }
            }
            if (pose.centerPosition != null) humanPose.bodyPosition = pose.centerPosition._ToUnityEngine();
            if (pose.centerRotation != null) humanPose.bodyRotation = pose.centerRotation._ToUnityEngine();

            poseHandler.SetHumanPose(ref humanPose);
        }

        public void setJumpSpeedRate(float jumpSpeedRate)
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？
            playerController.jumpSpeedRate = jumpSpeedRate;
        }

        public void setMoveSpeedRate(float moveSpeedRate)
        {
            //CSETODO 距離チェックどうする？
            //CSETODO 実行数チェックどうする？
            playerController.moveSpeedRate = moveSpeedRate;
        }

        public void setPosition(
            EmulateVector3 position
        )
        {
            playerController.setPosition(position._ToUnityEngine());
        }

        public void setRotation(
            EmulateQuaternion rotation
        )
        {
            //Ｙ軸回転(鉛直軸)のみ
            var r = rotation._ToUnityEngine().eulerAngles;
            var y = Quaternion.Euler(0, r.y, 0);
            playerController.setRotation(y);
        }

        public override string ToString()
        {
            return string.Format("[PlayerHandle][{0}][{1}]", playerController.vrm.name, id);
        }
    }
}
