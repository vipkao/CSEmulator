using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class ItemHandle
    {

        public string id
        {
            get => csItemHandler.item.Id.ToString();
        }

        public Components.CSEmulatorItemHandler csItemHandler { get; private set; }
        readonly Components.CSEmulatorItemHandler csOwnerItemHandler;
        readonly IMessageSender messageSender;
        readonly ClusterVR.CreatorKit.Item.Implements.MovableItem movableItem;

        readonly List<Action> fixedUpdateQueue = new List<Action>();

        //csOwnerItemHandlerとはこのハンドルがいるスクリプト空間($)のこと。
        public ItemHandle(
            Components.CSEmulatorItemHandler csItemHandler,
            Components.CSEmulatorItemHandler csOwnerItemHandler,
            IMessageSender messageSender
        )
        {
            this.csItemHandler = csItemHandler;
            movableItem = csItemHandler.gameObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.MovableItem>();

            this.csOwnerItemHandler = csOwnerItemHandler;

            this.messageSender = messageSender;

            csItemHandler.OnFixedUpdate += CsItemHandler_OnFixedUpdate;
        }

        private void CsItemHandler_OnFixedUpdate()
        {
            foreach(var Action in fixedUpdateQueue)
            {
                Action();
            }
            fixedUpdateQueue.Clear();
        }

        public void addImpulsiveForce(EmulateVector3 force)
        {
            //AddInstantForceItemGimmickを参考にしている。
            if (movableItem == null)
            {
                UnityEngine.Debug.LogWarning("Need MovableItem");
                return;
            }
            fixedUpdateQueue.Add(() =>
            {
                movableItem.AddForce(force._ToUnityEngine(), UnityEngine.ForceMode.Impulse);
            });
        }

        public void addImpulsiveForceAt(EmulateVector3 impluse, EmulateVector3 position)
        {
            fixedUpdateQueue.Add(() =>
            {
                movableItem.AddForceAtPosition(
                    impluse._ToUnityEngine(),
                    position._ToUnityEngine(),
                    UnityEngine.ForceMode.Impulse
                );
            });
        }

        public void addImpulsiveTorque(EmulateVector3 torque)
        {
            fixedUpdateQueue.Add(() =>
            {
                movableItem.AddForce(torque._ToUnityEngine(), UnityEngine.ForceMode.Impulse);
            });
        }

        public bool exists()
        {
            //「ロード中でもtrueを返すことがあります。」という記述が気になるけど、いったんこれで。
            return csItemHandler.item.Id.IsValid() && !csItemHandler.item.IsDestroyed;
        }

        public void send(string requestName, object arg)
        {
            //CSETODO Sendableで例えば独自クラスなどを送った場合どうなるかの確認が必要。
            var sanitized = StateProxy.SanitizeSingleValue(arg);
            messageSender.send(csItemHandler.item.Id.Value, requestName, sanitized, csOwnerItemHandler);
        }

        public override string ToString()
        {
            return string.Format("[ItemHandle][{0}][{1}]", csItemHandler.item.gameObject.name, id);
        }

    }
}
