
namespace SE.TerrainImpacts {
    public class BasicSmooth : TerrainUnitData.Impact {

        public BasicSmooth() {
            Active = true;
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(ref TerrainUnitData Data) {

            long[] basemap = Data.BaseMap;

            basemap[1] = (basemap[0] + basemap[2]) / 2;
            basemap[3] = (basemap[0] + basemap[6]) / 2;
            basemap[4] = (basemap[0] + basemap[2] + basemap[6] + basemap[8]) / 4;
            basemap[5] = (basemap[2] + basemap[8]) / 2;
            basemap[7] = (basemap[6] + basemap[8]) / 2;
        }
    }
}