using System.Collections.Generic;

namespace SE.Modules {
    public class ObjectUpdateManager : IModule {

        public class Settings {
            //The max task amount limit per loop for object calculating (anti-block)
            public int LoopTaskMaxLimit = 200;
        }

        private const bool
            OPERATOR_ADD = true,
            OPERATOR_REMOVE = false;

        public Settings _Settings;
        private SBTree<Object> ObjectHeap;
        private List<Pair<bool, Object>> OperateList;

        private object ThreadControlLock;
        private bool NeedAlive, Alive;
        Kernel Kernel;

        public ObjectUpdateManager(Settings Settings) {
            _Settings = Settings;
            ObjectHeap = new SBTree<Object>(new Comparers.KernelIDSmallFirstObjectComparer());
            OperateList = new List<Pair<bool, Object>>();
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
                    Kernel.ThreadManager.Async(UpdateThread);
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
            lock (OperateList)
                OperateList.Add(new Pair<bool, Object>(OPERATOR_ADD, NewObject));
        }

        public void Unregist(Object OldObject) {
            lock (OperateList)
                OperateList.Add(new Pair<bool, Object>(OPERATOR_REMOVE, OldObject));
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

                    //Call Object.Update() asynchronously
                    while (!ObjectHeap.Empty
                        && ObjectUpdateTimeIsReached(ObjectHeap.First(), System.DateTime.Now.ToBinary())) {

                        Object obj = ObjectHeap.First();
                        System.Threading.AutoResetEvent flag = new System.Threading.AutoResetEvent(false);
                        WaitList.Add(flag);
                        Kernel.ThreadManager.AsyncInPool(delegate () {
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
