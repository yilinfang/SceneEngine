﻿using System.Collections.Generic;

namespace SE.Modules {
    public partial class TerrainManager : IModule {
        public class CalculateNode {

            public long[,] Map = null;
            public CalculateNode[,] Child = null;
            public List<System.Action> DestroyCallBack = null;

            public ManagedTerrain ManagedTerrainRoot;
            public TerrainUnitData InitialData;
            public long MinHeight, MaxHeight;
            public LongVector3 CenterAdjust;
            public double Range, Key;

            public CalculateNode(ManagedTerrain ManagedTerrainRoot, ref TerrainUnitData InitialData) {
                this.ManagedTerrainRoot = ManagedTerrainRoot;
                this.InitialData = InitialData;
                MaxHeight = System.Math.Max(
                    System.Math.Max(InitialData.BaseMap[0], InitialData.BaseMap[2]),
                    System.Math.Max(InitialData.BaseMap[6], InitialData.BaseMap[8])
                );
                MinHeight = System.Math.Min(
                    System.Math.Min(InitialData.BaseMap[0], InitialData.BaseMap[2]),
                    System.Math.Min(InitialData.BaseMap[6], InitialData.BaseMap[8])
                );
            }
        }
        public class ApplyBlock {
            public class StorageTree {
                private static class BitCounter {
                    public static void Increase(ref uint Counter, int Index) {
                        if (BitBool.Get(Counter, Index * 2)) {
                            if (BitBool.Get(Counter, Index * 2 + 1))
                                throw new System.Exception("BitCounter Increase " + Index + ": Counter is full.");
                            else
                                BitBool.SetTrue(ref Counter, Index * 2 + 1);
                        } else
                            BitBool.SetTrue(ref Counter, Index * 2);
                    }
                    public static void Decrease(ref uint Counter, int Index) {
                        if (BitBool.Get(Counter, Index * 2 + 1))
                            BitBool.SetFalse(ref Counter, Index * 2 + 1);
                        else {
                            if (BitBool.Get(Counter, Index * 2))
                                BitBool.SetFalse(ref Counter, Index * 2);
                            else
                                throw new System.Exception("BitCounter Decrease " + Index + ": Counter is Empty.");
                        }
                    }
                    public static bool IsNotZero(uint Counter, int Index) {
                        return BitBool.Get(Counter, Index * 2);
                    }
                    public static int GetInt(uint Counter, int Index) {
                        return (BitBool.Get(Counter, Index * 2 + 1) ? 1 : 0) + (BitBool.Get(Counter, Index * 2) ? 1 : 0);
                    }
                    public static void SetInt(ref uint Counter, int Index, int Value) {
                        if (Value != 0) {
                            BitBool.SetTrue(ref Counter, Index * 2);
                            if (Value == 2)
                                BitBool.SetTrue(ref Counter, Index * 2 + 1);
                        }
                    }
                    public static uint Init(int[] Values) {
                        uint n = 0;
                        for (int i = 0; i < Values.Length; i++)
                            SetInt(ref n, i, Values[i]);
                        return n;
                    }
                }

                private class StorageNode {
                    public uint Counter = 0;
                    public long[] Height = new long[5] { 0, 0, 0, 0, 0, };
                    public StorageNode[] Nodes = new StorageNode[4] { null, null, null, null, };
                }

                private static ObjectPool<StorageNode> NodePool = new ObjectPool<StorageNode>(
                    1000,
                    delegate (StorageNode Node) {
                        for (int i = 0; i < 4; i++) Node.Nodes[i] = null;
                        for (int i = 0; i < 5; i++) Node.Height[i] = 0;
                        Node.Counter = 0;
                        return true;
                    },
                    delegate (StorageNode Node) { return true; }
                );

                public uint VertexCounter = 0;
                public long[] VertexHeight;
                private StorageNode NodeRoot;
                public int Depth;
                public long MaxHeight, MinHeight;
                public int Count { get; private set; }
                public Geometries.Rectangle<long> Region;

                public StorageTree(TerrainUnitData Data) {
                    Count = 0;
                    VertexHeight = new long[4];
                    NodeRoot = NodePool.Get();
                    Region = Data.Region;
                    Depth = 1;
                    MaxHeight = System.Math.Max(
                        System.Math.Max(VertexHeight[0], VertexHeight[1]),
                        System.Math.Max(VertexHeight[2], VertexHeight[3])
                    );
                    MinHeight = System.Math.Min(
                        System.Math.Min(VertexHeight[0], VertexHeight[1]),
                        System.Math.Min(VertexHeight[2], VertexHeight[3])
                    );
                }
                private StorageTree(long[] VertexHeight, uint VertexCounter, ref Geometries.Rectangle<long> Region, StorageNode NodeRoot) {
                    this.VertexCounter = VertexCounter;
                    this.VertexHeight = VertexHeight;
                    this.NodeRoot = NodeRoot;
                    //Initialise......
                    if (this.NodeRoot.Height[0] == 0)
                        this.NodeRoot.Height[0] = (VertexHeight[0] + VertexHeight[1]) / 2;
                    if (this.NodeRoot.Height[1] == 0)
                        this.NodeRoot.Height[1] = (VertexHeight[0] + VertexHeight[2]) / 2;
                    if (this.NodeRoot.Height[2] == 0)
                        this.NodeRoot.Height[2] = (VertexHeight[0] + VertexHeight[1] + VertexHeight[2] + VertexHeight[3]) / 4;
                    if (this.NodeRoot.Height[3] == 0)
                        this.NodeRoot.Height[3] = (VertexHeight[1] + VertexHeight[3]) / 2;
                    if (this.NodeRoot.Height[4] == 0)
                        this.NodeRoot.Height[4] = (VertexHeight[2] + VertexHeight[3]) / 2;
                    this.Region = Region;

                    //Get Count & Depth & MaxHeight & MinHeight
                    Count = 0;
                    Depth = 0;
                    MaxHeight = System.Math.Max(
                        System.Math.Max(VertexHeight[0], VertexHeight[1]),
                        System.Math.Max(VertexHeight[2], VertexHeight[3])
                    );
                    MinHeight = System.Math.Min(
                        System.Math.Min(VertexHeight[0], VertexHeight[1]),
                        System.Math.Min(VertexHeight[2], VertexHeight[3])
                    );

                    Stack<Pair<StorageNode, int>> s = new Stack<Pair<StorageNode, int>>();
                    s.Push(new Pair<StorageNode, int>(NodeRoot, 1));
                    while (s.Count != 0) {
                        Pair<StorageNode, int> now = s.Pop();
                        Count++;
                        Depth = System.Math.Max(Depth, now.Second);
                        for (int i = 0; i < 5; i++)
                            if (BitCounter.IsNotZero(now.First.Counter, i)) {
                                MaxHeight = System.Math.Max(MaxHeight, now.First.Height[i]);
                                MinHeight = System.Math.Min(MinHeight, now.First.Height[i]);
                            }
                        for (int i = 0; i < 4; i++)
                            if (ChildIsNotNullOrEmpty(now.First, i))
                                s.Push(new Pair<StorageNode, int>(now.First.Nodes[i], now.Second + 1));
                    }
                }
                public static StorageTree Merge(StorageTree[] ChildTree) {
                    Geometries.Rectangle<long> Region = new Geometries.Rectangle<long>(
                        ChildTree[0].Region.x1,
                        ChildTree[3].Region.x2,
                        ChildTree[0].Region.y1,
                        ChildTree[3].Region.y2
                    );

                    StorageNode Node = NodePool.Get();
                    Node.Counter = BitCounter.Init(new int[5] {
                            BitCounter.GetInt(ChildTree[0].VertexCounter, 1),
                            BitCounter.GetInt(ChildTree[0].VertexCounter, 2),
                            BitCounter.GetInt(ChildTree[0].VertexCounter, 3),
                            BitCounter.GetInt(ChildTree[1].VertexCounter, 3),
                            BitCounter.GetInt(ChildTree[2].VertexCounter, 3),
                        });
                    Node.Height = new long[5] {
                            ChildTree[0].VertexHeight[1],
                            ChildTree[0].VertexHeight[2],
                            ChildTree[0].VertexHeight[3],
                            ChildTree[1].VertexHeight[3],
                            ChildTree[2].VertexHeight[3],
                        };
                    for (int i = 0; i < 4; i++)
                        Node.Nodes[i] = ChildTree[i].NodeRoot;
                    uint VertexCounter = 0;
                    long[] VertexHeight = new long[4];
                    for (int i = 0; i < 4; i++) {
                        BitCounter.SetInt(ref VertexCounter, i, BitCounter.GetInt(ChildTree[i].VertexCounter, i));
                        VertexHeight[i] = ChildTree[i].VertexHeight[i];
                    }

                    return new StorageTree(VertexHeight, VertexCounter, ref Region, Node);
                }

                public static StorageTree[] Split(StorageTree Tree) {

                    long[][] ChildVertexHeight = new long[4][] {
                            new long[4] {
                                Tree.VertexHeight[0],
                                Tree.NodeRoot.Height[0],
                                Tree.NodeRoot.Height[1],
                                Tree.NodeRoot.Height[2],
                            },
                            new long[4] {
                                Tree.NodeRoot.Height[0],
                                Tree.VertexHeight[1],
                                Tree.NodeRoot.Height[2],
                                Tree.NodeRoot.Height[3],
                            },
                            new long[4] {
                                Tree.NodeRoot.Height[1],
                                Tree.NodeRoot.Height[2],
                                Tree.VertexHeight[2],
                                Tree.NodeRoot.Height[4],
                            },
                            new long[4] {
                                Tree.NodeRoot.Height[2],
                                Tree.NodeRoot.Height[3],
                                Tree.NodeRoot.Height[4],
                                Tree.VertexHeight[3],
                            },
                        };
                    uint[] ChildVertexCounter = new uint[4] {
                            BitCounter.Init(new int[4] {
                                BitCounter.GetInt(Tree.VertexCounter, 0),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 0),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 1),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 2),
                            }),
                            BitCounter.Init(new int[4] {
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 0),
                                BitCounter.GetInt(Tree.VertexCounter, 1),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 2),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 3),
                            }),
                            BitCounter.Init(new int[4] {
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 1),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 2),
                                BitCounter.GetInt(Tree.VertexCounter, 2),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 4),
                            }),
                            BitCounter.Init(new int[4] {
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 2),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 3),
                                BitCounter.GetInt(Tree.NodeRoot.Counter, 4),
                                BitCounter.GetInt(Tree.VertexCounter, 3),
                            }),
                        };

                    Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref Tree.Region);
                    StorageNode[] ChildNode = new StorageNode[4];
                    for (int i = 0; i < 4; i++)
                        ChildNode[i] = (Tree.NodeRoot.Nodes[i] == null) ? NodePool.Get() : Tree.NodeRoot.Nodes[i];
                    StorageTree[] arr = new StorageTree[4];
                    for (int i = 0; i < 4; i++)
                        arr[i] = new StorageTree(ChildVertexHeight[i], ChildVertexCounter[i], ref ChildRegion[i], ChildNode[i]);
                    return arr;
                }

                private static void ChildNodePrepare(StorageNode Node, int Index) {
                    if (Node.Nodes[Index] == null)
                        Node.Nodes[Index] = NodePool.Get();
                }

                public void Insert(Geometries.Point<long, long> NewPoint) {
                    bool Inserted = false;
                    if (NewPoint.y == Region.y1) {
                        if (NewPoint.x == Region.x1) {
                            Inserted = true;
                            BitCounter.Increase(ref VertexCounter, 0);
                            VertexHeight[0] = NewPoint.h;
                        } else if (NewPoint.x == Region.x2) {
                            Inserted = true;
                            BitCounter.Increase(ref VertexCounter, 1);
                            VertexHeight[1] = NewPoint.h;
                        }
                    } else if (NewPoint.y == Region.y2) {
                        if (NewPoint.x == Region.x1) {
                            Inserted = true;
                            BitCounter.Increase(ref VertexCounter, 2);
                            VertexHeight[2] = NewPoint.h;
                        } else if (NewPoint.x == Region.x2) {
                            Inserted = true;
                            BitCounter.Increase(ref VertexCounter, 3);
                            VertexHeight[3] = NewPoint.h;
                        }
                    }
                    if (!Inserted) {
                        Queue<Group<StorageNode, Geometries.Rectangle<long>, int>>
                            q = new Queue<Group<StorageNode, Geometries.Rectangle<long>, int>>();
                        q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, int>(NodeRoot, ref Region, 1));
                        while (q.Count != 0) {
                            Group<StorageNode, Geometries.Rectangle<long>, int> now = q.Dequeue();
                            Depth = System.Math.Max(Depth, now.Third);
                            long
                                xmid = (now.Second.x1 + now.Second.x2) / 2,
                                ymid = (now.Second.y1 + now.Second.y2) / 2;

                            if (NewPoint.x == xmid) {
                                if (NewPoint.y == now.Second.y1) {
                                    BitCounter.Increase(ref now.First.Counter, 0);
                                    now.First.Height[0] = NewPoint.h;
                                    Inserted = true;
                                    continue;
                                } else if (NewPoint.y == now.Second.y2) {
                                    BitCounter.Increase(ref now.First.Counter, 4);
                                    now.First.Height[4] = NewPoint.h;
                                    Inserted = true;
                                    continue;
                                } else if (NewPoint.y == ymid) {
                                    BitCounter.Increase(ref now.First.Counter, 2);
                                    now.First.Height[2] = NewPoint.h;
                                    Inserted = true;
                                    continue;
                                }
                            } else if (NewPoint.y == ymid) {
                                if (NewPoint.x == now.Second.x1) {
                                    BitCounter.Increase(ref now.First.Counter, 1);
                                    now.First.Height[1] = NewPoint.h;
                                    Inserted = true;
                                    continue;
                                } else if (NewPoint.x == now.Second.x2) {
                                    BitCounter.Increase(ref now.First.Counter, 3);
                                    now.First.Height[3] = NewPoint.h;
                                    Inserted = true;
                                    continue;
                                }
                            }

                            Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref now.Second);
                            if (NewPoint.y <= ymid) {
                                if (NewPoint.x <= xmid) {
                                    ChildNodePrepare(now.First, 0);
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, int>(now.First.Nodes[0], ref ChildRegion[0], now.Third + 1));
                                }
                                if (NewPoint.x >= xmid) {
                                    ChildNodePrepare(now.First, 1);
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, int>(now.First.Nodes[1], ref ChildRegion[1], now.Third + 1));
                                }
                            }
                            if (NewPoint.y >= ymid) {
                                if (NewPoint.x <= xmid) {
                                    ChildNodePrepare(now.First, 2);
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, int>(now.First.Nodes[2], ref ChildRegion[2], now.Third + 1));
                                }
                                if (NewPoint.x >= xmid) {
                                    ChildNodePrepare(now.First, 3);
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, int>(now.First.Nodes[3], ref ChildRegion[3], now.Third + 1));
                                }
                            }
                        }
                    }

                    if (!Inserted)
                        throw new System.Exception("StorageTree: 数据(" + NewPoint.x + "," + NewPoint.y + "," + NewPoint.h + ")未被插入.");
                    Count++;
                    MaxHeight = System.Math.Max(MaxHeight, NewPoint.h);
                    MinHeight = System.Math.Min(MinHeight, NewPoint.h);
                }

                private static bool ChildIsNotNullOrEmpty(StorageNode Node, int Index) {
                    if (Node.Nodes[Index] == null) return false;
                    for (int i = 0; i < 4; i++)
                        if (ChildIsNotNullOrEmpty(Node.Nodes[Index], i))
                            return true;
                    if (Node.Nodes[Index].Counter == 0) {
                        NodePool.Put(Node.Nodes[Index]);
                        Node.Nodes[Index] = null;
                        return false;
                    }
                    return true;
                }

                public void Delete(Geometries.Point<long, long> OldPoint) {
                    bool Deleted = false;
                    int TempDepth = Depth;

                    if (OldPoint.y == Region.y1) {
                        if (OldPoint.x == Region.x1) {
                            TempDepth = 0;
                            Deleted = true;
                            BitCounter.Decrease(ref VertexCounter, 0);
                        } else if (OldPoint.x == Region.x2) {
                            TempDepth = 0;
                            Deleted = true;
                            BitCounter.Decrease(ref VertexCounter, 1);
                        }
                    } else if (OldPoint.y == Region.y2) {
                        if (OldPoint.x == Region.x1) {
                            TempDepth = 0;
                            Deleted = true;
                            BitCounter.Decrease(ref VertexCounter, 2);
                        } else if (OldPoint.x == Region.x2) {
                            TempDepth = 0;
                            Deleted = true;
                            BitCounter.Decrease(ref VertexCounter, 3);
                        }
                    }
                    if (!Deleted) {
                        Queue<Group<StorageNode, Geometries.Rectangle<long>, byte>>
                            q = new Queue<Group<StorageNode, Geometries.Rectangle<long>, byte>>();
                        q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, byte>(NodeRoot, ref Region, 1));
                        while (q.Count != 0) {
                            Group<StorageNode, Geometries.Rectangle<long>, byte> now = q.Dequeue();
                            long
                                xmid = (now.Second.x1 + now.Second.x2) / 2,
                                ymid = (now.Second.y1 + now.Second.y2) / 2;

                            //判断当前节点是否存在(是则结束该分支)
                            if (OldPoint.x == xmid) {
                                if (OldPoint.y == now.Second.y1) {
                                    BitCounter.Decrease(ref now.First.Counter, 0);
                                    TempDepth = now.Third;
                                    Deleted = true;
                                    continue;
                                } else if (OldPoint.y == now.Second.y2) {
                                    BitCounter.Decrease(ref now.First.Counter, 4);
                                    TempDepth = now.Third;
                                    Deleted = true;
                                    continue;
                                } else if (OldPoint.y == ymid) {
                                    BitCounter.Decrease(ref now.First.Counter, 2);
                                    TempDepth = now.Third;
                                    Deleted = true;
                                    continue;
                                }
                            } else if (OldPoint.y == ymid) {
                                if (OldPoint.x == now.Second.x1) {
                                    BitCounter.Decrease(ref now.First.Counter, 1);
                                    TempDepth = now.Third;
                                    Deleted = true;
                                    continue;
                                } else if (OldPoint.x == now.Second.x2) {
                                    BitCounter.Decrease(ref now.First.Counter, 3);
                                    TempDepth = now.Third;
                                    Deleted = true;
                                    continue;
                                }
                            }

                            Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref now.Second);
                            if (OldPoint.y <= ymid) {
                                if (OldPoint.x <= xmid && ChildIsNotNullOrEmpty(now.First, 0))
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, byte>(now.First.Nodes[0], ref ChildRegion[0], (byte)(now.Third + 1)));
                                if (OldPoint.x >= xmid && ChildIsNotNullOrEmpty(now.First, 1))
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, byte>(now.First.Nodes[1], ref ChildRegion[1], (byte)(now.Third + 1)));
                            }
                            if (OldPoint.y >= ymid) {
                                if (OldPoint.x <= xmid && ChildIsNotNullOrEmpty(now.First, 2))
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, byte>(now.First.Nodes[2], ref ChildRegion[2], (byte)(now.Third + 1)));
                                if (OldPoint.x >= xmid && ChildIsNotNullOrEmpty(now.First, 3))
                                    q.Enqueue(new Group<StorageNode, Geometries.Rectangle<long>, byte>(now.First.Nodes[3], ref ChildRegion[3], (byte)(now.Third + 1)));
                            }
                        }
                    }

                    if (!Deleted)
                        throw new System.Exception("StorageTree: 数据(" + OldPoint.x + "," + OldPoint.y + ")未被删除.");
                    Count--;
                    if (TempDepth == Depth) UpdateDepth();
                }

                private void UpdateDepth() {
                    Stack<Pair<StorageNode, byte>> s = new Stack<Pair<StorageNode, byte>>();
                    s.Push(new Pair<StorageNode, byte>(NodeRoot, 1));
                    byte TempDepth = 0;
                    while (s.Count != 0) {
                        Pair<StorageNode, byte> now = s.Pop();
                        TempDepth = (byte)System.Math.Max(TempDepth, now.Second);
                        for (int i = 0; i < 4; i++)
                            if (ChildIsNotNullOrEmpty(now.First, i))
                                s.Push(new Pair<StorageNode, byte>(now.First.Nodes[i], (byte)(now.Second + 1)));
                    }
                    Depth = System.Math.Min(Depth, TempDepth);
                }

                public float[,] GetInterPolatedHeightMap(int Size) {

                    Stack<StorageNode> s = new Stack<StorageNode>();
                    s.Push(NodeRoot);
                    while (s.Count != 0) {
                        StorageNode now = s.Pop();
                        for (int i = 0; i < 4; i++)
                            if (ChildIsNotNullOrEmpty(now, i))
                                s.Push(now.Nodes[i]);
                    }

                    /*
                     * 注意HeightMap中xy坐标与世界坐标相反!!!!!!!!!!!!
                     */

                    long HeightRange = MaxHeight - MinHeight + 1;
                    float[,] map = new float[Size, Size];
                    map[0, 0] = (float)(VertexHeight[0] - MinHeight) / HeightRange;
                    map[0, Size - 1] = (float)(VertexHeight[1] - MinHeight) / HeightRange;
                    map[Size - 1, 0] = (float)(VertexHeight[2] - MinHeight) / HeightRange;
                    map[Size - 1, Size - 1] = (float)(VertexHeight[3] - MinHeight) / HeightRange;

                    Queue<Pair<StorageNode, Geometries.Square<byte>>>
                        q = new Queue<Pair<StorageNode, Geometries.Square<byte>>>();
                    q.Enqueue(new Pair<StorageNode, Geometries.Square<byte>>(NodeRoot, new Geometries.Square<byte>(0, 0, (byte)(Size - 1))));
                    while (q.Count != 0) {
                        Pair<StorageNode, Geometries.Square<byte>> now = q.Dequeue();
                        int
                            x1 = now.Second.x,
                            x2 = x1 + now.Second.Length,
                            y1 = now.Second.y,
                            y2 = y1 + now.Second.Length,
                            xmid = x1 + now.Second.Length / 2,
                            ymid = y1 + now.Second.Length / 2;

                        if (now.First != null) {
                            StorageNode node = now.First;
                            map[y1, xmid] = BitCounter.IsNotZero(node.Counter, 0) ?
                                (float)(node.Height[0] - MinHeight) / HeightRange : (map[y1, x1] + map[y1, x2]) / 2;
                            map[ymid, x1] = BitCounter.IsNotZero(node.Counter, 1) ?
                                (float)(node.Height[1] - MinHeight) / HeightRange : (map[y1, x1] + map[y2, x1]) / 2;
                            map[ymid, xmid] = BitCounter.IsNotZero(node.Counter, 2) ?
                                (float)(node.Height[2] - MinHeight) / HeightRange : (map[y1, x1] + map[y1, x2] + map[y2, x1] + map[y2, x2]) / 4;
                            map[ymid, x2] = BitCounter.IsNotZero(node.Counter, 3) ?
                                (float)(node.Height[3] - MinHeight) / HeightRange : (map[y1, x2] + map[y2, x2]) / 2;
                            map[y2, xmid] = BitCounter.IsNotZero(node.Counter, 4) ?
                                (float)(node.Height[4] - MinHeight) / HeightRange : (map[y2, x1] + map[y2, x2]) / 2;

                            if (now.Second.Length != 2) {
                                Geometries.Square<byte>[] ChildArrayRegion = Geometries.Split(ref now.Second);
                                for (int i = 0; i < 4; i++)
                                    q.Enqueue(new Pair<StorageNode, Geometries.Square<byte>>(node.Nodes[i], ref ChildArrayRegion[i]));
                            }
                        } else {
                            //Inter polate
                            float SqrOfArrayLength = now.Second.Length * now.Second.Length;
                            for (int i = x1; i <= x2; i++)
                                for (int j = y1; j <= y2; j++)
                                    map[j, i] = (//对每个空白点进行插值计算
                                        map[y1, x1] * (x2 - i) * (y2 - j)
                                        + map[y1, x2] * (i - x1) * (y2 - j)
                                        + map[y2, x1] * (x2 - i) * (j - y1)
                                        + map[y2, x2] * (i - x1) * (j - y1)
                                    ) / SqrOfArrayLength;
                        }
                    }
                    return map;
                }
            }

            public int Changed;
            public bool TerrainEntityReady = false;
            public UnityEngine.GameObject TerrainEntity = null;
            public LongVector3 TerrainPosition;
            public ApplyBlock[] Child = null;

            public ManagedTerrain ManagedTerrainRoot;
            public Geometries.Rectangle<long> Region;
            public StorageTree StorageTreeRoot;
            public LongVector3 CenterAdjust;
            public float Range;
            public double Key;

            public ApplyBlock(ManagedTerrain ManagedTerrainRoot, ref TerrainUnitData InitialData) {
                Changed = 0;
                this.ManagedTerrainRoot = ManagedTerrainRoot;
                Region = InitialData.Region;
                StorageTreeRoot = new StorageTree(InitialData);
            }
            public ApplyBlock(ApplyBlock Father, ref Geometries.Rectangle<long> Region, StorageTree StorageTreeRoot) {
                Changed = StorageTreeRoot.Count;
                ManagedTerrainRoot = Father.ManagedTerrainRoot;
                this.Region = Region;
                this.StorageTreeRoot = StorageTreeRoot;
            }
        }
        public sealed class ManagedTerrain : Object {
            private TerrainManager TerrainManager;
            public bool SeparateFromFatherObject;
            public TerrainUnitData InitialData;
            public CalculateNode CalculateNodeRoot;
            public ApplyBlock ApplyBlockRoot;

            public ManagedTerrain(TerrainManager tTerrainManager, TerrainUnitData InitialData, bool SeparateFromFatherObject = false) {
                long
                    xlen = InitialData.Region.x2 - InitialData.Region.x1,
                    ylen = InitialData.Region.y2 - InitialData.Region.y1;
                CenterAdjust = new LongVector3(xlen / 2, 0, ylen / 2).toVector3();
                Range = (System.Math.Sqrt((double)xlen * xlen + (double)ylen * ylen) / 1000) / 2;

                TerrainManager = tTerrainManager;
                this.InitialData = InitialData;
                this.SeparateFromFatherObject = SeparateFromFatherObject;
                if (SeparateFromFatherObject == true)
                    SEPosition = new LongVector3(0, 0, 0);
                CalculateNodeRoot = new CalculateNode(this, ref InitialData);
                ApplyBlockRoot = new ApplyBlock(this, ref InitialData);
            }

            override public void Start() {
                TerrainManager._Regist(this);
            }
            override public void Destory() {
                TerrainManager._Unregist(this);
            }
        }
    }
}