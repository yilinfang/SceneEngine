

namespace System.Collections.Generic.LockFree {
  public class LimitedPriorityQueue<T,P> {

    private IPriorityConverter<P> converter;
    private LockFreeQueue<T>[] queueList;

    public LimitedPriorityQueue(IPriorityConverter<P> converter) {
      this.converter = converter;
      queueList = new LockFreeQueue<T>[converter.PriorityCount];
      for (int i = 0; i < queueList.Length; i++) {
        queueList[i] = new LockFreeQueue<T>();
      }
    }

    public void Enqueue(T item, P priority) {
      queueList[converter.Convert(priority)].Enqueue(item);
    }

    public bool Dequeue(out T item) {
      foreach (LockFreeQueue<T> q in queueList) {
        if (q.Dequeue(out item)) {
          return true;
        }
      }
      item = default(T);
      return false;
    }

    public T Dequeue() {
      T result;
      Dequeue(out result);
      return result;
    }
  }
}
