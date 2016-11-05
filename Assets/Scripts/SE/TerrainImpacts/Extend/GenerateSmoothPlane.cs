
namespace SE.TerrainImpacts {
    public class GenerateSmoothPlane : TerrainUnitData.Impact {

        public GenerateSmoothPlane() {
            Active = true;
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(ref TerrainUnitData Data) {

            if (Data.Region.x2 - Data.Region.x1 > 5000 && Data.Region.x2 - Data.Region.x1 < 10000 && Data.Seed[0].NextRandomNum(10) == 1) {

                Data.Impacts.Add(new SmoothPlane(
                    new Geometries.Rectangle<long>(Data.Region.x1 + 1000, Data.Region.x2 - 1000, Data.Region.y1 + 1000, Data.Region.y2 - 1000),
                    new Geometries.Rectangle<long>(Data.Region.x1 + 2000, Data.Region.x2 - 2000, Data.Region.y1 + 2000, Data.Region.y2 - 2000),
                    Data.ExtendMap[4]
                ));
                Data.Impacts.Add(new SmoothPlane(
                    new Geometries.Rectangle<long>(Data.Region.x1 + 2000, Data.Region.x2 - 2000, Data.Region.y1 + 2000, Data.Region.y2 - 2000),
                    new Geometries.Rectangle<long>(Data.Region.x1 + 3000, Data.Region.x2 - 3000, Data.Region.y1 + 3000, Data.Region.y2 - 3000),
                    Data.ExtendMap[4]+2 * 1000
                ));
            }
        }
    }
}