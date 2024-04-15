using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ClusterVR.CreatorKit;
using UnityEditor;
using UnityEngine.UIElements;
using Assets.KaomoLab.CSEmulator.Components;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class ClusterScript
    {
        readonly GameObject gameObject;
        readonly ICckComponentFacade cckComponentFacade;
        readonly IItemLifecycler itemLifecycler;
        readonly IStartListenerBinder startListenerBinder;
        readonly IUpdateListenerBinder updateListenerBinder;
        readonly IUpdateListenerBinder fixedUpdateListenerBinder;
        readonly IReceiveListenerBinder receiveListenerBinder;
        readonly IMessageSender messageSender;
        readonly ITextInputListenerBinder textInputListenerBinder;
        readonly IItemOwnerHandler itemOwnerHandler;
        readonly IPlayerHandleFactory playerHandleFactory;
        readonly IProgramStatus programStatus;
        readonly IItemExceptionFactory itemExceptionFactory;
        readonly IExternalCaller externalCaller;
        readonly IMaterialSubstituter materialSubstituter;
        readonly StateProxy stateProxy;
        readonly ILogger logger;

        readonly ClusterVR.CreatorKit.Item.Implements.MovableItem movableItem;
        readonly ClusterVR.CreatorKit.Item.IItem item;
        readonly Components.CSEmulatorItemHandler csItemHandler;

        readonly bool hasMovableItem;
        readonly bool hasCharacterItem;

        bool isInFixedUpdate = false;

        readonly BurstableThrottle createItemThrottle = new BurstableThrottle(0.09d, 5);
        IChargeThrottle callExternalThrottle = new PassThroughThrottle();

        Action<Collision> OnCollideHandler = _ => { };
        Action<bool, bool, PlayerHandle> OnGrabHandler = (_, _, _) => { };
        Action<PlayerHandle> OnInteractHandler = _ => { };
        Action<bool, PlayerHandle> OnRideHandler = (_, _) => { };
        Action<bool, PlayerHandle> OnUseHandler = (_, _) => { };
        Action<string, string, TextInputStatus> OnTextInputHandler = (_, _, _) => { };
        Action<string, string, string> OnExternalCallEndHandler = (_, _, _) => { };

        Action OnInteractInitialize = () => { };

        public ClusterScript(
            GameObject gameObject,
            ICckComponentFacadeFactory cckComponentFacadeFactory,
            IItemLifecycler itemLifecycler,
            IStartListenerBinder startListenerBinder,
            IUpdateListenerBinder updateListenerBinder,
            IUpdateListenerBinder fixedUpdateListenerBinder,
            IReceiveListenerBinder receiveListenerBinder,
            IMessageSender messageSender,
            ITextInputListenerBinder textInputListenerBinder,
            IItemOwnerHandler itemOwnerHandler,
            IPlayerHandleFactory playerHandleFactory,
            IItemExceptionFactory itemExceptionFactory,
            IExternalCaller externalCaller,
            IMaterialSubstituter materialSubstituer,
            StateProxy stateProxy,
            ILogger logger
        )
        {
            this.gameObject = gameObject;
            this.cckComponentFacade = cckComponentFacadeFactory.Create(gameObject);
            this.itemLifecycler = itemLifecycler;
            this.startListenerBinder = startListenerBinder;
            this.updateListenerBinder = updateListenerBinder;
            this.fixedUpdateListenerBinder = fixedUpdateListenerBinder;
            this.receiveListenerBinder = receiveListenerBinder;
            this.messageSender = messageSender;
            this.textInputListenerBinder = textInputListenerBinder;
            this.itemOwnerHandler = itemOwnerHandler;
            this.playerHandleFactory = playerHandleFactory;
            this.itemExceptionFactory = itemExceptionFactory;
            this.externalCaller = externalCaller;
            this.materialSubstituter = materialSubstituer;
            this.stateProxy = stateProxy;
            this.logger = logger;

            item = this.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItem>();
            csItemHandler = this.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            csItemHandler.OnCollision += CsItemHandler_OnCollision;
            hasMovableItem = this.gameObject.TryGetComponent(out movableItem);
            hasCharacterItem = this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Item.Implements.CharacterItem>(out var _);

            cckComponentFacade.onGrabbed += CckComponentFacade_onGrabbed;
            cckComponentFacade.onRide += CckComponentFacade_onRide;
            cckComponentFacade.onInteract += CckComponentFacade_onInteract;
            cckComponentFacade.onUse += CckComponentFacade_onUse;

            this.externalCaller.OnChangeLimit += ApplyCallExternalLimit;
            ApplyCallExternalLimit();
        }

        ClusterVR.CreatorKit.Item.ItemId itemId
        {
            //itemIdはGameObject生成直後は0なので、都度取得にすることで、0になる状況を緩和。
            get => item.Id;
        }

        public EmulateVector3 angularVelocity
        {
            get {
                if (!hasMovableItem) return new EmulateVector3(0, 0, 0);
                return new EmulateVector3(movableItem.AngularVelocity);
            }
            set
            {
                if (cckComponentFacade.isGrab) return;
                if (!hasMovableItem) throw csItemHandler.itemExceptionFactory.CreateGeneral("MovableItemが必要です。");
                movableItem.Rigidbody.angularVelocity = value._ToUnityEngine();
            }
        }

        public string id
        {
            get => csItemHandler.id;
        }

        public ItemHandle itemHandle
        {
            //cacheしてもいいかもしれないけど、
            //都度newするという想定から外れるとロクなことが起きないのでnewしている。
            get => new ItemHandle(csItemHandler, this.csItemHandler, messageSender);
        }

        public ItemTemplateId itemTemplateId
        {
            get
            {
                var prefabItem = gameObject.GetComponent<CSEmulatorPrefabItem>();

                if (prefabItem == null) return null;

                return new ItemTemplateId(prefabItem.id);
            }
        }

        public StateProxy state
        {
            get => stateProxy;
        }

        public bool useGravity
        {
            get
            {
                if (!hasMovableItem) return false;
                if (movableItem.Rigidbody.isKinematic) return false;
                return movableItem.Rigidbody.useGravity;
            }
            set
            {
                if (!hasMovableItem) throw csItemHandler.itemExceptionFactory.CreateGeneral("MovableItemが必要です。");
                if (movableItem.Rigidbody.isKinematic) throw csItemHandler.itemExceptionFactory.CreateGeneral("非Kinematicにしてください。");
                movableItem.Rigidbody.useGravity = value;
            }
        }

        public EmulateVector3 velocity
        {
            get
            {
                if (!hasMovableItem) return new EmulateVector3(0, 0, 0);
                return new EmulateVector3(movableItem.Velocity);
            }
            set
            {
                if (cckComponentFacade.isGrab) return;
                if (!hasMovableItem) throw csItemHandler.itemExceptionFactory.CreateGeneral("MovableItemが必要です。");
                movableItem.Rigidbody.velocity = value._ToUnityEngine();
            }
        }

        public void addForce(EmulateVector3 force)
        {
            if (!isInFixedUpdate)
                throw csItemHandler.itemExceptionFactory.CreateExecutionNotAllowed("onPhysicsUpdate内でのみ実行可能です。");
            movableItem.AddForce(force._ToUnityEngine(), ForceMode.Force);
        }

        public void addForceAt(EmulateVector3 force, EmulateVector3 position)
        {
            if (!isInFixedUpdate)
                throw csItemHandler.itemExceptionFactory.CreateExecutionNotAllowed("onPhysicsUpdate内でのみ実行可能です。");
            movableItem.AddForceAtPosition(
                force._ToUnityEngine(), position._ToUnityEngine(), ForceMode.Force
            );
        }

        public void addImpulsiveForce(EmulateVector3 impulsiveForce)
        {
            movableItem.AddForce(impulsiveForce._ToUnityEngine(), ForceMode.Impulse);
        }

        public void addImpulsiveForceAt(EmulateVector3 impulsiveForce, EmulateVector3 position)
        {
            movableItem.AddForceAtPosition(
                impulsiveForce._ToUnityEngine(),
                position._ToUnityEngine(),
                ForceMode.Impulse
            );
        }

        public void addImpulsiveTorque(EmulateVector3 impulsiveTorque)
        {
            movableItem.AddTorque(
                impulsiveTorque._ToUnityEngine(), ForceMode.Impulse
            );
        }

        public void addTorque(EmulateVector3 torque)
        {
            if (!isInFixedUpdate)
                throw csItemHandler.itemExceptionFactory.CreateExecutionNotAllowed("onPhysicsUpdate内でのみ実行可能です。");
            movableItem.AddTorque(
                torque._ToUnityEngine(), ForceMode.Force
            );

        }

        public ApiAudio audio(string itemAudioSetId)
        {
            var itemAudioSetList = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItemAudioSetList>();
            //無い場合は、各値のデフォルト値が入った構造体が渡される。
            var itemAudioSet = itemAudioSetList.ItemAudioSets.FirstOrDefault(set => set.Id == itemAudioSetId);
            var apiAudio = new ApiAudio(itemAudioSet, gameObject);

            return apiAudio;
        }

        public void callExternal(
            string request,
            string meta
        )
        {
            CheckCallExternalSizeLimit(request, meta);
            CheckCallExternalOperationLimit();

            externalCaller.CallExternal(request, meta);
        }
        void ApplyCallExternalLimit()
        {
            callExternalThrottle = this.externalCaller.rateLimit switch
            {
                CallExternalRateLimit.unlimited => new PassThroughThrottle(),
                CallExternalRateLimit.limit5 => new BurstableThrottle(12.0d, 5),
                CallExternalRateLimit.limit100 => new BurstableThrottle(60d / 100d, 5),
                _ => throw new NotImplementedException()
            };
        }
        void CheckCallExternalSizeLimit(string request, string meta)
        {
            if (Encoding.UTF8.GetByteCount(request) > 1000)
            {
                throw itemExceptionFactory.CreateRequestSizeLimitExceeded(
                    String.Format("[{0}][request]", gameObject.name)
                );
            }
            if (Encoding.UTF8.GetByteCount(meta) > 100)
            {
                throw itemExceptionFactory.CreateRequestSizeLimitExceeded(
                    String.Format("[{0}][meta]", gameObject.name)
                );
            }
        }
        void CheckCallExternalOperationLimit()
        {
            var result = callExternalThrottle.TryCharge();
            if (result) return;

            throw itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]", gameObject.name)
            );
        }

        public int computeSendableSize(object obj)
        {
            var size = StateProxy.CalcSendableSize(obj, 0);
            return size;
        }

        public ItemHandle createItem(
            ItemTemplateId itemTemplateId,
            EmulateVector3 position,
            EmulateQuaternion rotation
        )
        {
            CheckCreateItemOperationLimit();
            CheckCreateItemDistanceLimit(position._ToUnityEngine());

            var create = itemLifecycler.CreateItem(itemTemplateId, position, rotation);
            if (create == null) return null;

            var csItemHandler = create.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            var ret = new ItemHandle(csItemHandler, this.csItemHandler, messageSender);

            return ret;
        }
        void CheckCreateItemDistanceLimit(Vector3 target)
        {
            var p1 = gameObject.transform.position;
            var d = UnityEngine.Vector3.Distance(p1, target);
            //30メートル以内はOK
            if (d <= 30f) return;

            throw itemExceptionFactory.CreateDistanceLimitExceeded(
                String.Format("[{0}]>>>[{1}]", gameObject.transform.position, target)
            );
        }
        void CheckCreateItemOperationLimit()
        {
            var result = createItemThrottle.TryCharge();
            if (result) return;

            throw itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]", gameObject.name)
            );
        }
        public void destroy()
        {
            if (!arrowDestroy() && !csItemHandler.isCreatedItem)
                throw csItemHandler.itemExceptionFactory.CreateExecutionNotAllowed("動的アイテムのみ実行可能です。クラフトアイテムの場合は[CS Emulator Prefab Item]コンポーネントを付けてください。");
            itemLifecycler.DestroyItem(item);
        }
        bool arrowDestroy()
        {
            var prefabItem = gameObject.GetComponent<Components.CSEmulatorPrefabItem>();
            if (prefabItem == null) return false;
            var allow = prefabItem.allowDestroy;
            return allow;
        }

        public ItemHandle[] getItemsNear(EmulateVector3 position, float radius)
        {
            var handles = Physics.OverlapSphere(
                position._ToUnityEngine(), radius,
                CSEmulator.Commons.BuildLayerMask(0, 11, 14, 18), //Default, RidingItem, InteractableItem, GrabbingItem
                QueryTriggerInteraction.Collide
            )
                .Select(c => new {
                    i = c.gameObject.GetComponentInParent<ClusterVR.CreatorKit.Item.IItem>(),
                    c = c
                })
                .Where(t => t.i != null)
                .Where(t => !t.i.Id.Equals(item.Id))
                .Where(t =>
                {
                    if (null != t.c.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IPhysicalShape>())
                        return true;
                    if (null != t.c.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IOverlapSourceShape>())
                        return true;
                    if (!t.c.isTrigger)
                        return true;
                    return false;
                })
                .Select(t => t.i.gameObject.GetComponent<Components.CSEmulatorItemHandler>())
                .Select(h => new ItemHandle(h, this.csItemHandler, messageSender))
                .ToArray();
            return handles;
        }

        public Overlap[] getOverlaps()
        {
            var overlaps = csItemHandler.GetOverlaps()
                .Select(o =>
                {
                    var hitObject = HitObject.Create(
                        o.Item2, this.csItemHandler, o.Item3,
                        playerHandleFactory,
                        messageSender
                    );
                    object selfNode = o.Item1 == "" ? this : subNode(o.Item1);
                    var ret = new Overlap(hitObject, selfNode);
                    return ret;
                }).ToArray();
            return overlaps;
        }

        public PlayerHandle[] getPlayersNear(EmulateVector3 position, float radius)
        {
            var handles = Physics.OverlapSphere(
                position._ToUnityEngine(), radius,
                -1,
                QueryTriggerInteraction.Collide
            )
                .Select(c => c.gameObject.GetComponentInChildren<Components.CSEmulatorPlayerHandler>())
                .Where(h => h != null)
                .Select(h => playerHandleFactory.CreateById(h.id, csItemHandler))
                //いつの間にか重複破棄していた？v2.7.0.4確認
                .GroupBy(h => h.id)
                .Select(g => g.First())
                .ToArray();

            return handles;
        }

        public EmulateVector3 getPosition()
        {
            return new EmulateVector3(gameObject.transform.position);
        }

        public EmulateQuaternion getRotation()
        {
            return new EmulateQuaternion(gameObject.transform.rotation);
        }


        public object getStateCompat(string target, string key, string parameterType)
        {
            var sendable = cckComponentFacade.GetState(target, key, parameterType);
            return sendable;
        }

        public HumanoidAnimation humanoidAnimation(string humanoidAnimationId)
        {
            var list = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IHumanoidAnimationList>();
            var entry = (ClusterVR.CreatorKit.Item.Implements.HumanoidAnimationListEntry)list.HumanoidAnimations.FirstOrDefault(entry => entry.Id == humanoidAnimationId);
            var ha = ClusterVR.CreatorKit.Editor.Builder.HumanoidAnimationBuilder.Build(entry.Animation);
            entry.SetHumanoidAnimation(ha);
            var humanoidAnimation = new HumanoidAnimation(entry);

            return humanoidAnimation;
        }

        public void log(object v)
        {
            if (v == null)
            {
                logger.Info("");
            }
            else if (v is System.Object[] oa)
            {
                logger.Info(CSEmulator.Commons.ObjectArrayToString(oa));
            }
            else if (v is System.Dynamic.ExpandoObject eo)
            {
                logger.Info(CSEmulator.Commons.ExpandoObjectToString(eo, openb: "{", closeb: "}", indent: "", separator: ","));
            }
            else if (v is Jint.Native.Error.JsError je)
            {
                logger.Exception(je);
            }
            else
            {
                logger.Info(v.ToString());
            }
        }

        public MaterialHandle material(string materialId)
        {
            var itemMaterialSetList = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItemMaterialSetList>();
            if (itemMaterialSetList == null)
            {
                logger.Warning("ItemMaterialSetListが指定されていません。");
                return new MaterialHandle(null, itemExceptionFactory);
            }
            var set = itemMaterialSetList.ItemMaterialSets.FirstOrDefault(set => set.Id == materialId);
            if (set.Material == null)
            {
                logger.Warning(String.Format("materialId:{0}がありません。", materialId));
                return new MaterialHandle(null, itemExceptionFactory);
            }

            //アイテム毎にMaterialを複製して使用するような動きの模様
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            var prepared = materialSubstituter.Prepare(set.Material);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i].GetInstanceID() != set.Material.GetInstanceID()) continue;
                    materials[i] = prepared;
                }
                renderer.sharedMaterials = materials;
            }

            var ret = new MaterialHandle(prepared, itemExceptionFactory);
            return ret;
        }

        public void onCollide(Action<Collision> Callback)
        {
            OnCollideHandler = Callback;
        }
        private void CsItemHandler_OnCollision(UnityEngine.Collision data)
        {
            //親でも子でもなくItem本体に付いているか(2.95検証)
            var rigid = gameObject.GetComponent<Rigidbody>();
            if (rigid == null) return;

            //kinematicはNG（2.95検証）
            if (rigid.isKinematic) return;

            var points = data.contacts.Select(c =>
            {
                var point = new CollidePoint(
                    new Hit(
                        new EmulateVector3(c.normal),
                        new EmulateVector3(c.point)
                    ),
                    gameObject.GetInstanceID() == c.thisCollider.gameObject.GetInstanceID()
                        ? this
                        : subNode(c.thisCollider.gameObject.name)
                ); ;
                return point;
            });
            //よくわからないけどRigidbodyが入っている場合はそちらが優先される仕様の模様？(2.95)
            var hitObject = GameObjectToHitObject(
                data.rigidbody?.gameObject ?? data.collider.gameObject
            );
            var collision = new Collision(
                points,
                new EmulateVector3(data.impulse),
                hitObject,
                new EmulateVector3(data.relativeVelocity)
            );
            OnCollideHandler.Invoke(collision);
        }

        public void onExternalCallEnd(Action<string, string, string> Callback)
        {
            externalCaller.SetCallEndCallback(Callback);
        }

        public void onGrab(Action<bool, bool, PlayerHandle> Callback)
        {
            if (!cckComponentFacade.hasGrabbableItem)
            {
                logger.Warning(String.Format("[{0}]onGrab() need [Grabbable Item] component.", this.gameObject.name));
            }
            OnGrabHandler = Callback;
        }
        private void CckComponentFacade_onGrabbed(bool isLeftHand, bool isGrab)
        {
            try
            {
                //一旦右手＆オーナーの検出機能実装まで固定
                var owner = playerHandleFactory.CreateById(
                    itemOwnerHandler.GetOwnerId(),
                    csItemHandler
                );
                owner._ChangeGrabbing(isGrab);
                OnGrabHandler(isGrab, false, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }
        }

        public void onInteract(Action<PlayerHandle> Callback)
        {
            if (!cckComponentFacade.hasCollider)
            {
                logger.Warning(String.Format("[{0}]onInteract() need [Collider] component.", this.gameObject.name));
                return;
            }

            //コライダーがある場合にのみInteractItemTriggerが付く仕様らしい
            cckComponentFacade.AddInteractItemTrigger();
            OnInteractHandler = Callback;
        }
        private void CckComponentFacade_onInteract()
        {
            try
            {
                var owner = playerHandleFactory.CreateById(
                    itemOwnerHandler.GetOwnerId(),
                    csItemHandler
                );
                OnInteractHandler(owner);
            }
            catch (Exception ex)
            {
                Commons.ExceptionLogger(ex, gameObject);
            }
        }

        public void onPhysicsUpdate(Action<double> Callback)
        {
            Action<double> Wrapped = v =>
            {
                isInFixedUpdate = true;
                Callback(v);
                isInFixedUpdate = false;
            };
            fixedUpdateListenerBinder.SetUpdateCallback(gameObject.name, gameObject, Wrapped);
        }

        public void onReceive(Action<string, object, ItemHandle> Callback)
        {
            receiveListenerBinder.SetReceiveCallback(this.csItemHandler, Callback);
        }

        public void onRide(Action<bool, PlayerHandle> Callback)
        {
            if (!cckComponentFacade.hasRidableItem)
            {
                logger.Warning(String.Format("[{0}]onRide() need [Ridable Item] component.", this.gameObject.name));
            }
            OnRideHandler = Callback;
        }
        private void CckComponentFacade_onRide(bool isOn)
        {
            try
            {
                var owner = playerHandleFactory.CreateById(
                    itemOwnerHandler.GetOwnerId(),
                    csItemHandler
                );
                OnRideHandler(isOn, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }
        }

        public void onStart(Action Callback)
        {
            startListenerBinder.SetUpdateCallback(Callback);
        }

        public void onTextInput(Action<string, string, TextInputStatus> Callback)
        {
            textInputListenerBinder.SetReceiveCallback(this.csItemHandler, Callback);
        }

        public void onUpdate(Action<double> Callback)
        {
            updateListenerBinder.SetUpdateCallback(gameObject.name, gameObject, Callback);
        }

        public void onUse(Action<bool, PlayerHandle> Callback)
        {
            OnUseHandler = Callback;
        }
        private void CckComponentFacade_onUse(bool isDown)
        {
            try
            {
                var owner = playerHandleFactory.CreateById(
                    itemOwnerHandler.GetOwnerId(),
                    csItemHandler
                );
                OnUseHandler(isDown, owner);
            }
            catch (Exception ex)
            {
                Commons.ExceptionLogger(ex, gameObject);
            }
        }

        public RaycastResult raycast(
            EmulateVector3 origin, EmulateVector3 direction, float maxDistance
        )
        {
            var isHit = Physics.Raycast(
                origin._ToUnityEngine(),
                direction._ToUnityEngine(),
                out var raycastHit,
                maxDistance,
                -1,
                //トリガーには反応しない仕様らしい
                QueryTriggerInteraction.Ignore
            );

            if (!isHit) return null;

            var hit = new Hit(
                new EmulateVector3(raycastHit.normal),
                new EmulateVector3(raycastHit.point)
            );
            var hitObject = GameObjectToHitObject(raycastHit.transform.gameObject);
            var ret = new RaycastResult(hit, hitObject);

            return ret;

        }

        public RaycastResult[] raycastAll(
            EmulateVector3 origin, EmulateVector3 direction, float maxDistance
        )
        {
            var raycastHits = Physics.RaycastAll(
                origin._ToUnityEngine(),
                direction._ToUnityEngine(),
                maxDistance,
                -1,
                QueryTriggerInteraction.Ignore
            );

            var ret = raycastHits.Select(raycastHit =>
            {
                var hit = new Hit(
                    new EmulateVector3(raycastHit.normal),
                    new EmulateVector3(raycastHit.point)
                );
                var hitObject = GameObjectToHitObject(raycastHit.transform.gameObject);
                var raycastResult = new RaycastResult(hit, hitObject);
                return raycastResult;
            }).ToArray();

            return ret;

        }

        HitObject GameObjectToHitObject(GameObject gameObject)
        {
            //SubNodeにあたることを考えてInParent。Mainの方にあたっても反応する。
            var csItemHandler = gameObject.GetComponentInParent<Components.CSEmulatorItemHandler>();
            //DesktopPlayerControllerにhitするのでchild
            var csPlayerHandler = gameObject.GetComponentInChildren<Components.CSEmulatorPlayerHandler>();
            var hitObject = HitObject.Create(
                csItemHandler, this.csItemHandler, csPlayerHandler,
                playerHandleFactory, messageSender
            );

            return hitObject;
        }

        public void sendSignalCompat(string target, string key)
        {
            cckComponentFacade.SendSignal(target, key);
        }

        public void setPosition(EmulateVector3 v)
        {
            if (!hasMovableItem && !hasCharacterItem)
            {
                logger.Warning(String.Format("[{0}]setPosition() need [Movable Item] or [Character Item] component.", this.gameObject.name));
                return;
            }
            //movableItem.SetPositionAndRotation(
            //    v._ToUnityEngine(), gameObject.transform.rotation, false
            //);
            gameObject.transform.position = v._ToUnityEngine();
        }

        public void setRotation(EmulateQuaternion v)
        {
            if (!hasMovableItem && !hasCharacterItem)
            {
                logger.Warning(String.Format("[{0}]setPosition() need [Movable Item] or [Character Item] component.", this.gameObject.name));
                return;
            }
            //movableItem.SetPositionAndRotation(
            //    gameObject.transform.position, v._ToUnityEngine(), false
            //);
            gameObject.transform.rotation = v._ToUnityEngine();
        }

        public void setStateCompat(string target, string key, object value)
        {
            cckComponentFacade.SetState(target, key, value);
        }

        public SubNode subNode(string subNodeName)
        {
            var child = FindChild(gameObject.transform, subNodeName);
            if (child == null)
            {
                logger.Warning(String.Format("subNode:[{0}] is null.", subNodeName));
                return null;
            }
            var textView = child.gameObject.GetComponent<ClusterVR.CreatorKit.World.ITextView>();
            var ret = new SubNode(
                child, item, textView, updateListenerBinder
            );
            return ret;
        }

        Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;

            var result = parent.Find(name);
            if (result != null)
                return result;

            foreach (Transform child in parent)
            {
                result = FindChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }


        public void DischargeOperateLimit(double time)
        {
            createItemThrottle.Discharge(time);
            callExternalThrottle.Discharge(time);
        }

        public void Shutdown()
        {
            startListenerBinder.DeleteStartCallback();
            updateListenerBinder.DeleteUpdateCallback(gameObject.name);
            fixedUpdateListenerBinder.DeleteUpdateCallback(gameObject.name);
            receiveListenerBinder.DeleteReceiveCallback(this.csItemHandler);
            textInputListenerBinder.DeleteReceiveCallback(this.csItemHandler);
            //プロファイラを見てるとPlayModeを抜ける時に破棄されているようだけど念のため
            materialSubstituter.Destroy();
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.angularVelocity = angularVelocity.clone();
            o.state = new object();
            o.useGravity = useGravity;
            o.velocity = velocity.clone();
            o.id = id;
            o.itemHandle = itemHandle;
            o.itemTemplateId = itemTemplateId;
            return o;
        }
        public override string ToString()
        {
            return String.Format("[ClusterScript][{0}]", gameObject == null ? null : gameObject.name);
        }

    }
}
