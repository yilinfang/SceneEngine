

namespace SE {
    namespace Objects {
        //Object can be used to storage some shared datas
        class A : Object {
            private Kernel Kernel;
            public A(Kernel tKernel) {
                Kernel = tKernel;
                //0 -> Lod.Length-1 the precision will be higher
                Lod = new LodCase[2]{
                    null,
                    new ALod(Kernel),
                };
                //The radius of bounding sphere 
                Range = 999;
                //Declare the ChildManager
                Child = new ChildManagers.Standard(Kernel, this);
                UnityEngine.Debug.Log("A built");
            }
            //Used for data calculating, but don't use these functions for scene building
            override public void Start() { UnityEngine.Debug.Log("A Start"); }
            override public void Destory() { UnityEngine.Debug.Log("A Destory"); }
        }
        class ALod : Object.LodCase {
            private Kernel Kernel;
            public static UnityEngine.Object res = null;
            public static int count = 0;
            private UnityEngine.GameObject t;
            private A[] ch = new A[5];
            public ALod(Kernel tKernel) {
                Kernel = tKernel;
                //The precision
                PrecisionRange = 99;
            }
            //Used for Scene building :
            //    Start() & Destory() : Used for data calculating
            //    StartForUnity() & DestoryForUnity() : Used for scene building
            override public void Start() {
                UnityEngine.Debug.Log("ALod Start");
                for (int i = 0; i < 5; i++)
                    ObjectRoot.Child.Add(
                        i.ToString(),
                        ch[i]=new A(Kernel),
                        new LongVector3(0, 0, 0),
                        new UnityEngine.Quaternion()
                    );
            }
            override public void StartForUnity() {
                if ((count++) == 0)
                    res = UnityEngine.Resources.Load("test", typeof(UnityEngine.GameObject));
                t = UnityEngine.Object.Instantiate(res) as UnityEngine.GameObject;
                t.transform.parent = ObjectRoot.UnityRoot.transform;
                UnityEngine.Debug.Log("ALod StartForUnity");
            }
            override public void Destroy() {
                for (int i = 0; i < 5; i++)
                    ObjectRoot.Child.Remove(ch[i]);
                UnityEngine.Debug.Log("ALod Destory");
            }
            override public void DestroyForUnity() {
                UnityEngine.Object.Destroy(t);
                if ((--count) == 0)
                    UnityEngine.Object.Destroy(res);
                UnityEngine.Debug.Log("ALod DestoryForUnity");
            }
        }
    }
}
