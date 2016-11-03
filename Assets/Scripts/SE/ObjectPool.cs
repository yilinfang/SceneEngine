
namespace SE {
    public class ObjectPool<T> where T : new() {

        public delegate bool Operator(T Object);

        private T[] d;
        private int Size, Top;
        private Operator Reset, Collect;

        public ObjectPool(int Size, Operator Reset, Operator Collect) {
            Top = 0;
            d = new T[Size];
            this.Size = Size;
            this.Reset = Reset;
            this.Collect = Collect;
        }

        public void Put(T Object) {

            if (Top == Size) return;

            if (!Collect(Object))
                throw new System.Exception("ObjectPool : " + Object.GetType() + " can't be destruct by Destructor.");

            d[Top++] = (Object);
        }

        public T Get() {

            if (Top == 0) return new T();

            if (!Reset(d[--Top]))
                throw new System.Exception("ObjectCollector : " + d[Top].GetType() + " can't be instruct by Intructor.");

            return d[Top];
        }
    }
}
