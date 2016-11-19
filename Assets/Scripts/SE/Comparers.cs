using System.Collections.Generic;

namespace SE.Comparers {

    public class KernelIDSmallFirstObjectComparer : IComparer<Object> {
        public int Compare(Object a, Object b) {

            return (a.KernelID == b.KernelID) ? 0 : (
                (a.KernelID > b.KernelID) ? 1 : -1
            );
        }
    }

    public class LastUpdateTimeSmallFirstObjectComparer : IComparer<Object> {
        public int Compare(Object a, Object b) {

            return (a.LastUpdateTime == b.LastUpdateTime && a.KernelID == b.KernelID) ? 0 : (
                 (a.LastUpdateTime > b.LastUpdateTime || (a.LastUpdateTime == b.LastUpdateTime && a.KernelID > b.KernelID)) ? 1 : -1
             );
        }
    }

    public class MaintainEvaluationSmallFirstObjectComparer : IComparer<Object> {
        public int Compare(Object a, Object b) {

            return (a.MaintainEvaluation == b.MaintainEvaluation && a.KernelID == b.KernelID) ? 0 : (
                (a.MaintainEvaluation > b.MaintainEvaluation || (a.MaintainEvaluation == b.MaintainEvaluation && a.KernelID > b.KernelID)) ? 1 : -1
            );
        }
    }

    public class MaintainEvaluationBigFirstObjectComparer : IComparer<Object> {
        public int Compare(Object a, Object b) {

            return (a.MaintainEvaluation == b.MaintainEvaluation && a.KernelID == b.KernelID) ? 0 : (
                (a.MaintainEvaluation < b.MaintainEvaluation || (a.MaintainEvaluation == b.MaintainEvaluation && a.KernelID < b.KernelID)) ? 1 : -1
            );
        }
    }

    public class CharacteristicStringSmallFirstObjectComparer : IComparer<Object> {
        public int Compare(Object a, Object b) {
            return string.Compare(a.ChStr, b.ChStr);
        }
    }

    public class KernelIDSmallFirstManagedTerrainComparer : IComparer<Modules.TerrainManager.ManagedTerrain> {
        public int Compare(Modules.TerrainManager.ManagedTerrain a, Modules.TerrainManager.ManagedTerrain b) {
            return (a.KernelID == b.KernelID) ? 0 : ((a.KernelID > b.KernelID) ? 1 : -1);
        }
    }
    public class KeyBigFirstCalculateNodeComparer : IComparer<Modules.TerrainManager.CalculateNode> {
        public int Compare(Modules.TerrainManager.CalculateNode a, Modules.TerrainManager.CalculateNode b) {
            return (a.Key == b.Key) ? 0 : ((a.Key > b.Key) ? 1 : -1);
        }
    }
    public class PositionSmallFirstTerrainBlockPointComparer : IComparer<Geometries.Point<long, long>> {
        public int Compare(Geometries.Point<long, long> a, Geometries.Point<long, long> b) {
            return (a.x == b.x && a.y == b.y) ? 0 : ((a.x > b.x || (a.x == b.x && a.y > b.y)) ? 1 : -1);
        }
    }
    public class KeyBigFirstApplyBlockComparer : IComparer<Modules.TerrainManager.ApplyBlock> {
        public int Compare(Modules.TerrainManager.ApplyBlock a, Modules.TerrainManager.ApplyBlock b) {
            return (a.Key == b.Key) ? 0 : ((a.Key < b.Key) ? 1 : -1);
        }
    }
}