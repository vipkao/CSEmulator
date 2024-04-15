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
                new Engine.NearDetectorPreparer(),
                new Implements.UnityDebugLog()
            );
        }

        //CSETODO テストのことを考えてつくったけど、不要になるかもしれない。
        public EngineFacade Create(
            Engine.ItemCollector itemCollector,
            Engine.VrmPreparer vrmPreparer,
            Engine.NearDetectorPreparer nearDetectorPreparer,
            ILogger logger
        )
        {
            return new EngineFacade(
                options,
                itemCollector,
                vrmPreparer,
                nearDetectorPreparer,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.ItemDestroyer,
                ClusterVR.CreatorKit.Editor.Preview.Bootstrap.SpawnPointManager,
                logger
            );
        }
    }
}
