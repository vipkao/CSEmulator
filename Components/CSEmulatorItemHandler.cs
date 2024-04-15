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

        public CSEmulatorNearDetectorHandler nearDetectorHandler { get; private set; }

        Dictionary<string, Dictionary<ulong, CSEmulatorItemHandler>> subNodeItemOverlaps = new Dictionary<string, Dictionary<ulong, CSEmulatorItemHandler>>();
        Dictionary<string, Dictionary<string, CSEmulatorPlayerHandler>> subNodePlayerOverlaps = new Dictionary<string, Dictionary<string, CSEmulatorPlayerHandler>>();
        Dictionary<ulong, CSEmulatorItemHandler> itemOverlaps = new Dictionary<ulong, CSEmulatorItemHandler>();
        Dictionary<string, CSEmulatorPlayerHandler> playerOverlaps = new Dictionary<string, CSEmulatorPlayerHandler>();

        public void Construct(
            CSEmulatorNearDetectorHandler nearDetectorHandler
        )
        {
            Construct(nearDetectorHandler, false);
        }
        public void Construct(
            CSEmulatorNearDetectorHandler nearDetectorHandler,
            bool isCreatedItem
        )
        {
            this.nearDetectorHandler = nearDetectorHandler;
            this.isCreatedItem = isCreatedItem;

            var detectors = gameObject.GetComponentsInChildren<ClusterVR.CreatorKit.Item.Implements.OverlapDetectorShape>()
                    .Where(c => c.gameObject.GetInstanceID() != gameObject.GetInstanceID())
                    .Select(c => c.gameObject);
            foreach(var d in detectors)
            {
                var c = Commons.AddComponent<CSEmulatorSubNodeHandler>(d);
                c.Construct(this);
            }
        }

        public void SetSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler
        )
        {
            if (!subNodeItemOverlaps.ContainsKey(name))
                subNodeItemOverlaps[name] = new Dictionary<ulong, CSEmulatorItemHandler>();
            if (!subNodePlayerOverlaps.ContainsKey(name))
                subNodePlayerOverlaps[name] = new Dictionary<string, CSEmulatorPlayerHandler>();

            if (itemHandler != null)
                subNodeItemOverlaps[name][itemHandler.item.Id.Value] = itemHandler;

            if (playerHandler != null)
                subNodePlayerOverlaps[name][playerHandler.id] = playerHandler;
        }

        public void RemoveSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler
        )
        {
            if (!subNodeItemOverlaps.ContainsKey(name))
                subNodeItemOverlaps[name] = new Dictionary<ulong, CSEmulatorItemHandler>();
            if (!subNodePlayerOverlaps.ContainsKey(name))
                subNodePlayerOverlaps[name] = new Dictionary<string, CSEmulatorPlayerHandler>();

            if (itemHandler != null)
                subNodeItemOverlaps[name].Remove(itemHandler.item.Id.Value);

            if (playerHandler != null)
                subNodePlayerOverlaps[name].Remove(playerHandler.id);
        }

        public (string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)[] GetOverlaps()
        {
            var ret = new List<(string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)>();

            foreach (var key in itemOverlaps.Keys)
                ret.Add(("", itemOverlaps[key], null));
            foreach (var key in playerOverlaps.Keys)
                ret.Add(("", null, playerOverlaps[key]));

            foreach (var name in subNodeItemOverlaps.Keys)
                foreach (var key in subNodeItemOverlaps[name].Keys)
                    ret.Add((name, subNodeItemOverlaps[name][key], null));
            foreach (var name in subNodePlayerOverlaps.Keys)
                foreach (var key in subNodePlayerOverlaps[name].Keys)
                    ret.Add((name, null, subNodePlayerOverlaps[name][key]));

            return ret.ToArray();
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

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOverlapTarget(gameObject, other)) return;
            var (itemHandler, playerHandler) = GetHandler(other);

            if (itemHandler != null)
                itemOverlaps[itemHandler.item.Id.Value] = itemHandler;
            if (playerHandler != null)
                playerOverlaps[playerHandler.id] = playerHandler;
        }

        private void OnTriggerStay(Collider other)
        {
            //必ずEnterが入るならいらない？
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsOverlapTarget(gameObject, other)) return;
            var (itemHandler, playerHandler) = GetHandler(other);

            if (itemHandler != null)
                itemOverlaps.Remove(itemHandler.item.Id.Value);
            if (playerHandler != null)
                playerOverlaps.Remove(playerHandler.id);
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

        public static bool IsOverlapTarget(GameObject self, Collider collider)
        {
            //MovableItemならRigidbodyある。
            //CSETODO このあたりの条件が不安。相手のcolliderはitemのsubnodeもある？
            if (null == collider.gameObject.GetComponent<CharacterController>()
                && null == collider.gameObject.GetComponent<Rigidbody>()
                && null == collider.gameObject.GetComponentInChildren<CSEmulatorPlayerHandler>()
                && null == self.GetComponent<CharacterController>()
                && null == self.GetComponent<Rigidbody>()
                && null == self.GetComponentInChildren<CSEmulatorPlayerHandler>()
            ) return false;

            if (null != collider.gameObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.PhysicalShape>())
                return true;
            if (null != collider.gameObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.OverlapSourceShape>())
                return true;
            if (!collider.isTrigger) return true;

            return false;
        }

        public static (CSEmulatorItemHandler, CSEmulatorPlayerHandler) GetHandler(Collider collider)
        {
            var itemHandler = collider.GetComponentInParent<CSEmulatorItemHandler>();
            var playerHandler = collider.GetComponentInChildren<CSEmulatorPlayerHandler>();

            return (itemHandler, playerHandler);
        }

        public override string ToString()
        {
            return String.Format("[Item][{0}][{1}]", gameObject.name, item.Id.ToString());
        }
    }
}
