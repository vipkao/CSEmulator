using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class ItemHandle
        : ISendableSize
    {

        public string id
        {
            get => csItemHandler.id;
        }

        public Components.CSEmulatorItemHandler csItemHandler { get; private set; }
        //ownerの切り替えにはnewを強制しておきたいためprivate
        readonly Components.CSEmulatorItemHandler csOwnerItemHandler;
        readonly IMessageSender messageSender;
        readonly ClusterVR.CreatorKit.Item.IMovableItem movableItem;

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

            //おそらくメモリーリークの原因になるのでNG
            //csItemHandler.OnFixedUpdate += CsItemHandler_OnFixedUpdate;
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
            csItemHandler.AddFixedUpdateAction(() =>
            {
                movableItem.AddForce(force._ToUnityEngine(), UnityEngine.ForceMode.Impulse);
            });
        }

        public void addImpulsiveForceAt(EmulateVector3 impluse, EmulateVector3 position)
        {
            if (!csItemHandler.Exists() || !csOwnerItemHandler.Exists()) return;
            CheckOwnerOperationLimit();
            CheckOwnerDistanceLimit();

            csItemHandler.AddFixedUpdateAction(() =>
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

            csItemHandler.AddFixedUpdateAction(() =>
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
            //CSETODO ワーニング対応がエラー対応になるまでの仮
            try
            {
                CheckRequestSizeLimit(requestName, arg);
            }catch(Exception ex)
            {
                UnityEngine.Debug.LogWarning(ex.Message);
            }

            var sanitized = StateProxy.SanitizeSingleValue(arg);
            messageSender.Send(csItemHandler.item.Id.Value, requestName, sanitized, csOwnerItemHandler);
        }

        void CheckRequestSizeLimit(string requestName, object arg)
        {
            var rlen = Encoding.UTF8.GetByteCount(requestName);
            var alen = StateProxy.CalcSendableSize(arg, 0);
            if (rlen <= 100 && alen <= 1000) return;

            throw csOwnerItemHandler.itemExceptionFactory.CreateRequestSizeLimitExceeded(
                String.Format("[{0}][messageType:{1}][arg:{2}]", csItemHandler, rlen, alen)
            );
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

        public int GetSize()
        {
            //おそらく固定。アイテム名やGameObject名では差がない。
            return 13;
        }

    }
}
