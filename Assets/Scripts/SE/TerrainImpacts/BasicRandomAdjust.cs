
namespace SE.TerrainImpacts {
	public class BasicRandomAdjust : TerrainUnitData.Impact {

		public BasicRandomAdjust() {
            Static = true;
			Region = new AffectedRegions.Whole();
		}

        public override void Main(TerrainUnitData Data) {

            long[] map = Data.Map;

            long
                RandomRange = System.Math.Max(Data.Region.x2 - Data.Region.x1, Data.Region.y2 - Data.Region.y1) / 5,
                t = RandomRange / 2;

            map[1] += Data.Seed[1].NextRandomNum(RandomRange) - t;
			map[3] += Data.Seed[2].NextRandomNum(RandomRange) - t;
			map[4] += Data.Seed[0].NextRandomNum(RandomRange) - t;
			map[5] += Data.Seed[3].NextRandomNum(RandomRange) - t;
			map[7] += Data.Seed[4].NextRandomNum(RandomRange) - t;
        }
	}
}
