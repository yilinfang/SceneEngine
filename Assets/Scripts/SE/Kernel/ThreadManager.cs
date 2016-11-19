using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Generic.LockFree;

namespace SE {
    public partial class Kernel {
        public class ThreadManager {
            private class ThreadPool : UnityEngine.MonoBehaviour {
                private struct Item {
                    public Action<object> Action;
                    public object Data;
                    public float Time;
                }

                private LockFreeQueue<Item> Actions = new LockFreeQueue<Item>();

                void Update() {
                    float UpdateStartTime = UnityEngine.Time.time;

                    Item item;
                    while (UnityEngine.Time.time - UpdateStartTime < 0.05f && Actions.Dequeue(out item))
                        if (item.Time <= 0) {
                            item.Action(item.Data);
                        } else {
                            item.Time -= UnityEngine.Time.deltaTime;
                            Actions.Enqueue(item);
                        }
                }

                public void Add(Action<object> Action, object Data, float Time = -1) {
                    Actions.Enqueue(new Item {
                        Action = Action,
                        Data = Data,
                        Time = Time,
                    });
                }
            }

            private ThreadPool TaskPool;

            public ThreadManager(Kernel Kernel) { TaskPool = Kernel.SEUnityRoot.AddComponent<ThreadPool>(); }

            public void QueueOnMainThread(Action Action, float Time = -1) {
                TaskPool.Add(delegate (object t) { Action(); }, null, Time);
            }
            public void QueueOnMainThread(Action<object> Action, object Data, float Time = -1) {
                TaskPool.Add(Action, Data, Time);
            }

            public void AsyncInPool(Action Action) {
                System.Threading.ThreadPool.QueueUserWorkItem(delegate (object t) { Action(); });
            }
            public void AsyncInPool(WaitCallback Action, object Data) {
                System.Threading.ThreadPool.QueueUserWorkItem(Action, Data);
            }

            public Thread Async(ThreadStart Func) {
                Thread th = new Thread(Func);
                th.Start();
                return th;
            }

            public Thread Async(ParameterizedThreadStart Func, object Data) {
                Thread th = new Thread(new ParameterizedThreadStart(Func));
                th.Start(Data);
                return th;
            }
        }
    }
}