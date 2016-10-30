

namespace SE.TerrainImpacts.AffectedRegions {
    public sealed class Whole : TerrainUnitData.Impact.AffectedRegion {

        public override bool OverLapped(long x, long y) {
            return true;
        }

        public override bool OverLapped(ref Geometries.Rectangle<long> UnitRegion) {
            return true;
        }
    }
}