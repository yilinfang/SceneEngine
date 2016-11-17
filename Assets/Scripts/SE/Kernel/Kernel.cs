using System.Collections.Generic;

namespace SE {
    public class Kernel {
        public class Settings {
            //The preload range of the scene
            public float PreloadRange = 20;
            //The switch range of the scene center
            public float SwitchRange_SceneCenter = 10;
            //The switch range for coordinate origin (especially used in (SEPosition <=> UnityPosition) translation)
            public float SwitchRange_CoordinateOrigin = 5 * 1000;
            //The max range of the scene
            public float VisibleRange = 10 * 1000;
            //The max interval time limit for sence center updating (System.DateTime.Now.ToBinary())
            public long SenceCenterUpdateIntervalMaxLimit = 10 * 1000;
            //The max memory limit of the scene (UnityEngine.Profiler.GetTotalAllocatedMemory())
            public long MemoryMaxLimit = 500 * 1024 * 1024;
            //The min FPS limit of the scene
            public float FPSMinLimit = 60;
            //The time Interval between two Resources.UnloadUnusedAssets() calls
            public float CleanAssetsInterval = 10f;
        }

        public Settings _Settings;
        //The temporary scene center of the scene
        private LongVector3 tSceneCenter;
        //Current scene center of the scene
        private LongVector3 SceneCenter;
        //Current coordinate origin of the scene
        private LongVector3 CoordinateOrigin;
        //The last time changing the scene center
        private long LastSceneCenterUpdateTime;

        public UnityEngine.GameObject SEUnityRoot;
        public Listeners.KernelListener Listener;
        public Modules.ThreadManager ThreadManager;
        public Modules.ObjectManager ObjectManager;
        private IModule[] Modules;
        private SBTree<Object> RootObjects;
        private int MainThreadId;

        public Kernel(Settings Settings) {
            _Settings = Settings;
            tSceneCenter = new LongVector3(0, 0, 0);
            SceneCenter = new LongVector3(0, 0, 0);
            CoordinateOrigin = new LongVector3(0, 0, 0);
            LastSceneCenterUpdateTime = 0;

            SEUnityRoot = new UnityEngine.GameObject("SE");
            Listener = SEUnityRoot.AddComponent<Listeners.KernelListener>();
            Listener._UnloadInterval = _Settings.CleanAssetsInterval;
            ThreadManager = null;
            ObjectManager = null;
            Modules = new IModule[0];
            RootObjects = new SBTree<Object>(new Comparers.KernelIDSmallFirstObjectComparer());
            MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }
        public void AssignThreadManager(Modules.ThreadManager tThreadManager) {
            ThreadManager = tThreadManager;
            ThreadManager._Assigned(this);
        }
        public void AssignObjectManager(Modules.ObjectManager tObjectManager) {
            ObjectManager = tObjectManager;
            ObjectManager._Assigned(this);
        }
        public void AssignModules(IModule[] tModules) {
            Modules = tModules;
            for (int i = 0; i < Modules.Length; i++)
                Modules[i]._Assigned(this);
        }
        public void Start() {
            if (ThreadManager == null) throw new System.Exception("Kernel : The ThreadManager is not assigned.");
            if (ObjectManager == null) throw new System.Exception("Kernel : The ObjectManager is not assigned.");
            ThreadManager._Start();
            ObjectManager._Start();
            for (int i = 0; i < Modules.Length; i++)
                Modules[i]._Start();
        }
        public void Stop() {
            for (int i = 0; i < Modules.Length; i++)
                Modules[i]._Stop();
            ObjectManager._Stop();
            ThreadManager._Stop();
        }

        public void RegistRootObject(string CharacteristicString, Object NewRootObject, LongVector3 Position, UnityEngine.Quaternion Quaternion) {
            NewRootObject.CharacteristicString = CharacteristicString;
            ObjectManager._Regist(null, NewRootObject, Position, Quaternion);
            lock (RootObjects)
                RootObjects.Add(NewRootObject);
        }
        public void UnregistRootObject(Object OldRootObject) {
            lock (RootObjects)
                RootObjects.Remove(OldRootObject);
            ObjectManager._Unregist(OldRootObject);
        }

        public void SetSceneCenter(LongVector3 NewSceneCenter) {

            tSceneCenter = NewSceneCenter;

            if (System.DateTime.Now.ToBinary() - LastSceneCenterUpdateTime > _Settings.SenceCenterUpdateIntervalMaxLimit
                || (tSceneCenter - SceneCenter).toVector3().magnitude > _Settings.SwitchRange_SceneCenter) {

                SceneCenter = tSceneCenter;
                LastSceneCenterUpdateTime = System.DateTime.Now.ToBinary();

                for (int i = 0; i < Modules.Length; i++)
                    Modules[i]._ChangeSceneCenter(ref SceneCenter);
                ObjectManager._ChangeSceneCenter(ref SceneCenter);
                ThreadManager._ChangeSceneCenter(ref SceneCenter);

                if ((SceneCenter - CoordinateOrigin).toVector3().magnitude > _Settings.SwitchRange_CoordinateOrigin) {
                    //Attention: The position translate functions is only used in main thread
                    //           and all positions in SE are SEPosition ( LongVector3 ), 
                    //           so coordinate origin changing is thread safe.
                    //           ( Actually there is only one main thread )
                    ThreadManager.QueueOnMainThread(delegate () {

                        CoordinateOrigin = SceneCenter;

                        //Attention: Only the position of root object that need to be changed.
                        foreach (var obj in RootObjects)
                            if (obj.UnityRoot != null)
                                obj.UnityRoot.transform.localPosition = Position_SEToUnity(ref obj.SEPosition);

                        for (int i = 0; i < Modules.Length; i++)
                            Modules[i]._ChangeCoordinateOrigin(ref CoordinateOrigin);
                        ObjectManager._ChangeCoordinateOrigin(ref CoordinateOrigin);//extend ?
                        ThreadManager._ChangeCoordinateOrigin(ref CoordinateOrigin);
                    });
                }
            }
        }

        public UnityEngine.Vector3 Position_SEToUnity(LongVector3 SEPosition) {
            if (MainThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new System.Exception("Kernel : position translation is only used in main thread.");
            return (SEPosition - CoordinateOrigin).toVector3();
        }
        public UnityEngine.Vector3 Position_SEToUnity(ref LongVector3 SEPosition) {
            if (MainThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new System.Exception("Kernel : position translation is only used in main thread.");
            return (SEPosition - CoordinateOrigin).toVector3();
        }

        public LongVector3 Position_UnityToSE(UnityEngine.Vector3 UnityPosition) {
            if (MainThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new System.Exception("Kernel : position translation is only used in main thread.");
            return (new LongVector3(
                (long)UnityPosition.x * 1000,
                (long)UnityPosition.y * 1000,
                (long)UnityPosition.z * 1000
            )) + CoordinateOrigin;
        }
        public LongVector3 Position_UnityToSE(ref UnityEngine.Vector3 UnityPosition) {
            if (MainThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new System.Exception("Kernel : position translation is only used in main thread.");
            return (new LongVector3(
                (long)UnityPosition.x * 1000,
                (long)UnityPosition.y * 1000,
                (long)UnityPosition.z * 1000
            )) + CoordinateOrigin;
        }
    }
}