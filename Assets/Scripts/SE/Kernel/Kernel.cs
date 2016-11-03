using System.Collections.Generic;

namespace SE {
    public static partial class Kernel {

        //Kernel���в������������:

        private static LongVector3

            TemporarySenceCenter = new LongVector3(0, 0, 0),

            CurrentCoordinateOrigin = new LongVector3(0, 0, 0);

        public static LongVector3

            CurrentSceneCenter { get; private set; }

        private static float

            CurrentFPS = 100;//��ǰ��FPS

        public static long

            CurrentSceneMemory = 0,//����ռ�õ��ڴ�:���������������Դ

            LastSenceCenterUpdateTime = System.DateTime.Now.ToBinary();

        public static long

            MaxSenceCenterUpdateInterval = 5000 * 10000,//�������������¼��ms

            MemoryLimit = 200 * 1024 * 1024;

        public static int

            FPSLimit = 60,

            ObjectThreadCalculateLimit = 200;

        public static float

            ObjectPrecisionLimit = 0.01F,//������ؾ�������

            SceneFullLoadRange = 10,//ȫ���ؾ���(���������л����)m

            SceneVisibleRange = 5 * 1000,//������ɾ���m

            CoordinateOriginSwitchRange = 5 * 1000;//����ԭ���л�����m   (����UnityPosition <=> SEPosition)

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
            
            //��ʼ����ģ��
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

                        //����ԭ��ĵ���ʹ�����̷߳�ֹ����
                        //cause:��Ҫ�ȴ������߳̽���
                        Thread.Async(
                            delegate () {

                                ObjectManager.ThreadCompulsoryStop();
                                TerrainManager.ThreadCompulsoryStop();

                                while (ObjectManager.Alive || TerrainManager.Alive)
                                    System.Threading.Thread.Sleep(1);

                                //�л������߳��е�������ԭ�㲢���¸�Ԫ������
                                Thread.QueueOnMainThread(
                                    delegate () {

                                        //�ڸ��½���ǰ��������ת������
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