using System.Collections.Generic;
using System.Collections.Generic.LockFree;

namespace SE.ObjectManagers {
    public partial class ObjectManager : IObjectManager {
        public class ObjectUpdateManager {

            public class Settings {
                //The max task amount limit per loop for object calculating (anti-block)
                public int LoopTaskMaxLimit = 200;
            }

            private const bool
                OPERATOR_ADD = true,
                OPERATOR_REMOVE = false;

            public Settings _Settings;
            private SBTree<Object> ObjectHeap;
            private LockFreeQueue<Pair<bool, Object>> OperateQueue;

            private object ThreadControlLock;
            private bool NeedAlive, Alive;
            Kernel Kernel;

            public ObjectUpdateManager(Settings Settings) {
                _Settings = Settings;
                ObjectHeap = new SBTree<Object>(new Comparers.KernelIDSmallFirstObjectComparer());
                OperateQueue = new LockFreeQueue<Pair<bool, Object>>();
                ThreadControlLock = new object();
                NeedAlive = false;
                Alive = false;
            }
            public void _Assigned(Kernel tKernel) { Kernel = tKernel; }
            public void _Start() {
                lock (ThreadControlLock)
                    if (NeedAlive) {
                        throw new System.Exception("Object Update Manager : Thread is already started.");
                    } else {
                        NeedAlive = true;
                        Kernel.Threading.Async(UpdateThread);
                    }
            }
            public void _ChangeSceneCenter(ref LongVector3 Position) { }
            public void _ChangeCoordinateOrigin(ref LongVector3 Position) { }
            public void _Stop() {
                lock (ThreadControlLock) {
                    if (NeedAlive) {
                        NeedAlive = false;
                    } else {
                        throw new System.Exception("Object Update Manager : Thread is already stopped.");
                    }
                }
            }

            public void Regist(Object NewObject) {
                NewObject.LastUpdateTime = System.DateTime.Now.ToBinary();
                OperateQueue.Enqueue(new Pair<bool, Object>(OPERATOR_ADD, NewObject));
            }

            public void Unregist(Object OldObject) {
                OperateQueue.Enqueue(new Pair<bool, Object>(OPERATOR_REMOVE, OldObject));
            }

            private bool ObjectUpdateTimeIsReached(Object obj, long CurrentTime) {
                return obj.LastUpdateTime + obj.UpdateInterval > CurrentTime;
            }

            private void UpdateThread() {
                try {
                    lock (ThreadControlLock) Alive = true;

                    List<System.Threading.WaitHandle>
                        WaitList = new List<System.Threading.WaitHandle>();

                    while (NeedAlive) {
                        Pair<bool, Object> temp;
                        while (OperateQueue.Dequeue(out temp)) {
                            if (temp.First == OPERATOR_ADD)
                                ObjectHeap.Add(temp.Second);
                            else
                                ObjectHeap.Remove(temp.Second);
                        }

                        //Call Object.Update() asynchronously
                        while (!ObjectHeap.Empty
                            && ObjectUpdateTimeIsReached(ObjectHeap.First(), System.DateTime.Now.ToBinary())) {

                            Object obj = ObjectHeap.First();
                            System.Threading.AutoResetEvent flag = new System.Threading.AutoResetEvent(false);
                            WaitList.Add(flag);
                            Kernel.Threading.AsyncInPool(delegate () {
                                obj.LastUpdateTime = System.DateTime.Now.ToBinary();
                                obj.Update();
                                flag.Set();
                            });
                        }
                        foreach (var f in WaitList) f.WaitOne();
                        WaitList.Clear();

                        System.Threading.Thread.Sleep(10);
                    };

                    lock (ThreadControlLock) Alive = false;
                } catch (System.Exception e) {
                    UnityEngine.Debug.Log(e);
                }
            }
        }
    }
}