﻿using System;
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
        public event KaomoLab.CSEmulator.Handler<Collision> OnCollision = delegate { };

        public string id
        {
            get => item.Id.ToString();
        }

        public ClusterVR.CreatorKit.Item.IItem item
        {
            get => GetItem();
        }
        ClusterVR.CreatorKit.Item.IItem _item = null;

        public bool isCreatedItem { get; private set; } = false;

        public string gameObjectName { get; private set; } = "";

        //検証の結果…
        //・制限はitemとplayerで分かれている模様。
        //・ロジックやパラメータもおおよそこんな感じ。
        readonly BurstableThrottle itemThrottle = new BurstableThrottle(0.09d, 5);
        readonly BurstableThrottle sendThrottle = new BurstableThrottle(0.09d, 5);
        readonly BurstableThrottle playerhrottle = new BurstableThrottle(0.09d, 5);

        OverlapManager<CSEmulatorItemHandler, CSEmulatorPlayerHandler> overlapManager;

        ActionBatch fixedUpdateActions;

        //実装上は妥当だとは思うけど、権能上は妥当ではないように思える。
        //CSETODO しかも設定するタイミングがContructでは行えないという気持ち悪さ。
        //そもそもの設計に無理がある？Constructで一発でできるというのが幻想？
        //＞itemExceptionFactoryという名前にしたところ、
        //＞この例外はitemに依存しているという意味が加わった気がする。
        //＞そうすると、ここにこの機能を持たせてもいい気がしてきた。
        //＞依然として、Constructで設定できない気持ち悪さはあるけども。
        public IItemExceptionFactory itemExceptionFactory = null;

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

            fixedUpdateActions = new ActionBatch();

            gameObjectName = gameObject.name;
        }

        public bool Exists()
        {
            //「ロード中でもtrueを返すことがあります。」という記述が気になるけど、いったんこれで。
            return item.Id.IsValid() && !item.IsDestroyed;
        }

        public (string, CSEmulatorItemHandler, CSEmulatorPlayerHandler)[] GetOverlaps()
        {
            return overlapManager.GetOverlaps();
        }

        public bool TryItemOperate()
        {
            return itemThrottle.TryCharge();
        }
        public bool TrySendOperate()
        {
            return sendThrottle.TryCharge();
        }
        public bool TryPlayerOperate()
        {
            return playerhrottle.TryCharge();
        }

        public void DischargeOperateLimit(double time)
        {
            itemThrottle.Discharge(time);
            sendThrottle.Discharge(time);
            playerhrottle.Discharge(time);
        }

        public void AddFixedUpdateAction(Action action)
        {
            fixedUpdateActions.Add(action);
        }

        void FixedUpdate()
        {
            //非アクティブになったら物理演算も行えないので問題はなさそうと考える。
            OnFixedUpdate.Invoke();

            fixedUpdateActions?.Do();
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
            GameObject gameObject,
            Collider collider
        )
        {
            overlapManager.SetSubNodeOverlap(gameObject, collider);
        }
        public void RemoveSubNodeOverlap(
            GameObject gameObject,
            Collider collider
        )
        {
            overlapManager.RemoveSubNodeOverlap(gameObject, collider);
        }


        private void OnCollisionEnter(Collision collision)
        {
            OnCollision.Invoke(collision);
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
