using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CleanNote {
	public static class HtmlExtensions {
		public static bool IsAnyTag(this HtmlNode self, string name, params string[] names) {
			if (IsTag(self, name)) {
				return true;
			}

			if (null != names) {
				foreach (var tmpName in names) {
					if (IsTag(self, tmpName)) {
						return true;
					}
				}
			}

			return false;
		}
        public static bool IsTag(this HtmlNode self, string name) {
			if (null == self) { return false; }

			var rslt = string.Compare(self.Name, name, StringComparison.InvariantCultureIgnoreCase);
			return (0 == rslt);
		}
	}
}
