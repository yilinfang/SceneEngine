

namespace SE.TerrainImpacts.CollisionRegions {
    public class Rectangle : TerrainUnitData.Impact.CollisionRegion {

        private Geometries.Rectangle<long> GeoRegion;

        public Rectangle(Geometries.Rectangle<long> Region) { GeoRegion = Region; }

        override public bool Collided(TerrainUnitData.Impact.CollisionRegion Region) {
            return Geometries.OverLapped(ref GeoRegion, ref ((Rectangle)Region).GeoRegion);
        }
        public override bool OverLapped(ref Geometries.Rectangle<long> UnitRegion) {
            return Geometries.OverLapped(ref GeoRegion, ref UnitRegion);
        }
    }
}