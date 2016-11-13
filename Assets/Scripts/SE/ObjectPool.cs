
namespace SE {
    public class ObjectPool<T> where T : new() {

        public delegate bool Operator(T Object);

        private T[] d;
        private int Size;
        public int Count { get; private set; }
        private Operator Reset, Collect;

        public ObjectPool(int Size, Operator Reset, Operator Collect) {
            Count = 0;
            d = new T[Size];
            this.Size = Size;
            this.Reset = Reset;
            this.Collect = Collect;
        }


        public void Put(T Object) {

            if (Count == Size) return;

            if (!Collect(Object))
                throw new System.Exception("ObjectPool : " + Object.GetType() + " can't be destruct by Destructor.");

            d[Count++] = (Object);
        }

        public T Get() {

            if (Count == 0) return new T();

            if (!Reset(d[--Count]))
                throw new System.Exception("ObjectCollector : " + d[Count].GetType() + " can't be instruct by Intructor.");

            return d[Count];
        }
    }
}
