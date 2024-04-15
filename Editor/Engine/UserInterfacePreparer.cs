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
        UIs.TextInput.Handler uiTextInput = null;

        public UserInterfacePreparer(
            CckPreviewFinder previewFinder
        )
        {
            this.previewFinder = previewFinder;
        }

        T LoadHandler<T>(string path)
        {
            var ret = GameObject.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(path),
                previewFinder.previewRoot.transform
            ).GetComponent<T>();
            return ret;
        }

        public bool isTextInputting => uiTextInput != null ? uiTextInput.isInputting : false;

        public void StartTextInput(string caption, Action<string> SendCallback, Action CancelCallback, Action BusyCallback)
        {
            //都度Instantiateする方法に変更した。
            //インスタンスを残していると「Destroy may not be called from edit mode!」が出るため。
            //なお、ExitingPlayModeでDestroyImmidiateしてもタイミングのよってはエラーが出てしまう。
            uiTextInput = LoadHandler<UIs.TextInput.Handler>(
                "Assets/KaomoLab/CSEmulator/UIs/TextInput/View.prefab"
            );

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
                    GameObject.DestroyImmediate(uiTextInput.gameObject);
                    uiTextInput = null;
                    SendCallback(text);
                },
                () => {
                    image.raycastTarget = true;
                    controller.enabled = true;
                    GameObject.DestroyImmediate(uiTextInput.gameObject);
                    uiTextInput = null;
                    CancelCallback();
                },
                () => {
                    image.raycastTarget = true;
                    controller.enabled = true;
                    GameObject.DestroyImmediate(uiTextInput.gameObject);
                    uiTextInput = null;
                    BusyCallback();
                }
            );
        }

    }
}
