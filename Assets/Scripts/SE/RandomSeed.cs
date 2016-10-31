

namespace SE {
    public struct RandomSeed {

        private ulong a, b;

        public RandomSeed(ulong Seed) {
            a = b = Seed;
        }

        public ulong NextRandomNum() {
            return b = (214013 * b + 2531011);
        }
        public ulong NextRandomNum(ulong Top) {
            return b = (214013 * b + 2531011) % Top;
        }

        public RandomSeed GetRandomSeed(ulong RandNum) {
            return new RandomSeed(214013 * (b * RandNum) + 2531011);
        }

        public void Recover() { b = a; }

        public static RandomSeed 
            Static = new RandomSeed(1432414);
    }
}