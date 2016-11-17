

namespace SE {
    public class SceneCenter {

        private Kernel Kernel;
        private LongVector3 Position;

        public SceneCenter(Kernel Kernel, LongVector3 InitialPosition) {
            this.Kernel = Kernel;
            Position = InitialPosition;
        }
        public SceneCenter(Kernel Kernel, ref LongVector3 InitialPosition) {
            this.Kernel = Kernel;
            Position = InitialPosition;
        }

        public void Change(ref LongVector3 NewPosition) { Position = NewPosition; }

        public float GetDistence(UnityEngine.Vector3 Position) {
            return Position.magnitude;
        }
        public float GetDistence(ref UnityEngine.Vector3 Position) {
            return Position.magnitude;
        }
        public float GetDistence(Object Object) {
            return (Object.UnityGlobalPosition + Object.CenterAdjust - Position.toVector3()).magnitude;
        }
        public float GetDistence(ref LongVector3 Position) {
            return (Position - this.Position).toVector3().magnitude;
        }

        public double Evaluate(Object Object) { return Evaluate(Object, Object.CurrentLodCaseIndex); }
        public double Evaluate(Object Object, int LodCaseIndex) {
            double
                d = GetDistence(Object),
                m = d - Object.Range;
            if (m >= Kernel._Settings.VisibleRange) return 0;
            if (m <= Kernel._Settings.PreloadRange + 0.1 || Object.CompulsoryCalculate) return 9999999999f;
            return ((LodCaseIndex == 0) ? Object.Range : Object.Lod[LodCaseIndex].PrecisionRange)
                / (d - Kernel._Settings.PreloadRange);
        }
        public double Evaluate(Modules.TerrainManager.CalculateNode Node) {
            double
                d = Node.ManagedTerrainRoot.SeparateFromFatherObject == true
                ? GetDistence(ref Node.CenterAdjust)
                : (Node.CenterAdjust.toVector3() + Node.ManagedTerrainRoot.UnityGlobalPosition).magnitude,
                m = d - Node.Range;
            if (m >= Kernel._Settings.VisibleRange) return 0;
            if (m <= Kernel._Settings.PreloadRange + 0.1) return 999999999f;
            return (Node.Range / 16) / (d - Kernel._Settings.PreloadRange);
        }
        public double Evaluate(Modules.TerrainManager.ApplyBlock Block) {
            double
                d = Block.ManagedTerrainRoot.SeparateFromFatherObject == true
                    ? GetDistence(ref Block.CenterAdjust)
                    : (Block.CenterAdjust.toVector3() + Block.ManagedTerrainRoot.UnityGlobalPosition).magnitude,
                m = d - Block.Range;

            if (Block.StorageTreeRoot == null) return 999999999f;
            if (Block.Changed == 0) return 0;
            if (m <= Kernel._Settings.PreloadRange + 0.1) return 999999999f;
            return (Block.Range / (d - Kernel._Settings.PreloadRange)) * (1 + Block.Changed / 10);
        }
    }
}
