using UnityEngine;
using System.Collections.Generic;
using SE.TerrainImpacts;

public class StartSE : MonoBehaviour {

    private static bool Started = false;
    private static GameObject Center;
    private static SE.LongVector3 Position;

    void Start() {
        if (!Started) {
            SE.Control.QuickStart();
            Started = true;
        } else {
            SE.Control.QuickReset();
        }

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
        ManagedTerrainImpacts.Add(new ObjectGenerator(ref ManagedTerrainRegion, ManagedTerrainGenerateDataArray));
        //ManagedTerrainImpacts.Add(new SE.TerrainImpacts.GenerateSmoothPlane());

        Dictionary<int, List<SE.CollisionRegion>> ManagedTerrainCollisionRegions = new Dictionary<int, List<SE.CollisionRegion>>();

        SE.Kernel.RegistRootObject(
            "RootTerrain",
            new SE.TerrainManager.ManagedTerrain(
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
		if (Started) {
			Debug.Log ("SE Stop.");
			SE.Control.QuickReset ();
			Started = false;
		}
    }
}
