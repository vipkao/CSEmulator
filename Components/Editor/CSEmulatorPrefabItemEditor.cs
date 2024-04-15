using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Assets.KaomoLab.CSEmulator.Components.Editor
{
    [CustomEditor(typeof(CSEmulatorPrefabItem))]
    public class CSEmulatorPrefabItemEditor
         : UnityEditor.Editor
    {
        const string UUID_CAPTION = @"UUIDとは「7865f52c-1305-4489-b780-c3562109e5e8」というような文字列です。
クラフトアイテムのURLの末尾にある文字列がUUIDです。";


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var id = serializedObject.FindProperty("_id");
            EditorGUILayout.PropertyField(
                id, new GUIContent("UUID")
            );
            if (id.stringValue == "")
            {
                EditorGUILayout.HelpBox(
                    "UUIDを入力してください。\n\n" + UUID_CAPTION,
                    MessageType.Warning
                );
            }
            else if (!KaomoLab.CSEmulator.Commons.IsUUID(id.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "UUIDの形式ではありません。\n\n" + UUID_CAPTION,
                    MessageType.Error
                );
            }


            serializedObject.ApplyModifiedProperties();

        }
    }
}
