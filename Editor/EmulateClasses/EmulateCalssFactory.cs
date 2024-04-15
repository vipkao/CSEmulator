using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class EmulateClassFactory
    {
        //引数を要求しないこと。
        public EmulateClassFactory()
        {

        }

        public ClusterScript CreateDefaultClusterScript(
            UnityEngine.GameObject gameObject,
            IUpdateListenerBinder updateListenerBinder,
            IUpdateListenerBinder fixedUpdateListenerBinder,
            IReceiveListenerBinder receiveListenerBinder,
            IMessageSender messageSender,
            IPrefabItemHolder prefabItemHolder,
            IPlayerHandleHolder playerHandleHolder,
            IPlayerControllerFactory playerControllerFactory,
            IItemExceptionFactory itemExceptionFactory,
            StateProxy stateProxy,
            ILogger logger
        )
        {
            return new ClusterScript(
                gameObject,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.RoomStateRepository,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.SignalGenerator,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.GimmickManager,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemCreator,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemDestroyer,
                updateListenerBinder,
                fixedUpdateListenerBinder,
                receiveListenerBinder,
                messageSender,
                prefabItemHolder,
                playerHandleHolder,
                playerControllerFactory,
                itemExceptionFactory,
                stateProxy,
                logger
            );
        }
    }
}
