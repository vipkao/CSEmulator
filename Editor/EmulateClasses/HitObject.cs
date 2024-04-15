﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class HitObject
    {
        public readonly ItemHandle itemHandle;
        public readonly PlayerHandle playerHandle;

        public HitObject(
            ItemHandle itemHandle,
            PlayerHandle playerHandle
        )
        {
            this.itemHandle = itemHandle;
            this.playerHandle = playerHandle;
        }

        public static HitObject Create(
            CSEmulator.Components.CSEmulatorItemHandler csItemHandler,
            CSEmulator.Components.CSEmulatorItemHandler csItemOwnerHandler,
            CSEmulator.Components.CSEmulatorPlayerHandler csPlayerHandler,
            IPlayerHandleFactory playerHandleFactory,
            IMessageSender messageSender
        )
        {
            var itemHandler = csItemHandler == null ? null : new ItemHandle(csItemHandler, csItemOwnerHandler, messageSender);
            var playerHandler = csPlayerHandler == null ? null : playerHandleFactory.CreateById(
                csPlayerHandler.id,
                csItemOwnerHandler
            );
            var hitObject = new HitObject(itemHandler, playerHandler);
            return hitObject;
        }

        public object toJSON(string key)
        {
            return this;
        }
        public override string ToString()
        {
            return String.Format("[HitObject][{0}][{1}]", itemHandle, playerHandle);
        }
    }
}
