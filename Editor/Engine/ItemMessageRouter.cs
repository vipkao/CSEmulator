using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class ItemMessageRouter
        : EmulateClasses.IReceiveListenerBinder,
        EmulateClasses.IMessageSender
    {
        class Message
        {
            public readonly long tick;
            public readonly ulong targetId;
            public readonly string requestName;
            public readonly object arg;
            public readonly Components.CSEmulatorItemHandler sender;
            public Message(
                long tick,
                ulong targetId,
                string requestName,
                object arg,
                Components.CSEmulatorItemHandler sender
            )
            {
                this.tick = tick;
                this.targetId = targetId;
                this.requestName = requestName;
                this.arg = arg;
                this.sender = sender;
            }
        }

        readonly Dictionary<ulong, (Components.CSEmulatorItemHandler owner, Action<string, object, EmulateClasses.ItemHandle>)> receivers = new Dictionary<ulong, (Components.CSEmulatorItemHandler, Action<string, object, EmulateClasses.ItemHandle>)>();
        readonly List<Message> queue = new List<Message>();

        public ITicker ticker = new Implements.DateTimeTicks();

        public ItemMessageRouter()
        {
        }

        public void Send(
            ulong id,
            string requestName,
            object arg,
            Components.CSEmulatorItemHandler sender
        )
        {
            //ディレイがある方がよさそう？0.1秒後に通知。
            var tick = ticker.Ticks() + 1_000_000;
            var message = new Message(tick, id, requestName, arg, sender);
            queue.Add(message);
        }

        public void Routing()
        {
            var now = ticker.Ticks();
            //途中で削除するのでToArray
            foreach(var message in queue.ToArray())
            {
                if (message.tick > now) continue;
                queue.Remove(message);

                if (!receivers.ContainsKey(message.targetId)) continue;

                var (owner, receiver) = receivers[message.targetId];
                //このタイミングでItemHandleがスクリプト空間を超えるので
                //owner(スクリプト空間主＝$)が切り替わる。
                //切り替わるタイミングで、過去ownerがhandleを保持している可能性はあるのでnewで作り直す。
                var sender = new EmulateClasses.ItemHandle(
                    message.sender, //senderのItemHandleということ
                    owner,
                    this
                );
                var arg = EmulateClasses.StateProxy.SanitizeSingleValue(
                    message.arg,
                    h => new EmulateClasses.ItemHandle(
                        h.csItemHandler,
                        owner,
                        this
                    ),
                    h => new EmulateClasses.PlayerHandle(
                        h,
                        owner
                    )
                );
                receiver.Invoke(message.requestName, arg, sender);
            }
        }

        public void SetReceiveCallback(Components.CSEmulatorItemHandler owner, Action<string, object, EmulateClasses.ItemHandle> Callback)
        {
            receivers.Add(owner.item.Id.Value, (owner, Callback));
        }

        public void DeleteReceiveCallback(Components.CSEmulatorItemHandler owner)
        {
            if (!receivers.ContainsKey(owner.item.Id.Value)) return;
            receivers.Remove(owner.item.Id.Value);
        }
    }
}
