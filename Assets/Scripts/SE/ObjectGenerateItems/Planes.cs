


namespace SE.ObjectGenerateItems {
    public class Planes : TerrainImpacts.ObjectGenerator.ObjectGenerateItem {

        private long Size;

        public Planes(long Size) { this.Size = Size; }

        public override void Main(ref TerrainUnitData Data) {

            Geometries.Rectangle<long> WholeRegion = Geometries.TransformToRegionCenter(Size, Size, ref Data.Region);
            CollisionRegion CollisionRegion = new TerrainImpacts.CollisionRegions.Rectangle(ref WholeRegion);

            if (CollisionRegion.CollisionCheck(Data.CollisionRegions, 1, CollisionRegion)) {
                CollisionRegion.Put(Data.CollisionRegions, 1, CollisionRegion);
                Geometries.Rectangle<long> CentralRegion = Geometries.TransformToRegionCenter(Size / 2, Size / 2, ref Data.Region);
                Geometries.Rectangle<long> tWholeRegion = Geometries.TransformToRegionCenter(Size / 8, Size / 8, ref Data.Region);
                Geometries.Rectangle<long> tCentralRegion = Geometries.TransformToRegionCenter(Size / 16, Size / 16, ref Data.Region);
                Data.Impacts.Add(new TerrainImpacts.SmoothPlane(WholeRegion, CentralRegion, Data.ExtendMap[4]));
                Data.Impacts.Add(new TerrainImpacts.SmoothPlane(tWholeRegion, tCentralRegion, Data.ExtendMap[4] + Size / 16));
            }
        }
    }
}