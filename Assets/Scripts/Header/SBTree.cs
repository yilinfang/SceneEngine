using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Collections.Generic {
    [Serializable]
    public class SBTree<T> : IEnumerable<T>, ISerializable {
        [Serializable]
        sealed class Node : IEquatable<Node>, ISerializable {
            public Node(T data) { Data = data; Size = 1; }
            public Node() { }
            Node(SerializationInfo info, StreamingContext context) {
                Data = (T)info.GetValue("Data", typeof(T));
                Left = (Node)info.GetValue("Left", typeof(Node));
                Right = (Node)info.GetValue("Right", typeof(Node));
                Father = (Node)info.GetValue("Father", typeof(Node));
                Size = info.GetInt32("Size");
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context) {
                info.AddValue("Data", Data);
                info.AddValue("Left", Left);
                info.AddValue("Right", Right);
                info.AddValue("Father", Father);
                info.AddValue("Size", Size);
            }

            public T Data { get; internal set; }
            public int Size { get; internal set; }
            internal Node left, right;
            public Node Left { get { return left; } internal set { left = value; } }
            public Node Right { get { return right; } internal set { right = value; } }
            public Node Father { get; internal set; }

            public static void RightRotate(ref Node node) {
                Node T = node.Left;
                if ((node.Left = T.Right) != null) T.Right.Father = node;
                node.Size += (T.Right == null ? 0 : T.Right.Size) - T.Size;
                T.Right = node; T.Father = node.Father; node.Father = T;
                T.Size = T.Right.Size + (T.Left == null ? 0 : T.Left.Size) + 1;
                node = T;
            }
            public static void LeftRotate(ref Node node) {
                Node T = node.Right;
                if ((node.Right = T.Left) != null) T.Left.Father = node;
                node.Size += (T.Left == null ? 0 : T.Left.Size) - T.Size;
                T.Left = node; T.Father = node.Father; node.Father = T;
                T.Size = T.Left.Size + (T.Right == null ? 0 : T.Right.Size) + 1;
                node = T;
            }
            public static void Maintain(ref Node node, bool maintain_right) {
                if (node == null) return;
                if (maintain_right) {
                    if (node.Right == null) return;
                    int lsize;
                    if (node.Left == null) lsize = 0; else lsize = node.Left.Size;
                    if (node.Right.Right != null && node.Right.Right.Size > lsize)
                        Node.LeftRotate(ref node);
                    else if (node.Right.Left != null && node.Right.Left.Size > lsize) {
                        RightRotate(ref node.right);
                        LeftRotate(ref node);
                    } else return;
                } else {
                    if (node.Left == null) return;
                    int rsize;
                    if (node.Right == null) rsize = 0; else rsize = node.Right.Size;
                    if (node.Left.Left != null && node.Left.Left.Size > rsize)
                        Node.RightRotate(ref node);
                    else if (node.Left.Right != null && node.Left.Right.Size > rsize) {
                        Node.LeftRotate(ref node.left);
                        Node.RightRotate(ref node);
                    } else return;
                }

                Maintain(ref node.left, false);
                Maintain(ref node.right, true);
                Maintain(ref node, false);
                Maintain(ref node, true);
            }

            public bool Equals(Node a) {
                return Data.Equals(a.Data);
            }
            public bool Equals(T a) {
                return Data.Equals(a);
            }
            public override bool Equals(object a) {
                if (a is T)
                    return Equals((T)a);
                else if (a is Node)
                    return Equals((Node)a);
                return false;
            }
            public override int GetHashCode() {
                return Data.GetHashCode();
            }
            public override string ToString() {
                return Data.ToString();
            }
        }

        public SBTree() { comparer = Comparer<T>.Default; }
        public SBTree(IComparer<T> comparer) { this.comparer = comparer; }
        protected SBTree(SerializationInfo info, StreamingContext context) {
            root = LoadNodeFromArray((T[])info.GetValue("data", typeof(T[])));
            comparer = (IComparer<T>)info.GetValue("comparer", typeof(IComparer<T>));
        }

        //[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(
        SerializationInfo info, StreamingContext context) {
            info.AddValue("data", ToArray());
            info.AddValue("comparer", comparer);
        }

        Node root;
        IComparer<T> comparer;


        void Add(ref Node node, T v) {
            node.Size += 1;
            if (comparer.Compare(v, node.Data) <= 0) {
                if (node.left == null) { node.left = new Node(v); node.left.Father = node; } else Add(ref node.left, v);
                Node.Maintain(ref node, false);
            } else {
                if (node.right == null) { node.right = new Node(v); node.right.Father = node; } else Add(ref node.right, v);
                Node.Maintain(ref node, true);
            }
        }
        /// <summary>
        /// 在SBT中插入一个新的数据 复杂度O(ln(count))
        /// </summary>
        /// <param name="v"></param>
        public void Add(T v) {
            if (root == null)
                root = new Node(v);
            else
                Add(ref root, v);
        }

        T Remove(ref Node node, T v) {
            node.Size -= 1;
            int com_result = comparer.Compare(v, node.Data);
            if (com_result == 0 || (com_result < 0 && node.Left == null) || (com_result > 0 && node.Right == null)) {
                T re = node.Data;
                if (node.Left == null)
                    node = node.Right;
                else if (node.Right == null)
                    node = node.Left;
                else
                    node.Data = Remove(ref node.left, node.Right.Data);
                return re;
            } else if (com_result < 0)
                return Remove(ref node.left, v);
            else
                return Remove(ref node.right, v);
        }
        /// <summary>复杂度O(ln(count))</summary>
        /// <param name="v"></param>
        /// <returns>是否成功</returns>
        public bool Remove(T v) {
            if (!Contains(v)) return false;
            Remove(ref root, v);
            return true;
        }

        /// <summary>
        /// 寻找到的是 深度最浅的节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        Node Find(Node node, T v) {
            if (node == null) return null;
            int com_res = comparer.Compare(v, node.Data);
            if (com_res == 0)
                return node;
            if (com_res < 0)
                return Find(node.Left, v);
            else
                return Find(node.Right, v);
        }
        Node Find(T v) { return Find(root, v); }
        /// <summary>
        /// 此SBT中是否包含值为v的对象 复杂度O(ln(count))
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool Contains(T v) { return Find(v) != null; }
        bool Contains(Node node) {
            while (node.Father != null) node = node.Father;
            return Object.ReferenceEquals(root, node);
        }
        Node FindFirst(Node node, T v) {
            if (node == null) return null;
            int com_res = comparer.Compare(v, node.Data);
            if (com_res == 0) {
                Node re = node;
                while (re.Left != null && comparer.Compare(re.Left.Data, v) == 0) re = re.Left;
                return re;
            }
            if (com_res < 0)
                return Find(node.Left, v);
            else
                return Find(node.Right, v);
        }
        Node FindFirst(T v) { return FindFirst(root, v); }
        public T FindFirstData(T v) {
            var node = FindFirst(root, v);
            if (node == null) return default(T);
            return node.Data;
        }
        Node FindLast(Node node, T v) {
            if (node == null) return null;
            int com_res = comparer.Compare(v, node.Data);
            if (com_res == 0) {
                Node re = node;
                while (re.Right != null && comparer.Compare(re.Right.Data, v) == 0) re = re.Right;
                return re;
            }
            if (com_res < 0)
                return Find(node.Left, v);
            else
                return Find(node.Right, v);
        }
        Node FindLast(T v) { return FindLast(root, v); }
        public T FindLastData(T v) {
            var node = FindLast(root, v);
            if (node == null) return default(T);
            return node.Data;
        }
        /// <summary>
        /// 查找SBT中与v相同的数据的数量
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int NumberContains(T v) {
            int t = RankFirst(v);
            if (t == -1) return 0;
            return RankLast(v) - t + 1;
        }


        /// <summary>
        /// 是否为空 复杂度O(1)
        /// </summary>
        public bool Empty { get { return root == null; } }
        /// <summary>
        /// 数据数量 复杂度O(1)
        /// </summary>
        public int Count {
            get {
                if (Empty) return 0;
                return root.Size;
            }
        }

        int Rank(Node node) {
            if (node == null) return -1;
            int re;
            if (node.Left == null) re = 1;
            else re = node.Left.Size + 1;
            while (node.Father != null) {
                if (Object.ReferenceEquals(node.Father.Right, node))
                    re += node.Father.Size - node.Size;
                node = node.Father;
            }
            return re;
        }
        /// <summary>
        /// 从小到大，查询第一个 v 的排名(根据comparer进行排序) 复杂度O(ln(count))
        /// 排名从1开始
        /// </summary>
        /// <param name="v"></param>
        /// <returns>排名</returns>
        public int RankFirst(T v) {
            return Rank(FindFirst(v));
        }
        public int RankLast(T v) {
            return Rank(FindLast(v));
        }

        Node Select(Node node, int rank) {
            if (node == null) return null;
            int com_res = rank - ((node.Left == null) ? 0 : node.Left.Size) - 1;
            if (com_res == 0) return node;
            else if (com_res < 0) return Select(node.Left, rank);
            else return Select(node.Right, rank - ((node.Left == null) ? 0 : node.Left.Size) - 1);
        }
        /// <summary>
        /// 选中排名在rank处的数据， 复杂度O(ln(count))，排名从1开始
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public T Select(int rank) {
            if (rank > Count || rank < 1) throw new ArgumentException("rank小于1或者超过了Count", "rank");
            return Select(root, rank).Data;
        }

        /// <summary>
        /// O(1)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        Node Pred(Node node) {
            if (node.Left != null) {
                Node re = node.Left;
                while (re.Right != null) re = re.Right;
                return re;
            } else {
                Node re = node;
                while (re.Father != null && !Object.ReferenceEquals(re.Father.Right, re)) re = re.Father;
                return re.Father;
            }
        }
        /// <summary>
        /// O(1)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        Node Succ(Node node) {
            if (node.Right != null) {
                Node re = node.Right;
                while (re.Left != null) re = re.Left;
                return re;
            } else {
                Node re = node;
                while (re.Father != null && !Object.ReferenceEquals(re.Father.Left, re)) re = re.Father;
                return re.Father;
            }
        }
        /// <summary>
        /// 寻找比v小的最大的数据
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public T Pred(T v) {
            var note = Pred(FindFirst(root, v));
            if (note == null) return default(T);
            return note.Data;
        }
        /// <summary>
        /// 寻找比v大的最小的数据
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public T Succ(T v) {
            var note = Succ(FindLast(root, v));
            if (note == null) return default(T);
            return note.Data;
        }

        Node FirstNode() {
            if (root == null) return null;
            Node re = root;
            while (re.Left != null) re = re.Left;
            return re;
        }
        Node LastNode() {
            if (root == null) return null;
            Node re = root;
            while (re.Right != null) re = re.Right;
            return re;
        }
        public T First() {
            return FirstNode().Data;
        }
        public T Last() {
            return LastNode().Data;
        }

        public struct SBT_Enumrator : IEnumerator, IEnumerator<T> {
            internal SBT_Enumrator(SBTree<T> from, bool forward) {
                current_SBT = from;
                this.forward = forward;
				current_Node = null;
            }
            bool forward;
            Node current_Node;
			SBTree<T> current_SBT;
			object IEnumerator.Current { get { return Current; } }
			public T Current { get { return (current_Node == null) ? default(T) : current_Node.Data; } }
            public bool MoveNext() {
                Node re;
				if (current_Node == null) {
					if ((re = current_SBT.FirstNode()) == null)
						return false;
				} else {
					if (!current_SBT.Contains(current_Node)) throw new InvalidOperationException("已经对SBT集合进行了修改，并且Current已经被删除");
					if (forward)
						re = current_SBT.Succ (current_Node);
					else
						re = current_SBT.Pred (current_Node);
					if (re == null)
						return false;
				}
                current_Node = re;
                return true;
            }
            public void Reset() {
                current_Node = null;
            }
            public void Dispose() {
                current_SBT = null;
                current_Node = null;
            }
        }
        public IEnumerator<T> GetEnumerator() { return new SBT_Enumrator(this, true); }
        IEnumerator IEnumerable.GetEnumerator() { return new SBT_Enumrator(this, true); }
        public struct SBT_R_EnumeraDev : IEnumerable<T> {
            internal SBT_R_EnumeraDev(SBTree<T> from) { current_SBT = from; }
            SBTree<T> current_SBT;
            public IEnumerator<T> GetEnumerator() { return new SBT_Enumrator(current_SBT, false); }
            IEnumerator IEnumerable.GetEnumerator() { return new SBT_Enumrator(current_SBT, false); }
        }
        public IEnumerable AsReverse() { return new SBT_R_EnumeraDev(this); }

        public void Clear() {
            root = null;
        }
        public void CopyTo(T[] arr, int arrayIndex) {
            int i = 0;
            foreach (var a in this)
                arr[arrayIndex + i++] = a;
        }

        void writeToArray(T[] arr, ref int index, Node node) {
            if (node == null) return;
            writeToArray(arr, ref index, node.Left);
            arr[index++] = node.Data;
            writeToArray(arr, ref index, node.Right);
        }
        public T[] ToArray() {
            var re = new T[Count];
            int index = 0;
            writeToArray(re, ref index, root);
            return re;
        }
        /// <summary>
        /// 当sbt不为空时，复杂度：O(n*log(n)) n为arr.Length
        /// 当sbt为空时，复杂度：O(n) n为arr.Length
        /// </summary>
        /// <param name="arr"></param>
        public void AddRange(T[] arr) {
            if (root == null) { root = LoadNodeFromArray(arr); return; }
            foreach (var a in arr)
                Add(a);
        }
        static Node LoadNodeFromArray(T[] arr, int index, int len) {
            if (len == 0) return null;
            Node re = new Node(arr[index + (len >> 1)]) { Size = len };
            if ((re.Left = LoadNodeFromArray(arr, index, len >> 1)) != null) re.Left.Father = re;
            if ((re.Right = LoadNodeFromArray(arr, index + (len >> 1) + 1, len - (len >> 1) - 1)) != null) re.Right.Father = re;
            return re;
        }
        static Node LoadNodeFromArray(T[] arr) {
            return LoadNodeFromArray(arr, 0, arr.Length);
        }
        /// <summary>
        /// 复杂度 O(arr.Length)
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static SBTree<T> LoadFromArray(T[] arr, IComparer<T> compare = null) {
            if (compare == null) compare = Comparer<T>.Default;
            var re = new SBTree<T>(compare);
            re.root = LoadNodeFromArray(arr);
            return re;
        }
    }
}