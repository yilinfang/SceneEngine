

namespace SE {
    public interface IModule {
        void _Assigned(Kernel SEKernel);
        void _Start();
        void _ChangeSceneCenter(ref LongVector3 Position);
        void _ChangeCoordinateOrigin(ref LongVector3 Position);
        void _Stop();
        
    }
}