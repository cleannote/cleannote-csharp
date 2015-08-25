using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CleanNote.TESTS {
	[TestClass]
	public class Converter_Tests {
		[TestMethod]
		public void CanConvert_Borek01_To_NiceHtml() {
			var source = TestResources.Borek01;

			var converter = new Converter();

			var result = converter.Convert(source);

			Assert.Inconclusive();
		}

		[TestMethod]
		public void CanConvert_JustHeading_To_NiceHtml() {
			var source = TestResources.JustHeadings;

			var converter = new Converter();

			var result = converter.Convert(source);

			Assert.Inconclusive();
		}

		[TestMethod]
		public void CanConvert_Heading_And_Formating_To_NiceHtml() {
			var source = TestResources.Headings_And_Formating;

			var converter = new Converter();

			var result = converter.Convert(source);

			Assert.Inconclusive();
		}
	}
}
