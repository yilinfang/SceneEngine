using System.Collections.Generic;

namespace SE {
    public static partial class Kernel {

        //Kernel运行产生的相关数据:

        private static LongVector3

            TemporarySenceCenter = new LongVector3(0, 0, 0),

            CurrentCoordinateOrigin = new LongVector3(0, 0, 0);

        public static LongVector3

            CurrentSceneCenter { get; private set; }

        private static float

            CurrentFPS = 100;//当前的FPS

        public static long

            CurrentSceneMemory = 0,//场景占用的内存:包括各种组件及资源

            LastSenceCenterUpdateTime = System.DateTime.Now.ToBinary();

        public static long

            MaxSenceCenterUpdateInterval = 5000 * 10000,//场景中心最大更新间隔ms

            MemoryLimit = 200 * 1024 * 1024;

        public static int

            FPSLimit = 60,

            ObjectThreadCalculateLimit = 200;

        public static float

            ObjectPrecisionLimit = 0.01F,//物体加载精度限制

            SceneFullLoadRange = 10,//全加载距离(场景中心切换间距)m

            SceneVisibleRange = 5 * 1000,//最大生成距离m

            CoordinateOriginSwitchRange = 5 * 1000;//坐标原点切换距离m   (用于UnityPosition <=> SEPosition)

        //------------------------------------------------------------------------------------------------------------------------

        private static SBTree<Object>

            RootObjects = new SBTree<Object>(new Comparers.KernelIDSmallFirstObjectComparer());

        private static int 

            EffectIndex = 0;

        private static UnityEngine.GameObject

            SEUnityRoot = null;

        private static bool

            Initialised = false;

        private static object

            LockForPositionTranslate = new object();

        public static void Init() {

            if (!Initialised) {

                SEUnityRoot = new UnityEngine.GameObject(
                    "SE",
                    new System.Type[1] {
                        typeof(SEUnityNodeListener)
                    }
                );

                Thread.Init();

                Initialised = true;
            }
            
            //初始化子模块
            ObjectManager.ThreadNeedAlive();

            TerrainManager.ThreadNeedAlive();
        }

        public static void Stop() {

            ObjectManager.ThreadNeedAliveCancel();

            TerrainManager.ThreadNeedAliveCancel();
        }

        public static void RegistRootObject(string CharacteristicString, Object NewRootObject, LongVector3 Position, UnityEngine.Quaternion Quaternion) {

            NewRootObject.CharacteristicString = CharacteristicString;

            ObjectManager.Regist(null, NewRootObject, Position, Quaternion);

            lock (RootObjects)
                RootObjects.Add(NewRootObject);
        }

        public static void UnregistRootObject(Object OldRootObject) {

            lock (RootObjects)
                RootObjects.Remove(OldRootObject);

            ObjectManager.Unregist(OldRootObject);
        }

        public static void SetTemporarySenceCenter(LongVector3 NewSenceCenter) {
            lock (LockForPositionTranslate) {

                TemporarySenceCenter = NewSenceCenter;

                if (System.DateTime.Now.ToBinary() - LastSenceCenterUpdateTime > MaxSenceCenterUpdateInterval
                    || (TemporarySenceCenter - CurrentSceneCenter).toVector3().magnitude > SceneFullLoadRange) {

                    LastSenceCenterUpdateTime = System.DateTime.Now.ToBinary();
                    CurrentSceneCenter = TemporarySenceCenter;

                    if ((TemporarySenceCenter - CurrentCoordinateOrigin).toVector3().magnitude > CoordinateOriginSwitchRange) {

                        //坐标原点的调整使用新线程防止堵塞
                        //cause:需要等待其他线程结束
                        Thread.Async(
                            delegate () {

                                ObjectManager.ThreadCompulsoryStop();
                                TerrainManager.ThreadCompulsoryStop();

                                while (ObjectManager.Alive || TerrainManager.Alive)
                                    System.Threading.Thread.Sleep(1);

                                //切换到主线程中调整坐标原点并更新各元素坐标
                                Thread.QueueOnMainThread(
                                    delegate () {

                                        //在更新结束前堵塞坐标转换函数
                                        lock (LockForPositionTranslate) {

                                            CurrentCoordinateOrigin = NewSenceCenter;

                                            _ChangeCoordinateOrigin(CurrentCoordinateOrigin);
                                        }

                                        ObjectManager.ThreadCompulsoryStopCancel();
                                        TerrainManager.ThreadCompulsoryStopCancel();
                                    }
                                );
                            }
                        );
                    }
                }
            }
        }

        public static UnityEngine.Vector3 SEPositionToUnityPosition(LongVector3 SEPosition) {
            lock (LockForPositionTranslate) {
                return (SEPosition - CurrentCoordinateOrigin).toVector3();
            }
        }

        public static LongVector3 UnityPositionToSEPosition(UnityEngine.Vector3 UnityPosition) {
            lock (LockForPositionTranslate) {
                return (new LongVector3(
                    (long)(UnityPosition.x * 1000),
                    (long)(UnityPosition.y * 1000),
                    (long)(UnityPosition.z * 1000)
                )) + CurrentCoordinateOrigin;
            }
        }

        private static void _ChangeCoordinateOrigin(LongVector3 NewCoordinateOrigin) {

            foreach (var obj in RootObjects)
                if (obj.UnityRoot != null)
                    obj.UnityRoot.transform.localPosition = (obj.SEPosition - NewCoordinateOrigin).toVector3();

            TerrainManager._ChangeCoordinateOrigin(NewCoordinateOrigin);

            //do other entities localposition change for CoordinateOrigin
        }
    }
}