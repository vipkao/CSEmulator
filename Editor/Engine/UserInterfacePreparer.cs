using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class UserInterfacePreparer
        : EmulateClasses.IUserInterfaceHandler
    {
        readonly CckPreviewFinder previewFinder;
        readonly UIs.TextInput.Handler uiTextInput;

        public UserInterfacePreparer(
            CckPreviewFinder previewFinder
        )
        {
            this.previewFinder = previewFinder;
            uiTextInput = LoadHandler<UIs.TextInput.Handler>(
                "Assets/KaomoLab/CSEmulator/UIs/TextInput/View.prefab"
            );
        }

        T LoadHandler<T>(string path)
        {
            var ret = GameObject.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(path),
                previewFinder.previewRoot.transform
            ).GetComponent<T>();
            return ret;
        }

        public bool isTextInputting => uiTextInput.isInputting;

        public void StartTextInput(string caption, Action<string> SendCallback, Action CancelCallback, Action BusyCallback)
        {
            var image = previewFinder.panel.GetComponent<UnityEngine.UI.Image>();
            var controller = previewFinder.controller.GetComponent<ClusterVR.CreatorKit.Preview.PlayerController.DesktopPlayerController>();
            //ボタンクリックを拾わなくなるので
            image.raycastTarget = false;
            //WASDで移動してしまうので
            controller.enabled = false;

            uiTextInput.caption = caption;
            uiTextInput.text = "";
            uiTextInput.StartInput(
                text => {
                    image.raycastTarget = true;
                    controller.enabled = true;
                    SendCallback(text);
                },
                () => {
                    image.raycastTarget = true;
                    controller.enabled = true;
                    CancelCallback();
                },
                () => {
                    image.raycastTarget = true;
                    controller.enabled = true;
                    BusyCallback();
                }
            );
        }

    }
}
