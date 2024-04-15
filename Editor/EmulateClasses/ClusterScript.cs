using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ClusterVR.CreatorKit;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class ClusterScript
    {
        readonly GameObject gameObject;
        readonly ClusterVR.CreatorKit.Trigger.ISignalGenerator signalGenerator;
        readonly ClusterVR.CreatorKit.Gimmick.IGimmickUpdater gimmickUpdater;
        readonly ClusterVR.CreatorKit.Operation.ILogicStateRepository stateRepository;
        readonly ClusterVR.CreatorKit.Operation.LogicExecutor logicExecutor;
        readonly ClusterVR.CreatorKit.Editor.Preview.Item.ItemCreator itemCreator;
        readonly ClusterVR.CreatorKit.Editor.Preview.Item.ItemDestroyer itemDestroyer;
        readonly IUpdateListenerBinder updateListenerBinder;
        readonly IUpdateListenerBinder fixedUpdateListenerBinder;
        readonly IReceiveListenerBinder receiveListenerBinder;
        readonly IMessageSender messageSender;
        readonly IPrefabItemHolder prefabItemHolder;
        readonly IPlayerHandleHolder playerHandleHolder;
        readonly IPlayerControllerFactory playerControllerFactory;
        readonly IProgramStatus programStatus;
        readonly StateProxy stateProxy;
        readonly ILogger logger;

        readonly ClusterVR.CreatorKit.Item.Implements.MovableItem movableItem;
        readonly ClusterVR.CreatorKit.Item.IItem item;
        readonly Components.CSEmulatorItemHandler csItemHandler;

        readonly bool hasMovableItem;
        readonly bool hasCharacterItem;
        readonly bool hasGrabbableItem;
        readonly bool hasRidableItem;
        readonly bool hasCollider;

        bool isGrab = false;
        bool isInFixedUpdate = false;

        Action<Collision> OnCollideHandler = _ => { };
        Action<bool, bool, PlayerHandle> OnGrabHandler = (_, _, _) => { };
        Action<PlayerHandle> OnInteractHandler = _ => { };
        Action<bool, PlayerHandle> OnRideHandler = (_, _) => { };
        Action<bool, PlayerHandle> OnUseHandler = (_, _) => { };

        Action OnInteractInitialize = () => { };

        public ClusterScript(
            GameObject gameObject,
            ClusterVR.CreatorKit.Editor.Preview.RoomState.RoomStateRepository roomStateRepository,
            ClusterVR.CreatorKit.Trigger.ISignalGenerator signalGenerator,
            ClusterVR.CreatorKit.Gimmick.IGimmickUpdater gimmickUpdater,
            ClusterVR.CreatorKit.Editor.Preview.Item.ItemCreator itemCreator,
            ClusterVR.CreatorKit.Editor.Preview.Item.ItemDestroyer itemDestroyer,
            IUpdateListenerBinder updateListenerBinder,
            IUpdateListenerBinder fixedUpdateListenerBinder,
            IReceiveListenerBinder receiveListenerBinder,
            IMessageSender messageSender,
            IPrefabItemHolder prefabItemHolder,
            IPlayerHandleHolder playerHandleHolder,
            IPlayerControllerFactory playerControllerFactory,
            StateProxy stateProxy,
            ILogger logger
        )
        {
            this.gameObject = gameObject;
            this.stateRepository = CreateLogicStateRepositiory(roomStateRepository);
            this.logicExecutor = new ClusterVR.CreatorKit.Operation.LogicExecutor(
                signalGenerator, stateRepository, gimmickUpdater
            );
            this.signalGenerator = signalGenerator;
            this.gimmickUpdater = gimmickUpdater;
            this.itemCreator = itemCreator;
            this.itemDestroyer = itemDestroyer;
            this.updateListenerBinder = updateListenerBinder;
            this.fixedUpdateListenerBinder = fixedUpdateListenerBinder;
            this.receiveListenerBinder = receiveListenerBinder;
            this.messageSender = messageSender;
            this.prefabItemHolder = prefabItemHolder;
            this.playerHandleHolder = playerHandleHolder;
            this.playerControllerFactory = playerControllerFactory;
            this.stateProxy = stateProxy;
            this.logger = logger;

            item = this.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItem>();
            csItemHandler = this.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            csItemHandler.OnCollision += CsItemHandler_OnCollision;
            hasMovableItem = this.gameObject.TryGetComponent(out movableItem);
            hasCharacterItem = this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Item.Implements.CharacterItem>(out var _);
            hasGrabbableItem = false;
            hasRidableItem = false;
            hasCollider = (this.gameObject.GetComponentInChildren<Collider>() != null);

            //ItemTrigger系と重複した時の挙動とか含めて、たぶんこれであってると思うけど自信ない。
            if (this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Item.Implements.GrabbableItem>(out var grabbableItem))
            {
                hasGrabbableItem = true;

                //onGrabはGrabbableItemが必須で、共存可
                grabbableItem.OnGrabbed += GrabbableItem_OnGrabbed;
                grabbableItem.OnReleased += GrabbableItem_OnReleased;

                if (!this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Trigger.Implements.UseItemTrigger>(out var _))
                {
                    //onUseは既存のがあった場合は不発なので、こちらで追加して発火させる
                    var useItemTrigger = this.gameObject.AddComponent<ClusterVR.CreatorKit.Trigger.Implements.UseItemTrigger>();
                    useItemTrigger.TriggerEvent += UseItemTrigger_TriggerEvent;

                    //downTriggersだけにダミーを入れておいて、downかupかを判定する材料にする。
                    typeof(ClusterVR.CreatorKit.Trigger.Implements.UseItemTrigger)
                        .GetField("downTriggersCache", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(
                            useItemTrigger,
                            new ClusterVR.CreatorKit.Trigger.TriggerParam[]
                            {
                                new ClusterVR.CreatorKit.Trigger.TriggerParam(
                                    ClusterVR.CreatorKit.Trigger.TriggerTarget.Item,
                                    new ClusterVR.CreatorKit.Item.Implements.Item(),
                                    "ahupa40t4ohpiu", //重複しないように適当
                                    ClusterVR.CreatorKit.ParameterType.Bool,
                                    new ClusterVR.CreatorKit.Trigger.TriggerValue(true)
                                )
                            }
                        );
                    typeof(ClusterVR.CreatorKit.Trigger.Implements.UseItemTrigger)
                        .GetField("upTriggersCache", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(
                            useItemTrigger,
                            new ClusterVR.CreatorKit.Trigger.TriggerParam[0]
                        );
                }
            }
            else if (this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Item.Implements.RidableItem>(out var ridableItem))
            {
                hasRidableItem = true;
                //onRideはRidableItemが必須で、共存可
                ridableItem.OnGetOn += RidableItem_OnGetOn;
                ridableItem.OnGetOff += RidableItem_OnGetOff;
            }
            //ContactableItemは共存できないのでelse if
            else if (!this.gameObject.TryGetComponent<ClusterVR.CreatorKit.Trigger.Implements.InteractItemTrigger>(out var _))
            {
                //ここで実行すると、レイヤーが14:InteractableItem layerになってしまうため、onInteract登録時に実行する。
                OnInteractInitialize = () =>
                {
                    //onInteractは既存のがあった場合は不発で、こちらで追加して発火
                    var interactItemTrigger = this.gameObject.AddComponent<ClusterVR.CreatorKit.Trigger.Implements.InteractItemTrigger>();
                    interactItemTrigger.TriggerEvent += InteractItemTrigger_TriggerEvent;

                    typeof(ClusterVR.CreatorKit.Trigger.Implements.InteractItemTrigger)
                        .GetField("triggers", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(
                            interactItemTrigger,
                            new ClusterVR.CreatorKit.Trigger.Implements.ConstantTriggerParam[0]
                        );

                    OnInteractInitialize = () => { };
                };
            }

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
                if (isGrab) return;
                if (!hasMovableItem) throw new ClusterScriptError("Need MovableItem.");
                movableItem.Rigidbody.angularVelocity = value._ToUnityEngine();
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
                if (!hasMovableItem) throw new ClusterScriptError("Need MovableItem.");
                if (movableItem.Rigidbody.isKinematic) throw new ClusterScriptError("Need not Kinematic.");
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
                if (isGrab) return;
                if (!hasMovableItem) throw new ClusterScriptError("Need MovableItem.");
                movableItem.Rigidbody.velocity = value._ToUnityEngine();
            }
        }

        public void addForce(EmulateVector3 force)
        {
            if (!isInFixedUpdate)
                throw new ClusterScriptError("onPhysicsUpdate内でのみ実行可能") {
                    executionNotAllowed = true
                };
            movableItem.AddForce(force._ToUnityEngine(), ForceMode.Force);
        }

        public void addForceAt(EmulateVector3 force, EmulateVector3 position)
        {
            if (!isInFixedUpdate)
                throw new ClusterScriptError("onPhysicsUpdate内でのみ実行可能")
                {
                    executionNotAllowed = true
                };
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
                throw new ClusterScriptError("onPhysicsUpdate内でのみ実行可能")
                {
                    executionNotAllowed = true
                };
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

        public ItemHandle createItem(
            ItemTemplateId itemTemplateId,
            EmulateVector3 position,
            EmulateQuaternion rotation
        )
        {
            //実行ごとにテンプレートを登録
            var prefab = prefabItemHolder.GetPrefab(itemTemplateId.id);
            var template = new CreateItemTemplate(prefab, itemTemplateId);
            var AddItemTemplate = itemCreator.GetType().GetMethod("AddItemTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
            AddItemTemplate.Invoke(itemCreator, new object[1] { template });

            ClusterVR.CreatorKit.Item.IItem create = null;
            Action<ClusterVR.CreatorKit.Item.IItem> OnCreateCompleted = item =>
            {
                //イベント引数をキャプチャで取得する。
                create = item;
            };
            itemCreator.OnCreateCompleted += OnCreateCompleted;
            itemCreator.Create(
                itemTemplateId.cckId,
                position._ToUnityEngine(),
                rotation._ToUnityEngine()
            );
            itemCreator.OnCreateCompleted -= OnCreateCompleted;
            if (create == null) return null;

            var csItemHandler = create.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            var ret = new ItemHandle(csItemHandler, this.csItemHandler, messageSender);

            return ret;
        }

        public void destroy()
        {
            if (!csItemHandler.isCreatedItem)
                throw new ClusterScriptError("動的アイテムのみ") {
                    executionNotAllowed = true
                };
            itemDestroyer.Destroy(item);
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
                    var hitObject = HitObject.Create(o.Item2, this.csItemHandler, o.Item3, playerControllerFactory, messageSender);
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
                .Select(h => new PlayerHandle(playerControllerFactory.Create(h), csItemHandler))
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
            //LogicStateRepository経由で操作すると、itemとかplayerとかVector3とかの面倒なあれこれをやってくれるので便利
            var stateValueSet = stateRepository.GetRoomStateValueSet(
                new ClusterVR.CreatorKit.Operation.SourceState(
                    StringToGimmickTarget(target),
                    key,
                    StringToParameterType(parameterType)
                ),
                itemId
            );
            var sendable = StateValueSetToSendable(stateValueSet);
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

        public void onCollide(Action<Collision> Callback)
        {
            OnCollideHandler = Callback;
        }
        private void CsItemHandler_OnCollision(string name, UnityEngine.Collision data)
        {
            //動いていないColliderにキャラがぶつかりに行ってもOnCollisionが発生しないという問題は
            //既知の問題としてあるらしく、ひとまずはＯＫとする。
            //どうやらキャラ側のHitが来た後にめり込み修正が入り、
            //OnCollisionが発生しない可能性があるらしい。
            var points = data.contacts.Select(c =>
            {
                var point = new CollidePoint(
                    new Hit(
                        new EmulateVector3(c.normal),
                        new EmulateVector3(c.point)
                    ),
                    name == "" ? this : subNode(name)
                );
                return point;
            });
            var hitObject = GameObjectToHitObject(data.collider.gameObject);
            var collision = new Collision(
                points,
                new EmulateVector3(data.impulse),
                hitObject,
                new EmulateVector3(data.relativeVelocity)
            );
            OnCollideHandler.Invoke(collision);
        }

        public void onGrab(Action<bool, bool, PlayerHandle> Callback)
        {
            if (!hasGrabbableItem)
            {
                logger.Warning(String.Format("[{0}]onGrab() need [Grabbable Item] component.", this.gameObject.name));
            }
            OnGrabHandler = Callback;
        }
        void GrabbableItem_OnGrabbed(bool isLeftHand)
        {
            isGrab = true;
            try
            {
                //一旦右手＆オーナーの検出機能実装まで固定
                var owner = new PlayerHandle(
                    playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                    csItemHandler
                );
                OnGrabHandler(true, false, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }

        }
        void GrabbableItem_OnReleased(bool isLeftHand)
        {
            isGrab = false;
            try
            {
                //一旦右手＆オーナーの検出機能実装まで固定
                var owner = new PlayerHandle(
                    playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                    csItemHandler
                );
                OnGrabHandler(false, false, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }
        }

        public void onInteract(Action<PlayerHandle> Callback)
        {
            if (!hasCollider)
            {
                logger.Warning(String.Format("[{0}]onInteract() need [Collider] component.", this.gameObject.name));
                return;
            }

            //コライダーがある場合にのみInteractItemTriggerが付く仕様らしい
            OnInteractInitialize();
            OnInteractHandler = Callback;
        }
        void InteractItemTrigger_TriggerEvent(ClusterVR.CreatorKit.Trigger.IItemTrigger sender, ClusterVR.CreatorKit.Trigger.TriggerEventArgs e)
        {
            try
            {
                var owner = new PlayerHandle(
                    playerControllerFactory.Create(playerHandleHolder.GetOwner()),
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
            if (!hasRidableItem)
            {
                logger.Warning(String.Format("[{0}]onRide() need [Ridable Item] component.", this.gameObject.name));
            }
            OnRideHandler = Callback;
        }
        void RidableItem_OnGetOff()
        {
            try
            {
                var owner = new PlayerHandle(
                    playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                    csItemHandler
                );
                OnRideHandler(false, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }
        }
        void RidableItem_OnGetOn()
        {
            try
            {
                var owner = new PlayerHandle(
                    playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                    csItemHandler
                );
                OnRideHandler(true, owner);
            }
            catch (Exception e)
            {
                Commons.ExceptionLogger(e, gameObject);
            }
        }

        public void onUpdate(Action<double> Callback)
        {
            updateListenerBinder.SetUpdateCallback(gameObject.name, gameObject, Callback);
        }

        public void onUse(Action<bool, PlayerHandle> Callback)
        {
            OnUseHandler = Callback;
        }
        private void UseItemTrigger_TriggerEvent(ClusterVR.CreatorKit.Trigger.IItemTrigger sender, ClusterVR.CreatorKit.Trigger.TriggerEventArgs e)
        {
            //ダミーが入ってるならdown
            if (e.TriggerParams.Length > 0)
            {
                try
                {
                    var owner = new PlayerHandle(
                        playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                        csItemHandler
                    );
                    OnUseHandler(true, owner);
                }
                catch (Exception ex)
                {
                    Commons.ExceptionLogger(ex, gameObject);
                }
            }
            else
            {
                try
                {
                    var owner = new PlayerHandle(
                        playerControllerFactory.Create(playerHandleHolder.GetOwner()),
                        csItemHandler
                    );
                    OnUseHandler(false, owner);
                }
                catch (Exception ex)
                {
                    Commons.ExceptionLogger(ex, gameObject);
                }
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
            var csPlayerHandler = gameObject.GetComponentInParent<Components.CSEmulatorPlayerHandler>();
            var hitObject = HitObject.Create(
                csItemHandler, this.csItemHandler, csPlayerHandler, playerControllerFactory ,messageSender
            );

            return hitObject;
        }

        public void sendSignalCompat(string target, string key)
        {
            //Signalを送るLogicを作って実行させている
            var sendSignalLogic = new ClusterVR.CreatorKit.Operation.Logic(
                new ClusterVR.CreatorKit.Operation.Statement[]
                {
                    new ClusterVR.CreatorKit.Operation.Statement(
                        new ClusterVR.CreatorKit.Operation.SingleStatement(
                            new ClusterVR.CreatorKit.Operation.TargetState(
                                StringToTargetStateTarget(target),
                                key,
                                ClusterVR.CreatorKit.ParameterType.Signal
                            ),
                            new ClusterVR.CreatorKit.Operation.Expression(
                                new ClusterVR.CreatorKit.Operation.Value(
                                    new ClusterVR.CreatorKit.Operation.ConstantValue(true)
                                )
                            )
                        )
                    )
                }
            );
            logicExecutor.Execute(sendSignalLogic, itemId);
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
            //Logicを作って値を送る方式をとっていた。
            //しかし、ConstantValueでdouble値が送れないという問題が発覚した。
            //そのため、LogicExecuterの処理を参考に記述することにした。
            //将来LogicExecuterの処理がどう変更されるか分からないので要注意。
            var updatedKeys = new Queue<string>();
            var stateValueSet = ObjectToStateValueSet(value);
            var parameterType = stateValueSet.ParameterType;
            var targetState = new ClusterVR.CreatorKit.Operation.TargetState(
                StringToTargetStateTarget(target),
                key,
                stateValueSet.ParameterType
            );
            stateRepository.UpdateState(
                targetState,
                itemId,
                stateValueSet.CastTo(parameterType),
                updatedKeys
            );
            gimmickUpdater.OnStateUpdated(updatedKeys);


            ////値を設定するLogicを作って実行させている
            //var constantValue = ObjectToConstantValue(value);
            //var setStateLogic = new ClusterVR.CreatorKit.Operation.Logic(
            //    new ClusterVR.CreatorKit.Operation.Statement[]
            //    {
            //        new ClusterVR.CreatorKit.Operation.Statement(
            //            new ClusterVR.CreatorKit.Operation.SingleStatement(
            //                new ClusterVR.CreatorKit.Operation.TargetState(
            //                    StringToTargetStateTarget(target),
            //                    key,
            //                    constantValue.Type
            //                ),
            //                new ClusterVR.CreatorKit.Operation.Expression(
            //                    new ClusterVR.CreatorKit.Operation.Value(
            //                        constantValue
            //                    )
            //                )
            //            )
            //        )
            //    }
            //);
            //logicExecutor.Execute(setStateLogic, itemId);
        }


        public SubNode subNode(string subNodeName)
        {
            var child = FindChild(gameObject.transform, subNodeName);
            if (child == null)
                logger.Warning(String.Format("subNode:[{0}] is null.", subNodeName));
            var ret = new SubNode(child, item, updateListenerBinder);
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



        ClusterVR.CreatorKit.Operation.ILogicStateRepository CreateLogicStateRepositiory(
            ClusterVR.CreatorKit.Editor.Preview.RoomState.RoomStateRepository roomStateRepository
        )
        {
            //RoomStateRepositoryへの値の出し入れはLogicStateRepositoryが行っていそうなので
            var asm = typeof(ClusterVR.CreatorKit.Editor.Preview.Operation.LogicManager).Assembly;
            var type = asm.GetType(typeof(ClusterVR.CreatorKit.Editor.Preview.Operation.LogicManager).FullName + "+LogicStateRepository");
            var ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First();
            var ret = (ClusterVR.CreatorKit.Operation.ILogicStateRepository)ctor.Invoke(new object[] { roomStateRepository });
            return ret;
        }

        object StateValueSetToSendable(
            ClusterVR.CreatorKit.IStateValueSet stateValueSet
        )
        {
            switch (stateValueSet.ParameterType)
            {
                case ClusterVR.CreatorKit.ParameterType.Bool:
                    return stateValueSet.ToGimmickValue().BoolValue;
                case ClusterVR.CreatorKit.ParameterType.Double:
                    return stateValueSet.GetStateValue(new ClusterVR.CreatorKit.FieldName()).ToDouble();
                case ClusterVR.CreatorKit.ParameterType.Float:
                    return stateValueSet.GetStateValue(new ClusterVR.CreatorKit.FieldName()).ToFloat();
                case ClusterVR.CreatorKit.ParameterType.Integer:
                    return stateValueSet.GetStateValue(new ClusterVR.CreatorKit.FieldName()).ToInt();
                case ClusterVR.CreatorKit.ParameterType.Signal:
                    return stateValueSet.GetStateValue(new ClusterVR.CreatorKit.FieldName()).ToDateTime();
                case ClusterVR.CreatorKit.ParameterType.Vector2:
                    return new EmulateVector2(
                        stateValueSet.ToGimmickValue().Vector2Value
                    );
                case ClusterVR.CreatorKit.ParameterType.Vector3:
                    return new EmulateVector3(
                        stateValueSet.ToGimmickValue().Vector3Value
                    );
                default: throw new NotImplementedException(stateValueSet.ParameterType.ToString());
            }
        }

        ClusterVR.CreatorKit.Operation.TargetStateTarget StringToTargetStateTarget(
            string target
        )
        {
            switch (target)
            {
                case "this": return ClusterVR.CreatorKit.Operation.TargetStateTarget.Item;
                case "owner": return ClusterVR.CreatorKit.Operation.TargetStateTarget.Player;
                default: throw new ArgumentException(target);
            }
        }

        ClusterVR.CreatorKit.Gimmick.GimmickTarget StringToGimmickTarget(
            string target
        )
        {
            switch (target)
            {
                case "this": return ClusterVR.CreatorKit.Gimmick.GimmickTarget.Item;
                case "owner": return ClusterVR.CreatorKit.Gimmick.GimmickTarget.Player;
                case "global": return ClusterVR.CreatorKit.Gimmick.GimmickTarget.Global;
                default: throw new ArgumentException(target);
            }
        }

        ClusterVR.CreatorKit.ParameterType StringToParameterType(
            string parameterType
        )
        {
            switch (parameterType)
            {
                case "signal": return ClusterVR.CreatorKit.ParameterType.Signal;
                case "boolean": return ClusterVR.CreatorKit.ParameterType.Bool;
                case "float": return ClusterVR.CreatorKit.ParameterType.Float;
                case "double": return ClusterVR.CreatorKit.ParameterType.Double;
                case "integer": return ClusterVR.CreatorKit.ParameterType.Integer;
                case "vector2": return ClusterVR.CreatorKit.ParameterType.Vector2;
                case "vector3": return ClusterVR.CreatorKit.ParameterType.Vector3;
                default: throw new ArgumentException(parameterType);
            }
        }

        ClusterVR.CreatorKit.Operation.ConstantValue ObjectToConstantValue(
            object value
        )
        {
            switch (value)
            {
                case bool boolValue:
                    return new ClusterVR.CreatorKit.Operation.ConstantValue(boolValue);
                case double doubleValue:
                    //numberはdoubleになるけど、ConstantValueはfloatまで
                    return new ClusterVR.CreatorKit.Operation.ConstantValue((float)doubleValue);
                case EmulateVector2 vector2Value:
                    return new ClusterVR.CreatorKit.Operation.ConstantValue(
                        vector2Value._ToUnityEngine()
                    );
                case EmulateVector3 vector3Value:
                    return new ClusterVR.CreatorKit.Operation.ConstantValue(
                        vector3Value._ToUnityEngine()
                    );
                default: throw new NotImplementedException();
            }
        }

        ClusterVR.CreatorKit.IStateValueSet ObjectToStateValueSet(
            object value
        )
        {
            switch (value)
            {
                case bool boolValue:
                    return new ClusterVR.CreatorKit.BoolStateValueSet(boolValue);
                case double doubleValue:
                    return new ClusterVR.CreatorKit.DoubleStateValueSet(doubleValue);
                case EmulateVector2 vector2Value:
                    return new ClusterVR.CreatorKit.Vector2StateValueSet(
                        vector2Value._ToUnityEngine()
                    );
                case EmulateVector3 vector3Value:
                    return new ClusterVR.CreatorKit.Vector3StateValueSet(
                        vector3Value._ToUnityEngine()
                    );
                default: throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return String.Format("[ClusterScript][{0}]", gameObject.name);
        }

    }
}
