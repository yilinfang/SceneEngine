 

namespace System.Collections.Generic.LockFree {
  public interface IPriorityConverter<P> {
    int Convert(P priority);
    int PriorityCount { get; }
  }
}
