
namespace SE.TerrainImpacts {
    public class BasicSmooth : TerrainUnitData.Impact {

        public BasicSmooth() {
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(TerrainUnitData Data) {

            long[] map = Data.Map;

            map[1] = (map[0] + map[2]) / 2;
            map[3] = (map[0] + map[6]) / 2;
            map[4] = (map[0] + map[2] + map[6] + map[8]) / 4;
            map[5] = (map[2] + map[8]) / 2;
            map[7] = (map[6] + map[8]) / 2;
        }
    }
}