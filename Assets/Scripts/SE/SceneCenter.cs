

namespace SE {
    public class SceneCenter {

        private LongVector3
            Position = new LongVector3(0, 0, 0);

        public SceneCenter(LongVector3 InitialPosition) {
            Position = InitialPosition;
        }

        public void Update() { Position = Kernel.CurrentSceneCenter; }

        public bool NeedUpdate() {
            if ((Position - Kernel.CurrentSceneCenter).toVector3().magnitude > Kernel.SceneFullLoadRange / 2)
                return true;

            return false;
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
