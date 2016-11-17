using UnityEngine;

namespace SE.Listeners {
    public class ObjectListener : MonoBehaviour {
        public Object ObjectRoot;
        void Update() {
            ObjectRoot.UnityGlobalPosition = ObjectRoot.UnityRoot.transform.position;
        }
    }
}