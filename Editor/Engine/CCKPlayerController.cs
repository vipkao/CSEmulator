using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using ClusterVR.CreatorKit.Preview.PlayerController;
using ClusterVR.CreatorKit.Editor.Preview.World;
using ICckPlayerController = ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class CCKPlayerController
        : EmulateClasses.IPlayerController
    {
        readonly Components.CSEmulatorPlayerHandler csPlayerHandler;
        readonly Components.CSEmulatorPlayerController csPlayerController;
        readonly ICckPlayerController playerController;
        readonly SpawnPointManager spawnPointManager;

        public string id => csPlayerHandler.id;

        public Transform transform => playerController.PlayerTransform;

        //playerのswapn機能を追加して消去機能まで追加したらfalseにするようにする。
        public bool exists => true;

        public Animator animator => csPlayerController.animator;

        public GameObject vrm => csPlayerHandler.vrm;

        public float jumpSpeedRate
        {
            set => playerController.SetJumpSpeedRate(value);
        }
        public float moveSpeedRate
        {
            set => playerController.SetMoveSpeedRate(value);
        }

        public float gravity
        {
            get => csPlayerController.gravity;
            set => csPlayerController.gravity = value;
        }

        public PermissionType permissionType = PermissionType.Audience;

        public CCKPlayerController(
            Components.CSEmulatorPlayerHandler csPlayerHandler,
            Components.CSEmulatorPlayerController csPlayerController,
            ICckPlayerController playerController,
            SpawnPointManager spawnPointManager
        )
        {
            this.csPlayerHandler = csPlayerHandler;
            this.csPlayerController = csPlayerController;
            this.playerController = playerController;
            this.spawnPointManager = spawnPointManager;
        }

        public void Respawn()
        {
            //AudienceかPerformerかを変えるのが必要。
            var spawnPoint = spawnPointManager.GetRespawnPoint(permissionType);
            playerController.WarpTo(spawnPoint.Position);

            //PlayerPresenterがPlayerが一人のみ設計のようなのでコピペして引き取り。
            var yawOnlyRotation = Quaternion.Euler(0f, spawnPoint.YRotation, 0f);
            playerController.SetRotationKeepingHeadPitch(yawOnlyRotation);
            playerController.ResetCameraRotation(yawOnlyRotation);
        }

        public void AddVelocity(Vector3 velocity)
        {
            csPlayerController.AddVelocity(velocity);
        }

        public void SetPosition(Vector3 position)
        {
            playerController.WarpTo(position);
        }

        public void SetRotation(Quaternion rotation)
        {
            csPlayerController.ForceForward();
            playerController.SetRotationKeepingHeadPitch(rotation);
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
        public Quaternion GetRotation()
        {
            var ret = csPlayerController.ApplyDirection(playerController.RootRotation);
            return ret;
        }

        public void SetHumanPosition(Vector3? position)
        {
            csPlayerController.poseManager.SetPosition(position);
        }

        public void SetHumanRotation(Quaternion? rotation)
        {
            csPlayerController.poseManager.SetRotation(rotation);
        }

        public void SetHumanMuscles(float[] muscles, bool[] hasMascles)
        {
            csPlayerController.poseManager.SetMuscles(muscles, hasMascles);
        }

        public void InvalidateHumanMuscles()
        {
            csPlayerController.poseManager.InvalidateMuscles();
        }

        public void SetHumanTransition(double timeoutSeconds, double timeoutTransitionSeconds, double transitionSeconds)
        {
            csPlayerController.poseManager.SetHumanTransition(timeoutSeconds, timeoutTransitionSeconds, transitionSeconds);
        }

        public void InvalidateHumanTransition()
        {
            csPlayerController.poseManager.InvalidateHumanTransition();
        }

        public HumanPose GetHumanPose()
        {
            return csPlayerController.poseManager.GetHumanPose();
        }

        public void ChangeGrabbing(bool isGrab)
        {
            csPlayerController.ChangeGrabbing(isGrab);
        }
        public void ChangePerspective(bool isFirstPerson)
        {
            csPlayerController.ChangePerspective(isFirstPerson);
        }
    }
}
