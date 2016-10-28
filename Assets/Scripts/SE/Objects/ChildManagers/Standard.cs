using UnityEngine;
using System.Collections.Generic;
using System;

namespace SE {
    public static partial class Kernel {
        public sealed class ChildManager_Standard : Object.ChildManager {

            private Dictionary<string, Object>
                Objects = new Dictionary<string, Object>();

            public ChildManager_Standard(Object Object) {
                ObjectRoot = Object;
            }

            public override void Add(string CharacteristicString, Object NewObject, LongVector3 LocalPosition, Quaternion LocalQuaternion) {

                NewObject.CharacteristicString = CharacteristicString;

                lock (Objects)
                    Objects[CharacteristicString] = NewObject;

                ObjectManager.Regist(ObjectRoot, NewObject, LocalPosition, LocalQuaternion);
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

                throw new ArgumentException("指定Key不存在", "id");
            }

            public override void Remove(Object OldObject) {

                lock (Objects)
                    if (Objects.Remove(OldObject.CharacteristicString)) {

                        ObjectManager.Unregist(OldObject);
                        return;
                    }

                throw new ArgumentException("指定Object不存在", "id");
            }

            public override void Clear() {

                lock (Objects) {
                    foreach (var item in Objects)
                        ObjectManager.Unregist(item.Value);

                    Objects.Clear();
                }
            }
        }
    }
}