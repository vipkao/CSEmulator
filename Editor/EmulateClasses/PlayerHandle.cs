using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class PlayerHandle
        : ISendableSize
    {
        readonly IPlayerMeta playerMeta;
        readonly IPlayerController playerController;
        readonly IUserInterfaceHandler userInterfaceHandler;
        readonly ITextInputSender textInputSender;
        readonly IPostProcessApplier postEffectApplier;
        //ownerの切り替えにはnewを強制しておきたいためprivate
        readonly Components.CSEmulatorItemHandler csOwnerItemHandler;

        //csOwnerItemHandlerとはこのハンドルがいるスクリプト空間($)のこと。
        public PlayerHandle(
            IPlayerMeta playerMeta,
            IPlayerController playerController,
            IUserInterfaceHandler userInterfaceHandler,
            ITextInputSender textInputSender,
            IPostProcessApplier postEffectApplier,
            Components.CSEmulatorItemHandler csOwnerItemHandler
        )
        {
            this.playerMeta = playerMeta;
            this.playerController = playerController;
            this.userInterfaceHandler = userInterfaceHandler;
            this.textInputSender = textInputSender;
            this.postEffectApplier = postEffectApplier;
            this.csOwnerItemHandler = csOwnerItemHandler;
        }
        public PlayerHandle(
            PlayerHandle source,
            Components.CSEmulatorItemHandler csOwnerItemHandler
        )
        {
            this.playerMeta = source.playerMeta;
            this.playerController = source.playerController;
            this.userInterfaceHandler = source.userInterfaceHandler;
            this.textInputSender = source.textInputSender;
            this.csOwnerItemHandler = csOwnerItemHandler;
        }

        public string id => playerController.id;

        public string idfc => playerMeta.exists ? playerMeta.userIdfc : null;
        public string userId => playerMeta.exists ? playerMeta.userId : null;
        public string userDisplayName => playerMeta.exists ? playerMeta.userDisplayName : null;

        public void addVelocity(EmulateVector3 velocity)
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            playerController.AddVelocity(velocity._ToUnityEngine());
        }

        public bool exists()
        {
            return playerMeta.exists ? playerController.exists : false;
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

        public void requestTextInput(string meta, string title)
        {
            if (userInterfaceHandler.isTextInputting)
            {
                textInputSender.Send(csOwnerItemHandler.item.Id.Value, "", meta, TextInputStatus.Busy);
                return;
            }
            //呼び出し時のownerのidを保持しておく
            var id = csOwnerItemHandler.item.Id.Value;
            userInterfaceHandler.StartTextInput(
                title,
                text => textInputSender.Send(id, text, meta, TextInputStatus.Success),
                () => textInputSender.Send(id, "", meta, TextInputStatus.Refused),
                () => textInputSender.Send(id, "", meta, TextInputStatus.Busy)
            );
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
            HumanoidPose pose,
            SetHumanoidPoseOption setHumanoidPoseOption = null
        )
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            if(pose == null)
            {
                playerController.SetHumanPosition(null);
                playerController.SetHumanRotation(null);
                playerController.InvalidateHumanMuscles();
                playerController.InvalidateHumanTransition();
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

            if(setHumanoidPoseOption == null)
            {
                playerController.InvalidateHumanTransition();
            }
            else
            {
                var op = setHumanoidPoseOption.Nomalized();
                playerController.SetHumanTransition(
                    op.timeoutSeconds,
                    op.timeoutTransitionSeconds,
                    op.transitionSeconds
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

        public void setPostProcessEffects(
            PostProcessEffects effects
        )
        {
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            if (effects != null)
            {
                postEffectApplier.Apply(effects.bloom);
                postEffectApplier.Apply(effects.chromaticAberration);
                postEffectApplier.Apply(effects.colorGrading);
                postEffectApplier.Apply(effects.depthOfField);
                postEffectApplier.Apply(effects.fog);
                postEffectApplier.Apply(effects.grain);
                postEffectApplier.Apply(effects.lensDistortion);
                postEffectApplier.Apply(effects.motionBlur);
                postEffectApplier.Apply(effects.vignette);
            }
            else
            {
                postEffectApplier.Apply(new BloomSettings());
                postEffectApplier.Apply(new ChromaticAberrationSettings());
                postEffectApplier.Apply(new ColorGradingSettings());
                postEffectApplier.Apply(new DepthOfFieldSettings());
                postEffectApplier.Apply(new FogSettings());
                postEffectApplier.Apply(new GrainSettings());
                postEffectApplier.Apply(new LensDistortionSettings());
                postEffectApplier.Apply(new MotionBlurSettings());
                postEffectApplier.Apply(new VignetteSettings());
            }
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

        void CheckOwnerDistanceLimit()
        {
            var p1 = playerController.GetPosition();
            var p2 = csOwnerItemHandler.gameObject.transform.position;
            var d = UnityEngine.Vector3.Distance(p1, p2);
            //30メートル以内はOK
            if (d <= 30f) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateDistanceLimitExceeded(
                String.Format("[{0}]>>>[Player]", csOwnerItemHandler)
            );
        }
        void CheckOwnerOperationLimit()
        {
            var result = csOwnerItemHandler.TryPlayerOperate();
            if (result) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]>>>[Player]", csOwnerItemHandler)
            );
        }

        public void _ChangeGrabbing(bool isGrab)
        {
            playerController.ChangeGrabbing(isGrab);
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.id = id;
            o.userId = userId;
            o.userDisplayName = userDisplayName;
            return o;
        }
        public override string ToString()
        {
            var vrm = playerController.vrm;
            return string.Format("[PlayerHandle][{0}][{1}]", vrm == null ? null : vrm.name, id);
        }

        public int GetSize()
        {
            //2キャラで確認おそらく固定
            return 40;
        }
    }
}
