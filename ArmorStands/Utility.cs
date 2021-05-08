using System.Linq;
using System.Collections.Generic;
using System;

namespace ValheimStands {

    public static class Utility {
        public static IEnumerable<Tuple<T, int>> enumerate<T>(IEnumerable<T> source) {
            return source.Select((item, index) => Tuple.Create(item, index));
        }
    }

}