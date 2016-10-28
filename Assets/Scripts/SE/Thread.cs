using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace SE {
    public static class Thread {

        private class ThreadPool : UnityEngine.MonoBehaviour {

            private struct Item {
                public Action<object> Action;
                public object Data;
                public long Time;
            }

            private List<Item>
                Actions = new List<Item>(),
                CurrentActions = new List<Item>();

            private static IEnumerator CoroutineMethod(Item x) {
                yield return new UnityEngine.WaitForSeconds(((float)x.Time - DateTime.Now.ToBinary()) / 1000);
                x.Action(x.Data);
            }

            //In MainThread
            void Start() {
                Current = this;
            }

            void Update() {

                lock (Actions) {//防堵塞
                    CurrentActions.Clear();
                    CurrentActions.AddRange(Actions);
                    Actions.Clear();
                }

                foreach (var item in CurrentActions) {
                    if (item.Time <= DateTime.Now.ToBinary()) {
                        item.Action(item.Data);
                    } else {
                        StartCoroutine(CoroutineMethod(item));
                    }
                }
            }

            private static ThreadPool Current;

            public static void Add(Action<object> Action, object Data, long Time = 0) {

                lock (Current.Actions) {
                    Current.Actions.Add(new Item {
                        Action = Action,
                        Data = Data,
                        Time = DateTime.Now.ToBinary() + Time,
                    });
                }
            }
        }

        private static bool
            Initialised = false;

        //In MainThread
        public static void Init() {
            if (!Initialised) {

                (new UnityEngine.GameObject("SEThreadPool")).AddComponent<ThreadPool>();

                Initialised = true;
            }
        }

        public static void QueueOnMainThread(Action Action, long Time = 0) {
            QueueOnMainThread(delegate (object t) { Action(); }, null, Time);
        }
        public static void QueueOnMainThread(Action<object> Action, object Data, long Time = 0) {
            ThreadPool.Add(Action, Data, Time);
        }

        public static void AsyncInPool(Action Action) {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate (object a) {
                try {
                    ((Action)a)();
                } catch {

                };
            }, Action);
        }
        public static void AsyncInPool(WaitCallback Action, object Data) {
            System.Threading.ThreadPool.QueueUserWorkItem(Action, Data);
        }

        public static System.Threading.Thread Async(ThreadStart Func) {
            System.Threading.Thread th = new System.Threading.Thread(Func);
            th.Start();
            return th;
        }

        public static System.Threading.Thread Async(ParameterizedThreadStart Func, object Data) {
            System.Threading.Thread th = new System.Threading.Thread(new ParameterizedThreadStart(Func));
            th.Start(Data);
            return th;
        }
    }
}