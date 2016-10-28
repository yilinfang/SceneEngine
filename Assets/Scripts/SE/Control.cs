

namespace SE {
    public static class Control {

        public static void QuickStart() {
            Kernel.Init();
        }

        public static void QuickPause() {

        }

        public static void QuickReset() {
            Kernel.Stop();
        }

    }
};
