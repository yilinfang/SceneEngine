using UnityEngine;
using System.Collections.Generic;



public class StartSE : MonoBehaviour {
	
	private static bool

        Started = false;

    //SE.Objects.A a;

    // Use this for initialization
    void Start() {
        //SE.Control.QuickStart();

        if (!Started) {
            SE.Control.QuickStart();
            Started = true;
        } else {
            SE.Control.QuickReset();
        }

		SE.Geometries.Rectangle<long>
		    ManagedTerrainRegion = new SE.Geometries.Rectangle<long> (
			    0, 1000 * 1000 * 1000,
			    0, 1000 * 1000 * 1000
		    );
        long[]
		ManagedTerrainVertex = new long[4] { 0, 0, 0, 0, };
        SE.RandomSeed[]
            ManagedTerrainRandomSeed = new SE.RandomSeed[5] {
            new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
            };
        SE.TerrainUnitData.Impact[]
            ManagedTerrainImpacts = new SE.TerrainUnitData.Impact[2] {
                new SE.TerrainImpacts.BasicSmooth(),
			    new SE.TerrainImpacts.BasicRandomAdjust(),
            };

        SE.Kernel.RegistRootObject(
            "RootTerrain",
            new SE.TerrainManager.ManagedTerrain(
                new SE.TerrainUnitData(
                    ref ManagedTerrainRegion,
                    ref ManagedTerrainVertex,
                    ref ManagedTerrainRandomSeed,
                    ref ManagedTerrainImpacts
                ),
                true
            ),
            new SE.LongVector3(0, 0, 0),
            new Quaternion()
        );

        SE.Thread.Async(delegate () {
            SE.Kernel.SetTemporarySenceCenter(new SE.LongVector3(0, 0, 0));
            long x = 0;
            while (true) {
                x += 10000;
                SE.Kernel.SetTemporarySenceCenter(new SE.LongVector3(x, -x, x));
                System.Threading.Thread.Sleep(1000);
            }
        });
    }
	
	// Update is called once per frame
	void Update () {

    }

	~StartSE() {
		if (Started) {
			Debug.Log ("SE Stop.");
			SE.Control.QuickReset ();
			Started = false;
		}
    }
}
