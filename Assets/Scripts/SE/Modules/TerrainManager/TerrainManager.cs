using System.Collections.Generic;
using System.Collections.Generic.LockFree;

namespace SE.Modules {
    public partial class TerrainManager : IModule {
        private class KernelIDSmallFirstManagedTerrainComparer : IComparer<ManagedTerrain> {
            public int Compare(ManagedTerrain a, ManagedTerrain b) {
                return (a.KernelID == b.KernelID) ? 0 : ((a.KernelID > b.KernelID) ? 1 : -1);
            }
        }
        private class KeyBigFirstCalculateNodeComparer : IComparer<CalculateNode> {
            public int Compare(CalculateNode a, CalculateNode b) {
                return (a.Key == b.Key) ? 0 : ((a.Key > b.Key) ? 1 : -1);
            }
        }
        private class PositionSmallFirstTerrainBlockPointComparer : IComparer<Geometries.Point<long, long>> {
            public int Compare(Geometries.Point<long, long> a, Geometries.Point<long, long> b) {
                return (a.x == b.x && a.y == b.y) ? 0 : ((a.x > b.x || (a.x == b.x && a.y > b.y)) ? 1 : -1);
            }
        }
        private class KeyBigFirstApplyBlockComparer : IComparer<ApplyBlock> {
            public int Compare(ApplyBlock a, ApplyBlock b) {
                return (a.Key == b.Key) ? 0 : ((a.Key < b.Key) ? 1 : -1);
            }
        }
        public class Settings {
            //The max precision limit of terrain unit calculating
            public float UnitPrecisionMinLimit = 0.05F;
            //The min size limit of terrain unit size (mm)
            public long UnitSizeMinLimit = 600;
            //The max task amount limit per loop for terrain unit calculating (anti-block)
            public int CalculateLoopTaskMaxLimit = 100;
            //The max task amount limit per loop for terrain unit recycling (anti-block)
            public long CalculateLoopAppendRecycleTaskMaxLimit = 100;

            //The max depth limit of terrain block spliting
            public int BlockSplitDepthMaxLimit = 6;
            //The min depth limit of child terrain block merging
            public int BlockMergeDepthMinLimit = 4;
            //The max size limit of terrain block size (mm)
            public long BlockSizeMaxLimit = 1000 * 1000;
            //The max task amount limit per loop for terrain block applying (anti-block)
            public long ApplyLoopTaskMaxLimit = 8;
            //The max task amount limit per loop for terrain block recycling (anti-block)
            public long ApplyLoopAppendRecycleTaskMaxLimit = 8;
        }

        /*
         * The terrain maintain will split into two parts (Calculating & Applying) :
         *     Calculating thread will work when the scene center is changed.
         *     Applying thread will work with interval.
         */

        private const bool
            OPERATOR_ADD = true,
            OPERATOR_REMOVE = false;

        public Settings _Settings;
        private LockFreeQueue<Pair<bool, ManagedTerrain>> CalculateOperateQueue;
        private LockFreeQueue<Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>> ApplyOperateQueue;
        private static SBTree<ManagedTerrain> ManagedTerrains;
        private static SceneCenter CalculateSceneCenter, ApplySceneCenter, tCalculateSceneCenter, tApplySceneCenter;

        private bool NeedAlive, CalculateAlive, ApplyAlive;
        private object ThreadControlLock;
        private Kernel Kernel;

        public TerrainManager(Settings Settings) {
            _Settings = Settings;
            CalculateOperateQueue = new LockFreeQueue<Pair<bool, ManagedTerrain>>();
            ApplyOperateQueue = new LockFreeQueue<Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>>();
            ManagedTerrains = new SBTree<ManagedTerrain>(new KernelIDSmallFirstManagedTerrainComparer());
            CalculateSceneCenter = null;
            ApplySceneCenter = null;
            tCalculateSceneCenter = null;
            tApplySceneCenter = null;
            NeedAlive = false;
            CalculateAlive = false;
            ApplyAlive = false;
            ThreadControlLock = new object();
        }
        public void _Assigned(Kernel tKernel) {
            Kernel = tKernel;
            CalculateSceneCenter = new SceneCenter(Kernel, new LongVector3(0, 0, 0));
            ApplySceneCenter = new SceneCenter(Kernel, new LongVector3(0, 0, 0));
        }
        public void _Start() {
            lock (ThreadControlLock)
                if (NeedAlive) {
                    throw new System.Exception("Terrain Manager : Thread is already started.");
                } else {
                    NeedAlive = true;
                    Kernel.Threading.Async(CalculateThread);
                    Kernel.Threading.Async(ApplyThread);
                }
        }
        public void _ChangeSceneCenter(ref LongVector3 Position) {
            lock (ThreadControlLock) {
                if (tApplySceneCenter == null)
                    tApplySceneCenter = new SceneCenter(Kernel, ref Position);
                else
                    tApplySceneCenter.Change(ref Position);
                if (tCalculateSceneCenter == null)
                    tCalculateSceneCenter = new SceneCenter(Kernel, ref Position);
                else
                    tCalculateSceneCenter.Change(ref Position);
            }
        }
        public void _ChangeCoordinateOrigin(ref LongVector3 Position) {
            foreach (var terrain in ManagedTerrains)
                //The terrain blocks that have there own roots
                if (terrain.SeparateFromFatherObject == true) {
                    Queue<ApplyBlock> q = new Queue<ApplyBlock>();
                    q.Enqueue(terrain.ApplyBlockRoot);
                    while (q.Count != 0) {
                        ApplyBlock now = q.Dequeue();
                        if (now.TerrainEntity != null)
                            now.TerrainEntity.transform.localPosition = Kernel.Position_SEToUnity(terrain.SEPosition);
                        if (now.Child != null)
                            for (int i = 0; i < 4; i++)
                                q.Enqueue(now.Child[i]);
                    }
                }
        }
        public void _Stop() {
            lock (ThreadControlLock)
                if (NeedAlive) {
                    NeedAlive = false;
                } else {
                    throw new System.Exception("Terrain Manager : Thread is already stopped.");
                }
        }

        public void _Regist(ManagedTerrain NewManagedTerrain) {
            CalculateOperateQueue.Enqueue(new Pair<bool, ManagedTerrain>(OPERATOR_ADD, NewManagedTerrain));
        }
        public void _Unregist(ManagedTerrain NewManagedTerrain) {
            CalculateOperateQueue.Enqueue(new Pair<bool, ManagedTerrain>(OPERATOR_REMOVE, NewManagedTerrain));
        }

        private void Update(CalculateNode Node) {
            double
                zLength = (double)(Node.MaxHeight - Node.MinHeight) / 1000,
                xLength = (double)(Node.InitialData.Region.x2 - Node.InitialData.Region.x1) / 1000,
                yLength = (double)(Node.InitialData.Region.y2 - Node.InitialData.Region.y1) / 1000;
            Node.Range = System.Math.Sqrt(xLength * xLength + yLength * yLength + zLength * zLength) / 2;
            Node.CenterAdjust = new LongVector3(
                (Node.InitialData.Region.x1 + Node.InitialData.Region.x2) / 2,
                (Node.MaxHeight + Node.MinHeight) / 2,
                (Node.InitialData.Region.y1 + Node.InitialData.Region.y2) / 2
            );
            Node.Key = CalculateSceneCenter.Evaluate(Node);
        }

        private void NodeCalculate(CalculateNode Node) {

            //UnityEngine.Debug.Log("Node Calculate: (" + Node.InitialData.Region.x1 + "," + Node.InitialData.Region.x2
            //    + "," + Node.InitialData.Region.y1 + "," + Node.InitialData.Region.y2 + ") ");
            //System.Threading.Thread.Sleep(250);
            Node.Map = new long[17, 17];
            Node.Child = new CalculateNode[16, 16];
            Node.DestroyCallBack = new List<System.Action>();

            TerrainUnitData TempData = new TerrainUnitData(ref Node.InitialData);

            UnitCalculate(Node, ref TempData, Node.DestroyCallBack);
            //UnitCalculate(Node, 0, 0, 16, ref TempData);

            for (int i = 0; i < 17; i++)
                for (int j = 0; j < 17; j++) {

                    if (Node.Map[i, j] < Node.MinHeight)
                        Node.MinHeight = Node.Map[i, j];
                    if (Node.Map[i, j] > Node.MaxHeight)
                        Node.MaxHeight = Node.Map[i, j];
                }

            Update(Node);
        }

        private void UnitDataCalculate(ref TerrainUnitData UnitData, List<System.Action> DestroyCallBackList) {

            for (int i = 0; i < UnitData.Impacts.Count; i++) {
                System.Action CallBack = UnitData.Impacts[i].Start(ref UnitData);
                if (CallBack != null)
                    DestroyCallBackList.Add(CallBack);
            }
        }

        private void UnitDataApplyToNode(ref TerrainUnitData UnitData, CalculateNode Node, ref Geometries.Square<byte> ArrayRegion) {

            int
                mid = ArrayRegion.Length / 2;
            long
                xmid = (UnitData.Region.x2 + UnitData.Region.x1) / 2,
                ymid = (UnitData.Region.y2 + UnitData.Region.y1) / 2;

            Node.Map[ArrayRegion.x + mid, ArrayRegion.y] = UnitData.ExtendMap[1];
            Node.Map[ArrayRegion.x, ArrayRegion.y + mid] = UnitData.ExtendMap[3];
            Node.Map[ArrayRegion.x + mid, ArrayRegion.y + mid] = UnitData.ExtendMap[4];
            Node.Map[ArrayRegion.x + ArrayRegion.Length, ArrayRegion.y + mid] = UnitData.ExtendMap[5];
            Node.Map[ArrayRegion.x + mid, ArrayRegion.y + ArrayRegion.Length] = UnitData.ExtendMap[7];

            //注意四个顶点是不需要赋值的
            Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[5] {
                new Geometries.Point<long, long>(xmid, UnitData.Region.y1, UnitData.ExtendMap[1]),
                new Geometries.Point<long, long>(UnitData.Region.x1, ymid, UnitData.ExtendMap[3]),
                new Geometries.Point<long, long>(xmid, ymid, UnitData.ExtendMap[4]),
                new Geometries.Point<long, long>(UnitData.Region.x2, ymid, UnitData.ExtendMap[5]),
                new Geometries.Point<long, long>(xmid, UnitData.Region.y2, UnitData.ExtendMap[7]),
            };

            _Regist(Node.ManagedTerrainRoot, Points);
        }

        /*
         * 各数据所在索引或获取方式:
         * 
         * 顶点及子块   随机种子
         * 6- -7- -8   *-1-4-2-*
         * |   |   |   |   |   |
         *   2   3     2 3 4 4 2
         * |   |   |   |   |   |
         * 3- -4- -5   2-2-0-3-3
         * |   |   |   |   |   |
         *   0   1     1 1 1 2 1
         * |   |   |   |   |   |
         * 0- -1- -2   *-1-1-2-*
         */
        private TerrainUnitData[] UnitDataSplit(ref TerrainUnitData UnitData) {
            Geometries.Rectangle<long>[]
                ChildRegion = Geometries.Split(ref UnitData.Region);
            long[][] ChildBaseVertex = new long[4][] {
                new long[4] {
                    UnitData.BaseMap[0],UnitData.BaseMap[1],
                    UnitData.BaseMap[3],UnitData.BaseMap[4],
                },
                new long[4] {
                    UnitData.BaseMap[1],UnitData.BaseMap[2],
                    UnitData.BaseMap[4],UnitData.BaseMap[5],
                },
                new long[4] {
                    UnitData.BaseMap[3],UnitData.BaseMap[4],
                    UnitData.BaseMap[6],UnitData.BaseMap[7],
                },
                new long[4] {
                    UnitData.BaseMap[4],UnitData.BaseMap[5],
                    UnitData.BaseMap[7],UnitData.BaseMap[8],
                },
            }, ChildExtendVertex = new long[4][] {
                new long[4] {
                    UnitData.ExtendMap[0],UnitData.ExtendMap[1],
                    UnitData.ExtendMap[3],UnitData.ExtendMap[4],
                },
                new long[4] {
                    UnitData.ExtendMap[1],UnitData.ExtendMap[2],
                    UnitData.ExtendMap[4],UnitData.ExtendMap[5],
                },
                new long[4] {
                    UnitData.ExtendMap[3],UnitData.ExtendMap[4],
                    UnitData.ExtendMap[6],UnitData.ExtendMap[7],
                },
                new long[4] {
                    UnitData.ExtendMap[4],UnitData.ExtendMap[5],
                    UnitData.ExtendMap[7],UnitData.ExtendMap[8],
                },
            };
            RandomSeed[][] ChildRandomSeed = new RandomSeed[4][] {
                new RandomSeed[5] {
                    UnitData.Seed[0].GetRandomSeed(111111),
                    UnitData.Seed[1].GetRandomSeed(1111),UnitData.Seed[2].GetRandomSeed(1111),
                    UnitData.Seed[0].GetRandomSeed(1111),UnitData.Seed[0].GetRandomSeed(2222),
                },
                new RandomSeed[5] {
                    UnitData.Seed[0].GetRandomSeed(222222),
                    UnitData.Seed[1].GetRandomSeed(2222),UnitData.Seed[0].GetRandomSeed(1111),
                    UnitData.Seed[3].GetRandomSeed(1111),UnitData.Seed[0].GetRandomSeed(3333),
                },
                new RandomSeed[5] {
                    UnitData.Seed[0].GetRandomSeed(333333),
                    UnitData.Seed[0].GetRandomSeed(2222),UnitData.Seed[2].GetRandomSeed(2222),
                    UnitData.Seed[0].GetRandomSeed(4444),UnitData.Seed[4].GetRandomSeed(1111),
                },
                new RandomSeed[5] {
                    UnitData.Seed[0].GetRandomSeed(444444),
                    UnitData.Seed[0].GetRandomSeed(3333),UnitData.Seed[0].GetRandomSeed(4444),
                    UnitData.Seed[3].GetRandomSeed(2222),UnitData.Seed[4].GetRandomSeed(2222),
                },
            };
            List<TerrainUnitData.Impact>[] ChildImpact = new List<TerrainUnitData.Impact>[4];
            Dictionary<int, List<CollisionRegion>>[] ChildCollisionRegion = new Dictionary<int, List<CollisionRegion>>[4];
            TerrainUnitData[] arr = new TerrainUnitData[4];
            for (int i = 0; i < 4; i++)
                ChildImpact[i] = TerrainUnitData.Impact.ArrayFilter(UnitData.Impacts, ref ChildRegion[i]);
            for (int i = 0; i < 4; i++)
                ChildCollisionRegion[i] = CollisionRegion.DictionaryFliter(UnitData.CollisionRegions, ref ChildRegion[i]);
            for (int i = 0; i < 4; i++)
                arr[i] = new TerrainUnitData(ref ChildRegion[i], ChildBaseVertex[i], ChildExtendVertex[i], ChildRandomSeed[i], ChildImpact[i], ChildCollisionRegion[i]);
            return arr;
        }

        private void UnitCalculate(CalculateNode Node, ref TerrainUnitData UnitData, List<System.Action> DestroyCallBackList) {
            Queue<Pair<TerrainUnitData, Geometries.Square<byte>>>
                q = new Queue<Pair<TerrainUnitData, Geometries.Square<byte>>>();
            q.Enqueue(new Pair<TerrainUnitData, Geometries.Square<byte>>(ref UnitData, new Geometries.Square<byte>(0, 0, 16)));

            while (q.Count != 0) {
                Pair<TerrainUnitData, Geometries.Square<byte>> now = q.Dequeue();
                if (now.First.Region.x2 - now.First.Region.x1 <= _Settings.UnitSizeMinLimit * 2
                    && now.First.Region.y2 - now.First.Region.y1 <= _Settings.UnitSizeMinLimit * 2)
                    continue;

                UnitDataCalculate(ref now.First, DestroyCallBackList);
                UnitDataApplyToNode(ref now.First, Node, ref now.Second);
                TerrainUnitData[] ChildUnitData = UnitDataSplit(ref now.First);

                if (now.Second.Length == 2) {
                    Node.Child[now.Second.x, now.Second.y] = new CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[0]);
                    Node.Child[now.Second.x + 1, now.Second.y] = new CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[1]);
                    Node.Child[now.Second.x, now.Second.y + 1] = new CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[2]);
                    Node.Child[now.Second.x + 1, now.Second.y + 1] = new CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[3]);
                    Update(Node.Child[now.Second.x, now.Second.y]);
                    Update(Node.Child[now.Second.x + 1, now.Second.y]);
                    Update(Node.Child[now.Second.x, now.Second.y + 1]);
                    Update(Node.Child[now.Second.x + 1, now.Second.y + 1]);
                } else {
                    Geometries.Square<byte>[] ChildArrayRegion = Geometries.Split(ref now.Second);
                    for (int i = 0; i < 4; i++)
                        q.Enqueue(new Pair<TerrainUnitData, Geometries.Square<byte>>(ref ChildUnitData[i], ref ChildArrayRegion[i]));
                }
            }
        }

        private void NodeDestory(CalculateNode Node) {

            //UnityEngine.Debug.Log("NodeDestory : (" + Node.InitialData.Region.x1 + "," + Node.InitialData.Region.x2 + "," + Node.InitialData.Region.y1 + "," + Node.InitialData.Region.y2 + ")");
            //System.Threading.Thread.Sleep(250);

            if (Node.Map == null) return;
            for (int i = Node.DestroyCallBack.Count - 1; i >= 0; i--)
                Node.DestroyCallBack[i]();
            UnitDestory(Node, ref Node.InitialData.Region);
            //UnitDestory(Node, 0, 0, 16, ref Node.InitialData.Region);
            Node.Map = null;
            Node.Child = null;
            Node.DestroyCallBack = null;

            //UnityEngine.Debug.Log ("NodeDestory : ("+Node.InitialData.Region.x1+","+Node.InitialData.Region.x2+","+Node.InitialData.Region.y1+","+Node.InitialData.Region.y2+") Finished.");
        }

        private void UnitDestory(CalculateNode Node, ref Geometries.Rectangle<long> Region) {

            Queue<Pair<Geometries.Rectangle<long>, Geometries.Square<byte>>>
                q = new Queue<Pair<Geometries.Rectangle<long>, Geometries.Square<byte>>>();
            q.Enqueue(new Pair<Geometries.Rectangle<long>, Geometries.Square<byte>>(ref Region, new Geometries.Square<byte>(0, 0, 16)));

            while (q.Count != 0) {
                Pair<Geometries.Rectangle<long>, Geometries.Square<byte>> now = q.Dequeue();
                if (now.First.x2 - now.First.x1 <= _Settings.UnitSizeMinLimit * 2
                    && now.First.y2 - now.First.y1 <= _Settings.UnitSizeMinLimit * 2)
                    continue;
                //UnityEngine.Debug.Log("loop : (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ")");

                long
                    xmid = (now.First.x1 + now.First.x2) / 2,
                    ymid = (now.First.y1 + now.First.y2) / 2;
                int half = now.Second.Length / 2;
                Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[5] {
                    new Geometries.Point<long, long>(xmid, now.First.y1, Node.Map[now.Second.x + half, now.Second.y]),
                    new Geometries.Point<long, long>(now.First.x1, ymid, Node.Map[now.Second.x, now.Second.y + half]),
                    new Geometries.Point<long, long>(xmid, ymid, Node.Map[now.Second.x + half, now.Second.y + half]),
                    new Geometries.Point<long, long>(now.First.x2, ymid, Node.Map[now.Second.x + now.Second.Length, now.Second.y + half]),
                    new Geometries.Point<long, long>(xmid, now.First.y2, Node.Map[now.Second.x + half, now.Second.y + now.Second.Length]),
                };
                _Unregist(Node.ManagedTerrainRoot, Points);

                if (now.Second.Length == 2) {
                    NodeDestory(Node.Child[now.Second.x, now.Second.y]);
                    NodeDestory(Node.Child[now.Second.x + 1, now.Second.y]);
                    NodeDestory(Node.Child[now.Second.x, now.Second.y + 1]);
                    NodeDestory(Node.Child[now.Second.x + 1, now.Second.y + 1]);
                } else {
                    Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref now.First);
                    Geometries.Square<byte>[] ChildArrayRegion = Geometries.Split(ref now.Second);
                    for (int i = 0; i < 4; i++)
                        q.Enqueue(new Pair<Geometries.Rectangle<long>, Geometries.Square<byte>>(ref ChildRegion[i], ref ChildArrayRegion[i]));
                }
            }
        }

        /*
         * Node has three states:
         * 1:Not Calculated                           CalculateNode.Map == null
         * 2:Calculated but all children is destroy   CalculateNode.Map != null && ALL CalculateNode.Child[x,y].Map == null
         * 3:Calculate                                CalculateNode.Map != null && EXIST CalculateNode.Child[x,y].Map != null
         */

        //Terrain calculating use simple DFS scan & heap to update datas. (like IDA* ?)

        private void CalculateThread() {
            try {
                lock (ThreadControlLock) CalculateAlive = true;
                UnityEngine.Debug.Log("TerrainManager CalculateThread Start.");

                PriorityQueue<CalculateNode>
                    q = new PriorityQueue<CalculateNode>(new KeyBigFirstCalculateNodeComparer());
                while (NeedAlive) {
                    Pair<bool, ManagedTerrain> temp;
                    while (CalculateOperateQueue.Dequeue(out temp)) {
                        if (temp.First == OPERATOR_ADD) {
                            TerrainUnitData d = temp.Second.InitialData;
                            Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[4] {
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y1,d.BaseMap[0]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y1,d.BaseMap[2]),
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y2,d.BaseMap[6]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y2,d.BaseMap[8]),
                                };
                            _Regist(temp.Second.CalculateNodeRoot.ManagedTerrainRoot, Points);
                            lock (ManagedTerrains)
                                ManagedTerrains.Add(temp.Second);
                        } else {
                            TerrainUnitData d = temp.Second.InitialData;
                            Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[4] {
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y1,d.BaseMap[0]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y1,d.BaseMap[2]),
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y2,d.BaseMap[6]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y2,d.BaseMap[8]),
                                };
                            _Unregist(temp.Second.CalculateNodeRoot.ManagedTerrainRoot, Points);
                            lock (ManagedTerrains)
                                ManagedTerrains.Remove(temp.Second);
                        }
                    }

                    if (tCalculateSceneCenter != null) {
                        CalculateSceneCenter = tCalculateSceneCenter;
                        tCalculateSceneCenter = null;
                    }

                    int ReviseCounter = 0;
                    q.Clear();
                    lock (ManagedTerrains)
                        foreach (var terrain in ManagedTerrains) {
                            terrain.CalculateNodeRoot.Key = CalculateSceneCenter.Evaluate(terrain.CalculateNodeRoot);
                            q.Push(terrain.CalculateNodeRoot);
                        }

                    //UnityEngine.Debug.Log("Scan Start");
                    while (q.Count != 0 && ReviseCounter < _Settings.CalculateLoopTaskMaxLimit) {
                        CalculateNode now = q.Pop();
                        //UnityEngine.Debug.Log (now.Key);
                        if (now.Key > _Settings.UnitPrecisionMinLimit) {//Precision is fine......
                            if (now.Map != null) {//Calculated......
                                for (int i = 0; i < 16; i++)
                                    for (int j = 0; j < 16; j++)
                                        if (now.Child[i, j] != null) {
                                            now.Child[i, j].Key = CalculateSceneCenter.Evaluate(now.Child[i, j]);
                                            q.Push(now.Child[i, j]);
                                        }
                            } else {//Not Calculated......
                                ///UnityEngine.Debug.Log ("Node Calculate : (" + now.InitialData.Region.x1 + "," + now.InitialData.Region.x2 + "," + now.InitialData.Region.y1 + "," + now.InitialData.Region.y2 + ")");
                                NodeCalculate(now);
                                ReviseCounter++;
                                q.Push(now);
                            }
                        } else {//Precision is not fine......
                            if (now.Map != null) {//Calculated......
                                //UnityEngine.Debug.Log ("Node Destory : (" + now.InitialData.Region.x1 + "," + now.InitialData.Region.x2 + "," + now.InitialData.Region.y1 + "," + now.InitialData.Region.y2 + ")");
                                NodeDestory(now);
                            }
                        }
                    }

                    if (q.Count != 0 && ReviseCounter >= _Settings.CalculateLoopTaskMaxLimit) {
                        int TempCounter = 0;
                        while (q.Count != 0 && TempCounter < _Settings.CalculateLoopAppendRecycleTaskMaxLimit) {
                            CalculateNode now = q.Pop();
                            if (now.Key > _Settings.UnitPrecisionMinLimit) {
                                if (now.Map != null)
                                    for (int i = 0; i < 16; i++)
                                        for (int j = 0; j < 16; j++)
                                            if (now.Child[i, j] != null) {
                                                now.Child[i, j].Key = CalculateSceneCenter.Evaluate(now.Child[i, j]);
                                                q.Push(now.Child[i, j]);
                                            }
                            } else {
                                if (now.Map != null)
                                    NodeDestory(now);
                            }
                        }
                    }

                    //if (ReviseCounter <= _Settings.CalculateLoopTaskMaxLimit)
                    System.Threading.Thread.Sleep(100);
                }

                UnityEngine.Debug.Log("TerrainManager CalculateThread Quit.");
                lock (ThreadControlLock) CalculateAlive = false;
            } catch (System.Exception e) {
                UnityEngine.Debug.Log(e);
            }
        }

        private void _Regist(ManagedTerrain ManagedTerrainRoot, Geometries.Point<long, long>[] Points) {
            ApplyOperateQueue.Enqueue(new Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>(OPERATOR_ADD, ManagedTerrainRoot, Points));
        }
        private void _Unregist(ManagedTerrain ManagedTerrainRoot, Geometries.Point<long, long>[] Points) {
            ApplyOperateQueue.Enqueue(new Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>(OPERATOR_REMOVE, ManagedTerrainRoot, Points));
        }

        private void Update(ApplyBlock Block) {
            if (Block.StorageTreeRoot != null) {
                double
                    zLength = (double)(Block.StorageTreeRoot.MaxHeight - Block.StorageTreeRoot.MinHeight) / 1000,
                    xLength = (double)(Block.Region.x2 - Block.Region.x1) / 1000,
                    yLength = (double)(Block.Region.y2 - Block.Region.y1) / 1000;

                Block.Range = (float)System.Math.Sqrt(xLength * xLength + yLength * yLength + zLength * zLength) / 2;
                Block.CenterAdjust = new LongVector3(
                    (Block.Region.x1 + Block.Region.x2) / 2,
                    (Block.StorageTreeRoot.MaxHeight + Block.StorageTreeRoot.MinHeight) / 2,
                    (Block.Region.y1 + Block.Region.y2) / 2
                );
            }
            Block.Key = ApplySceneCenter.Evaluate(Block);
        }

        private void InsertPoint(ApplyBlock Block, ref Geometries.Point<long, long> NewPoint) {
            if (Block.StorageTreeRoot == null) {
                long
                    xmid = (Block.Region.x1 + Block.Region.x2) / 2,
                    ymid = (Block.Region.y1 + Block.Region.y2) / 2;
                if (NewPoint.x <= xmid) {
                    if (NewPoint.y <= ymid)
                        InsertPoint(Block.Child[0], ref NewPoint);
                    if (NewPoint.y >= ymid)
                        InsertPoint(Block.Child[2], ref NewPoint);
                }
                if (NewPoint.x >= xmid) {
                    if (NewPoint.y <= ymid)
                        InsertPoint(Block.Child[1], ref NewPoint);
                    if (NewPoint.y >= ymid)
                        InsertPoint(Block.Child[3], ref NewPoint);
                }
            } else {
                //if (NewPoint.x == 953 && NewPoint.y == 61035)
                //    UnityEngine.Debug.Log("StorageTree Insert : (953,61035). Region : (" + Block.Region.x1+","+ Block.Region.x2 + ","+ Block.Region.y1 + ","+ Block.Region.y2 + ")");
                Block.Changed++;
                Block.StorageTreeRoot.Insert(NewPoint);
            }
        }
        private void DeletePoint(ApplyBlock Block, ref Geometries.Point<long, long> OldPoint) {
            if (Block.StorageTreeRoot == null) {
                long
                    xmid = (Block.Region.x1 + Block.Region.x2) / 2,
                    ymid = (Block.Region.y1 + Block.Region.y2) / 2;
                if (OldPoint.x <= xmid) {
                    if (OldPoint.y <= ymid)
                        DeletePoint(Block.Child[0], ref OldPoint);
                    if (OldPoint.y >= ymid)
                        DeletePoint(Block.Child[2], ref OldPoint);
                }
                if (OldPoint.x >= xmid) {
                    if (OldPoint.y <= ymid)
                        DeletePoint(Block.Child[1], ref OldPoint);
                    if (OldPoint.y >= ymid)
                        DeletePoint(Block.Child[3], ref OldPoint);
                }
            } else {
                //if (OldPoint.x == 953 && OldPoint.y == 61035)
                //    UnityEngine.Debug.Log("StorageTree Delete : (953,61035). Region : (" + Block.Region.x1 + "," + Block.Region.x2 + "," + Block.Region.y1 + "," + Block.Region.y2 + ")");
                Block.Changed++;
                Block.StorageTreeRoot.Delete(OldPoint);
            }
        }

        public void Split(ApplyBlock Block) {
            Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref Block.Region);
            ApplyBlock.StorageTree[] ChildStorageTree = ApplyBlock.StorageTree.Split(Block.StorageTreeRoot);
            Block.Child = new ApplyBlock[4];
            for (int i = 0; i < 4; i++)
                Block.Child[i] = new ApplyBlock(Block, ref ChildRegion[i], ChildStorageTree[i]);
            Block.StorageTreeRoot = null;
            for (int i = 0; i < 4; i++)
                ApplyTerrainEntity(Block.Child[i]);
            RemoveTerrainEntity(Block);
        }
        public void Merge(ApplyBlock Block) {
            Block.StorageTreeRoot = ApplyBlock.StorageTree.Merge(new ApplyBlock.StorageTree[4] {
                Block.Child[0].StorageTreeRoot, Block.Child[1].StorageTreeRoot,
                Block.Child[2].StorageTreeRoot, Block.Child[3].StorageTreeRoot,
            });
            ApplyTerrainEntity(Block);
            Block.Changed = 0;
            for (int i = 0; i < 4; i++)
                RemoveTerrainEntity(Block.Child[i]);
            Block.Child = null;
        }

        public void ApplyTerrainEntity(ApplyBlock Block) {
            if (Block.Region.x2 - Block.Region.x1 >= _Settings.BlockSizeMaxLimit
                || Block.Region.y2 - Block.Region.y1 >= _Settings.BlockSizeMaxLimit) {
                //UnityEngine.Debug.Log ("ApplyTerrainEntity Reject : >= TerrainBlockSizeLimit");
                return;
            }

            UnityEngine.Vector3 TerrainDataSize;
            int TerrainDataHeightMapDetail;
            LongVector3 TerrainLocalPosition;
            float[,] TerrainDataHeightMap;

            TerrainDataSize = new LongVector3(
                Block.Region.x2 - Block.Region.x1,
                Block.StorageTreeRoot.MaxHeight - Block.StorageTreeRoot.MinHeight + 1,
                Block.Region.y2 - Block.Region.y1
            ).toVector3();

            int Depth = Block.StorageTreeRoot.Depth;
            TerrainDataHeightMapDetail = 1;
            for (int i = 0; i < Depth; i++)
                TerrainDataHeightMapDetail *= 2;

            TerrainDataHeightMapDetail = System.Math.Min(65, System.Math.Max(32, TerrainDataHeightMapDetail) + 1);
            TerrainLocalPosition = Block.TerrainPosition = new LongVector3(Block.Region.x1, Block.StorageTreeRoot.MinHeight, Block.Region.y1);
            TerrainDataHeightMap = Block.StorageTreeRoot.GetInterPolatedHeightMap(TerrainDataHeightMapDetail);

            //new & set Terrain

            Kernel.Threading.QueueOnMainThread(delegate () {
                ApplyBlock _Block = Block;
                UnityEngine.TerrainData TerrainData = new UnityEngine.TerrainData();
                TerrainData.heightmapResolution = TerrainDataHeightMapDetail;
                TerrainData.baseMapResolution = TerrainDataHeightMapDetail;
                TerrainData.size = TerrainDataSize;
                TerrainData.SetHeightsDelayLOD(0, 0, TerrainDataHeightMap);

                UnityEngine.Object.DestroyImmediate(_Block.TerrainEntity);
                _Block.TerrainEntity = UnityEngine.Terrain.CreateTerrainGameObject(TerrainData);

                if (_Block.ManagedTerrainRoot.SeparateFromFatherObject == false) {
                    _Block.TerrainEntity.transform.parent = _Block.ManagedTerrainRoot.UnityRoot.transform;
                    _Block.TerrainEntity.transform.localPosition = TerrainLocalPosition.toVector3();
                } else {
                    _Block.TerrainEntity.transform.localPosition = Kernel.Position_SEToUnity(_Block.ManagedTerrainRoot.SEPosition + TerrainLocalPosition);
                }
            });
        }
        public void RemoveTerrainEntity(ApplyBlock Block) {
            Kernel.Threading.QueueOnMainThread(delegate () {
                //if (Block.TerrainEntity == null) UnityEngine.Debug.Log("Block.TerrainEntity is null!!!!!");
                ApplyBlock _Block = Block;
                UnityEngine.Object.DestroyImmediate(_Block.TerrainEntity);
            });
        }

        public void ApplyThread() {
            try {
                lock (ThreadControlLock) ApplyAlive = true;
                UnityEngine.Debug.Log("TerrainManager ApplyThread Start.");

                PriorityQueue<ApplyBlock>
                    q = new PriorityQueue<ApplyBlock>(new KeyBigFirstApplyBlockComparer());
                while (NeedAlive) {
                    Group<bool, ManagedTerrain, Geometries.Point<long, long>[]> temp;
                    while (ApplyOperateQueue.Dequeue(out temp)) {
                        if (temp.First == OPERATOR_ADD)
                            for (int j = 0; j < temp.Third.Length; j++)
                                InsertPoint(temp.Second.ApplyBlockRoot, ref temp.Third[j]);
                        else
                            for (int j = 0; j < temp.Third.Length; j++)
                                DeletePoint(temp.Second.ApplyBlockRoot, ref temp.Third[j]);
                    }

                    if (tApplySceneCenter != null) {
                        ApplySceneCenter = tApplySceneCenter;
                        tApplySceneCenter = null;
                    }
                    //Apply thread uses the same method as Calculate thread.
                    //Attention : the split and merge operation is controled by main loop,
                    //            not the insert or delete operation.

                    int ReviseCounter = 0;
                    q.Clear();
                    lock (ManagedTerrains)
                        foreach (var terrain in ManagedTerrains) {
                            Update(terrain.ApplyBlockRoot);
                            q.Push(terrain.ApplyBlockRoot);
                        }
                    //UnityEngine.Debug.Log("Scan Start");
                    while (q.Count != 0 && ReviseCounter < _Settings.ApplyLoopTaskMaxLimit) {
                        ApplyBlock now = q.Pop();
                        //UnityEngine.Debug.Log("Block Scan : (" + now.Region.x1 + "," + now.Region.x2 + "," + now.Region.y1 + "," + now.Region.y2 + ")");
                        if (now.Key != 0) {
                            if (now.StorageTreeRoot == null) {//null node
                                if (now.Child[0].Child == null && now.Child[1].Child == null
                                    && now.Child[2].Child == null && now.Child[3].Child == null
                                    && System.Math.Max(
                                        System.Math.Max(now.Child[0].StorageTreeRoot.Depth, now.Child[1].StorageTreeRoot.Depth),
                                        System.Math.Max(now.Child[2].StorageTreeRoot.Depth, now.Child[3].StorageTreeRoot.Depth)
                                    ) <= _Settings.BlockMergeDepthMinLimit) {
                                    Merge(now);
                                    ReviseCounter++;
                                } else {
                                    for (int i = 0; i < 4; i++) {
                                        Update(now.Child[i]);
                                        q.Push(now.Child[i]);
                                    }
                                }
                            } else {//Leaf node
                                if (now.StorageTreeRoot.Depth >= _Settings.BlockSplitDepthMaxLimit) {
                                    Split(now);
                                    ReviseCounter += 4;
                                    q.Push(now);
                                } else if (now.Key > 0) {//Refresh
                                    ApplyTerrainEntity(now);
                                    now.Changed = 0;
                                    ReviseCounter++;
                                }
                            }
                        }
                    }

                    if (q.Count != 0 && ReviseCounter >= _Settings.ApplyLoopTaskMaxLimit) {
                        int TempCounter = 0;
                        while (q.Count != 0 && TempCounter < _Settings.ApplyLoopAppendRecycleTaskMaxLimit) {
                            ApplyBlock now = q.Pop();
                            if (now.StorageTreeRoot == null) {//空中间节点
                                if (now.Child[0].Child == null && now.Child[1].Child == null
                                    && now.Child[2].Child == null && now.Child[3].Child == null
                                    && System.Math.Max(
                                        System.Math.Max(now.Child[0].StorageTreeRoot.Depth, now.Child[1].StorageTreeRoot.Depth),
                                        System.Math.Max(now.Child[2].StorageTreeRoot.Depth, now.Child[3].StorageTreeRoot.Depth)
                                    ) <= _Settings.BlockMergeDepthMinLimit) {
                                    Merge(now);
                                    TempCounter++;
                                } else {
                                    for (int i = 0; i < 4; i++) {
                                        Update(now.Child[i]);
                                        q.Push(now.Child[i]);
                                    }
                                }
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }

                UnityEngine.Debug.Log("TerrainManager ApplyThread Quit.");
                lock (ThreadControlLock) ApplyAlive = false;
            } catch (System.Exception e) {
                UnityEngine.Debug.Log(e);
            }
        }
    }
}