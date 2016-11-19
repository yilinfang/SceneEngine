

namespace System.Collections.Generic.LockFree {
    internal class SingleLinkNode<T> {
        // Note; the Next member cannot be a property since it participates in
        // many CAS operations
        public SingleLinkNode<T> Next;
        public T Item;
    }
}
