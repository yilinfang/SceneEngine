﻿
namespace SE.TerrainImpacts {
    public class GenerateSmoothPlane : TerrainUnitData.Impact {

        public GenerateSmoothPlane() {
            Static = true;
            Region = new AffectedRegions.Whole();
        }

        public override void Main(ref TerrainUnitData Data) {

            if (Data.Region.x2 - Data.Region.x1 > 5000 && Data.Region.x2 - Data.Region.x1 < 10000 && Data.Seed[0].NextRandomNum(10) == 1) {

                TerrainUnitData.Impact[] TerrainImpacts = new TerrainUnitData.Impact[Data.Impacts.Length + 1];

                for (int i = 0; i < Data.Impacts.Length; i++)
                    TerrainImpacts[i] = Data.Impacts[i];

                TerrainImpacts[TerrainImpacts.Length - 1] = new SmoothPlane(
                    new Geometries.Rectangle<long>(Data.Region.x1 + 100, Data.Region.x2 - 1000, Data.Region.y1 + 1000, Data.Region.y2 - 1000),
                    new Geometries.Rectangle<long>(Data.Region.x1 + 3000, Data.Region.x2 - 3000, Data.Region.y1 + 3000, Data.Region.y2 - 3000),
                    Data.ExtendMap[2]
                );

                Data.Impacts = TerrainImpacts;
            }
        }
    }
}