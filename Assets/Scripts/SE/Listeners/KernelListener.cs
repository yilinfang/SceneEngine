using System.Collections.Generic;
using UnityEngine;

namespace SE.Listeners {
    public class KernelListener : MonoBehaviour {
        public float _UnloadInterval;
        public float FPS = 100;
        public long SceneMemory = 0;

        private float TimeSum = 1;
        private Queue<float> TimeRecord = new Queue<float>();
        private float LastUnloadUnusedAssetsTime = 0;

        void Start() {
            for (int i = 0; i < 100; i++)
                TimeRecord.Enqueue(0.01F);
            SceneMemory = Profiler.GetTotalAllocatedMemory();
        }

        void Update() {
            TimeSum += Time.deltaTime - TimeRecord.Dequeue();
            TimeRecord.Enqueue(Time.deltaTime);

            FPS = 100 / TimeSum;
            SceneMemory = Profiler.GetTotalAllocatedMemory();

            if (Time.time - LastUnloadUnusedAssetsTime > _UnloadInterval)
                Resources.UnloadUnusedAssets();
        }
    }
}