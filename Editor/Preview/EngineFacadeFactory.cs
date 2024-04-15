using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class EngineFacadeFactory
    {
        readonly OptionBridge options;

        public EngineFacadeFactory(
            OptionBridge options
        )
        {
            this.options = options;
        }

        public EngineFacade CreateDefault(
        )
        {
            return Create(
                new Engine.ItemCollector(
                    ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemCreator
                ),
                new Engine.VrmPreparer(
                    options.raw.vrm,
                    options
                )
            );
        }

        //CSETODO テストのことを考えてつくったけど、不要になるかもしれない。
        public EngineFacade Create(
            Engine.ItemCollector itemCollector,
            Engine.VrmPreparer vrmPreparer
        )
        {
            return new EngineFacade(
                itemCollector,
                vrmPreparer,
                options,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemDestroyer,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.SpawnPointManager
            );
        }
    }
}
