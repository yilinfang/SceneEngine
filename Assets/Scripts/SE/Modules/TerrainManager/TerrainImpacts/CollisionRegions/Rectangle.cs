

namespace SE.TerrainImpacts.CollisionRegions {
    public class Rectangle : CollisionRegion {

        public Geometries.Rectangle<long> GeoRegion;

        public Rectangle(ref Geometries.Rectangle<long> Region) { GeoRegion = Region; }

        override public bool Collided(CollisionRegion Region) {
            return Geometries.OverLapped(ref GeoRegion, ref ((Rectangle)Region).GeoRegion);
        }
        public override bool OverLapped(ref Geometries.Rectangle<long> UnitRegion) {
            return Geometries.OverLapped(ref GeoRegion, ref UnitRegion);
        }
    }
}