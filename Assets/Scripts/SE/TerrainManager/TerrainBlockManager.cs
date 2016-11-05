using System.Collections.Generic;

namespace SE {
    static partial class TerrainManager {
        private static class TerrainBlockManager {

            private class KeyBigFirstApplyBlockComparer : IComparer<ManagedTerrain.ApplyBlock> {
                public int Compare(ManagedTerrain.ApplyBlock a, ManagedTerrain.ApplyBlock b) {

                    return (a.Key == b.Key) ? 0 : (
                        (a.Key < b.Key) ? 1 : -1
                    );
                }
            }

            private static bool

                NeedAlive = false,

                Alive = false;

            private static List<Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>>

                OperateList = new List<Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>>();

            private const bool

                OPERATOR_ADD = true,
                OPERATOR_REMOVE = false;

            private static SceneCenter

                TerrainBlockManagerSceneCenter = new SceneCenter(new LongVector3(0, 0, 0));

            public static void ThreadStart() {
                if (NeedAlive)
                    throw new System.InvalidOperationException();

                NeedAlive = true;
                Thread.Async(TerrainBlockManagerThread);
            }

            public static void ThreadStop() {

                if (!NeedAlive)
                    throw new System.InvalidOperationException();

                NeedAlive = false;
            }

            public static void Regist(ManagedTerrain ManagedTerrainRoot, Geometries.Point<long, long>[] Points) {

                if (Points.Length != 4 && Points.Length != 5)
                    throw new System.Exception("向TerrainBlockManager添加的Point数量不为4/5.");

                //UnityEngine.Debug.Log("Regist.");

                lock (OperateList)
                    OperateList.Add(new Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>(OPERATOR_ADD, ManagedTerrainRoot, Points));
            }

            public static void Unregist(ManagedTerrain ManagedTerrainRoot, Geometries.Point<long, long>[] Points) {

                if (Points.Length != 4 && Points.Length != 5)
                    throw new System.Exception("向TerrainBlockManager删除的Point数量不为4/5.");

                lock (OperateList)
                    OperateList.Add(new Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>(OPERATOR_REMOVE, ManagedTerrainRoot, Points));
            }

            public static void _ChangeCoordinateOrigin(LongVector3 CoordinateOriginPosition) {

                foreach (var terrain in ManagedTerrains)

                    if (terrain.SeparateFromFatherObject == true) {//生成的地形独自为一个根时

                        Queue<ManagedTerrain.ApplyBlock> q = new Queue<ManagedTerrain.ApplyBlock>();

                        q.Enqueue(terrain.ApplyBlockRoot);

                        while (q.Count != 0) {

                            ManagedTerrain.ApplyBlock now = q.Dequeue();

                            if (now.TerrainEntity != null)
                                now.TerrainEntity.transform.localPosition = (
                                    terrain.SEPosition
                                    + now.TerrainPosition
                                    - CoordinateOriginPosition
                                ).toVector3();

                            if (now.Child != null)
                                for (int i = 0; i < 4; i++)
                                    q.Enqueue(now.Child[i]);
                        }
                    }
            }


            public static void ApplyBlockUpdate(ManagedTerrain.ApplyBlock Block) {

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

                double
                    d = Block.ManagedTerrainRoot.SeparateFromFatherObject == true ?
                        TerrainBlockManagerSceneCenter.GetDistence(Block.CenterAdjust)
                        : TerrainBlockManagerSceneCenter.GetDistence(Block.CenterAdjust.toVector3() + Block.ManagedTerrainRoot.UnityGlobalPosition),
                    m = d - Block.Range;

                if (m <= Kernel.SceneFullLoadRange + 0.1 || Block.StorageTreeRoot == null) {
                    Block.Key = 999999999;
                } else if (Block.Changed == 0) {
                    Block.Key = 0;
                } else {
                    Block.Key = (Block.Range / (d - Kernel.SceneFullLoadRange)) * (1 + Block.Changed / 10);
                }
            }

            private static void InsertPoint(ManagedTerrain.ApplyBlock Block, ref Geometries.Point<long, long> NewPoint) {

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

            private static void DeletePoint(ManagedTerrain.ApplyBlock Block, ref Geometries.Point<long, long> OldPoint) {

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

            public static void TerrainBlockManagerThread() {
                try {
                    if (Alive)
                        return;

                    Alive = true;

                    UnityEngine.Debug.Log("TerrainBlockManagerThread Start.");

                    PriorityQueue<ManagedTerrain.ApplyBlock>
                        q = new PriorityQueue<ManagedTerrain.ApplyBlock>(new KeyBigFirstApplyBlockComparer());

                    while (NeedAlive) {

                        //UnityEngine.Debug.Log("TerrainBlockManagerThread is looping commonly.");

                        Group<bool, ManagedTerrain, Geometries.Point<long, long>[]>[] TempArray;

                        //操作Point
                        if (OperateList.Count != 0) {
                            //UnityEngine.Debug.Log("Add");
                            lock (OperateList) {
                                TempArray = OperateList.ToArray();
                                OperateList.Clear();
                            }
                            for (int i = 0; i < TempArray.Length; i++) {
                                Group<bool, ManagedTerrain, Geometries.Point<long, long>[]> now = TempArray[i];
                                if (now.First == OPERATOR_ADD)
                                    for (int j = 0; j < now.Third.Length; j++)
                                        InsertPoint(now.Second.ApplyBlockRoot, ref now.Third[j]);
                                else
                                    for (int j = 0; j < now.Third.Length; j++)
                                        DeletePoint(now.Second.ApplyBlockRoot, ref now.Third[j]);
                            }

                        }

                        TerrainBlockManagerSceneCenter.Update();

                        //扫描ApplyBlocks并进行带权值的刷新

                        //cause: 单纯添加删除点只会在ApplyBlock分裂和合并时进行TerrainEntity的操作,在不满足限制的情况下要主动进行更新

                        int ReviseCounter = 0;

                        q.Clear();

                        lock (ManagedTerrains)
                            foreach (var terrain in ManagedTerrains) {

                                ApplyBlockUpdate(terrain.ApplyBlockRoot);

                                q.Push(terrain.ApplyBlockRoot);
                            }
                        //UnityEngine.Debug.Log("Scan Start");
                        while (q.Count != 0 && ReviseCounter < TerrainBlockManagerThreadCalculateLimit) {

                            ManagedTerrain.ApplyBlock now = q.Pop();

                            //UnityEngine.Debug.Log("Block Scan : (" + now.Region.x1 + "," + now.Region.x2 + "," + now.Region.y1 + "," + now.Region.y2 + ")");

                            if (now.Key != 0) {
                                if (now.StorageTreeRoot == null) {//空中间节点

                                    if (now.Child[0].Child == null && now.Child[1].Child == null
                                        && now.Child[2].Child == null && now.Child[3].Child == null
                                        && System.Math.Max(
                                            System.Math.Max(now.Child[0].StorageTreeRoot.Depth, now.Child[1].StorageTreeRoot.Depth),
                                            System.Math.Max(now.Child[2].StorageTreeRoot.Depth, now.Child[3].StorageTreeRoot.Depth)
                                        ) <= TerrainBlockMergeDepthLimit) {
                                        now.Merge();
                                        ReviseCounter++;

                                    } else {
                                        for (int i = 0; i < 4; i++) {
                                            ApplyBlockUpdate(now.Child[i]);
                                            q.Push(now.Child[i]);
                                        }
                                    }
                                } else {

                                    if (now.StorageTreeRoot.Depth >= TerrainBlockSplitDepthLimit) {
                                        now.Split();
                                        ReviseCounter += 4;
                                        for (int i = 0; i < 4; i++) {
                                            ApplyBlockUpdate(now.Child[i]);
                                            q.Push(now.Child[i]);
                                        }

                                    } else if (now.Key > TerrainPrecisionLimit) {//发生改变
                                        now.ApplyTerrainEntity();
                                        now.Changed = 0;
                                        ReviseCounter++;
                                    }
                                }
                            }
                        }

                        if (q.Count != 0 && ReviseCounter >= TerrainBlockManagerThreadCalculateLimit) {

                            int TempCounter = 0;
                            while (q.Count != 0 && TempCounter < TerrainBlockManagerThreadAppendRecycleLimit) {

                                ManagedTerrain.ApplyBlock now = q.Pop();

                                if (now.StorageTreeRoot == null) {//空中间节点
                                    if (now.Child[0].Child == null && now.Child[1].Child == null
                                        && now.Child[2].Child == null && now.Child[3].Child == null
                                        && System.Math.Max(
                                            System.Math.Max(now.Child[0].StorageTreeRoot.Depth, now.Child[1].StorageTreeRoot.Depth),
                                            System.Math.Max(now.Child[2].StorageTreeRoot.Depth, now.Child[3].StorageTreeRoot.Depth)
                                        ) <= TerrainBlockMergeDepthLimit) {
                                        now.Merge();
                                        TempCounter++;
                                    } else
                                        for (int i = 0; i < 4; i++) {
                                            ApplyBlockUpdate(now.Child[i]);
                                            q.Push(now.Child[i]);
                                        }
                                }
                            }
                        }

                        System.Threading.Thread.Sleep(50);
                    }

                    UnityEngine.Debug.Log("TerrainBlockManagerThread Quit.");

                    Alive = false;
                } catch (System.Exception e) {
                    UnityEngine.Debug.Log(e);
                }
            }
        }
    }
}