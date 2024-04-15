using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.KaomoLab.CSEmulator.Editor.Preview;

namespace Assets.KaomoLab.CSEmulator.Editor.Window
{
    public class EmulatorOptionsWindow : EditorWindow
    {
        const string UNIVRAM_PACKAGE = "Assets/VRM/package.json";
        bool isValidUniVrm = false;

        private Vector2 scroll = Vector2.zero;


        [System.Serializable]
        public class UniVrmPackage
        {
            public string version;
        }


        [MenuItem("Window/かおもラボ/CSEmulator")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<EmulatorOptionsWindow>(false, "CSEmulator");
        }

        void OnGUI()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Texture>("Assets/KaomoLab/CSEmulator/Editor/Window/logo.png");
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(new GUIContent(logo), GUILayout.Height(30));
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);


            CheckUniVrmVersion();

            var op = Bootstrap.options;
            op.fps = (EmulatorOptions.FpsLimit)EditorGUILayout.EnumPopup("FPSを制限する。", op.fps);
            EditorGUILayout.HelpBox(
                "環境によっては$.onUpdateに対してFPS制限が働きません。", MessageType.Info
            );
            //if (QualitySettings.vSyncCount > 0)
            //{
            //    EditorGUILayout.HelpBox(
            //        "VSYNCが有効なので、$.onUpdateに対してFPS制限が働きません。", MessageType.Warning
            //    );
            //}
            op.perspective = EditorGUILayout.Toggle("一人称視点", op.perspective);
            EditorGUILayout.HelpBox(
                "現在はVRMモデルの挙動のみに影響します。視点は一人称のままです。", MessageType.Info
            );

            EditorGUILayout.LabelField("PlayerHandle");
            op.exists = EditorGUILayout.Toggle(new GUIContent("　.exists", "PlayerHandle.existsの値を指定できます。"), op.exists);
            op.userIdfc = EditorGUILayout.TextField(new GUIContent("　.idfc", "PlayerHandle.idfcの値を指定できます。"), op.userIdfc);
            if (!op.userIdfcPattern.IsMatch(op.userIdfc))
            {
                EditorGUILayout.HelpBox(
                    "idfcの形式ではありません。", MessageType.Warning
                );
            }
            if (op.userIdfc == "")
                op.userIdfc = op.DefaultUserIdfc;
            op.userId = EditorGUILayout.TextField(new GUIContent("　.userId", "PlayerHandle.userIdの値を指定できます。"), op.userId);
            if (!op.userIdPattern.IsMatch(op.userId))
            {
                EditorGUILayout.HelpBox(
                    "userIdの形式ではありません。", MessageType.Warning
                );
            }
            if(op.userId == "")
                op.userId = op.DefaultUserId;
            op.userName = EditorGUILayout.TextField(new GUIContent("　.userDisplayName", "\"PlayerHandle.userDisplayNameの値を指定できます。"), op.userName);
            if (op.userName == "")
                op.userName = op.DefaultUserName;
            EditorGUILayout.Separator();

            op.callExternalUrl = EditorGUILayout.TextField(new GUIContent("callExternal用URL"), op.callExternalUrl);
            op.limitExternalCall = (EmulateClasses.CallExternalRateLimit)EditorGUILayout.EnumPopup(new GUIContent("　実行回数制限を行う", "連続してテストしたい場合などでOFFにしてください。"), op.limitExternalCall);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("以下はプレビュー開始前に設定してください。");
            op.enable = EditorGUILayout.Toggle("ClusterScriptを実行する。", op.enable);
            op.vrm = (GameObject)EditorGUILayout.ObjectField("動作確認用のVRM", op.vrm, typeof(GameObject), false);
            if (!op.IsVrmPrefab(op.vrm))
            {
                EditorGUILayout.HelpBox(
                    "VRMのPrefabを指定してください。", MessageType.Error
                );
            }
            op.debug = EditorGUILayout.Toggle("デバッグモードで実行する。", op.debug);
            EditorGUILayout.LabelField("　動作が遅くなりますが、ログ出力が詳細になります。");


            EditorGUILayout.EndScrollView();
        }

        void CheckUniVrmVersion()
        {
            if (!isValidUniVrm)
            {
                if (!System.IO.File.Exists(UNIVRAM_PACKAGE))
                {
                    //Debug.Logで出すと出すぎる。出すなら工夫が必要。
                    EditorGUILayout.HelpBox(
                        "UniVRM 0.61.1が必要です。",
                        MessageType.Error
                    );
                }
                else
                {
                    var packageJson = System.IO.File.ReadAllText("Assets/VRM/package.json");
                    var package = JsonUtility.FromJson<UniVrmPackage>(packageJson);

                    if (package.version != "0.61.1")
                    {
                        //Debug.Logで出すと出すぎる。出すなら工夫が必要。
                        EditorGUILayout.HelpBox(
                            "UniVRM 0.61.1が必要です。",
                            MessageType.Error
                        );
                    }
                    else
                    {
                        isValidUniVrm = true;
                    }
                }
            }
        }

    }
}
