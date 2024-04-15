using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    //ClusterScript.createItemのために作られたクラス。不要になったら削除可。
    public class CreateItemTemplate
        : ClusterVR.CreatorKit.Gimmick.ICreateItemGimmick
    {
        public IItem ItemTemplate { get; private set; }

        public ClusterVR.CreatorKit.Item.ItemTemplateId ItemTemplateId { get; private set; }

        public event CreateItemEventHandler OnCreateItem = delegate { };

        public CreateItemTemplate(
            UnityEngine.GameObject prefab,
            ItemTemplateId itemTemplateId
        )
        {
            var item = prefab.GetComponent<IItem>();
            this.ItemTemplate = item;
            this.ItemTemplateId = itemTemplateId.cckId;
        }
    }
}
