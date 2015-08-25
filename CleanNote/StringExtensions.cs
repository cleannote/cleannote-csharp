using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanNote {
	public static class StringExtensions {
		public static bool InvEquals(this string a, string b) {
			var rslt = string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase);
			return (0 == rslt);
		}
	}
}
