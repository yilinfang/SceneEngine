using UnityEngine;
using System.Collections.Generic;

namespace SE {
    public static partial class Kernel {
        private class SEUnityNodeListener : MonoBehaviour {

            //在这里实现只有主线程才能执行的函数以获取某些数据.
            //内存:
            //    Profiler.GetTotalAllocatedMemory()
            //    Profiler.GetMonoUsedSize()
            //    Profiler.GetMonoHeapSize()
            //帧数计算:
            //    Time.deltaTime
            //坐标获取:
            //    GameObject.transform.GlobalPosition

            private float TimeSum = 1;
            private Queue<float> TimeRecord = new Queue<float>();
            private float LastUnloadUnusedAssetsTime = 0;

            void Start() {

                for (int i = 0; i < 100; i++)
                    TimeRecord.Enqueue(0.01F);

                CurrentSceneMemory = Profiler.GetTotalAllocatedMemory();
            }

            void Update() {

                TimeSum += Time.deltaTime;

                TimeSum -= TimeRecord.Dequeue();

				TimeRecord.Enqueue(Time.deltaTime);

                CurrentFPS = 100 / TimeSum;

                //确保不会出现内存溢出问题
                if (Time.time - LastUnloadUnusedAssetsTime > 10) Resources.UnloadUnusedAssets();
            }

            void LateUpdate() {
                //CurrentSceneMemory = Profiler.GetTotalAllocatedMemory();
            }
        }

        private class ObjectNodeListener : MonoBehaviour {

            public Object ObjectRoot;

            void Update() {
                ObjectRoot.UnityGlobalPosition = ObjectRoot.UnityRoot.transform.position;
            }
        }
    }
}
