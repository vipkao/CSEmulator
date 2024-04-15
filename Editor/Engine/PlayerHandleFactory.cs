using Assets.KaomoLab.CSEmulator.Editor.EmulateClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class PlayerHandleFactory
        : IPlayerHandleFactory, IItemOwnerHandler
    {
        readonly Dictionary<string, (Components.CSEmulatorPlayerHandler, IPlayerMeta)> players = new ();

        readonly IUserInterfaceHandler userInterfaceHandler;
        readonly ITextInputSender textInputSender;
        readonly FogSettingsBridge fogSettingsBridge;
        readonly ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager;

        public PlayerHandleFactory(
            IUserInterfaceHandler userInterfaceHandler,
            ITextInputSender textInputSender,
            FogSettingsBridge fogSettingsBridge,
            ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager
        )
        {
            this.userInterfaceHandler = userInterfaceHandler;
            this.textInputSender = textInputSender;
            this.fogSettingsBridge = fogSettingsBridge;
            this.spawnPointManager = spawnPointManager;
        }

        public PlayerHandle CreateById(string id, Components.CSEmulatorItemHandler csOwnerItemHandler)
        {
            var (csPlayerHandler, playerMeta) = players[id];

            if (csPlayerHandler == null) return null;
            var desktopPlayerController = csPlayerHandler.GetComponentInParent<ClusterVR.CreatorKit.Preview.PlayerController.DesktopPlayerController>();
            if (desktopPlayerController == null) return null;

            var csPlayerController = desktopPlayerController.gameObject.GetComponent<Components.CSEmulatorPlayerController>();
            var playerController = new CCKPlayerController(
                csPlayerHandler, csPlayerController, desktopPlayerController, spawnPointManager
            );

            //VrmPrepare側での設定をここで意識する必要があるのはどうにもよくないと思うけど
            //いいアイデアが出るか、困った事になるまではこれで行く
            var postProcessApplier = new PostProcessApplier(
                csPlayerHandler.gameObject.transform.parent.parent.parent.gameObject,
                fogSettingsBridge
            );

            var handle = new PlayerHandle(
                playerMeta, playerController, userInterfaceHandler, textInputSender, postProcessApplier, csOwnerItemHandler
            );
            return handle;
        }

        public void AddPlayer(UnityEngine.GameObject vrm, IPlayerMeta playerMeta)
        {
            var playerHandler = vrm.GetComponent<Components.CSEmulatorPlayerHandler>();
            players.Add(playerHandler.id, (playerHandler, playerMeta));
        }

        //CSETODO 仮実装
        public string GetOwnerId()
        {
            return players.First().Value.Item1.id;
        }
    }
}
