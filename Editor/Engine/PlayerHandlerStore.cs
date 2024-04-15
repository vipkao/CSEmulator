using Assets.KaomoLab.CSEmulator.Editor.EmulateClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class PlayerHandlerStore
        : IPlayerHandleHolder
    {
        readonly Dictionary<string, Components.CSEmulatorPlayerHandler> vrms = new Dictionary<string, Components.CSEmulatorPlayerHandler>();
        Components.CSEmulatorPlayerHandler owner = null;

        public PlayerHandlerStore(
        )
        {

        }

        public void AddVrm(UnityEngine.GameObject vrm)
        {
            var playerHandler = vrm.GetComponent<Components.CSEmulatorPlayerHandler>();
            vrms.Add(playerHandler.id, playerHandler);
            //とりあえず最初のvrmをownerにしている。
            if (owner == null) owner = playerHandler;
        }

        public Components.CSEmulatorPlayerHandler GetById(string id)
        {
            if (!vrms.ContainsKey(id)) return null;
            var ret = vrms[id];
            return ret;
        }

        public Components.CSEmulatorPlayerHandler GetOwner()
        {
            return owner;
        }
    }
}
