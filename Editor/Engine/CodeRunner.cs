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

        readonly UnityEngine.GameObject gameObject;
        readonly string code;
        public readonly Components.CSEmulatorItemHandler csItemHandler;
        readonly PrefabItemStore prefabItemStore;
        readonly ItemMessageRouter itemMessageRouter;
        readonly TextInputRouter textInputRouter;
        readonly PlayerHandleFactory playerHandleFactory;
        readonly IRunnerOptions options;
        readonly ILoggerFactory loggerFactory;

        readonly EmulateClasses.StateProxy stateProxy;
        readonly OnStartInvoker onStartInvoker;
        readonly OnUpdateBridge onUpdateBridge;
        readonly OnUpdateBridge onFixedUpdateBridge;
        readonly CckComponentFacadeFactory cckComponentFacadeFactory;
        readonly ItemLifecycler itemLifecycler;

        List<Action> shutdownActions = new List<Action>();
        bool isRunning = false;

        public CodeRunner(
            ClusterVR.CreatorKit.Item.IScriptableItem scriptableItem,
            Components.CSEmulatorItemHandler csItemHandler,
            PrefabItemStore prefabItemStore,
            ItemMessageRouter itemMessageRouter,
            TextInputRouter textInputRouter,
            PlayerHandleFactory playerHandleFactory,
            IRunnerOptions options,
            ILoggerFactory loggerFactory
        )
        {
            this.gameObject = scriptableItem.Item.gameObject;
            this.csItemHandler = csItemHandler;
            this.prefabItemStore = prefabItemStore;
            this.itemMessageRouter = itemMessageRouter;
            this.textInputRouter = textInputRouter;
            this.playerHandleFactory = playerHandleFactory;
            this.options = options;
            this.loggerFactory = loggerFactory;

            code = scriptableItem.GetSourceCode(true);

            onStartInvoker = new OnStartInvoker();

            //キーはGameObjectのnameで行っている(v1を使いまわしている)ので、Item間での使いまわしは不可。
            //そのためここで各Item用にインスタンスを作っている。
            onUpdateBridge = new OnUpdateBridge();

            onFixedUpdateBridge = new OnUpdateBridge();

            stateProxy = new EmulateClasses.StateProxy();

            cckComponentFacadeFactory = new CckComponentFacadeFactory(
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.RoomStateRepository,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.SignalGenerator,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.GimmickManager
            );

            itemLifecycler = new ItemLifecycler(
                prefabItemStore,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemCreator,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemDestroyer
            );

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

            var externalHttpCaller = new ExternalHttpCaller(
                options.externalCallerOptions,
                logger
            );

            var materialSubstituer = new MaterialSubstituter(
            );

            var clusterScript = new EmulateClasses.ClusterScript(
                gameObject,
                cckComponentFacadeFactory,
                itemLifecycler,
                onStartInvoker,
                onUpdateBridge,
                onFixedUpdateBridge,
                itemMessageRouter,
                itemMessageRouter,
                textInputRouter,
                playerHandleFactory,
                playerHandleFactory,
                exceptionFactory,
                externalHttpCaller,
                materialSubstituer,
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
            SetClass<EmulateClasses.PostProcessEffects>(engine, "PostProcessEffects");
            engine.SetValue("ClusterScriptError", exceptionFactory.clusterScriptErrorConstructor);

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
            //Start>Updateの順で実行される模様 2.7.0.2調査
            onStartInvoker.InvokeStart();
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
