using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class VrmPreparer
    {
        readonly CckPreviewFinder previewFinder;
        readonly GameObject vrm;
        readonly Components.IPerspectiveChangeNotifier perspectiveChangeNotifier;

        public VrmPreparer(
            CckPreviewFinder previewFinder,
            GameObject vrm,
            Components.IPerspectiveChangeNotifier perspectiveChangeNotifier
        )
        {
            this.previewFinder = previewFinder;
            this.vrm = vrm;
            this.perspectiveChangeNotifier = perspectiveChangeNotifier;
        }

        public GameObject InstantiateVrm()
        {
            //挙動的にRootの下がよさそう
            var vrmInstance = GameObject.Instantiate(vrm, previewFinder.controllerRoot.transform);

            //プレビューのカメラに映ってしまうので
            var renderers = vrmInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var renderer in renderers)
            {
                renderer.gameObject.layer = 16; //OwnAvatar
            }

            var csPlayerHandler = vrmInstance.AddComponent<Components.CSEmulatorPlayerHandler>();
            var characterController = vrmInstance.GetComponentInParent<CharacterController>();
            var csPlayerController = characterController.gameObject.AddComponent<Components.CSEmulatorPlayerController>();
            var desktopPlayerControllerReflector = new DesktopPlayerControllerReflector(
                vrmInstance.GetComponentInParent<ClusterVR.CreatorKit.Preview.PlayerController.DesktopPlayerController>()
            );
            var animationController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/KaomoLab/CSEmulator/Components/Animations/Player.controller"
            );
            csPlayerController.Construct(
                new UnityCharacterController(characterController),
                animationController,
                desktopPlayerControllerReflector,
                desktopPlayerControllerReflector,
                perspectiveChangeNotifier,
                new Implements.UnityKeyInput()
            );

            //ポーズをA-Poseにする。アニメーション来たら多分不要。
            ResetAPose(csPlayerController);

            return vrmInstance;
        }

        void ResetAPose(Components.CSEmulatorPlayerController csPlayerController)
        {
            var poseHandler = new HumanPoseHandler(
                csPlayerController.animator.avatar,
                csPlayerController.animator.transform
            );
            var humanPose = new HumanPose();
            poseHandler.GetHumanPose(ref humanPose);
            humanPose.muscles = EmulateClasses.Muscles.SPOSE.ToArray();
            poseHandler.SetHumanPose(ref humanPose);
        }

    }
}
