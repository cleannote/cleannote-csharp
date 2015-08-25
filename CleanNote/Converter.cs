using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CleanNote {
	public class Converter {
		public Dictionary<string, string> DefaultStyles { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{ "margin", "0in" },
			{ "font-family", "Calibri" },
			{ "font-size", "11.0pt" }
		};
		public HashSet<string> ForbiddenAttributes { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "lang" }
		};

		public Dictionary<string, string> H1Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{"font-size", "16.0pt" },
			{"color", "#1E4E79" }
		};
		public Dictionary<string, string> H2Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{"font-size", "14.0pt" },
			{"color", "#2E75B5" }
		};
		public Dictionary<string, string> H3Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{"font-size", "12.0pt" },
			{"color", "#5B9BD5" }
		};
		public Dictionary<string, string> H4Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{"font-size", "12.0pt" },
			{"color", "#5B9BD5" },
			{"font-style", "italic" }
		};
		public Dictionary<string, string> H5Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			//{"font-size", "11.0pt" },
			{"color", "#2E75B5" },
		};
		public Dictionary<string, string> H6Style { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			//{"font-size", "11.0pt" },
			{"color", "#2E75B5" },
			{"font-style", "italic" }
		};

		public string Convert(string source) {
			var srcHtml = new HtmlDocument();
			srcHtml.LoadHtml(source);

			//// we want remove root empty whitespaces
			//RemoveWhitespaceNodes(srcHtml.DocumentNode);

			// we want remove root divs, which are useless
			RemoveWrapperNodesRecursively(srcHtml.DocumentNode, node => 0 == string.Compare(node.Name, "div", StringComparison.InvariantCultureIgnoreCase));

			//// we want remove root empty whitespaces ... again
			//RemoveWhitespaceNodes(srcHtml.DocumentNode);

			// we don't want default styles
			RemoveDefaultStylesRecursively(srcHtml.DocumentNode);

			// we don't want forbidden attributes
			RemoveForbiddenAttributesRecursively(srcHtml.DocumentNode);

			// we don't want empty attributes
			RemoveEmptyAttributesRecursively(srcHtml.DocumentNode);

			// we don't want empty paragraphs
			RemoveEmptyParagraphsRecursively(srcHtml.DocumentNode);

			// we don't want overlaping spans with no reasonable use
			RemoveOverlappingSpanInParagraphRecursively(srcHtml.DocumentNode);

			//// we want <b, i, u> instead of <span style='...'>
			//TransformFormatingSpansToFormatingElementsRecursively(srcHtml.DocumentNode);

			// we want h1-h6 instead of <p style='...'>
			TransformSuitableParagraphToHeadingRecursively(srcHtml.DocumentNode);

			// we want <b, i, u> instead of <span style='...'>
			TransformFormatingSpansToFormatingElementsRecursively(srcHtml.DocumentNode);

			// we don't want <span>...</span>
			RemoveMeaninglessSpanRecursively(srcHtml.DocumentNode);

			return srcHtml.DocumentNode.OuterHtml;
		}

		void RemoveMeaninglessSpanRecursively(HtmlNode parent) {
			RemoveMeaninglessSpan(parent);
			foreach (var child in parent.ChildNodes.ToList()) {
				RemoveMeaninglessSpanRecursively(child);
			}
        }
		void RemoveMeaninglessSpan(HtmlNode parent) {
			for (var i = parent.ChildNodes.Count - 1; i >= 0; i--) {
				var node = parent.ChildNodes[i];
				if (false == node.IsTag("span")) { continue; }
				if (0 < node.Attributes.Count) { continue; }

				parent.ChildNodes.RemoveAt(i);

				for (var b = node.ChildNodes.Count - 1; b >= 0; b--) {
					var child = node.ChildNodes[b];
					node.ChildNodes.RemoveAt(b);
					parent.ChildNodes.Insert(i, child);
				}
			}
		}

		void TransformFormatingSpansToFormatingElementsRecursively(HtmlNode node) {
			TransformFormatingSpansToFormatingElements(node);
			foreach (var child in node.ChildNodes.ToList()) {
				TransformFormatingSpansToFormatingElementsRecursively(child);
			}
		}
		void TransformFormatingSpansToFormatingElements(HtmlNode node) {
			if (false == node.IsAnyTag("span", "p")) { return; }

			var style = ParseStyle(node);
			if (style.Count < 1) { return; }

			var wasTransformed = false;
			var parentChildren = node.ParentNode.ChildNodes;
			Action<string> wrapSpan = (wrapperTagName) => {
				var wrapperNode = HtmlNode.CreateNode("<" + wrapperTagName + "></" + wrapperTagName + ">");
				wrapperNode.InnerHtml = node.InnerHtml;
				node.InnerHtml = wrapperNode.OuterHtml;
				wasTransformed = true;
			};
			Action<string, string, string> wrapSpanByStyle = (key, value, wrapperTagName) => {
				var styleValue = style.Find(key);
				styleValue = null != styleValue ? styleValue.Trim() : string.Empty;

				if (styleValue.InvEquals(value.Trim())) {
					wrapSpan(wrapperTagName);
					style.Remove(key);
				}
			};

			wrapSpanByStyle("font-weight", "bold", "b");
			wrapSpanByStyle("font-style", "italic", "i");
			wrapSpanByStyle("text-decoration", "underline", "u");

			if (false == wasTransformed) { return; }

			if (style.Count < 1) {
				node.Attributes.Remove("style");
			}
			else {
				var finalStyle = ComposeStyle(style);
				node.SetAttributeValue("style", finalStyle);
			}
		}

		void TransformSuitableParagraphToHeadingRecursively(HtmlNode node) {
			TransformSuitableParagraphToHeading(node);
			foreach (var child in node.ChildNodes.ToList()) {
				TransformSuitableParagraphToHeading(child);
			}
		}
		void TransformSuitableParagraphToHeading(HtmlNode node) {
			if (0 != string.Compare(node.Name, "p", StringComparison.InvariantCultureIgnoreCase)) { return; }
			if (1 != node.Attributes.Count) { return; }

			var attr0 = node.Attributes[0];
			if (0 != string.Compare(attr0.Name, "style", StringComparison.InvariantCultureIgnoreCase)) { return; }

			var style = ParseStyle(attr0.Value);
			string resolvedHeading = ResolvedHeadingByStyle(style);
			if (null == resolvedHeading) { return; }

			node.Name = resolvedHeading;
			node.Attributes.RemoveAt(0);
		}
		string ResolvedHeadingByStyle(IDictionary<string, string> style) {
			if (StyleAreEquals(H6Style, style)) {
				return "h6";
			}
			else if (StyleAreEquals(H5Style, style)) {
				return "h5";
			}
			else if (StyleAreEquals(H4Style, style)) {
				return "h4";
			}
			else if (StyleAreEquals(H3Style, style)) {
				return "h3";
			}
			else if (StyleAreEquals(H2Style, style)) {
				return "h2";
			}
			else if (StyleAreEquals(H1Style, style)) {
				return "h1";
			}

			return null;
		}

		bool StyleAreEquals(IDictionary<string, string> expected, IDictionary<string, string> current) {
			if (current.Count != expected.Count) { return false; }

			foreach (var kp in current) {
				string expectedValue;
				if (false == expected.TryGetValue(kp.Key, out expectedValue)) {
					return false;
				}

				if (null == expectedValue) { expectedValue = string.Empty; }
				else { expectedValue = expectedValue.Trim(); }

				var currentValue = kp.Value;
				if (null == currentValue) { currentValue = string.Empty; }
				else { currentValue = currentValue.Trim(); }

				if (0 != string.Compare(expectedValue, currentValue, StringComparison.InvariantCultureIgnoreCase)) { return false; }
			}

			return true;
		}

		void RemoveOverlappingSpanInParagraphRecursively(HtmlNode parent) {
			foreach (var node in parent.ChildNodes.ToList()) {
				RemoveOverlappingSpanInParagraph(node);
			}

			RemoveOverlappingSpanInParagraph(parent);
		}
		void RemoveOverlappingSpanInParagraph(HtmlNode parent) {
			if (0 != string.Compare(parent.Name, "p", StringComparison.InvariantCultureIgnoreCase)) { return; }

			if (parent.ChildNodes.Count != 1) {
				return;
			}

			var child = parent.ChildNodes[0];
			if (0 != string.Compare(child.Name, "span", StringComparison.InvariantCultureIgnoreCase)) { return; }

			if (child.Attributes.Count > 1) { return; }

			var attr0 = child.Attributes[0];
			if (0 != string.Compare(attr0.Name, "style", StringComparison.InvariantCultureIgnoreCase)){ return; }

			var parentStyle = ParseStyle(parent);
			var spanStyle = ParseStyle(attr0.Value);
			foreach (var kp in spanStyle) {
				parentStyle[kp.Key] = kp.Value;
			}

			var finalStyle = ComposeStyle(parentStyle);
			parent.SetAttributeValue("style", finalStyle);
			parent.InnerHtml = child.InnerHtml;
		}

		string ComposeStyle(IDictionary<string, string> style) {
			var result = string.Join(";", style.Select(x => x.Key + ":" + x.Value));
			return result;
		}
		Dictionary<string, string> ParseStyle(HtmlNode node) {
			var style = node.GetAttributeValue("style", null);
			return ParseStyle(style);
		}
		Dictionary<string, string> ParseStyle(string style) {
			var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			if (false == string.IsNullOrWhiteSpace(style)) {
				var parts = style.Split(';');
				foreach (var part in parts) {
					var subParts = part.Split(new[] { ':' }, 2);
					var key = subParts[0].Trim();
					var value = subParts.Length > 1 ? subParts[1].Trim() : null;

					result[key] = value;
				}
			}

			return result;
		}

		void RemoveEmptyParagraphsRecursively(HtmlNode parent) {
			RemoveEmptyParagraphs(parent);
			foreach (var node in parent.ChildNodes) {
				RemoveEmptyParagraphsRecursively(node);
			}
		}
		void RemoveEmptyParagraphs(HtmlNode parent) {
			var nodes = parent.ChildNodes.ToArray();
			foreach (var node in nodes) {
				if (0 != string.Compare(node.Name, "p", StringComparison.InvariantCultureIgnoreCase)) {
					continue;
				}

				var innerHtml = node.InnerHtml.Replace("&nbsp;", string.Empty);
				if (string.IsNullOrWhiteSpace(innerHtml)) {
					parent.ChildNodes.Remove(node);
				}
			}
		}

		void RemoveForbiddenAttributesRecursively(HtmlNode node) {
			RemoveForbiddenAttributes(node);
			foreach (var child in node.ChildNodes) {
				RemoveForbiddenAttributesRecursively(child);
			}
		}
		void RemoveForbiddenAttributes(HtmlNode node) {
			RemoveAttributes(node, attr => ForbiddenAttributes.Contains(attr.Name));
		}

		void RemoveEmptyAttributesRecursively(HtmlNode node) {
			RemoveEmptyAttributes(node);
			foreach (var child in node.ChildNodes) {
				RemoveEmptyAttributesRecursively(child);
			}
		}
		void RemoveEmptyAttributes(HtmlNode node) {
			RemoveAttributes(node, attr => string.IsNullOrWhiteSpace(attr.Value));
		}

		void RemoveAttributes(HtmlNode node, Func<HtmlAttribute, bool> predicate) {
			var attributes = node.Attributes.ToArray();
			foreach (var attr in attributes) {
				if (predicate(attr)) {
					node.Attributes.Remove(attr);
				}
			}
		}

		void RemoveDefaultStylesRecursively(HtmlNode node) {
			RemoveDefaultStyles(node);
			foreach (var child in node.ChildNodes) {
				RemoveDefaultStylesRecursively(child);
			}
		}
		void RemoveDefaultStyles(HtmlNode node) {
			var attrStyle = node.Attributes["style"];
			if (null == attrStyle) { return; }

			var strStyle = attrStyle.Value;
			if (string.IsNullOrWhiteSpace(strStyle)) { return; }

			var parts = strStyle.Split(';');
			var allowedStyleParts = new List<string>();
			var removedStylePartsCounter = 0;
			foreach (var part in parts) {
				allowedStyleParts.Add(part);
				var subParts = part.Split(':');
				if (2 != subParts.Length) { continue; }

				var key = subParts[0];
				string defaultValue;
				if (!DefaultStyles.TryGetValue(key, out defaultValue)) {
					continue;
				}

				var styleValue = subParts[1];
				defaultValue = defaultValue.Trim();
				if (0 != string.Compare(styleValue, defaultValue, StringComparison.InvariantCultureIgnoreCase)) {
					continue;
				}

				allowedStyleParts.Remove(part);
				removedStylePartsCounter++;
			}

			if (removedStylePartsCounter > 0) {
				var newStyleValue = string.Join(";", allowedStyleParts);
				node.SetAttributeValue("style", newStyleValue);
			}
		}

		void RemoveWhitespaceNodes(HtmlNode parent) {
			RemoveNodes(parent, n => n is HtmlTextNode && string.IsNullOrWhiteSpace(n.InnerHtml));
		}
		void RemoveNodes(HtmlNode parent, Func<HtmlNode, bool> predicate) {
			var nodes = parent.ChildNodes.ToArray();
			foreach (var node in nodes) {
				if (predicate(node)) {
					parent.ChildNodes.Remove(node);
				}
			}
		}

		int RemoveWrapperNodesRecursively(HtmlNode parent, Func<HtmlNode, bool> predicate) {
			var result = 0;
			var subResult = 0;
			do {
				subResult = RemoveWrapperNodes(parent, predicate);
				result += subResult;
			} while (subResult > 0);

			return result;
		}
		int RemoveWrapperNodes(HtmlNode parent, Func<HtmlNode, bool> predicate) {
			int result = 0;

			var nodes = parent.ChildNodes.ToArray();
			foreach (var node in nodes) {
				if (predicate(node)) {
					var ndx = parent.ChildNodes.IndexOf(node);

					var innerNodes = node.ChildNodes.Reverse().ToArray();
					foreach (var innerNode in innerNodes) {
						node.ChildNodes.Remove(innerNode);
						parent.ChildNodes.Insert(ndx, innerNode);
					}

					parent.ChildNodes.Remove(node);
					result++;
				}
			}

			return result;
		}
	}
}
