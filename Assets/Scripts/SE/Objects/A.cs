

namespace SE {
    namespace Objects {

        //Object可以用于存储和处理一些各LodCase共用的数据.
        class A : Object {

            //构造函数
            public A() {

                //0->Lod.Length-1精度依次升高
                Lod = new LodCase[2]{
                null,
                new ALod(),
            };

                //物体包围球半径
                Range = 999;

                //声明用于管理子Effect的EffectManager
                Child = new Kernel.ChildManager_Standard(this);

                UnityEngine.Debug.Log("A built");
            }

            //用于计算,不进行场景操作
            override public void Start() {
                UnityEngine.Debug.Log("A Start");
            }
            override public void Destory() {
                UnityEngine.Debug.Log("A Destory");
            }
        }

        class ALod : Object.LodCase {


            public static UnityEngine.Object res = null;
            public static int count = 0;


            private UnityEngine.GameObject t;
            private A[] ch = new A[5];

            public ALod() {
                PrecisionRange = 99;
            }

            //用于场景计算,注意xxxForUnity()会被添加到主线程的队列中,不能保证运行时机
            override public void Start() {
                UnityEngine.Debug.Log("ALod Start");
                for (int i = 0; i < 5; i++) {
                    ObjectRoot.Child.Add(i.ToString(), ch[i]=new A(),new LongVector3(0, 0, 0), new UnityEngine.Quaternion());
                }
            }

            override public void StartForUnity() {
                if ((count++) == 0) {
                    ALod.res = UnityEngine.Resources.Load("test", typeof(UnityEngine.GameObject));
                }
                t = UnityEngine.Object.Instantiate(ALod.res) as UnityEngine.GameObject;
                t.transform.parent = ObjectRoot.UnityRoot.transform;
                UnityEngine.Debug.Log("ALod StartForUnity");
            }

            override public void Destory() {
                for (int i = 0; i < 5; i++) {
                    ObjectRoot.Child.Remove(ch[i]);
                }
                UnityEngine.Debug.Log("ALod Destory");
            }

            override public void DestoryForUnity() {
                UnityEngine.Object.Destroy(t);
                if ((--count) == 0) {
                    UnityEngine.Object.Destroy(res);
                }
                UnityEngine.Debug.Log("ALod DestoryForUnity");
            }
        }
    }
}
