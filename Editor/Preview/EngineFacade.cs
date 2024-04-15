using Assets.KaomoLab.CSEmulator.Editor.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class EngineFacade
    {
        readonly ItemCollector itemCollector;
        readonly VrmPreparer vrmPreparer;

        readonly PrefabItemStore prefabItemStore;
        readonly ItemMessageRouter itemMessageRouter;
        readonly PlayerHandlerStore playerHandleStore;
        readonly PlayerControllerBridgeFactory playerControllerBridgeFactory;
        readonly OptionBridge optionBridge;

        List<CodeRunner> codeRunners = new List<CodeRunner>();
        UnityEngine.GameObject vrm = null;

        bool isRunning = false;

        public EngineFacade(
            ItemCollector itemCollector,
            VrmPreparer vrmPreparer,
            OptionBridge optionBridge,
            ClusterVR.CreatorKit.Editor.Preview.Item.ItemDestroyer itemDestroyer,
            ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager
        )
        {
            this.optionBridge = optionBridge;
            this.itemCollector = itemCollector;
            this.vrmPreparer = vrmPreparer;

            prefabItemStore = new PrefabItemStore(
                itemCollector.GetAllItemPrefabs()
            );
            itemMessageRouter = new ItemMessageRouter();
            playerHandleStore = new PlayerHandlerStore();
            playerControllerBridgeFactory = new PlayerControllerBridgeFactory(
                spawnPointManager
            );
            this.optionBridge = optionBridge;

            itemCollector.OnScriptableItemCreated += i =>
            {
                codeRunners.Add(StartRunner(i));
            };
            itemCollector.OnItemCreated += i =>
            {
                var csItemHandler = CSEmulator.Commons.AddComponent<Components.CSEmulatorItemHandler>(i.gameObject);
                csItemHandler.Construct(true);
            };
            itemDestroyer.OnDestroy += i =>
            {
                var destoryed = codeRunners
                    .FirstOrDefault(c => c.csItemHandler.item.Id.Value == i.Id.Value);
                if (destoryed == null) return;
                destoryed.Shutdown();
                codeRunners.Remove(destoryed);
            };
        }

        public void Start()
        {
            if (!optionBridge.raw.enable) return;

            foreach(var i in itemCollector.GetAllItems())
            {
                var csItemHandler = CSEmulator.Commons.AddComponent<Components.CSEmulatorItemHandler>(i.gameObject);
                csItemHandler.Construct(false);
            }

            //このタイミングでよさそう
            vrm = vrmPreparer.InstantiateVrm();
            playerHandleStore.AddVrm(vrm);

            //各種コンポーネントを付けてから実行した方がいい気がする。
            var newRunners = itemCollector
                .GetAllScriptableItem()
                .Select(i => StartRunner(i));
            codeRunners.AddRange(newRunners);

            isRunning = true;
        }

        CodeRunner StartRunner(
            ClusterVR.CreatorKit.Item.IScriptableItem scriptableItem
        )
        {
            var itemHandler = scriptableItem.Item.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            var loggerFactory = new DebugLogFactory(itemHandler.gameObject, optionBridge.raw);
            var ret = new CodeRunner(
                scriptableItem,
                itemHandler,
                prefabItemStore,
                itemMessageRouter,
                playerHandleStore,
                playerControllerBridgeFactory,
                optionBridge,
                loggerFactory
            );
            ret.Start();
            return ret;
        }

        public void Update()
        {
            //Update中にDestroyされて減ることがあるのでToArray
            foreach (var runner in codeRunners.ToArray())
            {
                runner.Update();
            }
            itemMessageRouter.Routing();
        }

        public void Shutdown()
        {
            if (!isRunning) return;

            foreach (var runner in codeRunners)
            {
                runner.Shutdown();
            }
            codeRunners.Clear();

            isRunning = false;
            vrm = null;
        }
    }
}
