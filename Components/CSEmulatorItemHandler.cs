using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(ClusterVR.CreatorKit.Item.Implements.Item))]
    public class CSEmulatorItemHandler
        : MonoBehaviour
    {
        public event KaomoLab.CSEmulator.Handler OnFixedUpdate = delegate { };
        public event KaomoLab.CSEmulator.Handler<string, Collision> OnCollision = delegate { };

        public ClusterVR.CreatorKit.Item.IItem item
        {
            get => GetItem();
        }
        ClusterVR.CreatorKit.Item.IItem _item = null;

        public bool isCreatedItem { get; private set; } = false;

        //検証の結果…
        //・制限はitemとplayerで分かれている模様。
        //・ロジックやパラメータもおおよそこんな感じ。
        readonly BurstableThrottle itemThrottle = new BurstableThrottle(0.09d, 5);
        readonly BurstableThrottle playerhrottle = new BurstableThrottle(0.09d, 5);

        OverlapManager<CSEmulatorItemHandler, CSEmulatorPlayerHandler> overlapManager;

        public void Construct(
            bool isCreatedItem
        )
        {
            this.isCreatedItem = isCreatedItem;

            var detectors = gameObject.GetComponentsInChildren<ClusterVR.CreatorKit.Item.Implements.OverlapDetectorShape>()
                    .Where(c => c.gameObject.GetInstanceID() != gameObject.GetInstanceID())
                    .Select(c => c.gameObject);
            foreach(var d in detectors)
            {
                var c = Commons.AddComponent<CSEmulatorSubNodeHandler>(d);
                c.Construct(this);
            }

            overlapManager = new OverlapManager<CSEmulatorItemHandler, CSEmulatorPlayerHandler>(gameObject);
        }


        public (string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)[] GetOverlaps()
        {
            return overlapManager.GetOverlaps();
        }

        public bool TryItemOperate()
        {
            return itemThrottle.TryCharge();
        }
        public bool TryPlayerOperate()
        {
            return playerhrottle.TryCharge();
        }

        public void DischargeOperateLimit(double time)
        {
            itemThrottle.Discharge(time);
            playerhrottle.Discharge(time);
        }

        void FixedUpdate()
        {
            //非アクティブになったら物理演算も行えないので問題はなさそうと考える。
            OnFixedUpdate.Invoke();
        }

        private void Update()
        {
            //ClusterScriptのonUpdateはGameObjectが非アクティブでも動く必要があるため、
            //ここでUpdateを拾ってはいけない。
        }

        private void LateUpdate()
        {
            //これがLateUpdateでいいかはわからない。
            overlapManager?.CheckOverlapsActive();
        }

        private void OnTriggerEnter(Collider other)
        {
            overlapManager.SetOverlap(other);
        }

        private void OnTriggerStay(Collider other)
        {
            //必ずEnterが入るならいらない？
        }

        private void OnTriggerExit(Collider other)
        {
            overlapManager.RemoveOverlap(other);
        }

        public void SetSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler,
            Collider collider
        )
        {
            overlapManager.SetSubNodeOverlap(name, itemHandler, playerHandler, collider);
        }
        public void RemoveSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler,
            Collider collider
        )
        {
            overlapManager.RemoveSubNodeOverlap(name, itemHandler, playerHandler, collider);
        }


        private void OnCollisionEnter(Collision collision)
        {
            OnCollision.Invoke("", collision);
        }

        public void OnCollisionSubNode(string name, Collision collision)
        {
            OnCollision.Invoke(name, collision);
        }

        ClusterVR.CreatorKit.Item.IItem GetItem()
        {
            if (_item == null)
                _item = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItem>();
            return _item;
        }

        public override string ToString()
        {
            return String.Format("[Item][{0}][{1}]", gameObject.name, item.Id.ToString());
        }
    }
}
