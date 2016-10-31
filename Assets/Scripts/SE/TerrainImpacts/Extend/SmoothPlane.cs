
namespace SE.TerrainImpacts {
    public class SmoothPlane : TerrainUnitData.Impact {

        private long PlaneHeight;
        private Geometries.Rectangle<long> CentralRegion, WholeRegion;

        public SmoothPlane(Geometries.Rectangle<long> WholeRegion, Geometries.Rectangle<long> CentralRegion, long PlaneHeight) {

            Static = true;
            Region = new AffectedRegions.Rectangle_Standard(ref WholeRegion);

            this.WholeRegion = WholeRegion;
            this.CentralRegion = CentralRegion;
            this.PlaneHeight = PlaneHeight;
        }

        private static void Operate(SmoothPlane Impact, ref TerrainUnitData Data, int Index, long x, long y) {

            if (Geometries.OverLapped(Impact.CentralRegion, x, y)) {

                Data.ExtendMap[Index] = Impact.PlaneHeight;

            } else if (Impact.Region.OverLapped(x, y)) {

                double HeightAdjustRate = 1;

                if (x < Impact.CentralRegion.x1)
                    HeightAdjustRate *= (double)(x - Impact.WholeRegion.x1) / (Impact.CentralRegion.x1 - Impact.WholeRegion.x1);
                else if (x > Impact.CentralRegion.x2)
                    HeightAdjustRate *= (double)(Impact.WholeRegion.x2 - x) / (Impact.WholeRegion.x2 - Impact.CentralRegion.x2);
                if (y < Impact.CentralRegion.y1)
                    HeightAdjustRate *= (double)(y - Impact.WholeRegion.y1) / (Impact.CentralRegion.y1 - Impact.WholeRegion.y1);
                else if (y > Impact.CentralRegion.y2)
                    HeightAdjustRate *= (double)(Impact.WholeRegion.y2 - y) / (Impact.WholeRegion.y2 - Impact.CentralRegion.y2);

                Data.ExtendMap[Index] = Data.ExtendMap[Index] + (long)((Impact.PlaneHeight - Data.ExtendMap[Index]) * HeightAdjustRate);
            }
        }

        public override void Main(ref TerrainUnitData Data) {

            long
                xmid = (Data.Region.x1 + Data.Region.x2) / 2,
                ymid = (Data.Region.y1 + Data.Region.y2) / 2;

            Operate(this, ref Data, 1, xmid, Data.Region.y1);
            Operate(this, ref Data, 3, Data.Region.x1, ymid);
            Operate(this, ref Data, 4, xmid, ymid);
            Operate(this, ref Data, 5, Data.Region.x2, ymid);
            Operate(this, ref Data, 7, xmid, Data.Region.y2);
        }
    }
}