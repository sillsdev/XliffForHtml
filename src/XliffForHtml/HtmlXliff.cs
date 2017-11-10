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
	/// <remarks>
	/// Note that this exceedingly ethnocentric code assumes English is always the source language.
	/// This could be changed if the need ever arises, probably by adding a command line argument
	/// for extracting and paying attention to the recorded source-language attribute for injection.
	/// </remarks>
	public class HtmlXliff
	{
		public const string kXliffNamespace = "urn:oasis:names:tc:xliff:document:1.2";
		public const string kHtmlNamespace = "http://www.w3.org/TR/html";	// probably bogus but good enough?
		public const string kSilNamespace = "http://sil.org/software/XLiff";

		private readonly string _originalHtml;
		/// <summary>
		/// The simple filename (without the full path) used for the "original" attribute.
		/// </summary>
		private readonly string _originalFilename;

		/// <summary>
		/// Document created by the Extract method.
		/// </summary>
		private XmlDocument _xliffDoc;

		/// <summary>
		/// Lookup map used by the InjectTranslations method.
		/// The keys are id strings derived from data-i18n, i81n, or id attributes in the HTML file
		/// and stored as the id attributes of trans-unit elements in the xliff file.
		/// The values are string representations of HTML fragments, often (usually?) just text but
		/// possibly with inline HTML markup.
		/// </summary>
		private Dictionary<string, string> _lookupTranslation = new Dictionary<string, string>();
		/// <summary>
		/// The target language ISO-639-1 code used in the InjectTranslation method.
		/// </summary>
		private string _targetLanguage;
		/// <summary>
		/// The name space manager needed to deal with XPath searches in the InjectTranslations method.
		/// </summary>
		private XmlNamespaceManager _nsmgr;
		/// <summary>
		/// Flag that the target language is written right to left.  This needs to be recorded in
		/// the output HTML file.
		/// </summary>
		private bool _rtl;

		private bool _verboseWarnings;

		/// <summary>
		/// Users must create one of these objects with either Load or Parse, which either Loads an
		/// HTML file (and then parses it), or Parses an HTML string to initialize the object.
		/// </summary>
		private HtmlXliff(string html, string filename)
		{
			_originalHtml = html;
			_originalFilename = Path.GetFileName(filename);
			// Crowdin behaves very badly if the original attribute ever changes.  (It deletes all the translations.)
			// Bloom has two files that got created and added to crowdin when this program momentarily handled markdown
			// input.  We decided that was a mistake because every markdown processor handles extensions differently
			// and the best processor we've found so far is in javascript and thus most useful at build time instead
			// of runtime.  To prevent possible problems in managing Bloom translations, we preserve these two
			// filenames as being markdown instead of HTML.  (This is what happens when people insist on flying the
			// airplane while it's still being built...)
			if (filename.Replace("\\","/").EndsWith("/DistFiles/IntegrityFailureAdvice-en.htm") ||
				filename.Replace("\\","/").EndsWith("/DistFiles/infoPages/TrainingVideos-en.htm"))
			{
				_originalFilename = Path.ChangeExtension(_originalFilename, "md");
			}
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
		/// Create a HtmlXliff object and initialize it by loading and parsing the specified HTML file.
		/// </summary>
		public static HtmlXliff Load(string filename)
		{
			return Parse(File.ReadAllText(filename, Encoding.UTF8), filename);
		}

		/// <summary>
		/// Create a HtmlXliff object and initialize it by by parsing the specified HTML string. The optional
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
				string id;
				if (!IsEmptyElement(node.Name) && TryExtractDataI18n(node, out id) && ContainsTranslatableText(node))
				{
					if (_idsUsed.Contains(id))
						continue;
					_idsUsed.Add(id);
					// See the description of GetProperNodesToProcess for why we need this method call
					// and the processing of its output list.
					List<HtmlNode> recurseNodes;	// child nodes to recurse on, usually none.
					var intlNode = GetProperNodesToProcess(node, out recurseNodes);
					var transUnit = ProcessTransUnit(intlNode, id);
					if (transUnit != null)
						xliffBody.AppendChild(transUnit);
					foreach (var child in recurseNodes)
						ProcessHtmlElement(child, xliffBody);
				}
				else if (node.NodeType == HtmlNodeType.Element)
				{
					ProcessHtmlElement(node, xliffBody);
				}
			}
		}

		/// <summary>
		/// Raw HTML and PUG allow us to put i18n attributes exactly where we want them, where we would expect
		/// them to be.  Markdown is far more limited.  It originally didn't allow attributes at all.  Extensions
		/// have been written to allow attributes but these differ in how they work between different markdown
		/// processors.  The best one we've found so far is markdown-it (javascript/node.js).  The problematic
		/// area remaining is with list items, and especially with lists embedded inside list items.  markdown-it
		/// allows list items to have attributes, and places the attributes directly on the list item (li element)
		/// itself.  The text of the list item is then embedded inside a paragraph (p element), at least when
		/// translating from a * markup.  Any sublist inside the list item follows that paragraph as either a ul
		/// or ol element containing its own li elements.  Raw HTML and PUG would presumably place the i18n
		/// attribute exactly where it is needed rather than one level above.  This breaks our normal simple
		/// recursive scan through the HTML, and also makes the XLIFF source data look strange if we don't
		/// drill down to the paragraph (p element) containing the text.
		/// Detecting this situation and providing the needed data to deal with it is what this method does.
		/// </summary>
		/// <returns>
		/// <c>node</c> if nothing special is needed (and <c>recurseNodes</c> will be empty in that case), or
		/// the first child element of node if appropriate, with other child elements that need further
		/// processing added to <c>recurseNodes</c>
		/// </returns>
		/// <param name="node">node that contains text we want to localize (tagged with i18n attribute)</param>
		/// <param name="recurseNodes">output child nodes that need further processing, if any</param>
		/// <remarks>
		/// Fortunately this method can be used both for extracting strings and injecting translations.
		/// "i18n attributes" in the summary above includes both i18n and data-i18n attributes.
		/// </remarks>
		private HtmlNode GetProperNodesToProcess(HtmlNode node, out List<HtmlNode> recurseNodes)
		{
			recurseNodes = new List<HtmlNode>();
			if (node.Name.ToLowerInvariant() != "li")
				return node;
			HtmlNode intlNode = null;
			foreach (var child in node.ChildNodes)
			{
				if (intlNode == null)
				{
					if (child.NodeType == HtmlNodeType.Text && String.IsNullOrWhiteSpace(child.InnerHtml))
						continue;
					// Markdown-it places the list item text in a bare paragraph element.
					if (child.Name.ToLowerInvariant() != "p" || child.Attributes.Count != 0)
						return node;
					// There's no need to copy the i18n attribute to the p element because it's already
					// served its purpose by triggering this code.
					intlNode = child;
				}
				else
				{
					if (child.NodeType == HtmlNodeType.Text && String.IsNullOrWhiteSpace(child.InnerHtml))
						continue;
					var childName = child.Name.ToLowerInvariant();
					// markdown-it doesn't actually handle placing i18n attributes on the second (or third
					// or ...) paragraph element inside a list item, but in case they ever fix that and we
					// ever make more complicated Markdown files that need it, we include checking for p here.
					if (childName == "ol" || childName == "ul" || childName == "p")
						recurseNodes.Add(child);
				}
			}
			if (intlNode == null)
				return node;	// shouldn't be possible, but paranoia never hurts.
			return intlNode;
		}

		/// <summary>
		/// HashSet to keep track of used id strings derived from html attributes.  If the id has
		/// already been used, we assume it's for the exact same string to translate.
		/// </summary>
		private HashSet<string> _idsUsed = new HashSet<string>();

		private XmlElement ProcessTransUnit(HtmlNode translatableNode, string id)
		{
			XmlElement transUnit = _xliffDoc.CreateElement("trans-unit");
			transUnit.SetAttribute("id", id);

			var newXliffSource = _xliffDoc.CreateElement("source");
			transUnit.AppendChild(newXliffSource);
			newXliffSource.SetAttribute("xml:lang", "en");
			ProcessSourceNode(translatableNode, newXliffSource);

			// crowdin creates the target element after all existing children elements if it
			// doesn't already exist.  The XLIFF 1.2 schema requires the target element to come
			// after the source and before any note elements.  They suggested creating empty
			// target elements as placeholders.  This may not matter in practice, but I don't
			// like having invalid XLIFF even though it's valid XML and probably handled okay
			// since we don't use a validating parser except in tests.
			var target = _xliffDoc.CreateElement("target");
			transUnit.AppendChild(target);

			var note = _xliffDoc.CreateElement("note");
			transUnit.AppendChild(note);
			note.AppendChild(_xliffDoc.CreateTextNode("ID: " + id));
			return transUnit;
		}

		private void ProcessSourceNode(HtmlNode translatableNode, XmlElement newXliffElement)
		{
			foreach (var node in translatableNode.ChildNodes)
			{
				if (node.NodeType == HtmlNodeType.Text)
				{
					var text = HttpUtility.HtmlDecode(node.InnerText);
					var tn = _xliffDoc.CreateTextNode(text);
					newXliffElement.AppendChild(tn);
				}
				else if (node.NodeType != HtmlNodeType.Element)
				{
					continue;
				}
				else if (IsEmptyElement(node.Name))
				{
					ProcessEmptyInlineElement(node, newXliffElement);
				}
				else
				{
					ProcessInlineElement(node, newXliffElement);
				}
			}
		}

		private void ProcessInlineElement(HtmlNode node, XmlElement newXliffElement)
		{
			// XLIFF uses "g" elements to represent inline elements that have content,
			// either text or even other elements, like <span> or <i>.
			var gNode = _xliffDoc.CreateElement("g");
			newXliffElement.AppendChild(gNode);
			gNode.SetAttribute("id", ExtractOrCreateIdValue(node));
			gNode.SetAttribute("ctype", GetXliffCTypeForElement(node.Name));
			CopyHtmlAttributes(gNode, node);
			ProcessSourceNode(node, gNode);
		}

		private void ProcessEmptyInlineElement(HtmlNode node, XmlElement newXliffElement)
		{
			// XLIFF uses "x" elements to represent inline elements that are always empty,
			// like <br> or <img>
			var xNode = _xliffDoc.CreateElement("x");
			newXliffElement.AppendChild(xNode);
			xNode.SetAttribute("id", ExtractOrCreateIdValue(node));
			xNode.SetAttribute("ctype", GetXliffCTypeForElement(node.Name));
			CopyHtmlAttributes(xNode, node);
			if (node.Name == "br")
				xNode.SetAttribute("equiv-text", "\n");
		}

		private bool TryExtractDataI18n(HtmlNode node, out string id)
		{
			id = null;
			var attr = node.Attributes["data-i18n"];
			if (attr == null || String.IsNullOrWhiteSpace(attr.Value))
			{
				attr = node.Attributes["i18n"];
				if (attr == null || String.IsNullOrWhiteSpace(attr.Value))
					return false;
			}
			id = attr.Value;
			return true;
		}

		/// <summary>
		/// Counter used to generate id values when no appropriate attribute exists (id / i18n / data-i18n).
		/// </summary>
		private int _idCounter;

		private string ExtractOrCreateIdValue(HtmlNode node)
		{
			string id;
			if (TryExtractDataI18n(node, out id))
				return id;
			return String.Format("genid-{0}", ++_idCounter);
		}

		private void CopyHtmlAttributes(XmlElement xml, HtmlNode html)
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
				return false;	// ignore comments, text handled already in recursion
			foreach (var tnode in node.ChildNodes)
			{
				switch (tnode.NodeType)
				{
				case HtmlNodeType.Text:
					// If we have non-empty text, return true;
					if (!String.IsNullOrWhiteSpace(tnode.InnerText))
						return true;
					break;
				case HtmlNodeType.Element:
					if (ContainsTranslatableText(tnode))
						return true;
					break;
				}
			}
			return false;
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
		/// Get the ctype value for the given HTML element name.
		/// These values come from the XLIFF 1.2 Representation Guide for HTML.
		/// (http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html#SectionDetailsElements)
		/// </summary>
		private string GetXliffCTypeForElement(string name)
		{
			switch (name)
			{
			case "b":			return "bold";
			case "br":			return "lb";
			case "caption":		return "caption";
			case "fieldset":	return "groupbox";
			case "form":		return "dialog";
			case "frame":		return "frame";
			case "head":		return "header";
			case "i":			return "italic";
			case "img":			return "image";
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
		public HtmlDocument InjectTranslations(string xliffFile, bool verboseWarnings)
		{
			var xliffDoc = new XmlDocument();
			xliffDoc.Load(xliffFile);
			return InjectTranslations(xliffDoc, verboseWarnings);
		}

		/// <summary>
		/// Injects the translations from the xliff XML object into the HTML loaded by the Create or Parse
		/// factory methods.  The modified HtmlDocument object is returned.
		/// </summary>
		public HtmlDocument InjectTranslations(XmlDocument xliff, bool verboseWarnings)
		{
			_nsmgr = new XmlNamespaceManager(xliff.NameTable);
			_nsmgr.AddNamespace("x", kXliffNamespace);
			_nsmgr.AddNamespace("html", kHtmlNamespace);
			_nsmgr.AddNamespace("sil", kSilNamespace);
			_verboseWarnings = verboseWarnings;
			PreProcessXliff(xliff);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(_originalHtml);
			if (_rtl)
				InsertRtlDiv(htmlDoc);
			TranslateHtmlElement(htmlDoc.DocumentNode);
			return htmlDoc;
		}

		private void InsertRtlDiv(HtmlDocument htmlDoc)
		{
			var divNode = htmlDoc.CreateElement("div");
			divNode.SetAttributeValue("dir", "rtl");
			divNode.SetAttributeValue("lang", _targetLanguage);
			divNode.SetAttributeValue("xml:lang", _targetLanguage);
			var docNode = htmlDoc.DocumentNode;
			HtmlNode bodyNode;
			if (docNode.FirstChild.Name == "html")
			{
				bodyNode = docNode.SelectSingleNode("/html/body");
			}
			else if (docNode.FirstChild.Name == "body")
			{
				bodyNode = docNode.FirstChild;
			}
			else
			{
				bodyNode = docNode;
			}
			divNode.AppendChildren(bodyNode.ChildNodes);
			bodyNode.ChildNodes.Clear();
			bodyNode.AppendChild(divNode);
		}

		private void TranslateHtmlElement(HtmlNode htmlElement)
		{
			foreach (var node in htmlElement.ChildNodes)
			{
				string id;
				if (!IsEmptyElement(node.Name) && TryExtractDataI18n(node, out id) && ContainsTranslatableText(node))
				{
					string translation;
					// See the description of GetProperNodesToProcess for why we need this method call
					// and the processing of its output list.
					List<HtmlNode> recurseNodes;	// child nodes to recurse on, usually none.
					var intlNode = GetProperNodesToProcess(node, out recurseNodes);
					if (_lookupTranslation.TryGetValue(id, out translation))
					{
						intlNode.InnerHtml = translation;
						if (!String.IsNullOrWhiteSpace(_targetLanguage))
						{
							intlNode.SetAttributeValue("lang", _targetLanguage);
							intlNode.SetAttributeValue("xml:lang", _targetLanguage);
						}
						if (_rtl)
							intlNode.SetAttributeValue("dir", "rtl");
					}
					else if (_verboseWarnings)
					{
						Console.WriteLine("Warning: cannot find translated string for id = \"{0}\"", id);
						if (intlNode != null && intlNode.Attributes != null)
						{
							if (intlNode.Attributes["lang"] == null && intlNode.Attributes["xml:lang"] == null)
							{
								intlNode.SetAttributeValue("lang", "en");
								intlNode.SetAttributeValue("xml:lang", "en");
							}
							if (_rtl && intlNode.Attributes["dir"] == null)
								intlNode.SetAttributeValue("dir", "ltr");
						}
					}
					foreach (var child in recurseNodes)
						TranslateHtmlElement(child);
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
			{
				_targetLanguage = targetLang.Value;
				_rtl = IsLanguageRtl(_targetLanguage);
			}
		}

		private bool IsLanguageRtl(string lang)
		{
			switch (lang)
			{
			// Check for languages known to be written exclusively (or by default) RTL
			// This list is not exhaustive, but covers major languages that might possibly
			// be localized (and some that are not very likely).
			case "ar":	// Arabic
			case "az":	// Azeri/Azerbaijani
			case "fa":	// Farsi/Persian
			case "kk":	// Kazakh
			case "ku":	// Kurdish
			case "pa":	// Panjabi/Punjabi
			case "ps":	// Pashto/Pushto
			case "sd":	// Sindhi
			case "ur":	// Urdu
			case "he":	// Hebrew
			case "dv":	// Divehi/Dhivehi/Maldivian
			case "pbu":	// Northern Pashto
			case "prs":	// Dari
				return true;
			default:
				// Check for known RTL scripts expressly contained in the language code
				return (lang.Contains("-Adlm") ||								// Adlam
						lang.Contains("-Arab") || lang.Contains("-Aran") ||		// Arabic variants
						lang.Contains("-Hebr") ||								// Hebrew
						lang.Contains("-Mand") ||								// Mandaic, Mandaean
						lang.Contains("-Mend") ||								// Mende Kikakui
						lang.Contains("-Nkoo") ||								// N’Ko
						lang.Contains("-Samr") ||								// Samaritan
						lang.Contains("-Syrc") || lang.Contains("-Syre") || lang.Contains("-Syrj") || lang.Contains("-Syrn") ||	// Syriac variants
						lang.Contains("-Thaa"));								// Thaana
			}
		}

		private string ProcessTarget(XmlNode target)
		{
			var bldr = new StringBuilder();
			foreach (XmlNode node in target.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Text)
				{
					bldr.Append(node.OuterXml);		// OuterXml quotes character entities the way we need them.
				}
				else if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
					case "g":
						bldr.Append("<");
						var elementName = GetElementNameFromXliffCtype(node.Attributes["ctype"].Value);
						bldr.Append(elementName);
						AppendHtmlAttributes(bldr, node);
						bldr.Append(">");
						bldr.Append(ProcessTarget(node));
						bldr.AppendFormat("</{0}>", elementName);
						break;
					case "x":
						bldr.Append("<");
						bldr.Append(GetElementNameFromXliffCtype(node.Attributes["ctype"].Value));
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
					bldr.AppendFormat(" {0}=\"{1}\"", attr.Name.Substring(5), FixNeededCharacterEntities(attr.Value));
				}
			}
		}

		private string FixNeededCharacterEntities(string attr)
		{
			return attr.Replace("&", "&amp;")
				.Replace("\"", "&quot;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;");
		}

		private string GetElementNameFromXliffCtype(string ctype)
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

