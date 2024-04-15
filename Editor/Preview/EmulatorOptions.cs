using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class EmulatorOptions
    {
        public event Handler OnChangedFps = delegate { };
        public event Handler OnChangedPerspective = delegate { };

        const string PrefsKeyEnable = "KaomoCSEmulator_enable";
        public bool enable {
            get => PlayerPrefs.GetInt(PrefsKeyEnable, 1) == 1;
            set => PlayerPrefs.SetInt(PrefsKeyEnable, value ? 1 : 0);
        }


        const string PrefsKeyFps = "KaomoCSEmulator_fps";
        public enum FpsLimit : int
        {
            unlimited,
            limit90,
            limit30
        };
        public FpsLimit fps
        {
            get => (FpsLimit)PlayerPrefs.GetInt(PrefsKeyFps, (int)FpsLimit.limit90);
            set {
                PlayerPrefs.SetInt(PrefsKeyFps, (int)value);
                OnChangedFps.Invoke();
            }
        }

        const string PrefsKeyFirstPersonPerspective = "KaomoCSEmulator_firstPersonPerspective";
        public bool perspective
        {
            get => PlayerPrefs.GetInt(PrefsKeyFirstPersonPerspective, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsKeyFirstPersonPerspective, value ? 1 : 0);
                OnChangedPerspective.Invoke();
            }
        }

        const string PrefsKeyVrm = "KaomoCSEmulator_vrm";
        public const string DefaultVrmPath = "Assets/KaomoLab/CSEmulator/VRM/CSEmulatorDummyHumanoid.prefab";
        public GameObject vrm
        {
            get
            {
                var path = PlayerPrefs.GetString(PrefsKeyVrm, DefaultVrmPath);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return prefab;
            }
            set
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(value);
                if (path == "") path = DefaultVrmPath;
                PlayerPrefs.SetString(PrefsKeyVrm, path);
            }
        }

        const string PrefsKeyUserId = "KaomoCSEmulator_userId";
        public readonly Regex userIdPattern = new Regex(".*");
        public string userId
        {
            get => PlayerPrefs.GetString(PrefsKeyUserId, "");
            set => PlayerPrefs.SetString(PrefsKeyUserId, value);
        }
        const string PrefsKeyUserName = "KaomoCSEmulator_userName";
        public string userName
        {
            get => PlayerPrefs.GetString(PrefsKeyUserName, "");
            set => PlayerPrefs.SetString(PrefsKeyUserName, value);
        }
        const string PrefsKeyExists = "KaomoCSEmulator_exists";
        public bool exists
        {
            get => PlayerPrefs.GetInt(PrefsKeyExists, 1) == 1;
            set => PlayerPrefs.SetInt(PrefsKeyExists, value ? 1 : 0);
        }

        const string PrefsKeyCallExternalUrl = "KaomoCSEmulator_callExternalUrl";
        public string callExternalUrl
        {
            get => PlayerPrefs.GetString(PrefsKeyCallExternalUrl, "");
            set => PlayerPrefs.SetString(PrefsKeyCallExternalUrl, value);
        }

        const string PrefsKeyDebug = "KaomoCSEmulator_debug";
        public bool debug
        {
            get => PlayerPrefs.GetInt(PrefsKeyDebug, 0) == 1;
            set => PlayerPrefs.SetInt(PrefsKeyDebug, value ? 1 : 0);
        }



        public EmulatorOptions()
        {
        }

        public bool IsVrmPrefab(GameObject gameObject)
        {
            if (gameObject == null) return false;
            //UniVRMを入れていないと型名をコンパイルできずにエラーになるため
            //UnityではGetTypeする時はアセンブリ名(DLL名)も併せて必要
            var vrmMetaType = Type.GetType("VRM.VRMMeta, VRM");
            if (vrmMetaType == null) return false;
            if (null == gameObject.GetComponent(vrmMetaType)) return false;

            return true;
        }
    }
}
