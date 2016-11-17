using UnityEngine;
using System.Collections.Generic;
using SE.TerrainImpacts;

public class StartSE : MonoBehaviour {

    public static SE.Kernel SEKernel;

    private static bool Started = false;
    private static GameObject Center;
    private static SE.LongVector3 Position;

    void Start() {

        SEKernel = new SE.Kernel(new SE.Kernel.Settings());
        SEKernel.AssignThreadManager(new SE.Modules.ThreadManager());
        SE.Modules.ObjectManager om = new SE.Modules.ObjectManager(new SE.Modules.ObjectManager.Settings());
        om.AssignObjectUpdateManager(new SE.Modules.ObjectUpdateManager(new SE.Modules.ObjectUpdateManager.Settings()));
        SEKernel.AssignObjectManager(om);
        SE.Modules.TerrainManager tm = new SE.Modules.TerrainManager(new SE.Modules.TerrainManager.Settings());
        SEKernel.AssignModules(new SE.IModule[1] { tm, });
        SEKernel.Start();

        SE.Geometries.Rectangle<long> ManagedTerrainRegion = new SE.Geometries.Rectangle<long>(
            0, 1000 * 1000 * 1000,
            0, 1000 * 1000 * 1000
        ), LiftWholeRegion = new SE.Geometries.Rectangle<long>(
            15 * 1000, 850 * 1000,
            15 * 1000, 850 * 1000
        ), LiftCentralRegion = new SE.Geometries.Rectangle<long>(
            30 * 1000, 700 * 1000,
            30 * 1000, 700 * 1000
        );

        long[] ManagedTerrainVertex = new long[4] { 0, 1000 * 1000, 1000 * 1000, 0, };

        SE.RandomSeed[] ManagedTerrainRandomSeed = new SE.RandomSeed[5] {
            new SE.RandomSeed(236565345),
			new SE.RandomSeed(236565345),
			new SE.RandomSeed(236565345),
			new SE.RandomSeed(236565345),
			new SE.RandomSeed(236565345),
        };

        long[] tt = new long[5] { 1000, 30 * 1000, 100 * 1000, 300 * 1000, 1000 * 1000 };

        SE.Pair<long, SE.RandomTree<ObjectGenerator.ObjectGenerateItem>>[] ManagedTerrainGenerateDataArray = new SE.Pair<long, SE.RandomTree<ObjectGenerator.ObjectGenerateItem>>[5];
        for (int i = 0; i < 5; i++) {
            ManagedTerrainGenerateDataArray[i] = new SE.Pair<long, SE.RandomTree<ObjectGenerator.ObjectGenerateItem>>(tt[i], new SE.RandomTree<ObjectGenerator.ObjectGenerateItem>(null,5));
            ManagedTerrainGenerateDataArray[i].Second.Add(new SE.ObjectGenerateItems.Planes(tt[i] / 2), 1, 0, 1, -1);
            ManagedTerrainGenerateDataArray[i].Second.Init();
        }

        List<SE.TerrainUnitData.Impact> ManagedTerrainImpacts = new List<SE.TerrainUnitData.Impact>();
        ManagedTerrainImpacts.Add(new BasicSmooth());
        ManagedTerrainImpacts.Add(new BasicRandomAdjust());
        ManagedTerrainImpacts.Add(new BasicToExtend());
        ManagedTerrainImpacts.Add(new ObjectGenerator(
            ref ManagedTerrainRegion,
            ManagedTerrainGenerateDataArray
        ));
        //ManagedTerrainImpacts.Add(new WaterGenerator(
        //    new WaterGenerator.StandardWaterFactory("Prefabs/MyWater"),
        //    ref ManagedTerrainRegion,
        //    10 * 1000,
        //    10 * 1000
        //));
        //ManagedTerrainImpacts.Add(new SE.TerrainImpacts.GenerateSmoothPlane());

        Dictionary<int, List<SE.CollisionRegion>> ManagedTerrainCollisionRegions = new Dictionary<int, List<SE.CollisionRegion>>();

        SEKernel.RegistRootObject(
            "RootTerrain",
            new SE.Modules.TerrainManager.ManagedTerrain(
                tm,
                new SE.TerrainUnitData(
                    ref ManagedTerrainRegion,
                    ManagedTerrainVertex,
                    ManagedTerrainVertex,
                    ManagedTerrainRandomSeed,
                    ManagedTerrainImpacts,
                    ManagedTerrainCollisionRegions
                ),
                true
            ),
            new SE.LongVector3(0, 0, 0),
            new Quaternion()
        );
    }

	~StartSE() {
        SEKernel.Stop();
    }
}
