using UnityEngine;
using System.Collections.Generic;
using System;

namespace SE.ChildManagers {
    public sealed class Standard : Object.ChildManager {

        private Kernel Kernel;
        private Dictionary<string, Object> Objects;

        public Standard(Kernel tKernel, Object tObjectRoot) {
            Kernel = tKernel;
            ObjectRoot = tObjectRoot;
            Objects = new Dictionary<string, Object>();
        }

        public override void Add(string CharacteristicString, Object NewObject, LongVector3 LocalPosition, Quaternion LocalQuaternion) {
            NewObject.CharacteristicString = CharacteristicString;
            lock (Objects)
                Objects[CharacteristicString] = NewObject;
            Kernel.ObjectManager._Regist(ObjectRoot, NewObject, LocalPosition, LocalQuaternion);
        }

        public override Object this[string CharacteristicString] {
            get {
                return Get(CharacteristicString);
            }
        }

        public override Object Get(string CharacteristicString) {
            lock (Objects)
                if (Objects.ContainsKey(CharacteristicString))
                    return Objects[CharacteristicString];
            throw new Exception("Child Manager Standard : ChStr \""+CharacteristicString+"\" is not existed.");
        }

        public override void Remove(Object OldObject) {
            lock (Objects)
                if (Objects.Remove(OldObject.CharacteristicString)) {
                    Kernel.ObjectManager._Unregist(OldObject);
                    return;
                }
            throw new Exception("Child Manager Standard : OldObject (ChStr is \"" + OldObject.CharacteristicString + "\") is not existed.");
        }

        public override void Clear() {
            lock (Objects) {
                foreach (var item in Objects)
                    Kernel.ObjectManager._Unregist(item.Value);
                Objects.Clear();
            }
        }
    }
}