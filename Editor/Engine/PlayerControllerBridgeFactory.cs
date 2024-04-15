using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class PlayerControllerBridgeFactory
        : CSEmulator.Editor.EmulateClasses.IPlayerControllerFactory
    {
        readonly ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager;

        public PlayerControllerBridgeFactory(
            ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager
        )
        {
            this.spawnPointManager = spawnPointManager;
        }

        public CSEmulator.Editor.EmulateClasses.IPlayerController Create(
            CSEmulator.Components.CSEmulatorPlayerHandler csPlayerHandler
        )
        {
            if (csPlayerHandler == null) return null;

            var desktopPlayerController = csPlayerHandler.GetComponentInParent<ClusterVR.CreatorKit.Preview.PlayerController.DesktopPlayerController>();
            if (desktopPlayerController != null)
            {
                var csPlayerController = desktopPlayerController.gameObject.GetComponent<Components.CSEmulatorPlayerController>();
                return new CSEmulator.Editor.EmulateClasses.CCKPlayerControllerBridge(
                    csPlayerHandler, csPlayerController, desktopPlayerController, spawnPointManager
                );
            }

            //モブキャラスポーン機能つけたら、この辺りに新しいIPlayerController生成を追加。
            UnityEngine.Debug.Log("IPlayerController not create." + csPlayerHandler);
            return null;
        }
    }
}
