using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using CCKBootstrap = ClusterVR.CreatorKit.Editor.Preview.Bootstrap;


namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    [InitializeOnLoad]
    public static class Bootstrap
    {
        const string TickerPrefab = "Assets/KaomoLab/CSEmulator/Editor/Preview/CSEmulatorTicker.prefab";

        public static EngineFacade engine = null;
        public static EmulatorOptions options = new EmulatorOptions();
        public static OptionBridge optionBridge = new OptionBridge(options);
        public static GameObject ticker = null;

        static bool isInPlayMode = false;

        //コンパイル後に１回。PlayModeに入る前に１回。
        static Bootstrap()
        {
            EditorApplication.playModeStateChanged += playMode =>
            {
                OnChangePlayMode(playMode);
            };
            options.OnChangedFps += Options_OnChangedFps;
        }

        static void OnChangePlayMode(PlayModeStateChange playMode)
        {
            if (playMode == PlayModeStateChange.EnteredPlayMode) EnteredPlayMode();
            if (playMode == PlayModeStateChange.ExitingPlayMode) ExitingPlayMode();
        }



        static bool IsInitializedCCK()
        {
            return CCKBootstrap.RoomStateRepository != null;
        }

        static void EnteredPlayMode()
        {
            isInPlayMode = true;
            if (IsInitializedCCK())
            {
                StartCSEmulator();
                return;
            }
            CCKBootstrap.OnInitializedEvent += CCKBootstrap_OnInitializedEvent;
        }

        static void CCKBootstrap_OnInitializedEvent()
        {
            CCKBootstrap.OnInitializedEvent -= CCKBootstrap_OnInitializedEvent;
            if (!isInPlayMode)
            {
                //asyncで動いているパッケージチェックが重く、
                //プレビューが開始しないからといって中止すると
                //EditModeに戻ってからパッケージチェックが終わりOnIntializedが発火する。
                //これを防ぐための実装。
                Debug.LogWarning("Check unnecessary [PreviewOnly] object.");
                return;
            }
            StartCSEmulator();
        }

        static void ExitingPlayMode()
        {
            isInPlayMode = false;
            ShutdownCSEmulator();
        }


        static void StartCSEmulator()
        {
            ShutdownCSEmulator();
            ApplyFpsLimit(); //念のため
            engine = new EngineFacadeFactory(optionBridge).CreateDefault();
            engine.Start();
            ticker = GameObject.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(TickerPrefab));
            ticker.GetComponent<CSEmulator.Components.CSEmulatorTicker>().OnUpdate += OnUpdate;
        }

        static void ShutdownCSEmulator()
        {
            if (ticker != null)
            {
                ticker.GetComponent<CSEmulator.Components.CSEmulatorTicker>().OnUpdate -= OnUpdate;
                GameObject.DestroyImmediate(ticker);
            }
            ticker = null;
            engine?.Shutdown();
            engine = null;
        }

        static void OnUpdate()
        {
            UpdateCSEmulator();
        }

        static void UpdateCSEmulator()
        {
            engine.Update();
        }

        private static void Options_OnChangedFps()
        {
            ApplyFpsLimit();
        }

        static void ApplyFpsLimit()
        {
            //VSYNC変えてもUpdateに変化がない？FixedUpdateはclusterとUnityで同じ。
            UnityEngine.Application.targetFrameRate = options.fps switch
            {
                EmulatorOptions.FpsLimit.unlimited => -1,
                EmulatorOptions.FpsLimit.limit90 => 90,
                EmulatorOptions.FpsLimit.limit30 => 30,
                _ => throw new InvalidOperationException("Invalid FpsLimit"),
            };
        }
    }
}
