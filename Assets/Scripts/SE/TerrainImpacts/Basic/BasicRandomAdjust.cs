
namespace SE.TerrainImpacts {
	public class BasicRandomAdjust : TerrainUnitData.Impact {

		public BasicRandomAdjust() {
            Active = true;
            Static = true;
			Region = new AffectedRegions.Whole();
		}

        public override void Main(ref TerrainUnitData Data) {

            long[] basemap = Data.BaseMap;

            ulong
                RandomRange = (ulong)System.Math.Max(Data.Region.x2 - Data.Region.x1, Data.Region.y2 - Data.Region.y1) / 5,
                t = RandomRange / 2;

            basemap[1] += (long)(Data.Seed[1].NextRandomNum(RandomRange) - t);
            basemap[3] += (long)(Data.Seed[2].NextRandomNum(RandomRange) - t);
            basemap[4] += (long)(Data.Seed[0].NextRandomNum(RandomRange) - t);
            basemap[5] += (long)(Data.Seed[3].NextRandomNum(RandomRange) - t);
            basemap[7] += (long)(Data.Seed[4].NextRandomNum(RandomRange) - t);
        }
	}
}
