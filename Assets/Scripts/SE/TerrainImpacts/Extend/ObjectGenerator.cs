using System.Collections.Generic;

namespace SE.TerrainImpacts {
    public class ObjectGenerator : TerrainUnitData.Impact {

        abstract public class ObjectGenerateItem {
            abstract public void Main(ref TerrainUnitData UnitData);
        }

        private Pair<long, RandomTree<ObjectGenerateItem>>[] GenerateDataArray;

        public ObjectGenerator(ref Geometries.Rectangle<long> Region, Pair<long, RandomTree<ObjectGenerateItem>>[] GenerateDataArray) {
            Active = true;
            Static = true;
            this.Region = new AffectedRegions.Rectangle(ref Region);
            this.GenerateDataArray = GenerateDataArray;
        }

        private static int BinarySearch(Pair<long, RandomTree<ObjectGenerateItem>>[] arr, long key) {
            if (arr.Length > 0 && key >= arr[arr.Length - 1].First) return arr.Length - 1;
            int left = 0, right = arr.Length - 2;
            while (left <= right) {
                int mid = (left + right) / 2;
                if (key < arr[mid].First) {
                    right = mid - 1;
                } else if (key >= arr[mid + 1].First) {
                    left = mid + 1;
                } else {
                    return mid;
                }
            }
            return -1;
        }

        public override void Main(ref TerrainUnitData Data) {
            int Key = BinarySearch(
                GenerateDataArray,
                System.Math.Min(Data.Region.x2 - Data.Region.x1, Data.Region.y2 - Data.Region.y1)
            );
            if (Key == -1) return;
            ObjectGenerateItem item = GenerateDataArray[Key].Second.GetLast((int)Data.Seed[0].NextRandomNum());
            if (item == null) return;
            //UnityEngine.Debug.Log("item Main ran.");
            item.Main(ref Data);
        }
    }
}