
namespace SE.TerrainImpacts {
    public class TestImpactForTerrain : TerrainUnitData.Impact {

        public TestImpactForTerrain() {
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(TerrainUnitData Data) {

            long[] map = Data.Map;

            map[1] = map[3] = map[4] = map[5] = map[7] = 100000;
        }
    }
}