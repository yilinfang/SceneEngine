using System;
using UnityEngine;

namespace SE  {

    /// <summary>
    /// 场景维护的基本单位,重写的元素有:
    /// CompulsoryCalculation,
    /// NeedUpdate,
    /// CenterAdjust,
    /// Range,
    /// Lod,
    /// UpdateInterval,
    /// Start(),
    /// Update(),
    /// Destory().
    /// </summary>
    abstract public class Object {

        abstract public class ChildManager {

            public Object ObjectRoot;

            abstract public void Add(string CharacteristicString, Object NewObject, LongVector3 LocalPosition = default(LongVector3), Quaternion LocalQuaternion = default(Quaternion));

            abstract public Object this[string CharacteristicString] { get; }

            abstract public Object Get(string CharacteristicString);

            abstract public void Remove(Object OldObject);

            abstract public void Clear();
        }

        /// <summary>
        /// 场景的实际组成元素,重写的元素有:
        /// PrecisionRange,
        /// Start(),
        /// Destory().
        /// StartForUnity(),
        /// DestoryForUnity().
        /// </summary>
        abstract public class LodCase {

            public Object ObjectRoot;

            public float PrecisionRange;

            virtual public void Start() { }
            virtual public void StartForUnity() { }

            virtual public void Destory() { }
            virtual public void DestoryForUnity() { }
        }

        public Object Father;

        public int KernelID;

        public GameObject UnityRoot = null;

		public Vector3 UnityGlobalPosition;

        public LongVector3 SEPosition;

        public double MaintainEvaluation;

        public long LastUpdateTime;

        public int CurrentLodCaseIndex = 0;

        //------------------------------------------------------------------------

        public string CharacteristicString;

        public bool

            CompulsoryCalculation = false,

            NeedUpdate = false;

        public Vector3 CenterAdjust = default(Vector3);

        public double Range = 0;

        public LodCase[] Lod = null;

        public ChildManager Child = null;

        virtual public void Start() { }

        public int UpdateInterval = 0;
        virtual public void Update() { }

        virtual public void Destory() { }
    }

    /*
     * ^
     * |
     * |2 3 3     6 7 8
     * |
     * |1 * 2     3 4 5
     * |
     * |0 0 1     0 1 2
     * 0------>
     * 
     */

    public struct TerrainUnitData {

        abstract public class Impact {

            abstract public class AffectedRegion {

                abstract public bool OverLapped(ref Geometries.Rectangle<long> UnitRegion);

                abstract public bool OverLapped(long x, long y);
            }

            public bool Static = true;

            public AffectedRegion Region;

            abstract public void Main(ref TerrainUnitData Data);

            virtual public Impact Clone(int Index, ref Geometries.Rectangle<long> Region) { throw new NotImplementedException(); }

            virtual public Impact Clone() { throw new NotImplementedException(); }

            public static Impact[] ArrayClone(ref Impact[] Impacts) {

                Impact[] t = new Impact[Impacts.Length];

                for (int i = 0; i < Impacts.Length; i++)
                    if (Impacts[i].Static)//内存优化
                        t[i] = Impacts[i];
                    else
                        t[i] = Impacts[i].Clone();

                return t;
            }
        }

        public long[] BaseMap;
        public long[] ExtendMap;

        public RandomSeed[] Seed;

        public Geometries.Rectangle<long> Region;

        public Impact[] Impacts;

        public TerrainUnitData(ref Geometries.Rectangle<long> Region, ref long[] BaseVertex, ref long[] ExtendVertex,
            ref RandomSeed[] Seed, ref Impact[] Impacts) {

            this.Region = Region;

            BaseMap = new long[9] {
                BaseVertex[0],
                0,
                BaseVertex[1],
                0,0,0,
                BaseVertex[2],
                0,
                BaseVertex[3],
            };
            ExtendMap = new long[9] {
                ExtendVertex[0],
                0,
                ExtendVertex[1],
                0,0,0,
                ExtendVertex[2],
                0,
                ExtendVertex[3],
            };

            this.Seed = Seed;
            
            this.Impacts = Impacts;
        }
        public TerrainUnitData(ref TerrainUnitData Data) {

            Region = Data.Region;

            //struct array
            BaseMap = (long[])Data.BaseMap.Clone();
            ExtendMap = (long[])Data.ExtendMap.Clone();

            Seed = (RandomSeed[])Data.Seed.Clone();

            //class array
            Impacts = Impact.ArrayClone(ref Data.Impacts);
        }
    }

    public struct LongVector3 {

        public long x, y, z;

        public LongVector3(LongVector3 copy) {
            x = copy.x;
            y = copy.y;
            z = copy.z;
        }
        public LongVector3(long xPosition, long yPosition, long zPosition) {
            x = xPosition;
            y = yPosition;
            z = zPosition;
        }

        public static LongVector3 operator +(LongVector3 l, LongVector3 r) {
            return (new LongVector3(l.x + r.x, l.y + r.y, l.z + r.z));
        }
        public static LongVector3 operator -(LongVector3 l, LongVector3 r) {
            return (new LongVector3(l.x - r.x, l.y - r.y, l.z - r.z));
        }
        public static bool operator ==(LongVector3 l, LongVector3 r) {
            return l.x == r.x && l.y == r.y && l.z == r.z;
        }
        public static bool operator !=(LongVector3 l, LongVector3 r) {
            return l.x != r.x || l.y != r.y || l.z != r.z;
        }

        public override bool Equals(object obj) {
            return ((LongVector3)obj) == this ? true : false;
        }
        public override int GetHashCode() {
            return (int)(x * y * z + 2531011);
        }

        public float GetLength() {
            float
                xx = (float)x / 1000,
                yy = (float)y / 1000,
                zz = (float)z / 1000;

            return (float)Math.Sqrt(xx*xx + yy*yy + zz*zz);
        }

        public Vector3 toVector3() {
            return new Vector3((float)x / 1000, (float)y / 1000, (float)z / 1000);
        }
    }

    public struct Pair<T1, T2> {
        public T1 First;
        public T2 Second;
        public Pair(T1 First,T2 Second) {
            this.First = First;
            this.Second = Second;
        }
        public Pair(T1 First, ref T2 Second) {
            this.First = First;
            this.Second = Second;
        }
        public Pair(ref T1 First, T2 Second) {
            this.First = First;
            this.Second = Second;
        }
        public Pair(ref T1 First, ref T2 Second) {
            this.First = First;
            this.Second = Second;
        }
    }

    public static class BitBool {

        public static uint Init(bool[] Bools) {

            uint BitBool = 0;

            for (int i = 0; i < Bools.Length; i++)
                if (Bools[i])
                    SetTrue(ref BitBool, i);

            return BitBool;
        }
        public static uint Init(bool BoolValue,int Length) {

            uint BitBool = 0;

            if (BoolValue)
                for (int i = 0; i < Length; i++)
                    BitBool = (BitBool << 1) | 1;

            return BitBool;
        }

        public static bool Get(uint BitBool, int Index) {
            return (((BitBool >> Index) & 1) == 0) ? false : true;
        }
        public static bool Get(ulong BitBool, int Index) {
            return (((BitBool >> Index) & 1) == 0) ? false : true;
        }

        public static void SetTrue(ref uint BitBool, int Index) {
            BitBool = BitBool | ((uint)1 << Index);
        }
        public static void SetTrue(ref ulong BitBool, int Index) {
            BitBool = BitBool | ((ulong)1 << Index);
        }

        public static void SetFalse(ref uint BitBool, int Index) {
            BitBool = BitBool & ~((uint)1 << Index);
        }
        public static void SetFalse(ref ulong BitBool, int Index) {
            BitBool = BitBool & ~((ulong)1 << Index);
        }
    }

    public struct Group<T1, T2, T3> {

        public T1 First;
        public T2 Second;
        public T3 Third;

        public Group(T1 First,T2 Second,T3 Third) {
            this.First = First;
            this.Second = Second;
            this.Third = Third;
        }
        public Group(T1 First,ref T2 Second, T3 Third) {
            this.First = First;
            this.Second = Second;
            this.Third = Third;
        }
    }
}