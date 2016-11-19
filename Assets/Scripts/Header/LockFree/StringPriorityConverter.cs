using System.Globalization;

namespace System.Collections.Generic.LockFree {
    public class StringPriorityConverter : IPriorityConverter<string> {

        int IPriorityConverter<string>.Convert(string priority) {

            if (priority == null) {
                throw new ArgumentNullException("Priority value should be set", "priority");
            }
            switch (priority.ToLower(CultureInfo.InvariantCulture)) {
            case ("high"): {
                    return 0;
                }
            case ("medium"): {
                    return 1;
                }
            default: {
                    return 2;
                }
            }
        }

        int IPriorityConverter<string>.PriorityCount {
            get {
                return 3;
            }
        }
    }
}
