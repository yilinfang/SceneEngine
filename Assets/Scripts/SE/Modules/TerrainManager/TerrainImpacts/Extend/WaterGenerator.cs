
/*
namespace SE.TerrainImpacts {
    public class WaterGenerator : TerrainUnitData.Impact {

        public interface IWaterFactory {
            UnityEngine.GameObject Get(Geometries.Rectangle<long> Region, long[] Height);
            void Put(UnityEngine.GameObject obj);
        }

        public class StandardWaterFactory : IWaterFactory {
            private ObjectPool<UnityEngine.GameObject> Pool = new SE.ObjectPool<UnityEngine.GameObject>(
                20,
                delegate (UnityEngine.GameObject obj) {
                    obj.SetActive(true);
                    return true;
                },
                delegate (UnityEngine.GameObject obj) {
                    obj.SetActive(false);
                    return true;
                });
            private UnityEngine.GameObject Prefab;

            public StandardWaterFactory(string PrefabPath) {
                Prefab = UnityEngine.Resources.Load(PrefabPath, typeof(UnityEngine.GameObject)) as UnityEngine.GameObject;
            }
            public UnityEngine.GameObject Get(Geometries.Rectangle<long> Region, long[] Height) {
                UnityEngine.GameObject Product;
                if (Pool.Count != 0) Product = Pool.Get();
                else Product = UnityEngine.GameObject.Instantiate(Prefab);

                long min = System.Math.Min(System.Math.Min(Height[0], Height[1]), System.Math.Min(Height[2], Height[3]));

                UnityEngine.Transform transform = Product.transform;
                transform.localPosition = Kernel.Position_SEToUnity(new LongVector3(
                    (Region.x1 + Region.x2) / 2,
                    min,
                    (Region.y1 + Region.y2) / 2
                ));
                transform.localScale = new UnityEngine.Vector3(
                    (Region.x2 - Region.x1) / 1000,
                    1,
                    (Region.y2 - Region.y1) / 1000
                );

                UnityEngine.Material material = Product.GetComponent<UnityEngine.MeshRenderer>().material;
                material.SetVector(
                    "_Region",
                    new UnityEngine.Vector4(Region.x1, Region.x2, Region.y1, Region.y2)
                );
                material.SetVector(
                    "_VertexHeight",
                    new UnityEngine.Vector4((Height[0]-min)/1000, (Height[1] - min) / 1000, (Height[2] - min) / 1000, (Height[3] - min) / 1000)
                );

                return Product;
            }
            public void Put(UnityEngine.GameObject obj) { Pool.Put(obj); }
        }

        private class WaterBlockManager {
            private class WaterBlock {

                private const long WaterBlockMaxLength = 1000 * 1000;

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
                public WaterBlock Father;
                public Geometries.Rectangle<long> Region;
                public long[] Height;
                public UnityEngine.GameObject Entity;
                public WaterBlock[] Child;

                public WaterBlock(WaterBlock Father, IWaterFactory Factory, ref Geometries.Rectangle<long> Region, long[] Height) {
                    this.Father = Father;
                    this.Region = Region;
                    this.Height = Height;
                    Child = new WaterBlock[4];
                    Entity = null;
                    if (Geometries.MaxLength(ref Region) < WaterBlockMaxLength)
                        Thread.QueueOnMainThread(delegate () { Entity = Factory.Get(this.Region, Height); });
                }

                public void Recycle(IWaterFactory Factory) {
                    if (Geometries.MaxLength(ref Region) < WaterBlockMaxLength)
                        Thread.QueueOnMainThread(delegate () { Factory.Put(Entity); });
                }
            }
            private IWaterFactory Factory;
            private WaterBlock Root;
            private int Index;
            private System.Collections.Generic.Dictionary<int, WaterBlock> Dic;
            public WaterBlockManager(IWaterFactory Factory) {
                this.Factory = Factory;
                Root = null;
                Index = 0;
                Dic = new System.Collections.Generic.Dictionary<int, WaterBlock>();
            }
            public int Add(ref TerrainUnitData Data) {
                int ID = System.Threading.Interlocked.Increment(ref Index);
                WaterBlock block = new WaterBlock(
                    null,
                    Factory,
                    ref Data.Region,
                    new long[4] {
                        Data.ExtendMap[0],
                        Data.ExtendMap[2],
                        Data.ExtendMap[6],
                        Data.ExtendMap[8]
                    }
                );

                if (Root == null) {
                    Root = block;
                    Dic.Add(ID, block);
                    return ID;
                }

                WaterBlock now = Root;
                while (true) {
                    long
                        xmid = (now.Region.x1 + now.Region.x2) / 2,
                        ymid = (now.Region.y1 + now.Region.y2) / 2;
                    if (Data.Region.y1 < ymid) {
                        if (Data.Region.x1 < xmid) {
                            if (now.Child[0] == null) {
                                now.Child[0] = block;
                                return ID;
                            };
                            now = now.Child[0];
                        } else if (Data.Region.x2 > xmid) {
                            if (now.Child[1] == null) {
                                now.Child[1] = block;
                                return ID;
                            };
                            now = now.Child[1];
                        }
                    } else if (Data.Region.y2 > ymid) {
                        if (Data.Region.x1 < xmid) {
                            if (now.Child[2] == null) {
                                now.Child[2] = block;
                                return ID;
                            };
                            now = now.Child[2];
                        } else if (Data.Region.x2 > xmid) {
                            if (now.Child[3] == null) {
                                now.Child[3] = block;
                                return ID;
                            };
                            now = now.Child[3];
                        }
                    }
                }
            }
            public void Remove(int id) { }
        }

        private WaterBlockManager Manager;
        
        private long SeaLevel;
        private long MinBlockSize;

        public WaterGenerator(IWaterFactory Factory, ref Geometries.Rectangle<long> Region, long SeaLevel, long MinBlockSize) {
            Active = true;
            Static = true;
            this.Region = new AffectedRegions.Rectangle(ref Region);
            this.SeaLevel = SeaLevel;
            this.MinBlockSize = MinBlockSize;
            Manager = new WaterBlockManager(Factory);
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
}*/