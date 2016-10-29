﻿using System.Collections.Generic;

namespace SE {
    static partial class TerrainManager {

        private class KernelIDSmallFirstManagedTerrainComparer : IComparer<ManagedTerrain> {
            public int Compare(ManagedTerrain a, ManagedTerrain b) {

                return (a.KernelID == b.KernelID) ? 0 : (
                    (a.KernelID > b.KernelID) ? 1 : -1
                );
            }
        }

        private class KeyBigFirstCalculateNodeComparer : IComparer<ManagedTerrain.CalculateNode> {
            public int Compare(ManagedTerrain.CalculateNode a, ManagedTerrain.CalculateNode b) {

                return (a.Key == b.Key) ? 0 : (
                    (a.Key > b.Key) ? 1 : -1
                );
            }
        }

        private class PositionSmallFirstTerrainBlockPointComparer : IComparer<Geometries.Point<long, long>> {
            public int Compare(Geometries.Point<long, long> a, Geometries.Point<long, long> b) {

                return (a.x == b.x && a.y == b.y) ? 0 : (
                    (a.x > b.x || (a.x == b.x && a.y > b.y)) ? 1 : -1
                );
            }
        }

        /*
         * 地形数据的维护与应用分为两个线程
         * 
         * 维护在场景中心发生变动时执行
         * 
         * 应用固定间隔执行
         */

        public static long

            TerrainBlockSplitDepthLimit = 6,//地形块分割限制

            TerrainBlockMergeDepthLimit = 4,//地形块合并限制

            TerrainCalculateUnitSizeLimit = 1000,//地形计算最小规格mm

            TerrainBlockSizeLimit = 1000 * 1000;//地形生成最大规格mm

        public static int

            TerrainThreadCalculateLimit = 100;

        public static float

            TerrainPrecisionLimit = 0.1F;//地形加载精度限制

        private static int

            NeedAlive = 0;

        private static bool

            CompulsoryStop = false;

        public static bool

            Alive = false;

        private static object

            LockForTerrainManagerThreadControl = new object();

        private static List<ManagedTerrain>

            AddList = new List<ManagedTerrain>(),

            RemoveList = new List<ManagedTerrain>();

        private static SBTree<ManagedTerrain>

            ManagedTerrains = new SBTree<ManagedTerrain>(new KernelIDSmallFirstManagedTerrainComparer());

        private static SceneCenter

            TerrainManagerSceneCenter = new SceneCenter(new LongVector3(0, 0, 0));

        public static void ThreadNeedAlive() {
            lock (LockForTerrainManagerThreadControl) {

                if (++NeedAlive > 0 && !Alive) {

                    Thread.Async(TerrainManagerThread);
                    TerrainBlockManager.ThreadStart();
                }
            }
        }

        public static void ThreadNeedAliveCancel() {
            lock (LockForTerrainManagerThreadControl) {

                if (--NeedAlive <= 0) {

                    TerrainBlockManager.ThreadStop();
                }
            }
        }

        public static void ThreadCompulsoryStop() {
            lock (LockForTerrainManagerThreadControl) {

                CompulsoryStop = true;

                if (NeedAlive > 0)
                    TerrainBlockManager.ThreadStop();
            }
        }

        public static void ThreadCompulsoryStopCancel() {

            lock (LockForTerrainManagerThreadControl) {

                CompulsoryStop = false;

                if (NeedAlive > 0) {

                    Thread.Async(TerrainManagerThread);
                    TerrainBlockManager.ThreadStart();
                }
            }
        }

        private static void Regist(ManagedTerrain NewManagedTerrain) {

            CalculateNodeUpdate(NewManagedTerrain.CalculateNodeRoot);

            lock (AddList)
                AddList.Add(NewManagedTerrain);
        }

        private static void Unregist(ManagedTerrain NewManagedTerrain) {

            lock (RemoveList)
                RemoveList.Add(NewManagedTerrain);
        }

        private static void CalculateNodeUpdate(ManagedTerrain.CalculateNode Node) {

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

            double
            d = Node.ManagedTerrainRoot.SeparateFromFatherObject == true ?
                TerrainManagerSceneCenter.GetDistence(Node.CenterAdjust)
                : TerrainManagerSceneCenter.GetDistence(Node.CenterAdjust.toVector3() + Node.ManagedTerrainRoot.UnityGlobalPosition),
            m = d - Node.Range;

            if (m >= Kernel.SceneVisibleRange)
                Node.Key = 0;
            else if (m <= Kernel.SceneFullLoadRange + 0.1)
                Node.Key = 999999999;
            else
                Node.Key = Node.Range / (d - Kernel.SceneFullLoadRange);
        }


        private static void NodeCalculate(ManagedTerrain.CalculateNode Node) {

            //UnityEngine.Debug.Log("Node Calculate: (" + Node.InitialData.Region.x1 + "," + Node.InitialData.Region.x2
            //    + "," + Node.InitialData.Region.y1 + "," + Node.InitialData.Region.y2 + ") ");
            //System.Threading.Thread.Sleep(250);
            Node.Map = new Geometries.Point<long, long>[17, 17];
            Node.Child = new ManagedTerrain.CalculateNode[16, 16];

            TerrainUnitData TempData = new TerrainUnitData(ref Node.InitialData);

            UnitCalculate(Node, 0, 0, 16, ref TempData);

            for (int i = 0; i < 17; i++)
                for (int j = 0; j < 17; j++) {

                    if (Node.Map[i, j].h < Node.MinHeight)
                        Node.MinHeight = Node.Map[i, j].h;
                    if (Node.Map[i, j].h > Node.MaxHeight)
                        Node.MaxHeight = Node.Map[i, j].h;
                }

            CalculateNodeUpdate(Node);
        }

        private static void UnitDataCalculate(ref TerrainUnitData UnitData) {

            for (int i = 0; i < UnitData.Impacts.Length; i++)
                UnitData.Impacts[i].Main(UnitData);
        }

        private static void UnitDataApplyToNode(ref TerrainUnitData UnitData, ManagedTerrain.CalculateNode Node, int x, int y, int len) {

            int
                mid = len / 2;
            long
                xmid = (UnitData.Region.x2 + UnitData.Region.x1) / 2,
                ymid = (UnitData.Region.y2 + UnitData.Region.y1) / 2;

            //注意四个顶点是不需要赋值的
            Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[5] {
                Node.Map[x + mid, y] = new Geometries.Point<long, long>(xmid, UnitData.Region.y1, UnitData.Map[1]),
                Node.Map[x, y + mid] = new Geometries.Point<long, long>(UnitData.Region.x1, ymid, UnitData.Map[3]),
                Node.Map[x + mid, y + mid] = new Geometries.Point<long, long>(xmid, ymid, UnitData.Map[4]),
                Node.Map[x + len, y + mid] = new Geometries.Point<long, long>(UnitData.Region.x2, ymid, UnitData.Map[5]),
                Node.Map[x + mid, y + len] = new Geometries.Point<long, long>(xmid, UnitData.Region.y2, UnitData.Map[7]),
            };

            TerrainBlockManager.Regist(Node.ManagedTerrainRoot, ref Points);
        }

        private static TerrainUnitData.Impact[] ImpactsFilter(ref TerrainUnitData.Impact[] Impacts, ref Geometries.Rectangle<long> Region) {

            List<TerrainUnitData.Impact> list = new List<TerrainUnitData.Impact>();

            for (int i = 0; i < Impacts.Length; i++)
                if (Impacts[i].Region.Overlapped(ref Region)) {
                    if (Impacts[i].Static)
                        list.Add(Impacts[i]);
                    else
                        list.Add(Impacts[i].Clone(i, ref Region));
                }

            return list.ToArray();
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
        private static TerrainUnitData[] UnitDataSplit(ref TerrainUnitData UnitData) {

            Geometries.Rectangle<long>[]
                ChildRegion = Geometries.Split(ref UnitData.Region);
            long[][]
                ChildVertex = new long[4][] {
                    new long[4] {
                        UnitData.Map[0],
                        UnitData.Map[1],
                        UnitData.Map[3],
                        UnitData.Map[4],
                    },
                    new long[4] {
                        UnitData.Map[1],
                        UnitData.Map[2],
                        UnitData.Map[4],
                        UnitData.Map[5],
                    },
                    new long[4] {
                        UnitData.Map[3],
                        UnitData.Map[4],
                        UnitData.Map[6],
                        UnitData.Map[7],
                    },
                    new long[4] {
                        UnitData.Map[4],
                        UnitData.Map[5],
                        UnitData.Map[7],
                        UnitData.Map[8],
                    },
                };
            RandomSeed[][]
                ChildRandomSeed = new RandomSeed[4][] {
                    new RandomSeed[5] {
                        UnitData.Seed[0].GetRandomSeed(111111),
                        UnitData.Seed[1].GetRandomSeed(1111),
                        UnitData.Seed[2].GetRandomSeed(1111),
                        UnitData.Seed[0].GetRandomSeed(1111),
                        UnitData.Seed[0].GetRandomSeed(2222),
                    },
                    new RandomSeed[5] {
                        UnitData.Seed[0].GetRandomSeed(222222),
                        UnitData.Seed[1].GetRandomSeed(2222),
                        UnitData.Seed[0].GetRandomSeed(1111),
                        UnitData.Seed[3].GetRandomSeed(1111),
                        UnitData.Seed[0].GetRandomSeed(3333),
                    },
                    new RandomSeed[5] {
                        UnitData.Seed[0].GetRandomSeed(333333),
                        UnitData.Seed[0].GetRandomSeed(2222),
                        UnitData.Seed[2].GetRandomSeed(2222),
                        UnitData.Seed[0].GetRandomSeed(4444),
                        UnitData.Seed[4].GetRandomSeed(1111),
                    },
                    new RandomSeed[5] {
                        UnitData.Seed[0].GetRandomSeed(444444),
                        UnitData.Seed[0].GetRandomSeed(3333),
                        UnitData.Seed[0].GetRandomSeed(4444),
                        UnitData.Seed[3].GetRandomSeed(2222),
                        UnitData.Seed[4].GetRandomSeed(2222),
                    },
                };
            TerrainUnitData.Impact[][]
                ChildImpacts = new TerrainUnitData.Impact[4][] {
                    ImpactsFilter(ref UnitData.Impacts,ref ChildRegion[0]),
                    ImpactsFilter(ref UnitData.Impacts,ref ChildRegion[1]),
                    ImpactsFilter(ref UnitData.Impacts,ref ChildRegion[2]),
                    ImpactsFilter(ref UnitData.Impacts,ref ChildRegion[3]),
                };

            return new TerrainUnitData[4] {
                new TerrainUnitData(ref ChildRegion[0], ref ChildVertex[0], ref ChildRandomSeed[0], ref ChildImpacts[0]),
                new TerrainUnitData(ref ChildRegion[1], ref ChildVertex[1], ref ChildRandomSeed[1], ref ChildImpacts[1]),
                new TerrainUnitData(ref ChildRegion[2], ref ChildVertex[2], ref ChildRandomSeed[2], ref ChildImpacts[2]),
                new TerrainUnitData(ref ChildRegion[3], ref ChildVertex[3], ref ChildRandomSeed[3], ref ChildImpacts[3]),
            };
        }

        private static void UnitCalculate(ManagedTerrain.CalculateNode Node, int x, int y, int len, ref TerrainUnitData UnitData) {

            if (UnitData.Region.x2 - UnitData.Region.x1 <= TerrainCalculateUnitSizeLimit * 2
                && UnitData.Region.y2 - UnitData.Region.y1 <= TerrainCalculateUnitSizeLimit * 2)
                return;

            int half = len / 2;

            UnitDataCalculate(ref UnitData);

            UnitDataApplyToNode(ref UnitData, Node, x, y, len);

            TerrainUnitData[] ChildUnitData = UnitDataSplit(ref UnitData);

            if (len == 2) {

                //ChildNode生成
                Node.Child[x, y] = new ManagedTerrain.CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[0]);
                Node.Child[x + 1, y] = new ManagedTerrain.CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[1]);
                Node.Child[x, y + 1] = new ManagedTerrain.CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[2]);
                Node.Child[x + 1, y + 1] = new ManagedTerrain.CalculateNode(Node.ManagedTerrainRoot, ref ChildUnitData[3]);
                CalculateNodeUpdate(Node.Child[x, y]);
                CalculateNodeUpdate(Node.Child[x + 1, y]);
                CalculateNodeUpdate(Node.Child[x, y + 1]);
                CalculateNodeUpdate(Node.Child[x + 1, y + 1]);

            } else {

                //继续Unit计算
                UnitCalculate(Node, x, y, half, ref ChildUnitData[0]);
                UnitCalculate(Node, x + half, y, half, ref ChildUnitData[1]);
                UnitCalculate(Node, x, y + half, half, ref ChildUnitData[2]);
                UnitCalculate(Node, x + half, y + half, half, ref ChildUnitData[3]);
            }
        }

        private static void NodeDestory(ManagedTerrain.CalculateNode Node) {

            //UnityEngine.Debug.Log("NodeDestory : (" + Node.InitialData.Region.x1 + "," + Node.InitialData.Region.x2 + "," + Node.InitialData.Region.y1 + "," + Node.InitialData.Region.y2 + ")");
            //System.Threading.Thread.Sleep(250);

            UnitDestory(Node, 0, 0, 16, ref Node.InitialData.Region);

            Node.Map = null;
            Node.Child = null;

            //UnityEngine.Debug.Log ("NodeDestory : ("+Node.InitialData.Region.x1+","+Node.InitialData.Region.x2+","+Node.InitialData.Region.y1+","+Node.InitialData.Region.y2+") Finished.");
        }

        private static void UnitDestory(ManagedTerrain.CalculateNode Node, int x, int y, int len, ref Geometries.Rectangle<long> Region) {

            if (Node.Map == null) return;

            if (Region.x2 - Region.x1 <= TerrainCalculateUnitSizeLimit * 2
                && Region.y2 - Region.y1 <= TerrainCalculateUnitSizeLimit * 2)
                return;

            //UnityEngine.Debug.Log("loop : (" + Region.x1 + "," + Region.x2 + "," + Region.y1 + "," + Region.y2 + ")");

            int half = len / 2;

            Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[5] {
                Node.Map[x + half, y],
                Node.Map[x, y + half],
                Node.Map[x + half, y + half],
                Node.Map[x + len, y + half],
                Node.Map[x + half, y + len],
            };

            TerrainBlockManager.Unregist(Node.ManagedTerrainRoot, ref Points);

            if (len == 2) {
                NodeDestory(Node.Child[x, y]);
                NodeDestory(Node.Child[x + 1, y]);
                NodeDestory(Node.Child[x, y + 1]);
                NodeDestory(Node.Child[x + 1, y + 1]);

            } else {

                Geometries.Rectangle<long>[] ChildRegion = Geometries.Split(ref Region);

                UnitDestory(Node, x, y, half, ref ChildRegion[0]);
                UnitDestory(Node, x + half, y, half, ref ChildRegion[1]);
                UnitDestory(Node, x, y + half, half, ref ChildRegion[2]);
                UnitDestory(Node, x + half, y + half, half, ref ChildRegion[3]);
            }

            //UnityEngine.Debug.Log ("NodeDestory : ("+Node.InitialData.Region.x1+","+Node.InitialData.Region.x2+","+Node.InitialData.Region.y1+","+Node.InitialData.Region.y2+") Finished.");
        }

        public static void _ChangeCoordinateOrigin(LongVector3 CoordinateOriginPosition) {
            TerrainBlockManager._ChangeCoordinateOrigin(CoordinateOriginPosition);
        }

        /*
         * Node的状态分为三种:
         * 1:未计算                           CalculateNode.Map == null
         * 2:已计算但孩子全部销毁             CalculateNode.Map != null && every CalculateNode.Child[x,y].Map == null
         * 3:已计算                           CalculateNode.Map != null && exist CalculateNode.Child[x,y].Map != null
         */

        //地形的生成采用带权重的扫描(便于适应变化及修改).

        private static void TerrainManagerThread() {
            try {
                if (Alive == true)
                    return;

                Alive = true;

                UnityEngine.Debug.Log("TerrainManagerThread Start.");

                PriorityQueue<ManagedTerrain.CalculateNode> q = new PriorityQueue<ManagedTerrain.CalculateNode>(new KeyBigFirstCalculateNodeComparer());

                while (NeedAlive > 0 && !CompulsoryStop) {

                    ManagedTerrain[] TempArray;

                    //加入ManagedTerrain
                    if (AddList.Count != 0) {
                        lock (AddList) {
                            TempArray = AddList.ToArray();
                            AddList.Clear();
                        }
                        lock (ManagedTerrains)
                            for (int i = 0; i < TempArray.Length; i++) {

                                TerrainUnitData d = TempArray[i].InitialData;

                                Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[4] {
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y1,d.Map[0]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y1,d.Map[2]),
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y2,d.Map[6]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y2,d.Map[8]),
                                };

                                TerrainBlockManager.Regist(TempArray[i].CalculateNodeRoot.ManagedTerrainRoot, ref Points);

                                ManagedTerrains.Add(TempArray[i]);
                            }
                    }

                    //删除ManagedTerrain
                    if (RemoveList.Count != 0) {
                        lock (RemoveList) {
                            TempArray = RemoveList.ToArray();
                            RemoveList.Clear();
                        }
                        lock (ManagedTerrains)
                            for (int i = 0; i < TempArray.Length; i++) {

                                TerrainUnitData d = TempArray[i].InitialData;

                                Geometries.Point<long, long>[] Points = new Geometries.Point<long, long>[4] {
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y1,d.Map[0]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y1,d.Map[2]),
                                    new Geometries.Point<long,long>(d.Region.x1,d.Region.y2,d.Map[6]),
                                    new Geometries.Point<long,long>(d.Region.x2,d.Region.y2,d.Map[8]),
                                };

                                TerrainBlockManager.Unregist(TempArray[i].CalculateNodeRoot.ManagedTerrainRoot, ref Points);

                                ManagedTerrains.Remove(TempArray[i]);
                            }
                    }

                    TerrainManagerSceneCenter.Update();

                    //以估值顺序更新Item(控制计算防止堵塞)
                    int ReviseCounter = 0;

                    q.Clear();
                    lock (ManagedTerrains)
                        foreach (var terrain in ManagedTerrains) {

                            CalculateNodeUpdate(terrain.CalculateNodeRoot);

                            q.Push(terrain.CalculateNodeRoot);
                        }
                    //UnityEngine.Debug.Log("Scan Start");
                    while (q.Count != 0 && ReviseCounter < TerrainThreadCalculateLimit) {

                        ManagedTerrain.CalculateNode now = q.Pop();

                        //UnityEngine.Debug.Log (now.Key);

                        if (now.Key > TerrainPrecisionLimit) {//该节点符合精度限制

                            if (now.Map != null) {//已计算
                                for (int i = 0; i < 16; i++)
                                    for (int j = 0; j < 16; j++)
                                        if (now.Child[i, j] != null) {
                                            CalculateNodeUpdate(now.Child[i, j]);
                                            q.Push(now.Child[i, j]);
                                        }

                            } else {//未计算
                                    //UnityEngine.Debug.Log ("Node Calculate : (" + now.InitialData.Region.x1 + "," + now.InitialData.Region.x2 + "," + now.InitialData.Region.y1 + "," + now.InitialData.Region.y2 + ")");

                                NodeCalculate(now);
                                ReviseCounter++;

                                q.Push(now);
                            }
                        } else {//该节点不符合精度限制

                            if (now.Map != null) {//已计算

                                //UnityEngine.Debug.Log ("Node Destory : (" + now.InitialData.Region.x1 + "," + now.InitialData.Region.x2 + "," + now.InitialData.Region.y1 + "," + now.InitialData.Region.y2 + ")");

                                NodeDestory(now);

                            }
                        }
                    }

                    if (ReviseCounter != TerrainThreadCalculateLimit)
                        System.Threading.Thread.Sleep(200);
                }

                UnityEngine.Debug.Log("TerrainManagerThread Quit.");

                Alive = false;
            } catch (System.Exception e) {
                UnityEngine.Debug.Log(e);
            }
        }
    }
}
 