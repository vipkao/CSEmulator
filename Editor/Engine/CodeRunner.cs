using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class CodeRunner
    {
        readonly UnityEngine.GameObject gameObject;
        readonly string code;
        public readonly Components.CSEmulatorItemHandler csItemHandler;
        readonly PrefabItemStore prefabItemStore;
        readonly EmulateClasses.EmulateClassFactory emulateClassFactory;
        readonly EmulateClasses.StateProxy stateProxy;
        readonly OnUpdateBridge onUpdateBridge;
        readonly OnUpdateBridge onFixedUpdateBridge;
        readonly ItemMessageRouter itemMessageRouter;
        readonly PlayerHandlerStore playerHandleStore;
        readonly PlayerControllerBridgeFactory playerControllerBridgeFactory;

        Jint.Engine engine;

        public CodeRunner(
            ClusterVR.CreatorKit.Item.IScriptableItem scriptableItem,
            Components.CSEmulatorItemHandler csItemHandler,
            PrefabItemStore prefabItemStore,
            ItemMessageRouter itemMessageRouter,
            PlayerHandlerStore playerHandleStore,
            PlayerControllerBridgeFactory playerControllerBridgeFactory
        )
        {
            this.gameObject = scriptableItem.Item.gameObject;
            this.csItemHandler = csItemHandler;
            this.prefabItemStore = prefabItemStore;
            this.itemMessageRouter = itemMessageRouter;
            this.playerHandleStore = playerHandleStore;
            this.playerControllerBridgeFactory = playerControllerBridgeFactory;

            code = scriptableItem.GetSourceCode(true);

            //キーはGameObjectのnameで行っている(v1を使いまわしている)ので、Item間での使いまわしは不可。
            //そのためここで各Item用にインスタンスを作っている。
            onUpdateBridge = new OnUpdateBridge();

            onFixedUpdateBridge = new OnUpdateBridge();

            emulateClassFactory = new EmulateClasses.EmulateClassFactory();

            stateProxy = emulateClassFactory.CreateDefaultStateProxy();

        }

        public void Start()
        {
            csItemHandler.OnFixedUpdate += CsItemHandler_OnFixedUpdate;

            var clusterScript = emulateClassFactory.CreateDefaultClusterScript(
                gameObject,
                onUpdateBridge,
                onFixedUpdateBridge,
                itemMessageRouter,
                itemMessageRouter,
                prefabItemStore,
                playerHandleStore,
                playerControllerBridgeFactory,
                stateProxy
            );

            engine = new Jint.Engine();
            engine.SetValue("$", clusterScript);
            SetClass<EmulateClasses.EmulateVector2>(engine, "Vector2");
            SetClass<EmulateClasses.EmulateVector3>(engine, "Vector3");
            SetClass<EmulateClasses.EmulateQuaternion>(engine, "Quaternion");
            SetClass<EmulateClasses.HumanoidBone>(engine, "HumanoidBone");
            SetClass<EmulateClasses.HumanoidPose>(engine, "HumanoidPose");
            SetClass<EmulateClasses.Muscles>(engine, "Muscles");
            SetClass<EmulateClasses.ItemTemplateId>(engine, "ItemTemplateId");

            try
            {
                engine.Execute(code);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject.name);
            }
        }
        private void CsItemHandler_OnFixedUpdate()
        {
            onFixedUpdateBridge.InvokeUpdate();
        }

        void SetClass<T>(Jint.Engine engine, string name)
        {
            engine.SetValue(
                name, GetTypeReference<T>(engine)
            );
        }
        Jint.Runtime.Interop.TypeReference GetTypeReference<T>(
            Jint.Engine engine
        )
        {
            return Jint.Runtime.Interop.TypeReference.CreateTypeReference(
                engine,
                typeof(T)
            );
        }

        public void Update()
        {
            onUpdateBridge.InvokeUpdate();
        }


        public void Restart()
        {
            Shutdown();
            Start();
        }

        public void Shutdown()
        {
            csItemHandler.OnFixedUpdate -= CsItemHandler_OnFixedUpdate;
            engine.Dispose();
        }
    }
}
