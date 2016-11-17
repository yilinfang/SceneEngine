using System;
using System.Threading;
using System.Collections.Generic;

namespace SE.Modules {
    public class ThreadManager : IModule {
        private class ThreadPool : UnityEngine.MonoBehaviour {
            private struct Item {
                public Action<object> Action;
                public object Data;
                public long Time;
            }

			private List<Item> Actions = new List<Item>();

            void Update() {
				long CurrentTime = DateTime.Now.ToBinary();
				Item[] CurrentActions;
				List<Item> tActions = new List<Item>();
                float UpdateStartTime = UnityEngine.Time.time;
                lock (Actions) {//anti-block
                    CurrentActions = Actions.ToArray();
                    Actions.Clear();
                }
                for (int i = 0; i < CurrentActions.Length; i++) {
                    Item item = CurrentActions[i];
					if (item.Time <= CurrentTime)
						item.Action(item.Data);
					else
						tActions.Add(item);
                    if (UnityEngine.Time.time - UpdateStartTime > 0.05) {
                        for (int j = i + 1; j < CurrentActions.Length; j++)
                                tActions.Add(item);
                        break;
                    }
                }
				lock (Actions)
					Actions.AddRange(tActions);
            }

            public void Add(Action<object> Action, object Data, long Time = 0) {
                lock (Actions)
                    Actions.Add(new Item {
                        Action = Action,
                        Data = Data,
                        Time = DateTime.Now.ToBinary() + Time,
                    });
            }
        }

        private ThreadPool TaskPool;

        public void _Assigned(Kernel Kernel) { TaskPool = Kernel.SEUnityRoot.AddComponent<ThreadPool>(); }
        public void _Start() { }
        public void _ChangeSceneCenter(ref LongVector3 Position) { }
        public void _ChangeCoordinateOrigin(ref LongVector3 Position) { }
        public void _Stop() { }

        public void QueueOnMainThread(Action Action, long Time = 0) {
            TaskPool.Add(delegate (object t) { Action(); }, null, Time);
        }
        public void QueueOnMainThread(Action<object> Action, object Data, long Time = 0) {
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