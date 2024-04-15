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
        readonly GameObject vrm;

        public VrmPreparer(
            GameObject vrm
        )
        {
            this.vrm = vrm;
        }

        public GameObject InstantiateVrm()
        {
            var controllers = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(
                    //PreviewOnlyの中にあるキャラを操作するアレ。
                    o => o.GetComponentsInChildren<ClusterVR.CreatorKit.Preview.PlayerController.DesktopPlayerController>(true)
                );
            if(controllers.Count() > 1)
            {
                UnityEngine.Debug.LogWarning("[PreviewOnly] exists multiple.");
            }

            //とりあえずLastで。
            var controller = controllers.Last();

            //挙動的にRootの下がよさそう
            var vrmInstance = GameObject.Instantiate(vrm, controller.transform.Find("Root"));

            //プレビューのカメラに映ってしまうので
            var renderers = vrmInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var renderer in renderers)
            {
                renderer.gameObject.layer = 16; //OwnAvatar
            }

            var csPlayerHandler = vrmInstance.AddComponent<Components.CSEmulatorPlayerHandler>();
            var characterController = vrmInstance.GetComponentInParent<CharacterController>();
            var csPlayerController = characterController.gameObject.AddComponent<Components.CSEmulatorPlayerController>();
            csPlayerController.Construct(characterController);

            return vrmInstance;
        }

    }
}
