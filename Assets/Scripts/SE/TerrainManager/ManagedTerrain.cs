using System.Collections.Generic;

namespace SE {
    static partial class TerrainManager {

        public sealed class ManagedTerrain : Object {

            public class CalculateNode {

                public Geometries.Point<long, long>[,] Map = null;

                public CalculateNode[,] Child = null;

                //--------------------------------------------------------------------------------------

                public ManagedTerrain ManagedTerrainRoot;

                public TerrainUnitData InitialData;

                public long MinHeight, MaxHeight;

                public LongVector3 CenterAdjust;

                public double Range, Key;

                public CalculateNode(ManagedTerrain ManagedTerrainRoot, ref TerrainUnitData InitialData) {

                    this.ManagedTerrainRoot = ManagedTerrainRoot;

                    this.InitialData = InitialData;

                    MaxHeight = System.Math.Max(
                        System.Math.Max(InitialData.Map[0], InitialData.Map[2]),
                        System.Math.Max(InitialData.Map[6], InitialData.Map[8])
                    );

                    MinHeight = System.Math.Min(
                        System.Math.Min(InitialData.Map[0], InitialData.Map[2]),
                        System.Math.Min(InitialData.Map[6], InitialData.Map[8])
                    );
                }
            }

            public class ApplyBlock {

                public class StorageTree {

                    private static class BitCounter {
                        public static void Increase(ref uint Counter, int Index) {
							if (BitBool.Get (Counter, Index * 2)) {
								//if (BitBool.Get (Counter, Index * 2 + 1))
								//	UnityEngine.Debug.Log ("BitCounter Increase "+Index+": Counter is full.");
								//else
									BitBool.SetTrue (ref Counter, Index * 2 + 1);
							} else
								BitBool.SetTrue (ref Counter, Index * 2);
						}
                        public static void Decrease(ref uint Counter, int Index) {
							if (BitBool.Get (Counter, Index * 2 + 1))
								BitBool.SetFalse (ref Counter, Index * 2 + 1);
							else {
								//if (BitBool.Get (Counter, Index * 2))
									BitBool.SetFalse (ref Counter, Index * 2);
								//else
								//	UnityEngine.Debug.Log ("BitCounter Decrease "+Index+": Counter is Empty.");
							}
							//if (GetInt (Counter, Index) == 0)
							//	UnityEngine.Debug.Log ("The Counter is decreased to 0.");
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
									BitBool.SetTrue (ref Counter, Index * 2 + 1);
								else if (Value != 1)
									throw new System.Exception ("Storage Tree BitCounter SetInt : Out of Range (0-2)");
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

                        public uint
                            Counter = 0;

                        public long[]
                            Height = new long[5] { 0, 0, 0, 0, 0, };

                        public StorageNode[]
                            Nodes = new StorageNode[4] { null, null, null, null, };
                    }

                    public uint VertexCounter = 0;

                    public long[] VertexHeight;

                    private StorageNode NodeRoot;

                    public int Depth;

                    public long
                        MaxHeight, MinHeight;

                    public Geometries.Rectangle<long> Region;

                    public StorageTree(TerrainUnitData Data) {

                        VertexHeight = new long[4];

                        NodeRoot = new StorageNode();

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
                    private StorageTree(ref long[] VertexHeight, uint VertexCounter, ref Geometries.Rectangle<long> Region, StorageNode NodeRoot) {

                        this.VertexCounter = VertexCounter;

                        this.VertexHeight = VertexHeight;

                        this.NodeRoot = NodeRoot;

                        this.Region = Region;
						
						//获取Depth & MaxHeight & MinHeight
						Depth = 0;
                        MaxHeight = System.Math.Max(
                            System.Math.Max(VertexHeight[0], VertexHeight[1]),
                            System.Math.Max(VertexHeight[2], VertexHeight[3])
                        );
                        MinHeight = System.Math.Min(
                            System.Math.Min(VertexHeight[0], VertexHeight[1]),
                            System.Math.Min(VertexHeight[2], VertexHeight[3])
                        );

                        Queue<Pair<StorageNode, int>>
						q = new Queue<Pair<StorageNode, int>>();

						q.Enqueue(new Pair<StorageNode, int>(NodeRoot, 1));

						while (q.Count != 0) {

							Pair<StorageNode, int> now = q.Dequeue();

							Depth = System.Math.Max(Depth, now.Second);

							for(int i=0;i<5;i++)
								if (BitCounter.IsNotZero(now.First.Counter,i)){
								    MaxHeight=System.Math.Max(MaxHeight,now.First.Height[i]);
									MinHeight=System.Math.Min(MinHeight,now.First.Height[i]);
								}

							for (int i = 0; i < 4; i++)
								if (ChildIsNotNullOrEmpty(now.First,i))
									q.Enqueue(new Pair<StorageNode, int>(now.First.Nodes[i], now.Second + 1));
						}
                    }
                    public static StorageTree Merge(StorageTree[] ChildTree) {

                        Geometries.Rectangle<long>
                            Region = new Geometries.Rectangle<long>(
                                ChildTree[0].Region.x1,
                                ChildTree[3].Region.x2,
                                ChildTree[0].Region.y1,
                                ChildTree[3].Region.y2
                            );

                        StorageNode
                            Node = new StorageNode();

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

                        return new StorageTree(ref VertexHeight, VertexCounter, ref Region, Node);
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
                            ChildNode[i] = (Tree.NodeRoot.Nodes[i] == null) ? new StorageNode() : Tree.NodeRoot.Nodes[i];

                        return new StorageTree[4] {
                            new StorageTree(ref ChildVertexHeight[0],ChildVertexCounter[0],ref ChildRegion[0],ChildNode[0]),
                            new StorageTree(ref ChildVertexHeight[1],ChildVertexCounter[1],ref ChildRegion[1],ChildNode[1]),
                            new StorageTree(ref ChildVertexHeight[2],ChildVertexCounter[2],ref ChildRegion[2],ChildNode[2]),
                            new StorageTree(ref ChildVertexHeight[3],ChildVertexCounter[3],ref ChildRegion[3],ChildNode[3]),
                        };
                    }

					private static void ChildNodePrepare(StorageNode Node, int Index) {
                        if (Node.Nodes[Index] == null)
                            Node.Nodes[Index] = new StorageNode();
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

							int TempDepth = 0;

							Queue<Group<StorageNode, Geometries.Rectangle<long>, int>>
							    q = new Queue<Group<StorageNode, Geometries.Rectangle<long>, int>>();
							q.Enqueue (new Group<StorageNode, Geometries.Rectangle<long>, int> (NodeRoot, ref Region, 1));

							while (q.Count != 0) {

								Group<StorageNode, Geometries.Rectangle<long>, int> now = q.Dequeue ();

                                //if (NewPoint.x == 953 && NewPoint.y == 61035)
                                //    UnityEngine.Debug.Log("Loop : (" + now.Second.x1 + "," + now.Second.x2 + "," + now.Second.y1 + "," + now.Second.y2 + ")");

                                TempDepth = System.Math.Max (TempDepth, now.Third);

								long
								xmid = (now.Second.x1 + now.Second.x2) / 2,
								ymid = (now.Second.y1 + now.Second.y2) / 2;

								//判断当前节点是否能够存储(是则结束该分支)
								if (NewPoint.x == xmid) {
									if (NewPoint.y == now.Second.y1) {
										BitCounter.Increase (ref now.First.Counter, 0);
										now.First.Height [0] = NewPoint.h;
										Inserted = true;
										continue;
									} else if (NewPoint.y == now.Second.y2) {
										BitCounter.Increase (ref now.First.Counter, 4);
										now.First.Height [4] = NewPoint.h;
										Inserted = true;
										continue;
									} else if (NewPoint.y == ymid) {
										BitCounter.Increase (ref now.First.Counter, 2);
										now.First.Height [2] = NewPoint.h;
										Inserted = true;
										continue;
									}
								} else if (NewPoint.y == ymid) {
									if (NewPoint.x == now.Second.x1) {
										BitCounter.Increase (ref now.First.Counter, 1);
										now.First.Height [1] = NewPoint.h;
										Inserted = true;
										continue;
									} else if (NewPoint.x == now.Second.x2) {
										BitCounter.Increase (ref now.First.Counter, 3);
										now.First.Height [3] = NewPoint.h;
										Inserted = true;
										continue;
									}
								}

								Geometries.Rectangle<long>[] ChildRegion = Geometries.Split (ref now.Second);

								//若当前节点不能存储则查找子节点
								if (NewPoint.y <= ymid) {
									if (NewPoint.x <= xmid) {
										ChildNodePrepare (now.First, 0);
										q.Enqueue (new Group<StorageNode, Geometries.Rectangle<long>, int> (now.First.Nodes [0], ref ChildRegion [0], now.Third + 1));
									}
									if (NewPoint.x >= xmid) {
										ChildNodePrepare (now.First, 1);
										q.Enqueue (new Group<StorageNode, Geometries.Rectangle<long>, int> (now.First.Nodes [1], ref ChildRegion [1], now.Third + 1));
									}
								}
								if (NewPoint.y >= ymid) {
									if (NewPoint.x <= xmid) {
										ChildNodePrepare (now.First, 2);
										q.Enqueue (new Group<StorageNode, Geometries.Rectangle<long>, int> (now.First.Nodes [2], ref ChildRegion [2], now.Third + 1));
									}
									if (NewPoint.x >= xmid) {
										ChildNodePrepare (now.First, 3);
										q.Enqueue (new Group<StorageNode, Geometries.Rectangle<long>, int> (now.First.Nodes [3], ref ChildRegion [3], now.Third + 1));
									}
								}
							}

							Depth = System.Math.Max(Depth, TempDepth);
						}

                        if (!Inserted)
                            throw new System.Exception("StorageTree: 数据(" + NewPoint.x + "," + NewPoint.y + "," + NewPoint.h + ")未被插入.");
						
                        MaxHeight = System.Math.Max(MaxHeight, NewPoint.h);
                        MinHeight = System.Math.Min(MinHeight, NewPoint.h);
                    }

					private static bool ChildIsNotNullOrEmpty(StorageNode Node, int Index) {

                        if (Node.Nodes[Index] == null) return false;

                        for (int i = 0; i < 4; i++)
                            if (ChildIsNotNullOrEmpty(Node.Nodes[Index], i))
                                return true;

                        if (Node.Nodes[Index].Counter == 0) {
                            Node.Nodes[Index] = null;
                            return false;
                        }

                        return true;
					}
                    private static bool NodeIsNotNullOrEmpty(StorageNode Node) {

                        if (Node == null) return false;

                        for (int i = 0; i < 4; i++)
                            if (ChildIsNotNullOrEmpty(Node, i))
                                return true;

                        if (Node.Counter == 0)
                            return false;

                        return true;
                    }

                    public void Delete(Geometries.Point<long, long> OldPoint) {

                        bool Deleted = false;

                        if (OldPoint.y == Region.y1) {
                            if (OldPoint.x == Region.x1) {
                                Deleted = true;
                                BitCounter.Decrease(ref VertexCounter, 0);
                            } else if (OldPoint.x == Region.x2) {
                                Deleted = true;
                                BitCounter.Decrease(ref VertexCounter, 1);
                            }
                        } else if (OldPoint.y == Region.y2) {
                            if (OldPoint.x == Region.x1) {
                                Deleted = true;
                                BitCounter.Decrease(ref VertexCounter, 2);
                            } else if (OldPoint.x == Region.x2) {
                                Deleted = true;
                                BitCounter.Decrease(ref VertexCounter, 3);
                            }
                        }

						if (!Deleted) {

							Queue<Pair<StorageNode, Geometries.Rectangle<long>>>
							    q = new Queue<Pair<StorageNode, Geometries.Rectangle<long>>>();
							q.Enqueue (new Pair<StorageNode, Geometries.Rectangle<long>> (NodeRoot, ref Region));

                            //if (OldPoint.x == 953 && OldPoint.y == 61035)
                            //    UnityEngine.Debug.Log("Loop : Start");

                            while (q.Count != 0) {

                                Pair <StorageNode, Geometries.Rectangle<long>> now = q.Dequeue ();

                                //if (OldPoint.x == 953 && OldPoint.y == 61035)
                                //    UnityEngine.Debug.Log("Loop : ("+now.Second.x1+","+ now.Second.x2 + ","+ now.Second.y1 + ","+ now.Second.y2 + ")");

                                long
                                    xmid = (now.Second.x1 + now.Second.x2) / 2,
                                    ymid = (now.Second.y1 + now.Second.y2) / 2;

								//判断当前节点是否存在(是则结束该分支)
								if (OldPoint.x == xmid) {
									if (OldPoint.y == now.Second.y1) {
										BitCounter.Decrease (ref now.First.Counter, 0);
										Deleted = true;
										continue;
									} else if (OldPoint.y == now.Second.y2) {
										BitCounter.Decrease (ref now.First.Counter, 4);
										Deleted = true;
										continue;
									} else if (OldPoint.y == ymid) {
										BitCounter.Decrease (ref now.First.Counter, 2);
										Deleted = true;
										continue;
									}
								} else if (OldPoint.y == ymid) {
									if (OldPoint.x == now.Second.x1) {
										BitCounter.Decrease (ref now.First.Counter, 1);
										Deleted = true;
										continue;
									} else if (OldPoint.x == now.Second.x2) {
										BitCounter.Decrease (ref now.First.Counter, 3);
										Deleted = true;
										continue;
									}
								}

								Geometries.Rectangle<long>[] ChildRegion = Geometries.Split (ref now.Second);

                                //若当前节点不存在则查找子节点(可能出现分支)
                                if (OldPoint.y <= ymid) {
                                    if (OldPoint.x <= xmid && ChildIsNotNullOrEmpty(now.First, 0))
                                        q.Enqueue(new Pair<StorageNode, Geometries.Rectangle<long>>(now.First.Nodes[0], ref ChildRegion[0]));
                                    if (OldPoint.x >= xmid && ChildIsNotNullOrEmpty(now.First, 1))
                                        q.Enqueue(new Pair<StorageNode, Geometries.Rectangle<long>>(now.First.Nodes[1], ref ChildRegion[1]));
                                }
                                if (OldPoint.y >= ymid) {
                                    if (OldPoint.x <= xmid && ChildIsNotNullOrEmpty(now.First, 2))
                                        q.Enqueue(new Pair<StorageNode, Geometries.Rectangle<long>>(now.First.Nodes[2], ref ChildRegion[2]));
                                    if (OldPoint.x >= xmid && ChildIsNotNullOrEmpty(now.First, 3))
                                        q.Enqueue(new Pair<StorageNode, Geometries.Rectangle<long>>(now.First.Nodes[3], ref ChildRegion[3]));
                                }
							}
						}

                        if (!Deleted)
                            throw new System.Exception("StorageTree: 数据(" + OldPoint.x + "," + OldPoint.y + ")未被删除.");

                        UpdateDepth();
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

                        //回收空的StorageNode (使用栈模拟递归过程)

                        Stack<StorageNode> s = new Stack<StorageNode>();
						s.Push(NodeRoot);
						
						while (s.Count != 0) {
												
							StorageNode now = s.Pop();

							for (int i = 0; i < 4; i++)
								if (ChildIsNotNullOrEmpty(now,i))
									s.Push(now.Nodes[i]);
						}

                        /*
                         * 注意HeightMap中xy坐标与世界坐标相反!!!!!!!!!!!!
                         */

                        long HeightRange = MaxHeight - MinHeight + 1;

                        float[,] map = new float[Size, Size];

						Queue<Pair<StorageNode, Geometries.Square<byte>>>
						    q = new Queue<Pair<StorageNode, Geometries.Square<byte>>>();

						q.Enqueue(new Pair<StorageNode, Geometries.Square<byte>>(
                            NodeRoot,
							new Geometries.Square<byte>(0, 0, (byte)(Size - 1))
                        ));

                        map[0, 0] = (float)(VertexHeight[0] - MinHeight) / HeightRange;
						map[0, Size - 1] = (float)(VertexHeight[1] - MinHeight) / HeightRange;
						map[Size - 1, 0] = (float)(VertexHeight[2] - MinHeight) / HeightRange;
                        map[Size - 1, Size - 1] = (float)(VertexHeight[3] - MinHeight) / HeightRange;

                        //UnityEngine.Debug.Log(map[0, 0] + "  " + map[Size - 1, 0] + "  " + map[0, Size - 1] + "  " + map[Size - 1, Size - 1]);

                        while (q.Count != 0) {

                            Pair<StorageNode, Geometries.Square<byte>>
                                now = q.Dequeue();

                            int
                                x1 = now.Second.x,
                                x2 = x1 + now.Second.Length,
                                y1 = now.Second.y,
                                y2 = y1 + now.Second.Length,
                                xmid = x1 + now.Second.Length / 2,
                                ymid = y1 + now.Second.Length / 2;

                            if (NodeIsNotNullOrEmpty(now.First)) {

                                //填充已知点高度并继续分割
                                StorageNode node = now.First;

                                map[y1, xmid] = BitCounter.IsNotZero(node.Counter, 0) ?
                                    (float)(node.Height[0] - MinHeight) / HeightRange : (map [y1, x1] + map [y1, x2]) / 2;
                                map[ymid, x1] = BitCounter.IsNotZero(node.Counter, 1) ?
                                    (float)(node.Height[1] - MinHeight) / HeightRange : (map [y1, x1] + map [y2, x1]) / 2;
                                map[ymid, xmid] = BitCounter.IsNotZero(node.Counter, 2) ?
                                    (float)(node.Height[2] - MinHeight) / HeightRange : (map [y1, x1] + map [y1, x2] + map [y2, x1] + map [y2, x2]) / 4;
                                map[ymid, x2] = BitCounter.IsNotZero(node.Counter, 3) ?
                                    (float)(node.Height[3] - MinHeight) / HeightRange : (map [y1, x2] + map [y2, x2]) / 2;
                                map[y2, xmid] = BitCounter.IsNotZero(node.Counter, 4) ?
                                    (float)(node.Height[4] - MinHeight) / HeightRange : (map [y2, x1] + map [y2, x2]) / 2;

                                if (now.Second.Length != 2) {

                                    Geometries.Square<byte>[] ChildArrayRegion = Geometries.Split(ref now.Second);

                                    for (int i = 0; i < 4; i++)
										q.Enqueue(new Pair<StorageNode, Geometries.Square<byte>>(node.Nodes[i], ref ChildArrayRegion[i]));
                                }
                            } else {

                                //插值填充空白区域

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

                public int Changed = 0;

                public UnityEngine.GameObject TerrainEntity = null;

                public LongVector3 TerrainPosition;

                public ApplyBlock[] Child = null;

                //------------------------------------------------------------------------------------

                public ManagedTerrain ManagedTerrainRoot;

                public Geometries.Rectangle<long> Region;

                public StorageTree StorageTreeRoot;

                public LongVector3 CenterAdjust;

                public float Range;
                public double Key;

                public ApplyBlock(ManagedTerrain ManagedTerrainRoot, ref TerrainUnitData InitialData) {

                    this.ManagedTerrainRoot = ManagedTerrainRoot;

                    Region = InitialData.Region;

                    StorageTreeRoot = new StorageTree(InitialData);
                }
                private ApplyBlock(ApplyBlock Father, ref Geometries.Rectangle<long> Region, StorageTree StorageTreeRoot) {

                    ManagedTerrainRoot = Father.ManagedTerrainRoot;

                    this.Region = Region;

                    this.StorageTreeRoot = StorageTreeRoot;
                }

                public void Split() {

					UnityEngine.Debug.Log ("ApplyBlock : Split (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ")");

                    Geometries.Rectangle<long>[]
                        ChildRegion = Geometries.Split(ref Region);

                    StorageTree[]
                        ChildStorageTree = StorageTree.Split(StorageTreeRoot);

                    Child = new ApplyBlock[4];
                    for (int i = 0; i < 4; i++)
                        Child[i] = new ApplyBlock(this, ref ChildRegion[i], ChildStorageTree[i]);

                    StorageTreeRoot = null;

                    for (int i = 0; i < 4; i++)
                        Child[i].ApplyTerrainEntity();

                    RemoveTerrainEntity();
                }

                public void Merge() {
					//System.Threading.Thread.Sleep (1000);
					UnityEngine.Debug.Log ("ApplyBlock : Merge (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ")");

					//UnityEngine.Debug.Log("ApplyBlock : Merge " +Child[0].StorageTreeRoot.Depth+","+Child[1].StorageTreeRoot.Depth+","+Child[2].StorageTreeRoot.Depth+","+Child[3].StorageTreeRoot.Depth);
                    StorageTreeRoot = StorageTree.Merge(new StorageTree[4] {
                        Child[0].StorageTreeRoot,
                        Child[1].StorageTreeRoot,
                        Child[2].StorageTreeRoot,
                        Child[3].StorageTreeRoot,
                    });
					//UnityEngine.Debug.Log("ApplyBlock : Merge " +Child[0].StorageTreeRoot.Depth+","+Child[1].StorageTreeRoot.Depth+","+Child[2].StorageTreeRoot.Depth+","+Child[3].StorageTreeRoot.Depth);
					//UnityEngine.Debug.Log ("ApplyBlock : Merge " +StorageTreeRoot.Depth);

                    ApplyTerrainEntity();
                    Changed = 0;

                    for (int i = 0; i < 4; i++)
                        Child[i].RemoveTerrainEntity();

                    Child = null;
                }

                public void ApplyTerrainEntity() {

					if (Region.x2 - Region.x1 >= TerrainBlockSizeLimit
                        || Region.y2 - Region.y1 >= TerrainBlockSizeLimit) {
						//UnityEngine.Debug.Log ("ApplyTerrainEntity Reject : >= TerrainBlockSizeLimit");
						return;
					}

                    //System.Threading.Thread.Sleep (250);

                    //UnityEngine.Debug.Log("ApplyTerrainEntity: (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ")");

                    //Prepare TerrainData :

                    //get size(size)
                    UnityEngine.Vector3
                        TerrainDataSize = new LongVector3(
                            Region.x2 - Region.x1,
                            StorageTreeRoot.MaxHeight - StorageTreeRoot.MinHeight + 1,
                            Region.y2 - Region.y1
                        ).toVector3();

                    //get heightmapResolution(heightmapdetail)
                    int
                        Depth = StorageTreeRoot.Depth,
                        TerrainDataHeightMapDetail = 1;

                    for (int i = 0; i < Depth; i++)
                        TerrainDataHeightMapDetail *= 2;

                    TerrainDataHeightMapDetail=System.Math.Max(32, TerrainDataHeightMapDetail) + 1;

                    //get position(Position)
                    TerrainPosition = new LongVector3(Region.x1, StorageTreeRoot.MinHeight, Region.y1);

                    //get heightmap(heightmap)
                    float[,] TerrainDataHeightMap = StorageTreeRoot.GetInterPolatedHeightMap(TerrainDataHeightMapDetail);

                    //new & set Terrain

                    Thread.QueueOnMainThread(
                        delegate () {

                            UnityEngine.TerrainData TerrainData = new UnityEngine.TerrainData();
                            TerrainData.heightmapResolution = TerrainDataHeightMapDetail;
                            TerrainData.baseMapResolution = TerrainDataHeightMapDetail;
                            TerrainData.size = TerrainDataSize;
                            TerrainData.SetHeightsDelayLOD(0, 0, TerrainDataHeightMap);
                            
                            //先应用再删除
                            UnityEngine.GameObject TempTerrainEntity = TerrainEntity;

                            TerrainEntity = UnityEngine.Terrain.CreateTerrainGameObject(TerrainData);

                            if (ManagedTerrainRoot.SeparateFromFatherObject == false) {
                                TerrainEntity.transform.parent = ManagedTerrainRoot.UnityRoot.transform;
                                TerrainEntity.transform.localPosition = TerrainPosition.toVector3();
                            } else {
                                TerrainEntity.transform.localPosition = Kernel.SEPositionToUnityPosition(ManagedTerrainRoot.SEPosition + TerrainPosition);
                            }

                            UnityEngine.Object.Destroy(TempTerrainEntity);
                        }
                    );

                    //UnityEngine.Debug.Log("ApplyTerrainEntity: (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ") Finished.");
                }

                public void RemoveTerrainEntity() {

					//System.Threading.Thread.Sleep (250);

                    Thread.QueueOnMainThread(
                        delegate () {
                            ApplyBlock t = this;
                            UnityEngine.Terrain.Destroy(t.TerrainEntity);
                            t.TerrainEntity = null;
                        }
                    );
                }
            }

            public bool SeparateFromFatherObject;

            public TerrainUnitData InitialData;

            public CalculateNode CalculateNodeRoot;

            public ApplyBlock ApplyBlockRoot;

            public ManagedTerrain(TerrainUnitData InitialData, bool SeparateFromFatherObject = false) {

                long
                    xlen = InitialData.Region.x2 - InitialData.Region.x1,
                    ylen = InitialData.Region.y2 - InitialData.Region.y1;

                CenterAdjust = new LongVector3(xlen / 2, 0, ylen / 2).toVector3();

                Range = (System.Math.Sqrt((double)xlen * xlen + (double)ylen * ylen) / 1000) / 2;

                this.InitialData = InitialData;

                this.SeparateFromFatherObject = SeparateFromFatherObject;

                if (SeparateFromFatherObject == true)
                    SEPosition = new LongVector3(0, 0, 0);

                CalculateNodeRoot = new CalculateNode(this, ref InitialData);

                ApplyBlockRoot = new ApplyBlock(this, ref InitialData);
            }

            override public void Start() {
                Regist(this);
            }

            override public void Destory() {
                Unregist(this);
            }
        }
    }
}