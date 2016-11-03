using System.Collections.Generic;

namespace SE {
    public static partial class Kernel {
        private static partial class ObjectManager {

            private static SceneCenter

                ObjectManagerSceneCenter = new SceneCenter(new LongVector3(0, 0, 0));

            private static SBTree<Object>

                RecycleHeap = new SBTree<Object>(new Comparers.MaintainEvaluationSmallFirstObjectComparer()),

                CalculateHeap = new SBTree<Object>(new Comparers.MaintainEvaluationBigFirstObjectComparer());

            private static List<Pair<bool, Object>>

                OperateList = new List<Pair<bool, Object>>();

            private const bool

                OPERATOR_ADD = true,
                OPERATOR_REMOVE = false;

            private static long

                LastSenceCenterUpdateTime = 0;

            private static int

                NeedAlive = 0;

            public static bool

                Alive = false;

            private static bool

                CompulsoryStop = false;

            private static object

                LockForObjectManagerThreadControl = new object();

            public static void Regist(Object Father, Object Child, LongVector3 LocalPosition, UnityEngine.Quaternion LocalQuaternion) {

                Child.KernelID = System.Threading.Interlocked.Increment(ref EffectIndex);

                Child.Father = Father;
                Child.SEPosition = LocalPosition;

                Child.Start();

                if (Child.NeedUpdate)
                    ObjectUpdateManager.Regist(Child);

                if (Child.Lod != null) {

                    Thread.QueueOnMainThread(
                        delegate () {

                            Child.UnityRoot = new UnityEngine.GameObject("Kernel_" + Child.KernelID);

                            if (Father == null)
                                Child.UnityRoot.transform.SetParent(SEUnityRoot.transform);
                            else
                                Child.UnityRoot.transform.SetParent(Father.UnityRoot.transform);

                            Child.UnityRoot.transform.localPosition = LocalPosition.toVector3();

                            Child.UnityRoot.transform.localRotation = LocalQuaternion;

                            Child.UnityRoot.AddComponent<ObjectNodeListener>().ObjectRoot = Father;

                            Child.UnityGlobalPosition = Child.UnityRoot.transform.position;

                            Child.MaintainEvaluation = Evaluate(Child);
                        }
                    );

                    lock (OperateList)
                        OperateList.Add(new Pair<bool, Object>(OPERATOR_ADD,Child));
                }
            }

            public static void Unregist(Object OldObject) {

                OldObject.Destory();

                if (OldObject.NeedUpdate)
                    ObjectUpdateManager.Unregist(OldObject);

                if (OldObject.Lod != null)
                    lock (OperateList)
                        OperateList.Add(new Pair<bool, Object>(OPERATOR_REMOVE, OldObject));
            }

            public static void ThreadNeedAlive() {

                lock (LockForObjectManagerThreadControl) {
                    if (++NeedAlive > 0 && !Alive) {

                        Thread.Async(ObjectManagerThread);
                        ObjectUpdateManager.ThreadStart();
                    }
                }
            }

            public static void ThreadNeedAliveCancel() {

                lock (LockForObjectManagerThreadControl) {
                    if (--NeedAlive <= 0) {

                        ObjectUpdateManager.ThreadStop();
                    }
                }
            }

            public static void ThreadCompulsoryStop() {

                lock (LockForObjectManagerThreadControl) {

                    CompulsoryStop = true;

                    if (NeedAlive > 0)
                        ObjectUpdateManager.ThreadStop();
                }
            }

            public static void ThreadCompulsoryStopCancel() {

                lock (LockForObjectManagerThreadControl) {

                    CompulsoryStop = false;

                    if (NeedAlive > 0) {

                        Thread.Async(ObjectManagerThread);
                        ObjectUpdateManager.ThreadStart();
                    }
                }
            }

            //无法降低精度只有一种情况:
            //当前LodCase为Lod[0].

            //无法提高精度有两种情况:
            //1.已经达到最高LodCase,即Lod[Lod.Length-1].
            //2.下一级LodCase的预估值小于Precision(精度过高).

            private static double Evaluate(Object Object) {
                return Evaluate(Object, Object.CurrentLodCaseIndex);
            }
            private static double Evaluate(Object Object, int LodCaseIndex) {

                double d = ObjectManagerSceneCenter.GetDistence(Object), m = d - Object.Range;

                if (m >= SceneVisibleRange)
                    return 0;
                else if (m <= SceneFullLoadRange + 0.1 || Object.CompulsoryCalculation)
                    return 9999999999.0F;
                else
                    return ((LodCaseIndex == 0) ? Object.Range : Object.Lod[LodCaseIndex].PrecisionRange) / (d - SceneFullLoadRange);
            }

            private static void CancelCurrentLodCase(Object obj) {

                obj.Lod[obj.CurrentLodCaseIndex].ObjectRoot = null;

                obj.Lod[obj.CurrentLodCaseIndex].Destory();

                Thread.QueueOnMainThread(
                    delegate () {
                        obj.Lod[obj.CurrentLodCaseIndex].DestoryForUnity();
                    }
                );

                obj.CurrentLodCaseIndex = 0;
            }

            private static void ApplyNewLodCase(Object obj, int NewLodCaseIndex) {

                if (obj.CurrentLodCaseIndex != 0)
                    CancelCurrentLodCase(obj);

                if (NewLodCaseIndex == 0)
                    return;

                obj.Lod[NewLodCaseIndex].ObjectRoot = obj;

                obj.CurrentLodCaseIndex = NewLodCaseIndex;

                obj.Lod[NewLodCaseIndex].Start();

                Thread.QueueOnMainThread(
                    delegate () {
                        obj.Lod[NewLodCaseIndex].StartForUnity();
                    }
                );
            }

            private static bool NeedRecycle() {
                return
                    CurrentSceneMemory >= MemoryLimit
                    || CurrentFPS <= FPSLimit;
            }


            private static void ObjectManagerThread() {

                if (Alive) return;

                Alive = true;

                while (NeedAlive > 0 && !CompulsoryStop) {

                    //新加入的Object
                    if (OperateList.Count != 0) {

                        Pair<bool, Object>[] TempPairArray;

                        lock (OperateList) {
                            TempPairArray = OperateList.ToArray();
                            OperateList.Clear();
                        }

                        for (int i = 0; i < TempPairArray.Length; i++)
                            if (TempPairArray[i].First == OPERATOR_ADD && TempPairArray[i].Second.Lod.Length > 1) {

                                CalculateHeap.Add(TempPairArray[i].Second);
                            } else {

                                Object obj = TempPairArray[i].Second;

                                //CalculateHeap
                                if (obj.CurrentLodCaseIndex < obj.Lod.Length - 1)
                                    CalculateHeap.Remove(obj);

                                //RecycleHeap
                                if (obj.CurrentLodCaseIndex > 0)
                                    RecycleHeap.Remove(obj);

                                Thread.QueueOnMainThread(
                                    delegate () {
                                        UnityEngine.Object.Destroy(obj.UnityRoot);
                                    }
                                );
                            }
                    }

                    Object[] TempArray;

                    if (ObjectManagerSceneCenter.NeedUpdate()) {

                        ObjectManagerSceneCenter.Update();
                        
                        //若场景中心变动,更新估价值(整体)
                        LastSenceCenterUpdateTime = System.DateTime.Now.ToBinary();

                        //CalculateHeap
                        TempArray = CalculateHeap.ToArray();
                        CalculateHeap.Clear();

                        for (int i = 0; i < TempArray.Length; i++) {

                            Object obj = TempArray[i];

                            obj.MaintainEvaluation = Evaluate(obj);
                            CalculateHeap.Add(obj);
                        }

                        //RecycleHeap
                        TempArray = RecycleHeap.ToArray();
                        RecycleHeap.Clear();

                        for (int i = 0; i < TempArray.Length; i++) {

                            Object obj = TempArray[i];

                            obj.MaintainEvaluation = Evaluate(obj);
                            RecycleHeap.Add(obj);
                        }
                    } else {

                        //较快频率遍历检查Object并更新估价值(个体)

                        //cause:Object的坐标可能会被自身或其他Object改变(活动物体).

                        if (System.DateTime.Now.ToBinary() - LastSenceCenterUpdateTime < 300) {

                            ObjectManagerSceneCenter.Update();

                            TempArray = CalculateHeap.ToArray();

                            for (int i = 0; i < TempArray.Length; i++) {

                                Object obj = TempArray[i];

                                if (obj.MaintainEvaluation != Evaluate(obj)) {

                                    CalculateHeap.Remove(obj);

                                    if (obj.CurrentLodCaseIndex != 0)
                                        RecycleHeap.Remove(obj);

                                    obj.MaintainEvaluation = Evaluate(obj);

                                    CalculateHeap.Add(obj);
                                    if (obj.CurrentLodCaseIndex != 0)
                                        RecycleHeap.Add(obj);
                                }
    ;
                            }

                            TempArray = RecycleHeap.ToArray();

                            for (int i = 0; i < TempArray.Length; i++) {

                                Object obj = TempArray[i];

                                if (obj.MaintainEvaluation != Evaluate(obj)) {

                                    RecycleHeap.Remove(obj);

                                    obj.MaintainEvaluation = Evaluate(obj);

                                    RecycleHeap.Add(obj);
                                }
                            }
                        }
                    }


                    int ReviseCounter = 0;//防堵塞

                    //(常态执行)过高的精确度(Evaluate(Object)<PrecisionLimit)

                    while (!RecycleHeap.Empty
                        && RecycleHeap.First().MaintainEvaluation < ObjectPrecisionLimit
                        && (ReviseCounter++) < ObjectThreadCalculateLimit) {

                        Object obj = RecycleHeap.First();

                        RecycleHeap.Remove(obj);

                        CancelCurrentLodCase(obj);

                        for (int i = obj.CurrentLodCaseIndex - 1; i > 0; i--)
                            if (Evaluate(obj, i) >= ObjectPrecisionLimit) {

                                ApplyNewLodCase(obj, i);
                                break;
                            }

                        obj.MaintainEvaluation = Evaluate(obj);

                        if (obj.CurrentLodCaseIndex != 0)
                            RecycleHeap.Add(obj);
                    }

                    //(仅资源不足时)修正不平衡的精度(由于场景中心变动造成)

                    //cause:在场景中心变动时精度较高的近处物体远离中心精度变得更高,而精度较低的远处物体靠近中心精度变得更低.

                    while (NeedRecycle()
                           && !RecycleHeap.Empty
                           && RecycleHeap.First().MaintainEvaluation < CalculateHeap.First().MaintainEvaluation
                           && (ReviseCounter++) < ObjectThreadCalculateLimit) {

                        Object obj = RecycleHeap.First();

                        RecycleHeap.Remove(obj);

                        CancelCurrentLodCase(obj);

                        double j = CalculateHeap.First().MaintainEvaluation;

                        for (int i = obj.CurrentLodCaseIndex - 1; i > 0; i--)
                            if (Evaluate(obj, i) > j) {

                                ApplyNewLodCase(obj, i);
                                break;
                            }

                        obj.MaintainEvaluation = Evaluate(obj);

                        if (obj.CurrentLodCaseIndex != 0) {
                            RecycleHeap.Add(obj);
                        }
                    }

                    //(仅资源仍然不足)依次降低LodCase

                    while (NeedRecycle()
                        && !RecycleHeap.Empty
                        && (ReviseCounter++) < ObjectThreadCalculateLimit) {

                        Object obj = RecycleHeap.First();

                        RecycleHeap.Remove(obj);

                        ApplyNewLodCase(obj, obj.CurrentLodCaseIndex - 1);

                        obj.MaintainEvaluation = Evaluate(obj);

                        if (obj.CurrentLodCaseIndex != 0) {
                            RecycleHeap.Add(obj);
                        }
                    }

                    //(仅资源充足)依次提高LodCase.

                    if (ReviseCounter == 0) {
                        while (!NeedRecycle()
                            && !CalculateHeap.Empty
                            && CalculateHeap.First().MaintainEvaluation < ObjectPrecisionLimit
                            && (ReviseCounter++) < ObjectThreadCalculateLimit) {

                            Object obj = CalculateHeap.First();

                            CalculateHeap.Remove(obj);

                            if (obj.CurrentLodCaseIndex == 0) {
                                RecycleHeap.Add(obj);
                            }

                            ApplyNewLodCase(obj, obj.CurrentLodCaseIndex + 1);

                            obj.MaintainEvaluation = Evaluate(obj);

                            if (obj.CurrentLodCaseIndex != obj.Lod.Length - 1) {
                                CalculateHeap.Add(obj);
                            }
                        }
                    }

                    if (ReviseCounter <= ObjectThreadCalculateLimit)
                        System.Threading.Thread.Sleep(10000);
                }

                Alive = false;
            }
        }
    }
}
