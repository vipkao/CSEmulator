using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class CodeRunner
    {
        //Startのタイミングが任意であるため、インスタンスメンバをできる限りnullにしたくないという思想。
        public class CodeEvaluater
            : EmulateClasses.ICodeEvaluater
        {
            readonly GameObject gameObject;
            readonly Jint.Engine engine;

            public CodeEvaluater(
                GameObject gameObject,
                Jint.Engine engine
            )
            {
                this.gameObject = gameObject;
                this.engine = engine;
            }

            public void Evaluate(string code)
            {
                try
                {
                    engine.Execute(code);
                }
                catch (Exception e)
                {
                    Commons.ExceptionLogger(e, gameObject);
                }
            }
        }

        readonly UnityEngine.GameObject gameObject;
        readonly string code;
        public readonly Components.CSEmulatorItemHandler csItemHandler;
        readonly PrefabItemStore prefabItemStore;
        readonly EmulateClasses.EmulateClassFactory emulateClassFactory;
        readonly EmulateClasses.StateProxy stateProxy;
        readonly OnUpdateBridge onUpdateBridge;
        readonly OnUpdateBridge onFixedUpdateBridge;
        readonly ItemMessageRouter itemMessageRouter;
        readonly TextInputRouter textInputRouter;
        readonly PlayerHandlerStore playerHandleStore;
        readonly PlayerControllerBridgeFactory playerControllerBridgeFactory;
        readonly UserInterfacePreparer userInterfacePreparer;
        readonly IRunnerOptions options;
        readonly ILoggerFactory loggerFactory;

        List<Action> shutdownActions = new List<Action>();
        bool isRunning = false;

        public CodeRunner(
            ClusterVR.CreatorKit.Item.IScriptableItem scriptableItem,
            Components.CSEmulatorItemHandler csItemHandler,
            PrefabItemStore prefabItemStore,
            ItemMessageRouter itemMessageRouter,
            TextInputRouter textInputRouter,
            PlayerHandlerStore playerHandleStore,
            PlayerControllerBridgeFactory playerControllerBridgeFactory,
            UserInterfacePreparer userInterfacePreparer,
            IRunnerOptions options,
            ILoggerFactory loggerFactory
        )
        {
            this.gameObject = scriptableItem.Item.gameObject;
            this.csItemHandler = csItemHandler;
            this.prefabItemStore = prefabItemStore;
            this.itemMessageRouter = itemMessageRouter;
            this.textInputRouter = textInputRouter;
            this.playerHandleStore = playerHandleStore;
            this.playerControllerBridgeFactory = playerControllerBridgeFactory;
            this.userInterfacePreparer = userInterfacePreparer;
            this.options = options;
            this.loggerFactory = loggerFactory;

            code = scriptableItem.GetSourceCode(true);

            //キーはGameObjectのnameで行っている(v1を使いまわしている)ので、Item間での使いまわしは不可。
            //そのためここで各Item用にインスタンスを作っている。
            onUpdateBridge = new OnUpdateBridge();

            onFixedUpdateBridge = new OnUpdateBridge();

            emulateClassFactory = new EmulateClasses.EmulateClassFactory();

            stateProxy = new EmulateClasses.StateProxy();

        }

        public void Start()
        {
            csItemHandler.OnFixedUpdate += CsItemHandler_OnFixedUpdate;
            shutdownActions.Add(() => csItemHandler.OnFixedUpdate -= CsItemHandler_OnFixedUpdate);

            var engineOptions = new Jint.Options();
            if(options.isDebug)
                engineOptions.Debugger.Enabled = true;
            var engine = new Jint.Engine(engineOptions);
            shutdownActions.Add(() => engine.Dispose());

            var logger = loggerFactory.Create(new JintProgramStatus(engine));
            var exceptionFactory = new ByEngineExceptionFactory(engine);
            csItemHandler.itemExceptionFactory = exceptionFactory;
            var codeEvaluater = new CodeEvaluater(gameObject, engine);

            var clusterScript = emulateClassFactory.CreateDefaultClusterScript(
                gameObject,
                onUpdateBridge,
                onFixedUpdateBridge,
                itemMessageRouter,
                itemMessageRouter,
                textInputRouter,
                textInputRouter,
                prefabItemStore,
                playerHandleStore,
                playerControllerBridgeFactory,
                exceptionFactory,
                userInterfacePreparer,
                codeEvaluater,
                stateProxy,
                logger
            );
            shutdownActions.Add(() => clusterScript.Shutdown());

            engine.SetValue("$", clusterScript);
            SetClass<EmulateClasses.EmulateVector2>(engine, "Vector2");
            SetClass<EmulateClasses.EmulateVector3>(engine, "Vector3");
            SetClass<EmulateClasses.EmulateQuaternion>(engine, "Quaternion");
            SetClass<EmulateClasses.HumanoidBone>(engine, "HumanoidBone");
            SetClass<EmulateClasses.HumanoidPose>(engine, "HumanoidPose");
            SetClass<EmulateClasses.Muscles>(engine, "Muscles");
            SetClass<EmulateClasses.ItemTemplateId>(engine, "ItemTemplateId");
            SetClass<EmulateClasses.TextAlignment>(engine, "TextAlignment");
            SetClass<EmulateClasses.TextAnchor>(engine, "TextAnchor");
            SetClass<EmulateClasses.TextInputStatus>(engine, "TextInputStatus");

            try
            {
                engine.Execute(code);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }

            onUpdateBridge.SetLateUpdateCallback(
                csItemHandler.gameObject.name + "_throttle",
                csItemHandler.gameObject,
                (dt) =>
                {
                    clusterScript.DischargeOperateLimit(dt);
                    csItemHandler.DischargeOperateLimit(dt);
                }
            );
            shutdownActions.Add(() => onUpdateBridge.DeleteLateUpdateCallback(
                csItemHandler.gameObject.name + "_throttle"
            ));


            isRunning = true;
            shutdownActions.Add(() => isRunning = false);
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
            if (!isRunning) return;
            onUpdateBridge.InvokeUpdate();
        }


        public void Restart()
        {
            Shutdown();
            Start();
        }

        public void Shutdown()
        {
            foreach(var Action in shutdownActions)
            {
                Action();
            }
        }
    }
}
