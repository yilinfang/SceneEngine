

namespace SE.TerrainImpacts.AffectedRegions {
    public sealed class Rectangle:TerrainUnitData.Impact.AffectedRegion {

        public Geometries.Rectangle<long> a;

        public Rectangle(ref Geometries.Rectangle<long> Region) {
            a = Region;
        }

        public override bool OverLapped(ref Geometries.Rectangle<long> b) {
            return Geometries.OverLapped(ref a, ref b);
        }

        public override bool OverLapped(long x, long y) {
            return Geometries.OverLapped(ref a, x, y);
        }
    }
}