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
            var worldRuntimeSettings = ClusterVR.CreatorKit.Editor.Builder.WorldRuntimeSettingGatherer.GatherWorldRuntimeSettings(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            ).FirstOrDefault();
            csPlayerController.Construct(
                new Components.CSEmulatorPlayerController.MovingPlatformSettings(
                    worldRuntimeSettings
                ),
                new UnityCharacterController(characterController),
                animationController,
                desktopPlayerControllerReflector,
                desktopPlayerControllerReflector,
                desktopPlayerControllerReflector,
                perspectiveChangeNotifier,
                new Implements.UnityKeyInput()
            );

            //ポーズをA-Poseにする。アニメーション来たら多分不要。
            ResetAPose(csPlayerController);

            BuildPostProcess(previewFinder.previewRoot);

            return vrmInstance;
        }

        void BuildPostProcess(GameObject root)
        {
            var postProcessObject = new GameObject("CSEmulatorPostProcess");
            postProcessObject.layer = 21;
            postProcessObject.transform.parent = root.transform;
            var postProcessVolume = postProcessObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 100;
            //プロファイルは実行時に変更するとロールバックされないのでCreateInstanceで追加
            var postProcessProfile = ScriptableObject.CreateInstance<UnityEngine.Rendering.PostProcessing.PostProcessProfile>();
            //activeだと未設定でも有効になる効果がある(DepthOfFieldとか)のでfalse指定をしている。
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.Bloom>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.ChromaticAberration>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.DepthOfField>().active = false;
            //FogはRenderSettings？
            //RenderSettingsは実行時に上書きするとロールバックされるのでそのままいじってよし
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.Grain>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.LensDistortion>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.MotionBlur>().active = false;
            postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.Vignette>().active = false;
            postProcessVolume.profile = postProcessProfile;
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
