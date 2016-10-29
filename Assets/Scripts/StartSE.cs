using UnityEngine;
using System.Collections.Generic;



public class StartSE : MonoBehaviour {
	
	private static bool

        Started = false;

    private static GameObject

        Center;

    private static SE.LongVector3

        Position;

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
        SE.RandomSeed[] ManagedTerrainRandomSeed = new SE.RandomSeed[5] {
            new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
			new SE.RandomSeed(432885767),
        };
        SE.TerrainUnitData.Impact[] ManagedTerrainImpacts = new SE.TerrainUnitData.Impact[2] {
            new SE.TerrainImpacts.BasicSmooth(),
			new SE.TerrainImpacts.BasicRandomAdjust(),
            //new SE.TerrainImpacts.TestImpactForTerrain(),
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

        Center = new GameObject("SceneCenter");
        Position = new SE.LongVector3(0, 0, 0);
        SE.Kernel.SetTemporarySenceCenter(Position);
        Center.transform.localPosition = SE.Kernel.SEPositionToUnityPosition(Position);
        //yield return new WaitForSeconds(10);
        //
        //Position = new SE.LongVector3(1000 * 1000, -1000 * 1000, 1000 * 1000);
        //SE.Kernel.SetTemporarySenceCenter(Position);
        //Center.transform.localPosition = SE.Kernel.SEPositionToUnityPosition(Position);
    }
	
	// Update is called once per frame
	void Update () {

        Position += new SE.LongVector3(3000, -3000, 3000);

        SE.Kernel.SetTemporarySenceCenter(Position);

        Center.transform.localPosition = SE.Kernel.SEPositionToUnityPosition(Position);

    }

	~StartSE() {
		if (Started) {
			Debug.Log ("SE Stop.");
			SE.Control.QuickReset ();
			Started = false;
		}
    }
}
