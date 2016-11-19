

namespace SE {
    public interface IModule {
        void _Assigned(Kernel SEKernel);
        void _Start();
        void _ChangeSceneCenter(ref LongVector3 Position);
        void _ChangeCoordinateOrigin(ref LongVector3 Position);
        void _Stop();
    }
    public interface IObjectManager {
        void _Assigned(Kernel SEKernel);
        void _Start();
        void _Regist(Object Father, Object Child, LongVector3 LocalPosition, UnityEngine.Quaternion LocalQuaternion);
        void _Unregist(Object OldObject);
        void _ChangeSceneCenter(ref LongVector3 Position);
        void _ChangeCoordinateOrigin(ref LongVector3 Position);
        void _Stop();
    }
    public interface IChildManager {
        void Add(string ChStr, Object NewObject, LongVector3 LocalPosition, UnityEngine.Quaternion LocalQuaternion);
        Object this[string ChStr] { get; }
        void Remove(Object OldObject);
        void Clear();
    }
}