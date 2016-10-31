﻿
namespace SE.TerrainImpacts {
    public class BasicToExtend : TerrainUnitData.Impact {

        public BasicToExtend() {
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(ref TerrainUnitData Data) {
            Data.ExtendMap = (long[])Data.BaseMap.Clone();
        }
    }
}
