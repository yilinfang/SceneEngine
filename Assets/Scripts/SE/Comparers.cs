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
}