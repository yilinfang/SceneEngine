

namespace SE.TerrainImpacts.AffectedRegions {
    public sealed class Rectangle_Standard:TerrainUnitData.Impact.AffectedRegion {

        private Geometries.Rectangle<long> a;

        public override bool Overlapped(ref Geometries.Rectangle<long> b) {
            return
                ((a.x1 >= b.x1 && a.x1 <= b.x2) || (a.x2 >= b.x1 && a.x2 <= b.x2))
                && ((a.y1 >= b.y1 && a.y1 <= b.y2) || (a.y2 >= b.y1 && a.y2 <= b.y2));
        }

        public override bool OverLapped(ref Geometries.Point<long, long> b) {
            return
                (b.x >= a.x1 && b.x <= a.x2 && b.y >= a.x1 && b.y <= a.x2);
        }

        public Rectangle_Standard(ref Geometries.Rectangle<long> Region) {
            a = Region;
        }
    }
}