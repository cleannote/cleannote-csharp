using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanNote {
	public static class DictionaryExtensions {
		public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key) {
			bool found;
			return Find<TKey, TValue>(self, key, out found);
		}
		public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, out bool found) {
			TValue result;
			found = self.TryGetValue(key, out result);
			return result;
		}
	}
}
