using UnityEngine;
using System.Collections.Generic;
using System;

namespace SE.ChildManagers {
    public sealed class Standard : IChildManager {

        private Kernel Kernel;
        private Dictionary<string, Object> Objects;
        private Object ObjectRoot;

        public Standard(Kernel tKernel, Object tObjectRoot) {
            Kernel = tKernel;
            ObjectRoot = tObjectRoot;
            Objects = new Dictionary<string, Object>();
        }
        public void Add(string ChStr, Object NewObject, LongVector3 LocalPosition, Quaternion LocalQuaternion) {
            NewObject.ChStr = ChStr;
            lock (Objects)
                Objects[ChStr] = NewObject;
            Kernel.ObjectManager._Regist(ObjectRoot, NewObject, LocalPosition, LocalQuaternion);
        }
        public Object this[string ChStr] {
            get {
                lock (Objects)
                    if (Objects.ContainsKey(ChStr))
                        return Objects[ChStr];
                throw new Exception("Child Manager Standard : ChStr \"" + ChStr + "\" is not existed.");
            }
        }
        public Object Get(string ChStr) {
            lock (Objects)
                if (Objects.ContainsKey(ChStr))
                    return Objects[ChStr];
            throw new Exception("Child Manager Standard : ChStr \""+ ChStr + "\" is not existed.");
        }
        public void Remove(Object OldObject) {
            lock (Objects)
                if (Objects.Remove(OldObject.ChStr)) {
                    Kernel.ObjectManager._Unregist(OldObject);
                    return;
                }
            throw new Exception("Child Manager Standard : OldObject (ChStr is \"" + OldObject.ChStr + "\") is not existed.");
        }
        public void Clear() {
            lock (Objects) {
                foreach (var item in Objects)
                    Kernel.ObjectManager._Unregist(item.Value);
                Objects.Clear();
            }
        }
    }
}