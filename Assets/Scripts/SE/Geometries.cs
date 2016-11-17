
namespace SE {
    public static class Geometries {

        public struct Rectangle<T> {

            public T
                x1, x2,
                y1, y2;

            public Rectangle(T x1, T x2, T y1, T y2) {
                this.x1 = x1;
                this.x2 = x2;
                this.y1 = y1;
                this.y2 = y2;
            }
        }

        public struct Square<T> {

            public T
                x, y, Length;

            public Square(T x, T y, T Length) {
                this.x = x;
                this.y = y;
                this.Length = Length;
            }
        }

        public struct Point<T> {

            public T x, y;

            public Point(T x, T y) {
                this.x = x;
                this.y = y;
            }
        }
        public struct Point<T1, T2> {

            public T1 x, y;

            public T2 h;

            public Point(T1 x, T1 y, T2 h) {
                this.x = x;
                this.y = y;
                this.h = h;
            }
            public Point(T1 x, T1 y) {
                this.x = x;
                this.y = y;
                h = default(T2);
            }
        }

        public static Rectangle<long>[] Split(ref Rectangle<long> Region) {

            long
                xmid = (Region.x2 + Region.x1) / 2,
                ymid = (Region.y2 + Region.y1) / 2;

            return new Rectangle<long>[4] {
                new Rectangle<long>(Region.x1, xmid, Region.y1, ymid),
                new Rectangle<long>(xmid, Region.x2, Region.y1, ymid),
                new Rectangle<long>(Region.x1, xmid, ymid, Region.y2),
                new Rectangle<long>(xmid, Region.x2, ymid, Region.y2),
            };
        }
        public static Square<byte>[] Split(ref Square<byte> Region) {

            byte
			    Lengthmid = (byte)(Region.Length / 2),
			    xmid = (byte)(Region.x + Lengthmid),
			    ymid = (byte)(Region.y + Lengthmid);

			return new Square<byte>[4] {
				    new Square<byte>(Region.x,Region.y,Lengthmid),
				    new Square<byte>(xmid,Region.y,Lengthmid),
				    new Square<byte>(Region.x,ymid,Lengthmid),
				    new Square<byte>(xmid,ymid,Lengthmid),
                };
        }

        public static bool Compare(ref Rectangle<long> a, ref Rectangle<long> b) {
            return (a.x1 == b.x1 && a.x2 == b.x2 && a.y1 == b.y1 && a.y2 == b.y2);
        }

        public static long MaxLength(ref Rectangle<long> a) {
            return (System.Math.Max(a.x2 - a.x1, a.y2 - a.y1));
        }

        public static bool OverLapped(ref Rectangle<long> a, ref Rectangle<long> b) {

            if (((a.x1 <= b.x1 && b.x1 <= a.x2) || (a.x1 <= b.x2 && b.x2 <= a.x2))
                && ((a.y1 <= b.y1 && b.y1 <= a.y2) || (a.y1 <= b.y2 && b.y2 <= a.y2)))
                return true;
            else if (((b.x1 <= a.x1 && a.x1 <= b.x2) || (b.x1 <= a.x2 && a.x2 <= b.x2))
                && ((b.y1 <= a.y1 && a.y1 <= b.y2) || (b.y1 <= a.y2 && a.y2 <= b.y2)))
                return true;
            return false;
        }
        public static bool OverLapped(ref Rectangle<long> a, long x, long y) {

            if (a.x1 <= x && x <= a.x2 && a.y1 <= y && y <= a.y2)
                return true;
            else
                return false;
        }

        public static Rectangle<long> TransformToRegionCenter(long xLength, long yLength, ref Rectangle<long> Region) {
            return new Rectangle<long>(
                (Region.x1 + Region.x2) / 2 - xLength / 2, (Region.x1 + Region.x2) / 2 + xLength - xLength / 2,
                (Region.y1 + Region.y2) / 2 - yLength / 2, (Region.y1 + Region.y2) / 2 + yLength - yLength / 2
            );
        }
    }
}
