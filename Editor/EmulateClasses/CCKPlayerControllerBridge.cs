using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using ClusterVR.CreatorKit.Preview.PlayerController;
using ClusterVR.CreatorKit.Editor.Preview.World;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class CCKPlayerControllerBridge
        : IPlayerController
    {
        readonly Components.CSEmulatorPlayerHandler csPlayerHandler;
        readonly DesktopPlayerController playerController;
        readonly SpawnPointManager spawnPointManager;

        public string id => csPlayerHandler.id;

        public Transform transform
        {
            get => playerController.transform;
        }

        //playerのswapn機能を追加して消去機能まで追加したらfalseにするようにする。
        public bool exists => true;

        public Animator animator
        {
            get
            {
                if (_animator == null)
                    _animator = csPlayerHandler.gameObject.GetComponent<Animator>();
                return _animator;
            }
        }
        Animator _animator = null;

        public GameObject vrm => csPlayerHandler.vrm;

        public float jumpSpeedRate
        {
            set => ((ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController)playerController).SetJumpSpeedRate(value);
        }
        public float moveSpeedRate
        {
            set => ((ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController)playerController).SetMoveSpeedRate(value);
        }

        public float gravity { get; set; } = CSEmulator.Commons.STANDARD_GRAVITY;

        public PermissionType permissionType = PermissionType.Audience;

        FieldInfo velocityY_controller;
        MethodInfo SetRotation_controller;

        public CCKPlayerControllerBridge(
            Components.CSEmulatorPlayerHandler csPlayerHandler,
            DesktopPlayerController playerController,
            SpawnPointManager spawnPointManager
        )
        {
            this.csPlayerHandler = csPlayerHandler;
            this.playerController = playerController;
            this.spawnPointManager = spawnPointManager;

            csPlayerHandler.OnUpdate += CsPlayerHandler_OnUpdate;

            velocityY_controller = typeof(DesktopPlayerController)
                .GetField("velocityY", BindingFlags.NonPublic | BindingFlags.Instance);
            SetRotation_controller = typeof(DesktopPlayerController)
                .GetMethod("SetRotation", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void CsPlayerHandler_OnUpdate()
        {
            if (gravity != CSEmulator.Commons.STANDARD_GRAVITY)
            {
                //DesktopPlayerControllerの重力加速度が決め打ちなので
                var delta = Time.deltaTime * (gravity - CSEmulator.Commons.STANDARD_GRAVITY);
                var v = (float)velocityY_controller.GetValue(playerController);
                velocityY_controller.SetValue(playerController, v + delta);
            }
        }

        public void Respawn()
        {
            //AudienceかPerformerかを変えるのが必要。
            var spawnPoint = spawnPointManager.GetRespawnPoint(permissionType);
            playerController.WarpTo(spawnPoint.Position);

            //PlayerPresenterがPlayerが一人のみ設計のようなのでコピペして引き取り。
            var yawOnlyRotation = Quaternion.Euler(0f, spawnPoint.YRotation, 0f);
            ((ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController)playerController)
                .SetRotationKeepingHeadPitch(yawOnlyRotation);
            ((ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController)playerController)
                .ResetCameraRotation(yawOnlyRotation);
        }

        public void setPosition(Vector3 position)
        {
            playerController.WarpTo(position);
        }

        public void setRotation(Quaternion rotation)
        {
            ((ClusterVR.CreatorKit.Preview.PlayerController.IPlayerController)playerController)
                .SetRotationKeepingHeadPitch(rotation);
        }

        public Vector3 getPosition()
        {
            return playerController.transform.position;
        }
        public Quaternion getRotation()
        {
            return playerController.transform.Find("Root").transform.rotation;
        }
    }
}
