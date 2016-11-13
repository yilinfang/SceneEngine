

namespace SE.TerrainImpacts {
    public class WaterGenerator : TerrainUnitData.Impact {
        private class WaterBlockManager {
            private class WaterBlock {
                private static ObjectPool<UnityEngine.GameObject> EntityPool = new ObjectPool<UnityEngine.GameObject>(
                    20,
                    delegate (UnityEngine.GameObject Object) {
                        Object.SetActive(true);
                        return true;
                    },
                    delegate (UnityEngine.GameObject Object) {
                        Object.SetActive(false);
                        return true;
                    }
                );

                public WaterBlock Father = null;
                public Geometries.Rectangle<long> Region;
                public long[] Height;
                public UnityEngine.GameObject Entity = null;
                public WaterBlock[] Child = null;

                public WaterBlock(UnityEngine.GameObject Prefab, Geometries.Rectangle<long> Region, long[] Height) {
                    this.Region = Region;
                    this.Height = Height;
                    Thread.QueueOnMainThread(delegate () {
                        if (EntityPool.Count != 0)
                            Entity = EntityPool.Get();
                        else
                            Entity = UnityEngine.GameObject.Instantiate<UnityEngine.GameObject>(Prefab);
                        long min = System.Math.Min(System.Math.Min(Height[0], Height[1]), System.Math.Min(Height[2], Height[3]));

                        Entity.transform.localPosition = Kernel.SEPositionToUnityPosition(new LongVector3(
                            (Region.x1 + Region.x2) / 2,
                            min,
                            (Region.y1 + Region.y2) / 2
                        ));
                        UnityEngine.Material material = Entity.GetComponent<UnityEngine.MeshRenderer>().material;
                        material.SetVector(
                            "_Region",
                            new UnityEngine.Vector4(Region.x1, Region.x2, Region.y1, Region.y2)
                        );
                        material.SetVector(
                            "_VertexHeight",
                            new UnityEngine.Vector4(Height[0], Height[1], Height[2], Height[3])
                        );
                    });
                }
            }
            private UnityEngine.GameObject Prefab;
            public WaterBlockManager(string PrefabPath) {
                Thread.QueueOnMainThread(delegate () {
                    Prefab = UnityEngine.Resources.Load(PrefabPath, typeof(UnityEngine.GameObject)) as UnityEngine.GameObject;
                });
            }
            public int Add(ref TerrainUnitData Data) { return 0; }
            public void Remove(int id) { }
        }

        private WaterBlockManager Manager;
        
        private long SeaLevel;
        private long MinBlockSize;

        public WaterGenerator(string PrefabPath, ref Geometries.Rectangle<long> Region, long SeaLevel, long MinBlockSize) {
            Active = true;
            Static = true;
            this.Region = new AffectedRegions.Rectangle(ref Region);
            this.SeaLevel = SeaLevel;
            this.MinBlockSize = MinBlockSize;
            Manager = new WaterBlockManager(PrefabPath);
        }

        public override System.Action Start(ref TerrainUnitData Data) {
            for (int i=0;i<9;i++)
                if (Data.ExtendMap[i] < SeaLevel) {
                    int id = Manager.Add(ref Data);
                    return delegate () { Manager.Remove(id); };
                }
            return null;
        }
    }
}