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

            private static List<Pair<ManagedTerrain, Geometries.Point<long, long>[]>>

                AddList = new List<Pair<ManagedTerrain, Geometries.Point<long, long>[]>>(),

                RemoveList = new List<Pair<ManagedTerrain, Geometries.Point<long, long>[]>>();

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

            public static void Regist(ManagedTerrain ManagedTerrainRoot, ref Geometries.Point<long, long>[] Points) {

                if (Points.Length != 4 && Points.Length != 5)
                    throw new System.Exception("向TerrainBlockManager添加的Point数量不为4/5.");

                //UnityEngine.Debug.Log("Regist.");

                lock (AddList)
                    AddList.Add(new Pair<ManagedTerrain, Geometries.Point<long, long>[]>(ManagedTerrainRoot, ref Points));
            }

            public static void Unregist(ManagedTerrain ManagedTerrainRoot, ref Geometries.Point<long, long>[] Points) {

                if (Points.Length != 4 && Points.Length != 5)
                    throw new System.Exception("向TerrainBlockManager删除的Point数量不为4/5.");

                lock (RemoveList)
                    RemoveList.Add(new Pair<ManagedTerrain, Geometries.Point<long, long>[]>(ManagedTerrainRoot, ref Points));
            }

            public static void _ChangeCoordinateOrigin(LongVector3 CoordinateOriginPosition) {

                foreach (var terrain in ManagedTerrains)

                    if (terrain.SeparateFromFatherObject == true) {//生成的地形独自为一个根时

                        Queue<ManagedTerrain.ApplyBlock> q = new Queue<ManagedTerrain.ApplyBlock>();

                        q.Enqueue(terrain.ApplyBlockRoot);

                        while (q.Count != 0) {

                            ManagedTerrain.ApplyBlock now = q.Dequeue();

                            if (now.TerrainEntity == true)
                                now.TerrainEntity.transform.localPosition = (
                                    terrain.SEPosition
                                    + new LongVector3(now.Region.x1, now.StorageTreeRoot.MinHeight, now.Region.y1)
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

                if (m >= Kernel.SceneVisibleRange) {
                    Block.Key = 0;
                } else if (m <= Kernel.SceneFullLoadRange + 0.1) {
                    Block.Key = 999999999;
                } else {
                    Block.Key = (Block.Range / (d - Kernel.SceneFullLoadRange)) * Block.Changed;
                }
            }

            private static void InsertPoint(ManagedTerrain.ApplyBlock Block, ref Geometries.Point<long, long> NewPoint) {

                Block.Changed++;

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

                    Block.StorageTreeRoot.Insert(NewPoint);
                    if (Block.StorageTreeRoot.Depth == TerrainBlockSplitDepthLimit)
                        Block.Split();
                }
            }

            private static void DeletePoint(ManagedTerrain.ApplyBlock Block, ref Geometries.Point<long, long> OldPoint) {

                Block.Changed++;

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

                    if (Block.Child[0].Child != null || Block.Child[1].Child != null
                        || Block.Child[2].Child != null || Block.Child[3].Child != null)
                        return;

                    if (System.Math.Max(
                            System.Math.Max(Block.Child[0].StorageTreeRoot.Depth, Block.Child[1].StorageTreeRoot.Depth),
                            System.Math.Max(Block.Child[2].StorageTreeRoot.Depth, Block.Child[3].StorageTreeRoot.Depth)
                        ) <= TerrainBlockMergeDepthLimit) {

                        Block.Merge();//注意删除时先遍历再合并
                        ApplyBlockUpdate(Block);
                    }
                } else {

                    //if (OldPoint.x == 953 && OldPoint.y == 61035)
                    //    UnityEngine.Debug.Log("StorageTree Delete : (953,61035). Region : (" + Block.Region.x1 + "," + Block.Region.x2 + "," + Block.Region.y1 + "," + Block.Region.y2 + ")");

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

                        Pair<ManagedTerrain, Geometries.Point<long, long>[]>[] TempArray;

                        //加入Point
                        if (AddList.Count != 0) {
                            //UnityEngine.Debug.Log("Add");
                            lock (AddList) {
                                TempArray = AddList.ToArray();
                                AddList.Clear();
                            }
                            for (int i = 0; i < TempArray.Length; i++)
                                for (int j = 0; j < TempArray[i].Second.Length; j++)
                                    InsertPoint(TempArray[i].First.ApplyBlockRoot, ref TempArray[i].Second[j]);
                        }

                        //删除Point
                        if (RemoveList.Count != 0) {
                            //UnityEngine.Debug.Log("Remove");
                            lock (RemoveList) {
                                TempArray = RemoveList.ToArray();
                                RemoveList.Clear();
                            }
                            for (int i = 0; i < TempArray.Length; i++)
                                for (int j = 0; j < TempArray[i].Second.Length; j++)
                                    DeletePoint(TempArray[i].First.ApplyBlockRoot, ref TempArray[i].Second[j]);
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
                        while (q.Count != 0 && ReviseCounter < TerrainThreadCalculateLimit) {

                            ManagedTerrain.ApplyBlock now = q.Pop();

                            //UnityEngine.Debug.Log("Block Scan : (" + now.Region.x1 + "," + now.Region.x2 + "," + now.Region.y1 + "," + now.Region.y2 + ")");

                            if (now.Changed != 0 && now.Key != 0) {

                                if (now.StorageTreeRoot == null) {//空中间节点或未发生改变

                                    for (int i = 0; i < 4; i++) {

                                        ApplyBlockUpdate(now.Child[i]);
                                        q.Push(now.Child[i]);
                                    }
                                } else if (now.Key > TerrainPrecisionLimit) {//发生改变
                                                                             //UnityEngine.Debug.Log("Block Scan : (" + now.Region.x1 + "," + now.Region.x2 + "," + now.Region.y1 + "," + now.Region.y2 + ") Update TerrainEntity");
                                    now.ApplyTerrainEntity();
                                    now.Changed = 0;
                                    ReviseCounter++;
                                }
                            }
                        }

                        if (ReviseCounter != TerrainThreadCalculateLimit)
                            System.Threading.Thread.Sleep(200);
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