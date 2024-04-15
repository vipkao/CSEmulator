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
        //ownerの切り替えにはnewを強制しておきたいためprivate
        readonly Components.CSEmulatorItemHandler csOwnerItemHandler;
        readonly IMessageSender messageSender;
        readonly ClusterVR.CreatorKit.Item.IMovableItem movableItem;

        readonly List<Action> fixedUpdateQueue = new List<Action>();

        //csOwnerItemHandlerとはこのハンドルがいるスクリプト空間($)のこと。
        public ItemHandle(
            Components.CSEmulatorItemHandler csItemHandler,
            Components.CSEmulatorItemHandler csOwnerItemHandler,
            IMessageSender messageSender
        )
        {
            this.csItemHandler = csItemHandler;
            //CSETODO 将来的にスクリプト実行オーナーの概念をCSEmulatorItemHandlerから分離させたときに
            //canMove()といった関数でチェックさせるようにする。
            //この修正はそれまでの仮対応。
            if (csItemHandler.Exists())
                movableItem = csItemHandler.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IMovableItem>();
            else
                movableItem = null;

            this.csOwnerItemHandler = csOwnerItemHandler;

            this.messageSender = messageSender;

            csItemHandler.OnFixedUpdate += CsItemHandler_OnFixedUpdate;
        }

        private void CsItemHandler_OnFixedUpdate()
        {
            if (!csItemHandler.Exists()) return;
            foreach (var Action in fixedUpdateQueue)
            {
                Action();
            }
            fixedUpdateQueue.Clear();
        }

        public void addImpulsiveForce(EmulateVector3 force)
        {
            if (!csItemHandler.Exists() || !csOwnerItemHandler.Exists()) return;
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

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
            if (!csItemHandler.Exists() || !csOwnerItemHandler.Exists()) return;
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

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
            //CSETODO 単にreturnではなく親切にメッセージを出したい。
            if (!csItemHandler.Exists() || !csOwnerItemHandler.Exists()) return;
            if (!exists()) return;
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            fixedUpdateQueue.Add(() =>
            {
                movableItem.AddForce(torque._ToUnityEngine(), UnityEngine.ForceMode.Impulse);
            });
        }

        public bool exists()
        {
            return csItemHandler.Exists();
        }

        public void send(string requestName, object arg)
        {
            //CSETODO 単にreturnではなく親切にメッセージを出したい。
            if (!csItemHandler.Exists() || !csOwnerItemHandler.Exists()) return;
            CheckOwnerSendOperationLimit();
            CheckOwnerDistanceLimit();

            //CSETODO Sendableで例えば独自クラスなどを送った場合どうなるかの確認が必要。
            var sanitized = StateProxy.SanitizeSingleValue(arg);
            messageSender.Send(csItemHandler.item.Id.Value, requestName, sanitized, csOwnerItemHandler);
        }

        void CheckOwnerDistanceLimit()
        {
            var p1 = csItemHandler.gameObject.transform.position;
            var p2 = csOwnerItemHandler.gameObject.transform.position;
            var d = UnityEngine.Vector3.Distance(p1, p2);
            //30メートル以内はOK
            if (d <= 30f) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateDistanceLimitExceeded(
                String.Format("[{0}]>>>[{1}]", csOwnerItemHandler, csItemHandler)
            );
        }
        void CheckOwnerOperationLimit()
        {
            var result = csOwnerItemHandler.TryItemOperate();
            if (result) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]>>>[{1}]", csOwnerItemHandler, csItemHandler)
            );
        }
        void CheckOwnerSendOperationLimit()
        {
            var result = csOwnerItemHandler.TrySendOperate();
            if (result) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]>>>[{1}]", csOwnerItemHandler, csItemHandler)
            );
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.id = id;
            return o;
        }
        public override string ToString()
        {
            return string.Format("[ItemHandle][{0}][{1}]", csItemHandler == null ? "null" : csItemHandler.gameObjectName, id);
        }


    }
}
