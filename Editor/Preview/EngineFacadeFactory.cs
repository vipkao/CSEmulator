using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class EngineFacadeFactory
    {
        readonly EmulatorOptions options;

        public EngineFacadeFactory(
            EmulatorOptions options
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
                    options.vrm
                ),
                new Implements.UnityDebugLog()
            );
        }

        //CSETODO テストのことを考えてつくったけど、不要になるかもしれない。
        public EngineFacade Create(
            Engine.ItemCollector itemCollector,
            Engine.VrmPreparer vrmPreparer,
            ILogger logger
        )
        {
            return new EngineFacade(
                options,
                itemCollector,
                vrmPreparer,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemDestroyer,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.SpawnPointManager,
                logger
            );
        }
    }
}
