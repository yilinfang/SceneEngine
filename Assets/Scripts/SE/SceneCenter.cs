

namespace SE {
    public class SceneCenter {

        private LongVector3
            Position = new LongVector3(0, 0, 0);

        public SceneCenter(LongVector3 InitialPosition) {
            Position = InitialPosition;
        }

        public bool Update() {

            if (Position == Kernel.CurrentSceneCenter)
                return false;
            else {
                Position = Kernel.CurrentSceneCenter;
                return true;
            }
        }

        public float GetDistence(UnityEngine.Vector3 Position) {
            return (Position - this.Position.toVector3()).magnitude;
        }
        public float GetDistence(Object Object) {
            return (Object.UnityGlobalPosition + Object.CenterAdjust - Position.toVector3()).magnitude;
        }
        public float GetDistence(LongVector3 Position) {
            return (Kernel.SEPositionToUnityPosition(Position) - this.Position.toVector3()).magnitude;
        }
    }
}
