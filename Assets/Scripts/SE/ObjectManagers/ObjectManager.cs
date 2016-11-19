using System.Collections.Generic;
using System.Collections.Generic.LockFree;

namespace SE.Modules {
    public class ObjectManager : IObjectManager {
        public class Settings {
            //The max task amount limit per loop for object calculating (anti-block)
            public int LoopTaskMaxLimit = 200;
            //The min precision limit for object calculating
            public float PrecisionMinLimit = 0.01F;
            //The max interval between two refresh operate (For unexpected SEPosition change)
            public long RefreshIntervalMaxLimit = 300;
        }

        private const bool
            OPERATOR_ADD = true,
            OPERATOR_REMOVE = false;

        private Settings _Settings;
        private ObjectUpdateManager ObjectUpdateManager;
        private SceneCenter SceneCenter, tSceneCenter;
        private SBTree<Object> RecycleHeap, CalculateHeap;
        private LockFreeQueue<Pair<bool, Object>> OperateQueue;
        private int ObjectIndex;

        private long LastSenceCenterUpdateTime;
        private bool NeedAlive, Alive;
        private object ThreadControlLock;
        public Kernel Kernel;

        public ObjectManager(Settings Settings) {
            _Settings = Settings;
            SceneCenter = null;
            tSceneCenter = null;
            RecycleHeap = new SBTree<Object>(new Comparers.MaintainEvaluationSmallFirstObjectComparer());
            CalculateHeap = new SBTree<Object>(new Comparers.MaintainEvaluationBigFirstObjectComparer());
            OperateQueue = new LockFreeQueue<Pair<bool, Object>>();
            LastSenceCenterUpdateTime = 0;
            NeedAlive = false;
            Alive = false;
            ThreadControlLock = new object();
            ObjectIndex = 0;
        }
        public void AssignObjectUpdateManager(ObjectUpdateManager tObjectUpdateManager) {
            ObjectUpdateManager = tObjectUpdateManager;
        }
        public void _Assigned(Kernel tKernel) {
            Kernel = tKernel;
            SceneCenter = new SceneCenter(Kernel, new LongVector3(0, 0, 0));
            ObjectUpdateManager._Assigned(Kernel);
        }
        public void _Start() {
            lock (ThreadControlLock)
                if (NeedAlive) {
                    throw new System.Exception("Object Manager : Thread is already started.");
                } else {
                    NeedAlive = true;
                    Kernel.Threading.Async(CalculateThread);
                    ObjectUpdateManager._Start();
                }
        }
        public void _ChangeSceneCenter(ref LongVector3 Position) {
            ObjectUpdateManager._ChangeSceneCenter(ref Position);
            if (tSceneCenter == null)
                tSceneCenter = new SceneCenter(Kernel, ref Position);
            else
                tSceneCenter.Change(ref Position);
        }
        public void _ChangeCoordinateOrigin(ref LongVector3 Position) {
            ObjectUpdateManager._ChangeCoordinateOrigin(ref Position);
        }
        public void _Stop() {
            lock (ThreadControlLock) {
                if (NeedAlive) {
                    NeedAlive = false;
                    ObjectUpdateManager._Stop();
                } else {
                    throw new System.Exception("Object Manager : Thread is already stopped.");
                }
            }
        }

        public void _Regist(Object Father, Object Child, LongVector3 LocalPosition, UnityEngine.Quaternion LocalQuaternion) {
            Child.KernelID = System.Threading.Interlocked.Increment(ref ObjectIndex);
            Child.Father = Father;
            Child.SEPosition = LocalPosition;
            Child.Start();

            if (Child.NeedUpdate)
                ObjectUpdateManager.Regist(Child);
            if (Child.Lod != null) {
                Kernel.Threading.QueueOnMainThread(delegate () {
                    Child.UnityRoot = new UnityEngine.GameObject("Kernel_" + Child.KernelID);
                    if (Father == null)
                        Child.UnityRoot.transform.SetParent(Kernel.SEUnityRoot.transform);
                    else
                        Child.UnityRoot.transform.SetParent(Father.UnityRoot.transform);
                    Child.UnityRoot.transform.localPosition = LocalPosition.toVector3();
                    Child.UnityRoot.transform.localRotation = LocalQuaternion;
                    Child.UnityRoot.AddComponent<Listeners.ObjectListener>().ObjectRoot = Father;
                    Child.UnityGlobalPosition = Child.UnityRoot.transform.position;
                    Child.MaintainEvaluation = SceneCenter.Evaluate(Child);
                });
                OperateQueue.Enqueue(new Pair<bool, Object>(OPERATOR_ADD, Child));
            }
        }

        public void _Unregist(Object OldObject) {
            OldObject.Destory();
            if (OldObject.NeedUpdate)
                ObjectUpdateManager.Unregist(OldObject);
            if (OldObject.Lod != null)
                OperateQueue.Enqueue(new Pair<bool, Object>(OPERATOR_REMOVE, OldObject));
        }

        //无法降低精度只有一种情况:
        //当前LodCase为Lod[0].

        //无法提高精度有两种情况:
        //1.已经达到最高LodCase,即Lod[Lod.Length-1].
        //2.下一级LodCase的预估值小于Precision(精度过高).

        private void CancelCurrentLodCase(Object obj) {
            obj.Lod[obj.CurrentLodCaseIndex].ObjectRoot = null;
            obj.Lod[obj.CurrentLodCaseIndex].Destroy();
            Kernel.Threading.QueueOnMainThread(delegate () {
                obj.Lod[obj.CurrentLodCaseIndex].DestroyForUnity();
            });
            obj.CurrentLodCaseIndex = 0;
        }

        private void ApplyNewLodCase(Object obj, int NewLodCaseIndex) {
            if (obj.CurrentLodCaseIndex != 0) CancelCurrentLodCase(obj);
            if (NewLodCaseIndex == 0) return;
            obj.Lod[NewLodCaseIndex].ObjectRoot = obj;
            obj.CurrentLodCaseIndex = NewLodCaseIndex;
            obj.Lod[NewLodCaseIndex].Start();
            Kernel.Threading.QueueOnMainThread(delegate () {
                obj.Lod[NewLodCaseIndex].StartForUnity();
            });
        }

        private bool NeedRecycle() {
            return Kernel.Listener.SceneMemory >= Kernel._Settings.MemoryMaxLimit
                || Kernel.Listener.FPS <= Kernel._Settings.FPSMinLimit;
        }

        private void CalculateThread() {
            try {
                lock (ThreadControlLock) Alive = true;

                while (NeedAlive) {
                    Pair<bool, Object> temp;
                    while (OperateQueue.Dequeue(out temp)) {
                        if (temp.First == OPERATOR_ADD && temp.Second.Lod.Length > 1) {
                            CalculateHeap.Add(temp.Second);
                        } else {
                            Object obj = temp.Second;
                            //CalculateHeap
                            if (obj.CurrentLodCaseIndex < obj.Lod.Length - 1)
                                CalculateHeap.Remove(obj);
                            //RecycleHeap
                            if (obj.CurrentLodCaseIndex > 0)
                                RecycleHeap.Remove(obj);
                            Kernel.Threading.QueueOnMainThread(delegate () {
                                UnityEngine.Object.Destroy(obj.UnityRoot);
                            });
                        }
                    }

                    Object[] TempArray;
                    //If the scene center is changed, the evaluation of all objects will be refreshed.
                    if (tSceneCenter != null) {
                        SceneCenter = tSceneCenter;
                        tSceneCenter = null;

                        LastSenceCenterUpdateTime = System.DateTime.Now.ToBinary();

                        TempArray = CalculateHeap.ToArray();
                        CalculateHeap.Clear();
                        for (int i = 0; i < TempArray.Length; i++) {
                            Object obj = TempArray[i];
                            obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                            CalculateHeap.Add(obj);
                        }

                        //RecycleHeap
                        TempArray = RecycleHeap.ToArray();
                        RecycleHeap.Clear();
                        for (int i = 0; i < TempArray.Length; i++) {
                            Object obj = TempArray[i];
                            obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                            RecycleHeap.Add(obj);
                        }
                    } else if (System.DateTime.Now.ToBinary() - LastSenceCenterUpdateTime < _Settings.RefreshIntervalMaxLimit) {

                        TempArray = CalculateHeap.ToArray();
                        for (int i = 0; i < TempArray.Length; i++) {
                            Object obj = TempArray[i];
                            if (obj.MaintainEvaluation != SceneCenter.Evaluate(obj)) {
                                CalculateHeap.Remove(obj);
                                if (obj.CurrentLodCaseIndex != 0)
                                    RecycleHeap.Remove(obj);
                                obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                                CalculateHeap.Add(obj);
                                if (obj.CurrentLodCaseIndex != 0)
                                    RecycleHeap.Add(obj);
                            }
                        }

                        TempArray = RecycleHeap.ToArray();
                        for (int i = 0; i < TempArray.Length; i++) {
                            Object obj = TempArray[i];
                            if (obj.MaintainEvaluation != SceneCenter.Evaluate(obj)) {
                                RecycleHeap.Remove(obj);
                                obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                                RecycleHeap.Add(obj);
                            }
                        }
                    }

                    int ReviseCounter = 0;//anti-block

                    //(Normal Operate)Too high precision (Evaluate(Object) < PrecisionLimit)
                    while (!RecycleHeap.Empty
                        && RecycleHeap.First().MaintainEvaluation < _Settings.PrecisionMinLimit
                        && (ReviseCounter++) < _Settings.LoopTaskMaxLimit) {

                        Object obj = RecycleHeap.First();
                        RecycleHeap.Remove(obj);
                        CancelCurrentLodCase(obj);

                        for (int i = obj.CurrentLodCaseIndex - 1; i > 0; i--)
                            if (SceneCenter.Evaluate(obj, i) >= _Settings.PrecisionMinLimit) {
                                ApplyNewLodCase(obj, i);
                                break;
                            }

                        obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                        if (obj.CurrentLodCaseIndex != 0)
                            RecycleHeap.Add(obj);
                    }

                    //(仅资源不足时)修正不平衡的精度(由于场景中心变动造成)

                    //cause:在场景中心变动时精度较高的近处物体远离中心精度变得更高,而精度较低的远处物体靠近中心精度变得更低.

                    while (NeedRecycle()
                           && !RecycleHeap.Empty
                           && RecycleHeap.First().MaintainEvaluation < CalculateHeap.First().MaintainEvaluation
                           && (ReviseCounter++) < _Settings.LoopTaskMaxLimit) {

                        Object obj = RecycleHeap.First();
                        RecycleHeap.Remove(obj);
                        CancelCurrentLodCase(obj);

                        double j = CalculateHeap.First().MaintainEvaluation;
                        for (int i = obj.CurrentLodCaseIndex - 1; i > 0; i--)
                            if (SceneCenter.Evaluate(obj, i) > j) {
                                ApplyNewLodCase(obj, i);
                                break;
                            }

                        obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                        if (obj.CurrentLodCaseIndex != 0)
                            RecycleHeap.Add(obj);
                    }

                    //(仅资源仍然不足)依次降低LodCase

                    while (NeedRecycle()
                        && !RecycleHeap.Empty
                        && (ReviseCounter++) < _Settings.LoopTaskMaxLimit) {

                        Object obj = RecycleHeap.First();
                        RecycleHeap.Remove(obj);
                        ApplyNewLodCase(obj, obj.CurrentLodCaseIndex - 1);
                        obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                        if (obj.CurrentLodCaseIndex != 0)
                            RecycleHeap.Add(obj);
                    }

                    //(仅资源充足)依次提高LodCase.

                    if (ReviseCounter == 0) {
                        while (!NeedRecycle()
                            && !CalculateHeap.Empty
                            && CalculateHeap.First().MaintainEvaluation < _Settings.PrecisionMinLimit
                            && (ReviseCounter++) < _Settings.LoopTaskMaxLimit) {

                            Object obj = CalculateHeap.First();
                            CalculateHeap.Remove(obj);
                            if (obj.CurrentLodCaseIndex == 0)
                                RecycleHeap.Add(obj);

                            ApplyNewLodCase(obj, obj.CurrentLodCaseIndex + 1);
                            obj.MaintainEvaluation = SceneCenter.Evaluate(obj);
                            if (obj.CurrentLodCaseIndex != obj.Lod.Length - 1)
                                CalculateHeap.Add(obj);
                        }
                    }

                    if (ReviseCounter <= _Settings.LoopTaskMaxLimit)
                        System.Threading.Thread.Sleep(200);
                }

                lock (ThreadControlLock) Alive = false;
            } catch (System.Exception e) {
                UnityEngine.Debug.Log(e);
            }
        }
    }
}