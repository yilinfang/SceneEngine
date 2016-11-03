using System;
using System.Collections.Generic;

namespace SE {
    public static partial class Kernel {
        private static partial class ObjectManager {
            private static class ObjectUpdateManager {

                private static SBTree<Object>

                    ObjectHeap = new SBTree<Object>(new Comparers.KernelIDSmallFirstObjectComparer());

                private static List<Pair<bool, Object>>

                    OperateList = new List<Pair<bool, Object>>();

                private static bool

                    NeedAlive = false,

                    Alive = false;

                public static void Regist(Object NewObject) {

                    NewObject.LastUpdateTime = DateTime.Now.ToBinary();

                    lock (OperateList)
                        OperateList.Add(new Pair<bool,Object>(OPERATOR_ADD,NewObject));
                }

                public static void Unregist(Object OldObject) {
                    lock (OperateList)
                        OperateList.Add(new Pair<bool, Object>(OPERATOR_REMOVE,OldObject));
                }

                public static void ThreadStart() {

                    if (NeedAlive)
                        throw new InvalidOperationException();

                    NeedAlive = true;
                    Thread.Async(ObjectUpdateManagerThread);
                }

                public static void ThreadStop() {

                    if (!NeedAlive)
                        throw new InvalidOperationException();

                    NeedAlive = false;
                }

                private static bool ObjectUpdateTimeIsReached(Object obj, long CurrentTime) {
                    return obj.LastUpdateTime + obj.UpdateInterval > CurrentTime;
                }

                private static void ObjectUpdateManagerThread() {

                    if (Alive) return;

                    Alive = true;

                    List<System.Threading.WaitHandle> WaitList = new List<System.Threading.WaitHandle>();

                    while (NeedAlive) {

                        //操作Object
                        if (OperateList.Count != 0) {

                            Pair<bool, Object>[] TempArray;

                            lock (OperateList) {
                                TempArray = OperateList.ToArray();
                                OperateList.Clear();
                            }

                            for (int i = 0; i < TempArray.Length; i++)
                                if (TempArray[i].First == OPERATOR_ADD)
                                    ObjectHeap.Add(TempArray[i].Second);
                                else
                                    ObjectHeap.Remove(TempArray[i].Second);
                        }

                        //多线程执行各Object的Update函数

                        //cause:只要满足执行条件,Update函数就必须立刻执行.

                        while (!ObjectHeap.Empty
                            && ObjectUpdateTimeIsReached(ObjectHeap.First(), DateTime.Now.ToBinary())) {

                            Object obj = ObjectHeap.First();

                            System.Threading.AutoResetEvent flag = new System.Threading.AutoResetEvent(false);

                            WaitList.Add(flag);

                            Thread.AsyncInPool(
                                delegate () {

                                    obj.LastUpdateTime = DateTime.Now.ToBinary();
                                    obj.Update();
                                    flag.Set();
                                }
                            );
                        }

                        foreach (var f in WaitList)
                            f.WaitOne();

                        WaitList.Clear();

                        System.Threading.Thread.Sleep(100);
                    };

                    Alive = false;
                }
            }
        }
    }
}
