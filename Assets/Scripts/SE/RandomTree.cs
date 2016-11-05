using System.Collections.Generic;

namespace SE {
    public class RandomTree<T> where T : class {

        private struct InputItem {
            public int Father;
            public int Weight;
            public int ChildRandomRange;
            public List<int> Child;
            public T Data;
            public Node Node;
            public InputItem(int Father, T Data, int Weight, int ChildRandomRange) {
                this.Father = Father;
                this.Data = Data;
                this.Weight= Weight;
                this.ChildRandomRange = ChildRandomRange;
                Node = new Node();
                if (ChildRandomRange >= 0)
                    Child = new List<int>();
                else
                    Child = null;
            }
        }

        private class Node {
            public int Floor;
            public T Data;
            public Node[] Child;
            public int ChildRandomRange;
        }

        private Node Root;
        private Dictionary<int, InputItem> DataDic;

        public RandomTree(T RootData, int ChildRandomRange = 0) {

            DataDic = new Dictionary<int, InputItem>();
            DataDic.Add(0, new InputItem(0, RootData, 0, ChildRandomRange));
        }

        public void Add(T Data, int ID, int Father, int Weight, int ChildRandomRange) {

            if (DataDic.ContainsKey(Father) && DataDic[Father].Child != null) {
                if (!DataDic.ContainsKey(ID)) {
                    DataDic.Add(ID, new InputItem(Father, Data, Weight, ChildRandomRange));
                    DataDic[Father].Child.Add(ID);
                } else {
                    throw new System.Exception("RandomTree ; 节点" + ID + "已存在.");
                }
            } else {
                throw new System.Exception("RandomTree : 父节点" + ID + "未被插入或者为封闭节点.");
            }
        }

        public void Init() {
            if (Root != null) throw new System.Exception("RndomTree : 已经初始化!");

            Root = DataDic[0].Node;

            Queue<InputItem> q = new Queue<InputItem>();
            q.Enqueue(DataDic[0]);
            while (q.Count != 0) {
                InputItem now = q.Dequeue();

                now.Node.Data = now.Data;
                if (now.Child == null) {
                    now.Node.Child = null;
                } else {
                    now.Node.Child = new Node[now.Child.Count];
                    int CurrentFloor = 0;
                    for (int i = 0; i < now.Child.Count; i++) {
                        InputItem Next = DataDic[now.Child[i]];
                        now.Node.Child[i] = Next.Node;
                        now.Node.Child[i].Floor = (CurrentFloor += Next.Weight);
                        q.Enqueue(Next);
                    }
                    now.Node.ChildRandomRange = System.Math.Max(CurrentFloor, now.ChildRandomRange);
                };
            }

            DataDic = null;
        }

        private static int BinarySearch(Node[] arr, int key) {
            if (arr.Length > 0 && key < arr[0].Floor) return 0;
            int left = 1, right = arr.Length - 1;
            while (left <= right) {
                int mid = (left + right) / 2;
                if (key < arr[mid - 1].Floor) {
                    right = mid - 1;
                } else if (key >= arr[mid].Floor) {
                    left = mid + 1;
                } else {
                    return mid;
                }
            }
            return -1;
        }

        public T GetLast(int Seed) {
            if (Root == null) throw new System.Exception("RandomTree : 尚未初始化.");
            Node now = Root;
            System.Random Rand = new System.Random(Seed);
            while (now.Child != null) {
                int Next = BinarySearch(now.Child, Rand.Next(now.ChildRandomRange));
                if (Next == -1) return null;
                now = now.Child[Next];
            }
            return now.Data;
        }
        public T[] GetAll(int Seed) {
            if (Root == null) throw new System.Exception("RandomTree : 尚未初始化.");
            List<T> lis = new List<T>();
            Node now = Root;
            lis.Add(now.Data);
            System.Random Rand = new System.Random(Seed);
            while (now.Child != null) {
                int Next = BinarySearch(now.Child, Rand.Next(now.ChildRandomRange));
                if (Next == -1) return null;
                now = now.Child[Next];
                lis.Add(now.Data);
            }
            return lis.ToArray();
        }
    }
}