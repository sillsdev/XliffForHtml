// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using HtmlAgilityPack;

namespace XliffForHtml
{
	/// <summary>
	/// This class provides methods to scan HTML files for text elements tagged with either a
	/// data-i18n attribute or an i18n attribute, either extracting information from the HTML
	/// to create an XLIFF file or replacing information in the HTML from that found in an
	/// XLIFF file.  (XLIFF is the "XML Localization Interchange File Format" developed by
	/// OASIS (Organization for the Advancement of Structured Information Standards).  This
	/// uses XLIFF 1.2 as defined by http://docs.oasis-open.org/xliff/v1.2/os/xliff-core.html.
	/// Inline markup inside text elements is handled according to the recommendations of
	/// http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html.
	/// </summary>
	public class HtmlXliff
	{
		public const string kXliffNamespace = "urn:oasis:names:tc:xliff:document:1.2";
		public const string kHtmlNamespace = "http://www.w3.org/TR/html";	// probably bogus but good enough?
		public const string kSilNamespace = "http://sil.org/software/XLiff";

		private readonly string _originalHtml;
		private readonly string _originalFilename;

		/// <summary>
		/// Document created by the Extract method.
		/// </summary>
		private XmlDocument _xliffDoc;

		/// <summary>
		/// Lookup map used by the InjectTranslations method.
		/// </summary>
		private Dictionary<string, string> _lookupTranslation = new Dictionary<string, string>();

		private string _targetLanguage;
		private XmlNamespaceManager _nsmgr;

		/// <summary>
		/// Users must create one of these objects with either Load or Parse, which either Loads an
		/// HTML file (and then parses it), or Parses an HTML string to initialize the object.
		/// </summary>
		private HtmlXliff(string html, string filename)
		{
			_originalHtml = html;
			_originalFilename = filename;

			// Fix an HtmlAgility parser bug: form isn't always empty. We can't use a newer version of HtmlAgility
			// which appears to fix this bug (and maybe others) because it won't work with either Mono 4 or even
			// Mono 5.  But the code leaves a gaping visibility hole that lets us fix it at runtime...
			// This must be done before creating/loading any HtmlAgilityPack.HtmlDocument objects.
			// We could avoid this hack by including the HtmlAgility code with the fix in it in this project's
			// solution.  But I'd rather use a NuGet package than clutter up our repository with borrowed code.
			// (replaces "HtmlElementFlag.CanOverlap | HtmlElementFlag.Empty")
			HtmlNode.ElementsFlags["form"] = HtmlElementFlag.CanOverlap;
		}

		/// <summary>
		/// Create a extractor object and initialize it by loading and parsing the specified HTML.
		/// </summary>
		public static HtmlXliff Load(string filename)
		{
			return new HtmlXliff(File.ReadAllText(filename, Encoding.UTF8), filename);
		}

		/// <summary>
		/// Create a extractor object and initialize it by by parsing the specified HTML string. The optional
		/// filename argument will be used as the "original" attribute of the "file" element in the XLIFF if
		/// we extract XLIFF from the HTML.
		/// </summary>
		public static HtmlXliff Parse(string html, string filename = "test.html")
		{
			return new HtmlXliff(html, filename);
		}

		/// <summary>
		/// This method scans an HTML file for text elements tagged with either a data-i18n or i18n attribute,
		/// extracting a translation unit of an XLIFF 1.2 file for each such element.  (XLIFF 1.2 is defined
		/// by http://docs.oasis-open.org/xliff/v1.2/os/xliff-core.html.)  Inline elements inside such tagged
		/// text elements are processed using the "maximalist" markup approach described by 
		/// http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html.
		/// </summary>
		/// <returns>
		/// XmlDocument object containing the XLIFF 1.2 format output, or null if nothing was tagged for
		/// translation
		/// </returns>
		public XmlDocument Extract()
		{
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(_originalHtml);

			_xliffDoc = new XmlDocument();
			var decl = _xliffDoc.CreateXmlDeclaration("1.0", "utf-8", null);
			_xliffDoc.AppendChild(decl);
			var schema = new XmlSchema();
			schema.Namespaces.Add("xmlns", kXliffNamespace);
			schema.Namespaces.Add("html", kHtmlNamespace);
			schema.Namespaces.Add("sil", kSilNamespace);
			_xliffDoc.Schemas.Add(schema);

			var xliff = _xliffDoc.CreateElement("xliff");
			_xliffDoc.AppendChild(xliff);
			xliff.SetAttribute("version", "1.2");
			xliff.SetAttribute("xmlns", kXliffNamespace);
			xliff.SetAttribute("xmlns:html", kHtmlNamespace);	// only attribute allowed a colon...
			xliff.SetAttribute("xmlns:sil", kSilNamespace);		// only attribute allowed a colon...

			var file = _xliffDoc.CreateElement("file");
			xliff.AppendChild(file);
			file.SetAttribute("original", _originalFilename);
			file.SetAttribute("datatype", "html");
			file.SetAttribute("source-language", "en");

			var xliffBody = _xliffDoc.CreateElement("body");
			file.AppendChild(xliffBody);

			// Process whatever we have at the top level.
			ProcessHtmlElement(htmlDoc.DocumentNode, xliffBody);
			var tuList = _xliffDoc.SelectNodes("/xliff/file/body/trans-unit");
			if (tuList.Count > 0)
				return _xliffDoc;
			return null;
		}

		private void ProcessHtmlElement(HtmlNode htmlElement, XmlElement xliffBody)
		{
			foreach (var node in htmlElement.ChildNodes)
			{
				if (!IsEmptyElement(node.Name) && ContainsTranslatableText(node))
				{
					var transUnit = ProcessTransUnit(node);
					if (transUnit != null)
						xliffBody.AppendChild(transUnit);
				}
				else if (node.NodeType == HtmlNodeType.Element)
				{
					ProcessHtmlElement(node, xliffBody);
				}
			}
		}

		/// <summary>
		/// HashSet to keep track of used id strings derived from html attributes.  If the id has
		/// already been used, we assume it's for the exact same string to translate.
		/// </summary>
		private HashSet<string> _idsUsed = new HashSet<string>();

		private XmlElement ProcessTransUnit(HtmlNode translatableNode)
		{
			var id = ExtractDataI18n(translatableNode);
			if (String.IsNullOrWhiteSpace(id) || _idsUsed.Contains(id))
				return null;
			_idsUsed.Add(id);
			XmlElement transUnit = _xliffDoc.CreateElement("trans-unit");
			transUnit.SetAttribute("id", id);

			var source = _xliffDoc.CreateElement("source");
			transUnit.AppendChild(source);
			source.SetAttribute("xml:lang", "en");
			ProcessSourceNode(source, translatableNode);

			var note = _xliffDoc.CreateElement("note");
			transUnit.AppendChild(note);
			note.AppendChild(_xliffDoc.CreateTextNode("ID: " + id));
			return transUnit;
		}

		private void ProcessSourceNode(XmlElement source, HtmlNode translatableNode)
		{
			foreach (var node in translatableNode.ChildNodes)
			{
				if (node.NodeType == HtmlNodeType.Text)
				{
					var text = HttpUtility.HtmlDecode(node.InnerText);
					var tn = _xliffDoc.CreateTextNode(text);
					source.AppendChild(tn);
				}
				else if (node.NodeType != HtmlNodeType.Element)
				{
					continue;
				}
				else if (IsEmptyElement(node.Name))
				{
					ProcessEmptyElement(source, node);
				}
				else
				{
					var gNode = _xliffDoc.CreateElement("g");
					source.AppendChild(gNode);
					gNode.SetAttribute("id", ExtractOrCreateIdValue(node));
					gNode.SetAttribute("ctype", GetXliffTypeForElement(node.Name));
					CopyHtmlAttributes(gNode, node, true);
					ProcessSourceNode(gNode, node);
				}
			}
		}

		private void ProcessEmptyElement(XmlElement source, HtmlNode node)
		{
			var xNode = _xliffDoc.CreateElement("x");
			source.AppendChild(xNode);
			xNode.SetAttribute("id", ExtractOrCreateIdValue(node));
			xNode.SetAttribute("ctype", GetXliffTypeForElement(node.Name));
			CopyHtmlAttributes(xNode, node);
			if (node.Name == "br")
				xNode.SetAttribute("equiv-text", "\n");
		}

		private string ExtractDataI18n(HtmlNode node)
		{
			var attr = node.Attributes["data-i18n"];
			if (attr == null || String.IsNullOrWhiteSpace(attr.Value))
			{
				attr = node.Attributes["i18n"];
				if (attr == null || String.IsNullOrWhiteSpace(attr.Value))
					return null;
			}
			return attr.Value;
		}

		/// <summary>
		/// Counter used to generate id values when no appropriate attribute exists (id / i18n / data-i18n).
		/// </summary>
		private int _idCounter;

		private string ExtractOrCreateIdValue(HtmlNode node)
		{
			var id = ExtractDataI18n(node);
			if (!String.IsNullOrWhiteSpace(id))
				return id;
			return String.Format("genid-{0}", ++_idCounter);
		}

		private void CopyHtmlAttributes(XmlElement xml, HtmlNode html, bool checkForTranslatableAttribute = false)
		{
			foreach (var attr in html.Attributes)
			{
				if (attr.Name == "lang")
					xml.SetAttribute("xml:" + attr.Name, attr.Value);	// colon allowed for xml:lang attribute
				else
					xml.SetAttribute(attr.Name, kHtmlNamespace, attr.Value);
			}
		}

		private bool ContainsTranslatableText(HtmlNode node)
		{
			if (node.NodeType != HtmlNodeType.Element)
				return false;	// ignor comments, text handled already in recursion
			if (node.ChildNodes.Count == 0 || node.InnerHtml == "")
				return false;	// empty node shouldn't produce a trans-unit

			// Check whether any #text children have nonwhitespace characters.
			// If so, then we're at a node with translatable text.
			foreach (var tnode in node.Elements("#text"))
			{
				if (!String.IsNullOrWhiteSpace(tnode.InnerText))
					return true;
			}
			// Check whether all of the children that aren't #text or #comment
			// are inline elements.  If so, we can create a trans-unit.
			foreach (var tnode in node.ChildNodes)
			{
				if (tnode.NodeType != HtmlNodeType.Element)
					continue;
				if (!IsInlineElement(tnode.Name))
					return false;
				if (IsWrapperElement(tnode.Name))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check whether the named HTML element can be an inline element.
		/// These values come from the XLIFF 1.2 Representation Guide for HTML.
		/// (http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html#SectionDetailsElements)
		/// </summary>
		private bool IsInlineElement(string name)
		{
			switch (name)
			{
			case "a":
			case "abbr":
			case "acronym":
			case "applet":
			case "b":
			case "bdo":
			case "big":
			case "blink":
			case "br":
			case "button":
			case "cite":
			case "code":
			case "del":
			case "dfn":
			case "em":
			case "embed":
			case "face":
			case "font":
			case "i":
			case "iframe":
			case "img":
			case "input":
			case "ins":
			case "kbd":
			case "label":
			case "map":
			case "nobr":
			case "object":
			case "param":
			case "q":
			case "rb":
			case "rbc":
			case "rp":
			case "rt":
			case "rtc":
			case "ruby":
			case "s":
			case "samp":
			case "select":
			case "small":
			case "spacer":
			case "span":
			case "strike":
			case "strong":
			case "sub":
			case "sup":
			case "symbol":
			case "textarea":
			case "tt":
			case "u":
			case "var":
			case "wbr":
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check whether the named HTML element can "wrap a group".
		/// These values come from the XLIFF 1.2 Representation Guide for HTML.
		/// (http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html#SectionDetailsElements)
		/// </summary>
		private bool IsWrapperElement(string name)
		{
			switch (name)
			{
			case "a":
			case "applet":
			case "blockquote":
			case "body":
			case "colgroup":
			case "dir":
			case "dl":
			case "fieldset":
			case "form":
			case "head":
			case "html":
			case "menu":
			case "noembed":
			case "noframes":
			case "noscript":
			case "object":
			case "ol":
			case "optgroup":
			case "select":
			case "table":
			case "tbody":
			case "tfoot":
			case "thead":
			case "tr":
			case "ul":
			case "xml":
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check whether the named HTML element is always an empty element.
		/// These values come from the XLIFF 1.2 Representation Guide for HTML.
		/// (http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html#SectionDetailsElements)
		/// </summary>
		private bool IsEmptyElement(string name)
		{
			switch (name)
			{
			case "area":
			case "base":
			case "basefont":
			case "bgsound":
			case "br":
			case "col":
			case "frame":
			case "hr":
			case "img":
			case "input":
			case "isindex":
			case "link":
			case "meta":
			case "nobr":
			case "param":
			case "spacer":
			case "wbr":
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Get the ctype (or restype if not inline) value for the given HTML element name.
		/// These values come from the XLIFF 1.2 Representation Guide for HTML.
		/// (http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html#SectionDetailsElements)
		/// </summary>
		private string GetXliffTypeForElement(string name)
		{
			switch (name)
			{
			case "b":			return "bold";
			case "br":			return "lb";	// schema doesn't like this for restype, but does for ctype
			case "caption":		return "caption";
			case "fieldset":	return "groupbox";
			case "form":		return "dialog";
			case "frame":		return "frame";
			case "head":		return "header";
			case "i":			return "italic";
			case "img":			return "image";	// schema doesn't like this for restype, but does for ctype
			case "li":			return "listitem";
			case "menu":		return "menu";
			case "table":		return "table";
			case "td":			return "cell";
			case "tfoot":		return "footer";
			case "tr":			return "row";
			case "u":			return "underlined";
			default:			return "x-html-" + name;
			}
		}

		/// <summary>
		/// Injects the translations from the xliffFile into the HTML loaded by the Create or Parse
		/// factory methods.  The modified HtmlDocument object is returned.
		/// </summary>
		public HtmlDocument InjectTranslations(string xliffFile)
		{
			var xliffDoc = new XmlDocument();
			xliffDoc.Load(xliffFile);
			return InjectTranslations(xliffDoc);
		}

		/// <summary>
		/// Injects the translations from the xliff XML object into the HTML loaded by the Create or Parse
		/// factory methods.  The modified HtmlDocument object is returned.
		/// </summary>
		public HtmlDocument InjectTranslations(XmlDocument xliff)
		{
			_nsmgr = new XmlNamespaceManager(xliff.NameTable);
			_nsmgr.AddNamespace("x", kXliffNamespace);
			_nsmgr.AddNamespace("html", kHtmlNamespace);
			_nsmgr.AddNamespace("sil", kSilNamespace);
			PreProcessXliff(xliff);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(_originalHtml);
			TranslateHtmlElement(htmlDoc.DocumentNode);
			return htmlDoc;
		}

		private void TranslateHtmlElement(HtmlNode htmlElement)
		{
			foreach (var node in htmlElement.ChildNodes)
			{
				if (!IsEmptyElement(node.Name) && ContainsTranslatableText(node))
				{
					var id = ExtractDataI18n(node);
					if (String.IsNullOrWhiteSpace(id))
						continue;
					string translation;
					if (_lookupTranslation.TryGetValue(id, out translation))
					{
						node.InnerHtml = translation;
						if (!String.IsNullOrWhiteSpace(_targetLanguage))
						{
							node.SetAttributeValue("lang", _targetLanguage);
							node.SetAttributeValue("xml:lang", _targetLanguage);
						}
					}
				}
				else if (node.NodeType == HtmlNodeType.Element)
				{
					TranslateHtmlElement(node);
				}
			}
		}

		private void PreProcessXliff(XmlDocument xliffDoc)
		{
			foreach (XmlNode tu in xliffDoc.SelectNodes("/x:xliff/x:file/x:body//x:trans-unit", _nsmgr))
			{
				var id = tu.Attributes["id"].Value;
				if (_lookupTranslation.ContainsKey(id))
					continue;
				var target = tu.SelectSingleNode("x:target", _nsmgr);
				if (target != null)
				{
					var translation = ProcessTarget(target);
					if (!String.IsNullOrWhiteSpace(translation))
						_lookupTranslation.Add(id, translation);
				}
			}
			var targetLang = xliffDoc.SelectSingleNode("/x:xliff/x:file/@target-language", _nsmgr);
			if (targetLang != null)
				_targetLanguage = targetLang.Value;
		}

		private string ProcessTarget(XmlNode target)
		{
			var bldr = new StringBuilder();
			foreach (XmlNode node in target.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Text)
				{
					bldr.Append(node.InnerText);
				}
				else if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
					case "g":
						bldr.Append("<");
						var elementName = GetElementNameFromCtype(node.Attributes["ctype"].Value);
						bldr.Append(elementName);
						AppendHtmlAttributes(bldr, node);
						bldr.Append(">");
						bldr.Append(ProcessTarget(node));
						bldr.AppendFormat("</{0}>", elementName);
						break;
					case "x":
						bldr.Append("<");
						bldr.Append(GetElementNameFromCtype(node.Attributes["ctype"].Value));
						AppendHtmlAttributes(bldr, node);
						bldr.Append("/>");
						break;
					default:
						break;
					}
				}
			}
			return bldr.ToString();
		}

		private void AppendHtmlAttributes(StringBuilder bldr, XmlNode node)
		{
			foreach (XmlAttribute attr in node.Attributes)
			{
				if (attr.Name == "xml:lang")
				{
					bldr.AppendFormat(" lang=\"{0}\"", attr.Value);
				}
				else if (attr.Name.StartsWith("html:"))
				{
					// TODO: protect against double quotes in the attribute value.
					bldr.AppendFormat(" {0}=\"{1}\"", attr.Name.Substring(5), attr.Value);
				}
			}
		}

		private string GetElementNameFromCtype(string ctype)
		{
			if (ctype.StartsWith("x-html-"))
				return ctype.Substring(7);
			switch (ctype)
			{
			case "bold":		return "b";
			case "lb":			return "br";
			case "caption":		return "caption";
			case "groupbox":	return "fieldset";
			case "dialog":		return "form";
			case "frame":		return "frame";
			case "header":		return "head";
			case "italic":		return "i";
			case "image":		return "img";
			case "listitem":	return "li";
			case "menu":		return "menu";
			case "table":		return "table";
			case "row":			return "tr";
			case "cell":		return "td";
			case "footer":		return "tfoot";
			case "underlined":	return "u";
			default:
				return null;
			}
		}
	}
}

