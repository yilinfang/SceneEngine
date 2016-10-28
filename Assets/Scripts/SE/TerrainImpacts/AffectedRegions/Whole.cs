

namespace SE.TerrainImpacts.AffectedRegions {
    public sealed class Whole : TerrainUnitData.Impact.AffectedRegion {

        public override bool OverLapped(ref Geometries.Point<long, long> UnitPoint) {
            return true;
        }

        public override bool Overlapped(ref Geometries.Rectangle<long> UnitRegion) {
            return true;
        }
    }
}