

namespace SE {
    public struct RandomSeed {

        private long 
            a,
            b;

        public RandomSeed(long Seed) {
            a = b = Seed;
        }

        public long NextRandomNum() {
            return b = (214013 * b + 2531011);
        }
        public long NextRandomNum(long Top) {
            return b = (214013 * b + 2531011) % Top;
        }

        public RandomSeed GetRandomSeed(long RandNum) {
            return new RandomSeed(214013 * (b * RandNum) + 2531011);
        }

        public void Recover() { b = a; }
    }
}