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

        Dictionary<string, Dictionary<int, (CSEmulatorItemHandler, Collider)>> subNodeItemOverlaps = new Dictionary<string, Dictionary<int, (CSEmulatorItemHandler, Collider)>>();
        Dictionary<string, Dictionary<int, (CSEmulatorPlayerHandler, Collider)>> subNodePlayerOverlaps = new Dictionary<string, Dictionary<int, (CSEmulatorPlayerHandler, Collider)>>();
        Dictionary<string, Dictionary<int, Collider>> subNodeColliderOverlaps = new Dictionary<string, Dictionary<int, Collider>>();
        Dictionary<int, (CSEmulatorItemHandler, Collider)> itemOverlaps = new Dictionary<int, (CSEmulatorItemHandler, Collider)>();
        Dictionary<int, (CSEmulatorPlayerHandler, Collider)> playerOverlaps = new Dictionary<int, (CSEmulatorPlayerHandler, Collider)>();
        Dictionary<int, Collider> colliderOverlaps = new Dictionary<int, Collider>();

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
        }

        public void SetSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler,
            Collider collider
        )
        {
            if (!subNodeItemOverlaps.ContainsKey(name))
                subNodeItemOverlaps[name] = new Dictionary<int, (CSEmulatorItemHandler, Collider)>();
            if (!subNodePlayerOverlaps.ContainsKey(name))
                subNodePlayerOverlaps[name] = new Dictionary<int, (CSEmulatorPlayerHandler, Collider)>();
            if (!subNodeColliderOverlaps.ContainsKey(name))
                subNodeColliderOverlaps[name] = new Dictionary<int, Collider>();

            if (itemHandler != null)
                subNodeItemOverlaps[name][collider.GetInstanceID()] = (itemHandler, collider);

            if (playerHandler != null)
                subNodePlayerOverlaps[name][collider.GetInstanceID()] = (playerHandler, collider);

            if (IsOverlapCollider(itemHandler, playerHandler))
                subNodeColliderOverlaps[name][collider.GetInstanceID()] = collider;
        }

        public void RemoveSubNodeOverlap(
            string name,
            CSEmulatorItemHandler itemHandler,
            CSEmulatorPlayerHandler playerHandler,
            Collider collider
        )
        {
            if (!subNodeItemOverlaps.ContainsKey(name))
                subNodeItemOverlaps[name] = new Dictionary<int, (CSEmulatorItemHandler, Collider)>();
            if (!subNodePlayerOverlaps.ContainsKey(name))
                subNodePlayerOverlaps[name] = new Dictionary<int, (CSEmulatorPlayerHandler, Collider)>();
            if (!subNodeColliderOverlaps.ContainsKey(name))
                subNodeColliderOverlaps[name] = new Dictionary<int, Collider>();

            if (itemHandler != null)
                subNodeItemOverlaps[name].Remove(collider.GetInstanceID());

            if (playerHandler != null)
                subNodePlayerOverlaps[name].Remove(collider.GetInstanceID());

            if (collider != null)
                subNodeColliderOverlaps[name].Remove(collider.GetInstanceID());
        }

        public (string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)[] GetOverlaps()
        {
            var ret = new List<(string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)>();

            foreach (var key in itemOverlaps.Keys)
                ret.Add(("", itemOverlaps[key].Item1, null));
            foreach (var key in playerOverlaps.Keys)
                ret.Add(("", null, playerOverlaps[key].Item1));
            foreach (var _ in colliderOverlaps.Keys)
                ret.Add(("", null, null));

            foreach (var name in subNodeItemOverlaps.Keys)
                foreach (var key in subNodeItemOverlaps[name].Keys)
                    ret.Add((name, subNodeItemOverlaps[name][key].Item1, null));
            foreach (var name in subNodePlayerOverlaps.Keys)
                foreach (var key in subNodePlayerOverlaps[name].Keys)
                    ret.Add((name, null, subNodePlayerOverlaps[name][key].Item1));
            foreach (var name in subNodeColliderOverlaps.Keys)
                foreach (var _ in subNodeColliderOverlaps[name].Keys)
                    ret.Add((name, null, null));

            return ret.ToArray();
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
            CheckOverlapsActive();
        }
        void CheckOverlapsActive()
        {
            //SetActiveでfalseされた場合、OnTriggerExitが動かないので、
            //リスト中のActiveを確認し必要に応じて削除する。という挙動がある模様。
            foreach (var key in itemOverlaps.Keys.ToArray())
            {
                var g = itemOverlaps[key].Item2.gameObject;
                if (g != null && g.activeInHierarchy) continue;
                itemOverlaps.Remove(key);
            }
            foreach (var key in playerOverlaps.Keys.ToArray())
            {
                //playerもこの判定でいいかはわからないけど、プレビュー上ならどうにでもできる
                var g = playerOverlaps[key].Item2.gameObject;
                if (g != null && g.activeInHierarchy) continue;
                playerOverlaps.Remove(key);
            }
            foreach (var key in colliderOverlaps.Keys.ToArray())
            {
                var g = colliderOverlaps[key].gameObject;
                if (g != null && g.activeInHierarchy) continue;
                colliderOverlaps.Remove(key);
            }

            foreach (var name in subNodeItemOverlaps.Keys)
                foreach (var key in subNodeItemOverlaps[name].Keys.ToArray())
                {
                    var g = subNodeItemOverlaps[name][key].Item2.gameObject;
                    if (g != null && g.activeInHierarchy) continue;
                    subNodeItemOverlaps[name].Remove(key);
                }
            foreach (var name in subNodePlayerOverlaps.Keys)
                foreach (var key in subNodePlayerOverlaps[name].Keys.ToArray())
                {
                    //playerもこの判定でいいかはわからないけど、プレビュー上ならどうにでもできる
                    var g = subNodePlayerOverlaps[name][key].Item2.gameObject;
                    if (g != null && g.activeInHierarchy) continue;
                    subNodePlayerOverlaps[name].Remove(key);
                }
            foreach (var name in subNodeColliderOverlaps.Keys)
                foreach (var key in subNodeColliderOverlaps[name].Keys.ToArray())
                {
                    var g = subNodeColliderOverlaps[name][key].gameObject;
                    if (g != null && g.activeInHierarchy) continue;
                    subNodeColliderOverlaps[name].Remove(key);
                }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOverlapTarget(gameObject, other)) return;
            var (itemHandler, playerHandler) = GetHandler(other);

            if (itemHandler != null)
                itemOverlaps[other.GetInstanceID()] = (itemHandler, other);
            if (playerHandler != null)
                playerOverlaps[other.GetInstanceID()] = (playerHandler, other);
            if (IsOverlapCollider(itemHandler, playerHandler))
                colliderOverlaps[other.GetInstanceID()] = other;
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
                itemOverlaps.Remove(other.GetInstanceID());
            if (playerHandler != null)
                playerOverlaps.Remove(other.GetInstanceID());
            if (IsOverlapCollider(itemHandler, playerHandler))
                colliderOverlaps.Remove(other.GetInstanceID());
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
            //まずDetectorは自身に必須
            if (!self.GetComponent<ClusterVR.CreatorKit.Item.Implements.OverlapDetectorShape>())
                return false;

            //MovableItemならRigidbodyがある。
            if (
                //CharacterControllerが親にいても反応しない。
                //Rigidbodyが親にいるのには反応する。
                null == collider.gameObject.GetComponent<CharacterController>()
                && null == collider.gameObject.GetComponentInParent<Rigidbody>()
                && null == collider.gameObject.GetComponentInChildren<CSEmulatorPlayerHandler>()
                && null == self.GetComponent<CharacterController>()
                && null == self.GetComponentInParent<Rigidbody>()
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

            return (
                itemHandler, playerHandler
            );
        }
        public static bool IsOverlapCollider(CSEmulatorItemHandler item, CSEmulatorPlayerHandler handler)
        {
            return item == null && handler == null;
        }

        public override string ToString()
        {
            return String.Format("[Item][{0}][{1}]", gameObject.name, item.Id.ToString());
        }
    }
}
