// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using HtmlAgilityPack;
using XliffForHtml;
using System.Text;

namespace XliffForHtmlTests
{
	[TestFixture]
	public class TestExtractXliff
	{
		[Test]
		public void TestBareAmpersand()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""1 & 2 & 3"">One & two & three & etc.</p>
</body>
</html>");
			/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""1 &amp; 2 &amp; 3"">
    <source xml:lang=""en"">One &amp; two &amp; three &amp; etc.</source>
    <note>ID: 1 &amp; 2 &amp; 3</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestBrBetweenParas did not validate against schema: {0}");

			var file = xmlDoc.SelectSingleNode("/xliff/file");
			Assert.IsNotNull(file);
			Assert.AreEqual(3, file.Attributes.Count);
			Assert.AreEqual("test.html", file.Attributes["original"].Value);
			Assert.AreEqual("html", file.Attributes["datatype"].Value);
			Assert.AreEqual("en", file.Attributes["source-language"].Value);

			var body = file.SelectSingleNode("body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "1 & 2 & 3" },
				new [] { "One &amp; two &amp; three &amp; etc." });
		}

		[Test]
		public void TestBrBetweenParas()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Test 1"">This is a test.</p>
<br/>
<p data-i18n=""Test 2"">This is only a test.</p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Test 1"">
    <source xml:lang=""en"">This is a test.</source>
    <note>ID: Test 1</note>
   </trans-unit>
   <trans-unit id=""Test 2"">
    <source xml:lang=""en"">This is only a test.</source>
    <note>ID: Test 2</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestBrBetweenParas did not validate against schema: {0}");

			var file = xmlDoc.SelectSingleNode("/xliff/file");
			Assert.IsNotNull(file);
			Assert.AreEqual(3, file.Attributes.Count);
			Assert.AreEqual("test.html", file.Attributes["original"].Value);
			Assert.AreEqual("html", file.Attributes["datatype"].Value);
			Assert.AreEqual("en", file.Attributes["source-language"].Value);

			var body = file.SelectSingleNode("body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "Test 1", "Test 2" },
				new [] { "This is a test.", "This is only a test." });
		}

		[Test]
		public void TestBrInText()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Two.Lines"">First line<br>second line</p>
</body>
</html>");
/* expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Two.Lines"">
    <source xml:lang=""en"">First line<x id=""genid-1"" ctype=""lb"" equiv-text=""&#xA;"" />second line</source>
    <note>ID: Two.Lines</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestBrInText did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			Assert.AreEqual(1, body.ChildNodes.Count);

			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Two.Lines", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(3, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("First line", n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("x", n1.Name);
			Assert.AreEqual(3, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("lb", n1.Attributes["ctype"].Value);
			Assert.AreEqual("\n", n1.Attributes["equiv-text"].Value);
			var n2 = source.ChildNodes[2];
			Assert.AreEqual(XmlNodeType.Text, n2.NodeType);
			Assert.AreEqual("second line", n2.InnerText);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestClassAttributeIsIgnored()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<h2 class=""Article-Title"" i18n=""article-title"">Life and Habitat of the Marmot</h2>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""article-title"">
    <source xml:lang=""en"">Life and Habitat of the Marmot</source>
    <note>ID: article-title</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestClassAttribute did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "article-title" },
				new [] { "Life and Habitat of the Marmot" });
		}

		[Test]
		public void TestDuplicateI18nStrings()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <table class=""statistics clear"" style=""margin-left: 6px"">
   <tr>
    <td class=""tableTitle thisPageSection"" data-i18n=""EditTab.Toolbox.LeveledReaderTool.ThisPage"">This Page</td>
   </tr>
   <tr>
    <td class=""statistics-max"" data-i18n=""EditTab.Toolbox.LeveledReaderTool.Max"">Maximum</td>
   </tr>
  </table>
  <table class=""statistics clear"" style=""margin-left: 6px"">
   <tr>
    <td class=""tableTitle"" data-i18n=""EditTab.Toolbox.LeveledReaderTool.ThisBook"">This Book</td>
   </tr>
   <tr>
    <td class=""statistics-max"" data-i18n=""EditTab.Toolbox.LeveledReaderTool.Max"">Maximum</td>
   </tr>
  </table>
 </body>
</html>");
/*expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.ThisPage"">
    <source xml:lang=""en"">This Page</source>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.Max"">
    <source xml:lang=""en"">Maximum</source>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.ThisBook"">
    <source xml:lang=""en"">This Book</source>
   </trans-unit>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestDuplicateI18nStrings did not validate against schema: {0}");
			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "EditTab.Toolbox.LeveledReaderTool.ThisPage", "EditTab.Toolbox.LeveledReaderTool.Max", "EditTab.Toolbox.LeveledReaderTool.ThisBook" },
				new [] { "This Page", "Maximum", "This Book" });
		}

		[Test]
		public void TestFormInDiv()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <head>
  <script src=""/bloom/bookEdit/toolbox/bookSettings/bookSettings.js""></script>
  <link rel=""stylesheet"" href=""/bloom/bookEdit/toolbox/bookSettings/bookSettings.css""/>
 </head>
 <body>
  <h3 data-panelId=""bookSettingsTool"" data-order=""100""><img src=""/bloom/bookEdit/toolbox/bookSettings/icon.svg""/></h3>
  <div data-panelId=""bookSettingsTool"">
   <form id=""bookSettings"">
    <div class=""showOnlyWhenBookWouldNormallyBeLocked"">
     <p data-i18n=""EditTab.Toolbox.Settings.UnlockShellBookIntroductionText"">Bloom normally prevents most changes to shellbooks. If you need to add pages, change images, etc., tick the box below.</p>
     <input type=""checkbox"" name=""unlockShellBook"" onClick=""FrameExports.handleBookSettingCheckboxClick(this);""/>
     <label data-i18n=""EditTab.Toolbox.Settings.Unlock"">Allow changes to this shellbook</label>
    </div>
   </form>
  </div>
 </body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""EditTab.Toolbox.Settings.UnlockShellBookIntroductionText"">
    <source xml:lang=""en"">Bloom normally prevents most changes to shellbooks. If you need to add pages, change images, etc., tick the box below.</source>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.Settings.Unlock"">
    <source xml:lang=""en"">Allow changes to this shellbook</source>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestFormInDiv did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "EditTab.Toolbox.Settings.UnlockShellBookIntroductionText", "EditTab.Toolbox.Settings.Unlock" },
				new [] { "Bloom normally prevents most changes to shellbooks. If you need to add pages, change images, etc., tick the box below.", "Allow changes to this shellbook" });
		}

		[Test]
		public void TestHtmlFragment()
		{
			var extractor = HtmlXliff.Parse(@"
<div class=""bloom-ui bloomDialogContainer"" id=""text-properties-dialog"" style=""visibility: hidden;"">
  <div class=""bloomDialogTitleBar"" data-i18n=""EditTab.TextBoxProperties.Title"">Text Box Properties</div>
  <div class=""hideWhenFormattingEnabled bloomDialogMainPage"">
    <p data-i18n=""BookEditor.FormattingDisabled"">Sorry, Reader Templates do not allow changes to formatting.</p>
  </div>
</div>
");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
    <trans-unit id=""EditTab.TextBoxProperties.Title"">
     <source xml:lang=""en"">Text Box Properties</source>
    </trans-unit>
    <trans-unit id=""BookEditor.FormattingDisabled"">
     <source xml:lang=""en"">Sorry, Reader Templates do not allow changes to formatting.</source>
    </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestHtmlFragment did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			CheckTransUnits(body,
				new [] { "EditTab.TextBoxProperties.Title", "BookEditor.FormattingDisabled" },
				new [] { "Text Box Properties", "Sorry, Reader Templates do not allow changes to formatting." });
		}

		[Test]
		public void TestImg()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n='Mount Hood'>This is Mount Hood: <img src=""mthood.jpg""></p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">This is Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" /></source>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestImg did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			Assert.AreEqual(1, body.ChildNodes.Count);

			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Mount Hood", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(2, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("This is Mount Hood: ", n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("x", n1.Name);
			Assert.AreEqual(3, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("image", n1.Attributes["ctype"].Value);
			Assert.AreEqual("mthood.jpg", n1.Attributes["src", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestImgWithAlt()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Mount Hood"">My picture,
<img src=""mthood.jpg"" alt=""This is a shot of Mount Hood"" />
and there you have it.</p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">My picture,
<x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""This is a shot of Mount Hood"" />
and there you have it.</source>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestImgWithAlt did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Mount Hood", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(3, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("My picture,"+Environment.NewLine, n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("x", n1.Name);
			Assert.AreEqual(4, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("image", n1.Attributes["ctype"].Value);
			Assert.AreEqual("mthood.jpg", n1.Attributes["src", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("This is a shot of Mount Hood", n1.Attributes["alt", HtmlXliff.kHtmlNamespace].Value);
			var n2 = source.ChildNodes[2];
			Assert.AreEqual(XmlNodeType.Text, n2.NodeType);
			Assert.AreEqual(Environment.NewLine+"and there you have it.", n2.InnerText);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestTitleAndAltAreIgnored()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p title='Information about Mount Hood' i18n=""Mount Hood"">This is Mount Hood: <img src=""mthood.jpg"" alt=""Mount Hood with its snow-covered top""></p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">This is Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""Mount Hood with its snow-covered top"" /></source>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestImgWithTitleandAlt did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			// Note that the title attribute, which is theoretically translatable, is totally ignored in the xliff.
			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Mount Hood", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(2, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("This is Mount Hood: ", n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("x", n1.Name);
			Assert.AreEqual(4, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("image", n1.Attributes["ctype"].Value);
			Assert.AreEqual("mthood.jpg", n1.Attributes["src", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("Mount Hood with its snow-covered top", n1.Attributes["alt", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestInlineElements()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n='Portland mountain, river, & sea'>In Portland, Oregon one may <i>ski</i> on the mountain, <b>wind surf</b> in the gorge, and <i>surf</i> in the ocean, all on the same day.</p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Portland mountain, river, & sea"" restype=""x-html-p"">
    <source xml:lang=""en"">In Portland, Oregon one may <g id=""genid-1"" ctype=""italic"">ski</g> on the mountain, <g id=""genid-2"" ctype=""bold"">wind surf</g> in the gorge, and <g id=""genid-3"" ctype=""italic"">surf</g> in the ocean, all on the same day.</source>
    <note>ID: Portland mountain, river, &amp; sea</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestInlineElements did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			// Note that the title attribute, which is theoretically translatable, is totally ignored in the xliff.
			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Portland mountain, river, & sea", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(7, source.ChildNodes.Count);
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("In Portland, Oregon one may ", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(2, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("italic", n0.Attributes["ctype"].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("ski", n0.FirstChild.InnerText);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(" on the mountain, ", n0.InnerText);
					break;
				case 3:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(2, n0.Attributes.Count);
					Assert.AreEqual("genid-2", n0.Attributes["id"].Value);
					Assert.AreEqual("bold", n0.Attributes["ctype"].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("wind surf", n0.FirstChild.InnerText);
					break;
				case 4:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(" in the gorge, and ", n0.InnerText);
					break;
				case 5:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(2, n0.Attributes.Count);
					Assert.AreEqual("genid-3", n0.Attributes["id"].Value);
					Assert.AreEqual("italic", n0.Attributes["ctype"].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("surf", n0.FirstChild.InnerText);
					break;
				case 6:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(" in the ocean, all on the same day.", n0.InnerText);
					break;
				}
				++index;
			}
			CheckNoteElement(tu);
		}

		[Test]
		public void TestInlineElementWithLang()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<P i18n=""Mixed Language Motto"">The words <Q lang=""fr"">Je me souviens</Q> are the motto of Québec.</P>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Mixed Language Motto"" restype=""x-html-p"">
    <source xml:lang=""en"">The words <g id=""genid-1"" ctype=""x-html-q"" xml:lang=""fr"">Je me souviens</g> are the motto of Québec.</source>
    <note>ID: </note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestInlineElementWithLang did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			// Note that the title attribute, which is theoretically translatable, is totally ignored in the xliff.
			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("Mixed Language Motto", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(3, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("The words ", n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("g", n1.Name);
			Assert.AreEqual(3, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("x-html-q", n1.Attributes["ctype"].Value);
			Assert.AreEqual("fr", n1.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, n1.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, n1.FirstChild.NodeType);
			Assert.AreEqual("Je me souviens", n1.FirstChild.InnerText);
			var n2 = source.ChildNodes[2];
			Assert.AreEqual(XmlNodeType.Text, n2.NodeType);
			Assert.AreEqual(" are the motto of Québec.", n2.InnerText);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestInlineSpans()
		{
			// This tests inline elements with random HTML attributes.
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""colorful info"">Questions will appear in <span fontcolor=""#339966"">Green
face</span>, while answers will appear in <span fontcolor=""#333399"">Indigo
face</span>.</p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""colorful info"" restype=""x-html-p"">
    <source xml:lang=""en"">Questions will appear in <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#339966"">Green
face</g>, while answers will appear in <g id=""genid-2"" ctype=""x-html-span"" html:fontcolor=""#333399"">Indigo
face</g>.</source>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestInlineSpans did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			// Note that the title attribute, which is theoretically translatable, is totally ignored in the xliff.
			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("colorful info", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(5, source.ChildNodes.Count);
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("Questions will appear in ", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("x-html-span", n0.Attributes["ctype"].Value);
					Assert.AreEqual("#339966", n0.Attributes["fontcolor", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("Green" + Environment.NewLine + "face", n0.FirstChild.InnerText);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(", while answers will appear in ", n0.InnerText);
					break;
				case 3:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-2", n0.Attributes["id"].Value);
					Assert.AreEqual("x-html-span", n0.Attributes["ctype"].Value);
					Assert.AreEqual("#333399", n0.Attributes["fontcolor", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("Indigo" + Environment.NewLine + "face", n0.FirstChild.InnerText);
					break;
				case 4:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(".", n0.InnerText);
					break;
				}
				++index;
			}
			CheckNoteElement(tu);
		}

		[Test]
		public void TestSpanWithLang()
		{
			// This tests inline elements with both a random HTML attributes and a lang attribute.
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""French Quote She Said"">She added that ""<span lang='fr' fontcolor=""#339966"">je ne sais quoi</span>"" that made her casserole absolutely delicious.</p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""French Quote She Said"">
    <source xml:lang=""en"">She added that ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"" html:fontcolor="#339966">je ne sais quoi</g>"" that made her casserole absolutely delicious.</source>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestSpanWithLang did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			// Note that the title attribute, which is theoretically translatable, is totally ignored in the xliff.
			var tu = body.FirstChild;
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("French Quote She Said", tu.Attributes["id"].Value);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(3, source.ChildNodes.Count);
			var n0 = source.ChildNodes[0];
			Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
			Assert.AreEqual("She added that \"", n0.InnerText);
			var n1 = source.ChildNodes[1];
			Assert.AreEqual("g", n1.Name);
			Assert.AreEqual(4, n1.Attributes.Count);
			Assert.AreEqual("genid-1", n1.Attributes["id"].Value);
			Assert.AreEqual("x-html-span", n1.Attributes["ctype"].Value);
			Assert.AreEqual("fr", n1.Attributes["xml:lang"].Value);
			Assert.AreEqual("#339966", n1.Attributes["fontcolor", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual(1, n1.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, n1.FirstChild.NodeType);
			Assert.AreEqual("je ne sais quoi", n1.FirstChild.InnerText);
			var n2 = source.ChildNodes[2];
			Assert.AreEqual(XmlNodeType.Text, n2.NodeType);
			Assert.AreEqual("\" that made her casserole absolutely delicious.", n2.InnerText);
			CheckNoteElement(tu);
		}

		[Test]
		public void TestTableContent()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <h1 class=""title"" i18n=""Page.Title"">Report</h1>
  <table border=""1"" width=""100%"">
   <tr>
    <td valign=""top"" i18n=""Table.Cell-1.1"">Text in cell r1-c1</td>
    <td valign=""top"" i18n=""Table.Cell-1.2"">Text in cell r1-c2</td>
   </tr>
   <tr>
    <td bgcolor=""#C0C0C0"" i18n=""Table.Cell-2.1"">Text in cell r2-c1</td>
    <td i18n=""Table.Cell-2.2"">Text in cell r2-c2</td>
   </tr>
  </table>
  <p i18n=""Copyright.Notice"">All rights reserved (c) Gandalf Inc.</p>
 </body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""Page.Title"">
    <source xml:lang=""en"">Report</source>
    <note>ID: Page.Title</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.1"">
    <source xml:lang=""en"">Text in cell r1-c1</source>
    <note>ID: Table.Cell-1.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.2"">
    <source xml:lang=""en"">Text in cell r1-c2</source>
    <note>ID: Table.Cell-1.2</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.1"">
    <source xml:lang=""en"">Text in cell r2-c1</source>
    <note>ID: Table.Cell-2.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.2"">
    <source xml:lang=""en"">Text in cell r2-c2</source>
    <note>ID: Table.Cell-2.2</note>
   </trans-unit>
   <trans-unit id=""Copyright.Notice"">
    <source xml:lang=""en"">All rights reserved (c) Gandalf Inc.</source>
    <note>ID: Copyright.Notice</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestTableContent did not validate against schema: {0}");

			var file = xmlDoc.SelectSingleNode("/xliff/file");
			Assert.IsNotNull(file);
			Assert.AreEqual(3, file.Attributes.Count);
			Assert.AreEqual("test.html", file.Attributes["original"].Value);
			Assert.AreEqual("html", file.Attributes["datatype"].Value);
			Assert.AreEqual("en", file.Attributes["source-language"].Value);

			var body = file.SelectSingleNode("body");
			Assert.IsNotNull(body);
			Assert.AreEqual(0, body.Attributes.Count);
			CheckTransUnits(body,
				new [] { "Page.Title", "Table.Cell-1.1", "Table.Cell-1.2", "Table.Cell-2.1", "Table.Cell-2.2", "Copyright.Notice" },
				new [] { "Report", "Text in cell r1-c1", "Text in cell r1-c2", "Text in cell r2-c1", "Text in cell r2-c2", "All rights reserved (c) Gandalf Inc." });
		}

		[Test]
		public void TestZeroI18nMarks()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <h1 class=""title"">Report</h1>
  <table border=""1"" width=""100%"">
   <tr>
    <td valign=""top"">Text in cell r1-c1</td>
    <td valign=""top"">Text in cell r1-c2</td>
   </tr>
   <tr>
    <td bgcolor=""#C0C0C0"">Text in cell r2-c1</td>
    <td>Text in cell r2-c2</td>
   </tr>
  </table>
  <p>All rights reserved (c) Gandalf Inc.</p>
 </body>
</html>");
			/* Expected output: none, null, nada */
			var xmlDoc = extractor.Extract();
			Assert.IsNull(xmlDoc);
		}

		[Test]
		public void TestMultiParagraphUnit()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <div i18n=""lots of text"">
   <p>This is the first paragraph.</p>
   <p>This is the second paragraph.  It is even longer than the first paragraph.</p>
  </div>
  <p i18n=""single para"">This paragraph is independent of the others.</p>
 </body>
</html>");
			/* Expected output (ignore extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""lots of text"">
    <source xml:lang=""en"">
      <g id="genid-1" ctype="x-html-p">This is the first paragraph.</g>
      <g id="genid-2" ctype="x-html-p">This is the second paragraph.  It is even longer than the first paragraph.</g>
    </source>
    <note>ID: lots of text</note>
   </trans-unit>
   <trans-unit id=""single para"">
    <source xml:lang=""en"">This paragraph is independent of the others.</source>
    <note>ID: single para</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestMultiParagraphUnit did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(2, body.ChildNodes.Count);
			// check what's common in the trans-unit elements
			foreach (XmlNode tu in body.ChildNodes)
			{
				Assert.AreEqual("trans-unit", tu.Name);
				Assert.AreEqual(1, tu.Attributes.Count);
				Assert.AreEqual(3, tu.ChildNodes.Count);
				var source = tu.ChildNodes[0];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				var target = tu.ChildNodes[1];
				Assert.AreEqual("target", target.Name);
				Assert.AreEqual(0, target.Attributes.Count);
				Assert.AreEqual("", target.InnerXml);
				var note = tu.ChildNodes[2];
				Assert.AreEqual("note", note.Name);
				Assert.AreEqual(0, note.Attributes.Count);
				Assert.AreEqual("ID: " + tu.Attributes["id"].Value, note.InnerXml);
			}
			// check what is different in the trans-unit elements
			var tu0 = body.ChildNodes[0];
			Assert.AreEqual("lots of text", tu0.Attributes["id"].Value);
			var source0 = tu0.ChildNodes[0];
			Assert.AreEqual(5, source0.ChildNodes.Count);
			int index = 0;
			foreach (XmlNode xn in source0.ChildNodes)
			{
				switch (index)
				{
				case 0:
				case 2:
				case 4:
					Assert.AreEqual(XmlNodeType.Text, xn.NodeType);
					Assert.IsNotNullOrEmpty(xn.InnerText);
					Assert.IsTrue(String.IsNullOrWhiteSpace(xn.InnerText), String.Format("Expected whitespace, but have \"{0}\"", xn.InnerText));
					break;
				case 1:
					Assert.AreEqual("g", xn.Name);
					Assert.AreEqual(2, xn.Attributes.Count);
					Assert.AreEqual("genid-1", xn.Attributes["id"].Value);
					Assert.AreEqual("x-html-p", xn.Attributes["ctype"].Value);
					Assert.AreEqual(1, xn.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, xn.ChildNodes[0].NodeType);
					Assert.AreEqual("This is the first paragraph.", xn.ChildNodes[0].InnerText);
					break;
				case 3:
					Assert.AreEqual("g", xn.Name);
					Assert.AreEqual(2, xn.Attributes.Count);
					Assert.AreEqual("genid-2", xn.Attributes["id"].Value);
					Assert.AreEqual("x-html-p", xn.Attributes["ctype"].Value);
					Assert.AreEqual(1, xn.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, xn.ChildNodes[0].NodeType);
					Assert.AreEqual("This is the second paragraph.  It is even longer than the first paragraph.", xn.ChildNodes[0].InnerText);
					break;
				}
				++index;
			}
			var tu1 = body.ChildNodes[1];
			Assert.AreEqual("single para", tu1.Attributes["id"].Value);
			var source1 = tu1.ChildNodes[0];
			Assert.AreEqual(1, source1.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, source1.ChildNodes[0].NodeType);
			Assert.AreEqual("This paragraph is independent of the others.", source1.ChildNodes[0].InnerText);
		}

		[Test]
		public void TestWeirdAttributeChars()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <p i18n=""translate this!""><span data1=""' < & >"" data2='"" < & >'>This  is a test of strange characters: &lt; & > "" '.</span></p>
 </body>
</html>");
			/* Expected output (ignore extraneous whitespace)
<?xml version="1.0" encoding="utf-8"?>
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
 <file original="test.html" datatype="html" source-language="en">
  <body>
   <trans-unit id="translate this!">
    <source xml:lang="en"><g id="genid-1" ctype="x-html-span" html:data1="' &lt; &amp; &gt;" html:data2="&quot; &lt; &amp; &gt;">This  is a test of strange characters: &lt; &amp; &gt; " '.</g></source>
    <note>ID: translate this!</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestMultiParagraphUnit did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);
			Assert.AreEqual(1, body.ChildNodes.Count);

			var tu = body.ChildNodes[0];
			Assert.AreEqual("trans-unit", tu.Name);
			Assert.AreEqual(1, tu.Attributes.Count);
			Assert.AreEqual("translate this!", tu.Attributes["id"].Value);
			Assert.AreEqual(3, tu.ChildNodes.Count);

			var source = tu.ChildNodes[0];
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, source.ChildNodes.Count);
			Assert.AreEqual("g", source.ChildNodes[0].Name);
			Assert.AreEqual(4, source.ChildNodes[0].Attributes.Count);
			Assert.AreEqual("genid-1", source.ChildNodes[0].Attributes["id"].Value);
			Assert.AreEqual("x-html-span", source.ChildNodes[0].Attributes["ctype"].Value);
			Assert.AreEqual("' < & >", source.ChildNodes[0].Attributes["data1", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("\" < & >", source.ChildNodes[0].Attributes["data2", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual(1, source.ChildNodes[0].ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, source.ChildNodes[0].ChildNodes[0].NodeType);
			Assert.AreEqual("This  is a test of strange characters: &lt; &amp; &gt; \" '.", source.ChildNodes[0].InnerXml);
			Assert.AreEqual("This  is a test of strange characters: < & > \" '.", source.ChildNodes[0].ChildNodes[0].InnerText);

			var target = tu.ChildNodes[1];
			Assert.AreEqual("target", target.Name);
			Assert.AreEqual(0, target.Attributes.Count);
			Assert.AreEqual("", target.InnerXml);

			var note = tu.ChildNodes[2];
			Assert.AreEqual(0, note.Attributes.Count);
			Assert.AreEqual(1, note.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, note.ChildNodes[0].NodeType);
			Assert.AreEqual("ID: translate this!", note.ChildNodes[0].InnerText);
		}

		[Test]
		public void TestNestedHtmlListsFromMarkdownIt()
		{
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <ul>
   <li i18n=""integrity.todo.ideas.Reinstall"">
    <p>Run the Bloom installer again, and see if it starts up OK this time.</p>
   </li>
   <li i18n=""integrity.todo.ideas.Antivirus"">
    <p>If that doesn't fix it, it's time to talk to your anti-virus program.</p>
    <ul>
     <li i18n=""integrity.todo.ideas.AVAST"">
      <p>AVAST: <a href=""http://www.getavast.net/support/managing-exceptions"">Instructions</a>.</p>
     </li>
     <li i18n=""integrity.todo.ideas.Restart"">
      <p>Run the Bloom installer again, and see if it starts up OK this time.</p>
     </li>
    </ul>
   </li>
   <li i18n=""integrity.todo.ideas.Retrieve"">
    <p>You can also try and retrieve the part of Bloom that your anti-virus program took from it.</p>
   </li>
  </ul>
 </body>
</html>");
			/* Expected output (ignore extraneous whitespace)
<?xml version="1.0" encoding="utf-8"?>
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
 <file original="test.html" datatype="html" source-language="en">
  <body>
   <trans-unit id="integrity.todo.ideas.Reinstall">
    <source xml:lang="en">Run the Bloom installer again, and see if it starts up OK this time.</source>
    <note>ID: integrity.todo.ideas.Reinstall</note>
   </trans-unit>
   <trans-unit id="integrity.todo.ideas.Antivirus">
    <source xml:lang="en">If that doesn't fix it, it's time to talk to your anti-virus program.</source>
    <note>ID: integrity.todo.ideas.Antivirus</note>
   </trans-unit>
   <trans-unit id="integrity.todo.ideas.AVAST">
    <source xml:lang="en">AVAST: <g id="genid-1" ctype="x-html-a" html:href="http://www.getavast.net/support/managing-exceptions">Instructions</g>.</source>
    <note>ID: integrity.todo.ideas.AVAST</note>
   </trans-unit>
   <trans-unit id="integrity.todo.ideas.Restart">
    <source xml:lang="en">Run the Bloom installer again, and see if it starts up OK this time.</source>
    <note>ID: integrity.todo.ideas.Restart</note>
   </trans-unit>
   <trans-unit id="integrity.todo.ideas.Retrieve">
    <source xml:lang="en">You can also try and retrieve the part of Bloom that your anti-virus program took from it.</source>
    <note>ID: integrity.todo.ideas.Retrieve</note>
   </trans-unit>
  </body>
 </file>
</xliff>*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestMarkdownWithLinks did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);

			// Without the new code being tested by this method, only three source elements were extracted for translation,
			// one for each of the three top-level list items in the HTML.  The strings all contained <g> elements containing
			// the html markup, and the second string had a real mess containing all the content including both the initial
			// paragraph and the entire sublist encoded with (invalid) <g> elements.  It was very ugly (undesireable and even
			// invalid) content for the source elements in the XLIFF.

			Assert.AreEqual(5, body.ChildNodes.Count);
			foreach (XmlNode n0 in body.ChildNodes)
			{
				Assert.AreEqual("trans-unit", n0.Name);
				Assert.AreEqual(1, n0.Attributes.Count);
				Assert.AreEqual(3, n0.ChildNodes.Count);
				var source = n0.ChildNodes[0];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				var target = n0.ChildNodes[1];
				Assert.AreEqual("target", target.Name);
				Assert.AreEqual(0, target.Attributes.Count);
				Assert.AreEqual(0, target.ChildNodes.Count);
				Assert.AreEqual("", target.InnerXml);
				var note = n0.ChildNodes[2];
				Assert.AreEqual("note", note.Name);
				Assert.AreEqual(0, note.Attributes.Count);
				Assert.AreEqual(1, note.ChildNodes.Count);
				Assert.AreEqual("ID: " + n0.Attributes["id"].Value, note.InnerText);
			}

			var tu0 = body.ChildNodes[0];
			Assert.AreEqual(1, tu0.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Reinstall", tu0.Attributes["id"].Value);
			var src = tu0.ChildNodes[0];
			Assert.AreEqual(1, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("Run the Bloom installer again, and see if it starts up OK this time.", src.ChildNodes[0].InnerText);

			var tu1 = body.ChildNodes[1];
			Assert.AreEqual(1, tu1.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Antivirus", tu1.Attributes["id"].Value);
			src = tu1.ChildNodes[0];
			Assert.AreEqual(1, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("If that doesn't fix it, it's time to talk to your anti-virus program.", src.ChildNodes[0].InnerText);

			var tu2 = body.ChildNodes[2];
			Assert.AreEqual(1, tu2.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.AVAST", tu2.Attributes["id"].Value);
			src = tu2.ChildNodes[0];
			Assert.AreEqual(3, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("AVAST: ", src.ChildNodes[0].InnerText);
			Assert.AreEqual("g", src.ChildNodes[1].Name);
			Assert.AreEqual(3, src.ChildNodes[1].Attributes.Count);
			Assert.AreEqual("genid-1", src.ChildNodes[1].Attributes["id"].Value);
			Assert.AreEqual("x-html-a", src.ChildNodes[1].Attributes["ctype"].Value);
			Assert.AreEqual("http://www.getavast.net/support/managing-exceptions", src.ChildNodes[1].Attributes["href", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("Instructions", src.ChildNodes[1].InnerText);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[2].NodeType);
			Assert.AreEqual(".", src.ChildNodes[2].InnerText);

			var tu3 = body.ChildNodes[3];
			Assert.AreEqual(1, tu3.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Restart", tu3.Attributes["id"].Value);
			src = tu3.ChildNodes[0];
			Assert.AreEqual(1, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("Run the Bloom installer again, and see if it starts up OK this time.", src.ChildNodes[0].InnerText);

			var tu4 = body.ChildNodes[4];
			Assert.AreEqual(1, tu4.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Retrieve", tu4.Attributes["id"].Value);
			src = tu4.ChildNodes[0];
			Assert.AreEqual(1, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("You can also try and retrieve the part of Bloom that your anti-virus program took from it.", src.ChildNodes[0].InnerText);
		}

		/// <summary>
		/// Check all of the children of the xliff body element against the ids and values provided.
		/// </summary>
		/// <param name="body">Body x.</param>
		/// <param name="ids">Identifiers.</param>
		/// <param name="values">Values.</param>
		private void CheckTransUnits(XmlNode body, string[] ids, string[] values)
		{
			Assert.AreEqual(ids.Length, values.Length);
			Assert.AreEqual(ids.Length, body.ChildNodes.Count);
			int index = 0;
			foreach (XmlNode tu in body.ChildNodes)
			{
				Assert.AreEqual("trans-unit", tu.Name);
				Assert.AreEqual(1, tu.Attributes.Count);
				Assert.AreEqual(ids[index], tu.Attributes["id"].Value);
				Assert.AreEqual(3, tu.ChildNodes.Count);

				var source = tu.ChildNodes[0];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				Assert.AreEqual(1, source.ChildNodes.Count);
				Assert.AreEqual(XmlNodeType.Text, source.FirstChild.NodeType);
				Assert.AreEqual(values[index], source.InnerXml);

				CheckTargetElement(tu);
				CheckNoteElement(tu);
				++index;
			}
		}

		private void CheckTargetElement(XmlNode transUnit)
		{
			var target = transUnit.ChildNodes[1];
			Assert.AreEqual(0, target.Attributes.Count);
			Assert.AreEqual("", target.InnerXml);
		}

		private void CheckNoteElement(XmlNode transUnit)
		{
			var note = transUnit.ChildNodes[2];
			Assert.AreEqual("note", note.Name);
			Assert.AreEqual(0, note.Attributes.Count);
			Assert.AreEqual(1, note.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, note.FirstChild.NodeType);
			Assert.AreEqual("ID: " + transUnit.Attributes["id"].Value, note.FirstChild.InnerText);
		}

		/// <summary>
		/// Validate the xliff output.  Calling xmlDoc.Validate() in the test never caught
		/// a validation error even after assigning a schema for xliff.  Loading the XML into
		/// a new document with a schema assigned a priori does catch invalid xliff, and
		/// this approach presumably sets up Validate to catch validation warnings that don't
		/// cause an exception during loading.
		/// </summary>
		private void ValidateXliffOutput(string xliff, string errorMessageFormat)
		{
			try
			{
				var settings = new XmlReaderSettings();
				settings.Schemas.Add(null, "../../src/XliffForHtmlTests/xliff-core-1.2-transitional.xsd");
				settings.ValidationType = ValidationType.Schema;
				var reader = XmlReader.Create(new StringReader(xliff), settings);
				var document = new XmlDocument();
				document.Load(reader);		// throws here if invalid.  Validate() catches warnings as well as errors.
				document.Validate((sender, e) => { Assert.Fail(errorMessageFormat, e.Message); });
			}
			catch (System.Xml.Schema.XmlSchemaValidationException e)
			{
				Assert.Fail(errorMessageFormat, e.Message);
			}
		}

		[Test]
		public void TestAmpersandQuotes()
		{
			var extractor = HtmlXliff.Parse(@"<html>
  <body>
    <ul>
      <li i18n='integrity.todo.ideas.Reinstall'>
        <p>Run the Bloom installer again, and see if it starts up OK this time.</p>
      </li>
      <li i18n='integrity.todo.ideas.Antivirus'>
        <p>If that doesn't fix it, it's time to talk to your anti-virus program.
If the &quot;Missing Files&quot; section below shows any files which end in &quot;.exe&quot;,
consider &quot;whitelisting&quot; the Bloom program folder,
which is at <strong>{{installFolder}}</strong>.</p>
        <ul>
          <li i18n='integrity.todo.ideas.AVAST'>
            <p>AVAST: <a href='http://www.getavast.net/support/managing-exceptions'>Instructions</a>.</p>
          </li>
          <li i18n='integrity.todo.ideas.AVG'>
            <p>AVG: <a href='https://support.avg.com/SupportArticleView?l=en_US&amp;urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning'>Instructions from AVG</a>.</p>
          </li>
          <li i18n='integrity.todo.ideas.Others'>
            <p>Others: Google for &quot;whitelist directory name-of-your-antivirus&quot;</p>
          </li>
        </ul>
      </li>
    </ul>
  </body>
</html>");
			/* Expected output (ignore extraneous whitespace)
<?xml version="1.0" encoding="utf-8"?>
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
  <file original="foo.html" datatype="html" source-language="en">
    <body>
      <trans-unit id="integrity.todo.ideas.Reinstall">
        <source xml:lang="en">Run the Bloom installer again, and see if it starts up OK this time.</source>
        <note>ID: integrity.todo.ideas.Reinstall</note>
      </trans-unit>
      <trans-unit id="integrity.todo.ideas.Antivirus">
        <source xml:lang="en">If that doesn't fix it, it's time to talk to your anti-virus program.
If the "Missing Files" section below shows any files which end in ".exe",
consider "whitelisting" the Bloom program folder,
which is at <g id="genid-1" ctype="x-html-strong">{{installFolder}}</g>.</source>
        <note>ID: integrity.todo.ideas.Antivirus</note>
      </trans-unit>
      <trans-unit id="integrity.todo.ideas.AVAST">
        <source xml:lang="en">AVAST: <g id="genid-2" ctype="x-html-a" html:href="http://www.getavast.net/support/managing-exceptions">Instructions</g>.</source>
        <note>ID: integrity.todo.ideas.AVAST</note>
      </trans-unit>
      <trans-unit id="integrity.todo.ideas.AVG">
        <source xml:lang="en">AVG: <g id="genid-3" ctype="x-html-a" html:href="https://support.avg.com/SupportArticleView?l=en_US&amp;urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning">Instructions from AVG</g>.</source>
        <note>ID: integrity.todo.ideas.AVG</note>
      </trans-unit>
      <trans-unit id="integrity.todo.ideas.Others">
        <source xml:lang="en">Others: Google for "whitelist directory name-of-your-antivirus"</source>
        <note>ID: integrity.todo.ideas.Others</note>
      </trans-unit>
    </body>
  </file>
</xliff>
*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestAmpersandQuotes did not validate against schema: {0}");

			var body = xmlDoc.SelectSingleNode("/xliff/file/body");
			Assert.IsNotNull(body);

			Assert.AreEqual(5, body.ChildNodes.Count);
			foreach (XmlNode n0 in body.ChildNodes)
			{
				Assert.AreEqual("trans-unit", n0.Name);
				Assert.AreEqual(1, n0.Attributes.Count);
				Assert.AreEqual(3, n0.ChildNodes.Count);
				var source = n0.ChildNodes[0];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				var target = n0.ChildNodes[1];
				Assert.AreEqual("target", target.Name);
				Assert.AreEqual(0, target.Attributes.Count);
				Assert.AreEqual(0, target.ChildNodes.Count);
				Assert.AreEqual("", target.InnerXml);
				var note = n0.ChildNodes[2];
				Assert.AreEqual("note", note.Name);
				Assert.AreEqual(0, note.Attributes.Count);
				Assert.AreEqual(1, note.ChildNodes.Count);
				Assert.AreEqual("ID: " + n0.Attributes["id"].Value, note.InnerText);
			}
			// Check that &amp; in an attribute is preserved verbatim.
			var tu3 = body.ChildNodes[3];
			Assert.AreEqual(1, tu3.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.AVG", tu3.Attributes["id"].Value);
			var src = tu3.ChildNodes[0];
			Assert.AreEqual(3, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("AVG: ", src.ChildNodes[0].InnerText);
			Assert.AreEqual(XmlNodeType.Element, src.ChildNodes[1].NodeType);
			var g = src.ChildNodes[1];
			Assert.AreEqual("g", g.Name);
			Assert.AreEqual(3, g.Attributes.Count);
			Assert.AreEqual("genid-3", g.Attributes["id"].Value);
			Assert.AreEqual("x-html-a", g.Attributes["ctype"].Value);
			Assert.AreEqual("https://support.avg.com/SupportArticleView?l=en_US&urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning", g.Attributes["href", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("Instructions from AVG", g.InnerText);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[2].NodeType);
			Assert.AreEqual(".", src.ChildNodes[2].InnerText);

			// Check that &quot; in text is converted to ".
			var tu4 = body.ChildNodes[4];
			Assert.AreEqual(1, tu4.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Others", tu4.Attributes["id"].Value);
			src = tu4.ChildNodes[0];
			Assert.AreEqual(1, src.ChildNodes.Count);
			Assert.AreEqual(XmlNodeType.Text, src.ChildNodes[0].NodeType);
			Assert.AreEqual("Others: Google for \"whitelist directory name-of-your-antivirus\"", src.ChildNodes[0].InnerText);
		}

		[Test]
		public void CopyObsoleteUnitsToNewXliff_CreatesObsoleteAndMaintainsExisting_Works()
		{
			// Create the xliff from an original HTML file representation to mimic program behavior.
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <h3 i18n=""epubpreview.recommended.reader"">Recommended Reader</h3>
  <p class=""showForTalkingBooks"" i18n=""epubpreview.gitden.limits"">Disable Gitden's ""Read aloud (TTS)"" feature.</p>
  <h3 class=""showWhenNoAudio"" i18n=""epubpreview.make.it.talk"">Make it Talk Aloud</h3>
 </body>
</html>");
			var newXliff = extractor.Extract();
			// Mimic having a previous version of the generated xliff that has two strings that no longer exist
			// in the HTML file. One of the two is already marked "obsolete".
			var oldXliff = new XmlDocument();
			oldXliff.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
<xliff version='1.2' xmlns='urn:oasis:names:tc:xliff:document:1.2' xmlns:html='http://www.w3.org/TR/html' xmlns:sil='http://sil.org/software/XLiff'>
  <file original='testing.html' datatype='html' source-language='en'>
    <body>
      <trans-unit id='epubpreview.gitden.limits'>
        <source xml:lang='en'>Disable Gitden's ""Read aloud (TTS)"" feature.</source>
        <target />
        <note>ID: epubpreview.gitden.limits</note>
        <note>OLD TEXT (Gitden's wording changed): Disable Gitden's ""Text To Speech"" feature.</note>
      </trans-unit>
      <trans-unit id='epubpreview.make.it.talk'>
        <source xml:lang='en'>Make it Talk</source>
        <target />
        <note>ID: epubpreview.make.it.talk</note>
      </trans-unit>
      <trans-unit id='integrity.title'>
        <source xml:lang='en'>Bloom cannot find some of its own files, and cannot continue</source>
        <target />
        <note>ID: integrity.title</note>
      </trans-unit>
      <trans-unit id='integrity.title.2'>
        <source xml:lang='en'>Bloom cannot find anybody's files! Panic!</source>
        <target />
        <note>ID: integrity.title.2</note>
        <note>Obsolete in 5.2</note>
      </trans-unit>
    </body>
  </file>
</xliff>");

			//SUT
			HtmlXliff.CopyObsoleteUnitsToNewXliff(newXliff, oldXliff);

			var units = newXliff.SelectNodes("/xliff/file/body/trans-unit");
			// 4 from the old file + 1 new one in the new file (epubpreview.recommended.reader)
			Assert.AreEqual(5, units.Count);

			var tu = units.Item(3);
			CheckXliffWithNotes(tu, "integrity.title", 4, "Bloom cannot find some of its own files, and cannot continue",
				new[] { "ID: integrity.title", "Obsolete for {name} {version}" });
			var alreadyObsoleteTu = units.Item(4);
			CheckXliffWithNotes(alreadyObsoleteTu, "integrity.title.2", 4, "Bloom cannot find anybody's files! Panic!",
				new[] { "ID: integrity.title.2", "Obsolete in 5.2" });
		}

		[Test]
		public void TestCopyingNotesFromOldXliff()
		{
			// Create the xliff from an original HTML file representation to mimic program behavior.
			var extractor = HtmlXliff.Parse(@"<html>
 <body>
  <h3 i18n=""epubpreview.recommended.reader"">Recommended Reader</h3>
  <p class=""showForTalkingBooks"" i18n=""epubpreview.gitden.limits"">Disable Gitden's ""Read aloud (TTS)"" feature.</p>
  <h3 class=""showWhenNoAudio"" i18n=""epubpreview.make.it.talk"">Make it Talk Aloud</h3>
  <ul>
    <li i18n=""integrity.todo.ideas.AVG"">
      <p>AVG: <a href=""https://support.avg.com/SupportArticleView?l=en_US&amp;urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning"">Instructions from AVG</a>.</p>
    </li>
  </ul>
 </body>
</html>");
			var newXliff = extractor.Extract();
			// Mimic having a previous version of the generated xliff that has a couple
			// of notes added, and a string that no longer exists in the HTML file.
			var oldXliff = new XmlDocument();
			oldXliff.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
<xliff version='1.2' xmlns='urn:oasis:names:tc:xliff:document:1.2' xmlns:html='http://www.w3.org/TR/html' xmlns:sil='http://sil.org/software/XLiff'>
  <file original='testing.html' datatype='html' source-language='en'>
    <body>
      <trans-unit id='epubpreview.gitden.limits'>
        <source xml:lang='en'>Disable Gitden's ""Read aloud (TTS)"" feature.</source>
        <target />
        <note>ID: epubpreview.gitden.limits</note>
        <note>OLD TEXT (Gitden's wording changed): Disable Gitden's ""Text To Speech"" feature.</note>
      </trans-unit>
      <trans-unit id='epubpreview.make.it.talk'>
        <source xml:lang='en'>Make it Talk</source>
        <target />
        <note>ID: epubpreview.make.it.talk</note>
      </trans-unit>
      <trans-unit id='integrity.title'>
        <source xml:lang='en'>Bloom cannot find some of its own files, and cannot continue</source>
        <target />
        <note>ID: integrity.title</note>
      </trans-unit>
      <trans-unit id='integrity.todo.ideas.AVG'>
        <source xml:lang='en'>AVG: <g id='genid-1' ctype='x-html-a' html:href='https://support.avg.com/SupportArticleView?l=en_US&amp;urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning'>Instructions from AVG</g>.</source>
        <target />
        <note>ID: integrity.todo.ideas.AVG</note>
        <note>The former source text had an incorrect &amp;amp; following l=en_US in the href attribute value.</note>
      </trans-unit>
    </body>
  </file>
</xliff>");
			HtmlXliff.CopyMissingNotesToNewXliff(newXliff, oldXliff);

			var units = newXliff.SelectNodes("/xliff/file/body/trans-unit");
			Assert.AreEqual(4, units.Count);

			CheckXliffWithNotes(units.Item(0), "epubpreview.recommended.reader", 3, "Recommended Reader",
				new [] {"ID: epubpreview.recommended.reader"});
			CheckXliffWithNotes(units.Item(1), "epubpreview.gitden.limits", 4, "Disable Gitden's \"Read aloud (TTS)\" feature.",
				new [] {"ID: epubpreview.gitden.limits", "OLD TEXT (Gitden's wording changed): Disable Gitden's \"Text To Speech\" feature."});
			CheckXliffWithNotes(units.Item(2), "epubpreview.make.it.talk", 3, "Make it Talk Aloud",
				new [] {"ID: epubpreview.make.it.talk"});
			var tu = units.Item(3);
			CheckXliffWithNotes(tu, "integrity.todo.ideas.AVG", 4, null,
				new [] {"ID: integrity.todo.ideas.AVG", "The former source text had an incorrect &amp;amp; following l=en_US in the href attribute value."});
			// more complex source element to check
			var source = tu.ChildNodes.Item(0);
			Assert.AreEqual(3, source.ChildNodes.Count);
			Assert.AreEqual("AVG: ", source.ChildNodes.Item(0).Value);
			var g = source.ChildNodes.Item(1);
			Assert.AreEqual("g", g.Name);
			Assert.AreEqual(3, g.Attributes.Count);
			Assert.AreEqual("genid-1", g.Attributes["id"].Value);
			Assert.AreEqual("x-html-a", g.Attributes["ctype"].Value);
			Assert.AreEqual("https://support.avg.com/SupportArticleView?l=en_US&urlname=How-to-exclude-file-folder-or-website-from-AVG-scanning", g.Attributes["href", HtmlXliff.kHtmlNamespace].Value);
			Assert.AreEqual("Instructions from AVG", g.InnerXml);
			Assert.AreEqual(".", source.ChildNodes.Item(2).Value);
		}

		private void CheckXliffWithNotes(XmlNode tu, string id, int childCount, string sourceText, string[] notes)
		{
			Assert.AreEqual(id, tu.Attributes["id"].Value);
			Assert.AreEqual(childCount, tu.ChildNodes.Count);
			var source = tu.ChildNodes.Item(0);
			Assert.AreEqual("source", source.Name);
			Assert.AreEqual(1, source.Attributes.Count);
			Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
			if (!String.IsNullOrEmpty(sourceText))
				Assert.AreEqual(sourceText, source.InnerXml);
			var target = tu.ChildNodes.Item(1);
			Assert.AreEqual("target", target.Name);
			Assert.AreEqual(0, target.Attributes.Count);
			Assert.AreEqual("", target.InnerXml);
			for (int i = 0; i < notes.Length; ++i)
			{
				var note = tu.ChildNodes.Item(i+2);
				Assert.AreEqual("note", note.Name);
				Assert.AreEqual(0, note.Attributes.Count);
				Assert.AreEqual(notes[i], note.InnerXml);
			}
		}

		/// <summary>
		/// First test that only the global html: namespace tag is used for extracted elements instead of local
		/// per-element namepace declaration and prefix imposed by XmlNode.InnerXml.  This explicitly tests the
		/// following.
		/// 1) A &lt;g&gt; element inside &lt;source&gt; with one attribute in the HTML namespace uses the global
		///    html: prefix for that attribute and does not have a local HTML namespace declaration attribute.
		/// 2) The &lt;source&gt; content handles the standard character entities (&amp; &lt; &gt;) properly.
		/// </summary>
		/// <remarks>
		/// The sil: namespace should never be encountered in extracting XLIFF from HTML.
		/// </remarks>
		[Test]
		public void TestNamespaceFixing1()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<h3 i18n=""integrity.todo"">What To Do</h3>
<ul>
<li i18n=""integrity.todo.ideas.Antivirus"">
<p>Note 1 &lt; 2 &amp; 2 &gt; 1!  If the &quot;Missing Files&quot; section below shows any files which end in &quot;.exe&quot;,  consider &quot;whitelisting&quot; the Bloom program folder, which is at <strong>{{installFolder}}</strong>.</p>
<ul>
<li i18n=""integrity.todo.ideas.AVAST"">
<p>AVAST: <a href=""http://www.getavast.net/support/managing-exceptions"">Instructions</a>.</p>
</li>
<li i18n=""integrity.todo.ideas.Norton"">
<p>Norton Antivirus: <a href=""https://support.symantec.com/en_US/article.HOWTO80920.html"">Instructions from Symantec</a>.</p>
</li>
</ul>
</body>
</html>");
			/* Expected output (ignore extraneous whitespace)
			<?xml version="1.0" encoding="utf-8"?>
			<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
			  <file original="foo.html" datatype="html" source-language="en">
				<body>
				  <trans-unit id="integrity.todo">
					<source xml:lang="en">What To Do</source>
					<target />
					<note>ID: integrity.todo</note>
				  </trans-unit>
				  <trans-unit id="integrity.todo.ideas.Antivirus">
					<source xml:lang="en">Note 1 &lt; 2 &amp; 2 &gt; 1!   If the "Missing Files" section below shows any files which end in ".exe",  consider "whitelisting" the Bloom program folder, which is at <g id="genid-1" ctype="x-html-strong">{{installFolder}}</g>.</source>
					<target />
					<note>ID: integrity.todo.ideas.Antivirus</note>
				  </trans-unit>
				  <trans-unit id="integrity.todo.ideas.AVAST">
					<source xml:lang="en">AVAST: <g id="genid-2" ctype="x-html-a" html:href="http://www.getavast.net/support/managing-exceptions">Instructions</g>.</source>
					<target />
					<note>ID: integrity.todo.ideas.AVAST</note>
				  </trans-unit>
				  <trans-unit id="integrity.todo.ideas.Norton">
					<source xml:lang="en">Norton Antivirus: <g id="genid-3" ctype="x-html-a" html:href="https://support.symantec.com/en_US/article.HOWTO80920.html">Instructions from Symantec</g>.</source>
					<target />
					<note>ID: integrity.todo.ideas.Norton</note>
				  </trans-unit>
				</body>
			  </file>
			</xliff>
			*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Initial Xliff for TestNamespaceFixing1 did not validate against schema: {0}");

			var xdoc = TestWriteXliff(xmlDoc);
			ValidateXliffOutput(xdoc.OuterXml, "Cleaned up Xliff for TestNamespaceFixing1 did not validate against schema: {0}");

			var nsmgr = HtmlXliff.CreateNamespaceManager(xdoc);
			var body = xdoc.SelectSingleNode("/x:xliff/x:file/x:body", nsmgr);
			Assert.IsNotNull(body);
			VerifyTransUnitLayout(body, 4);

			// Test that standard character entities are handled properly.
			var xel = body.SelectSingleNode("./x:trans-unit[@id='integrity.todo.ideas.Antivirus']/x:source", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.That(xel.InnerXml.StartsWith("Note 1 &lt; 2 &amp; 2 &gt; 1!", StringComparison.Ordinal));
			Assert.That(xel.InnerText.StartsWith("Note 1 < 2 & 2 > 1!", StringComparison.Ordinal));

			// Test that a single HTML namespace attribute is handled correctly, using the global html: prefix
			// instead of having its own namespace attribute and prefix.
			xel = body.SelectSingleNode("./x:trans-unit[@id='integrity.todo.ideas.AVAST']/x:source/x:g[@id='genid-2']", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.AreEqual(3, xel.Attributes.Count);
			Assert.AreEqual("id", xel.Attributes[0].Name);
			Assert.AreEqual("", xel.Attributes[0].Prefix);
			Assert.AreEqual("genid-2", xel.Attributes[0].Value);
			Assert.AreEqual("ctype", xel.Attributes[1].Name);
			Assert.AreEqual("", xel.Attributes[1].Prefix);
			Assert.AreEqual("x-html-a", xel.Attributes[1].Value);
			Assert.AreEqual("html:href", xel.Attributes[2].Name);
			Assert.AreEqual("html", xel.Attributes[2].Prefix);
			Assert.AreEqual("http://www.getavast.net/support/managing-exceptions", xel.Attributes[2].Value);
			Assert.AreEqual("Instructions", xel.InnerXml);
			Assert.AreEqual("Instructions", xel.InnerText);
		}

		/// <summary>
		/// Second test that only the global html: namespace tag is used for extracted elements instead of local
		/// per-element namepace declaration and prefix imposed by XmlNode.InnerXml.  This explicitly the following.
		/// 1) An &lt;x&gt; element inside a &lt;source&gt; element with two attributes in the HTML namespace uses
		///    the global html: prefix for both attributes and does not have a local namespace declaration attribute.
		/// 2) The &lt;source&gt; content does not start with the indentation whitespace.
		/// </summary>
		/// <remarks>
		/// The sil: namespace should never be encountered in extracting XLIFF from HTML.
		/// </remarks>
		[Test]
		public void TestNamespaceFixing2()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""template.starter.labelsnottranslatable""><a id=""note3"">3</a>: People will not be able to translate your labels and descriptions into other national languages. If this is a problem, please contact the Bloom team.</p>
<p i18n=""template.starter.editrawhtml""><a id=""note4"">4</a>: If you want the Add Page screen to also provide a short description of the page, you'll need to quit Bloom and edit the template's html in Notepad, like this: <img src=""ReadMeImages/pageDescription.png"" alt=""pageDescription image""></p>
</body>
</html>");
/* Expected output (ignore extraneous whitespace)
<?xml version="1.0" encoding="utf-8"?>
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
  <file original="foo.html" datatype="html" source-language="en">
    <body>
      <trans-unit id="template.starter.labelsnottranslatable">
        <source xml:lang="en"><g id="genid-1" ctype="x-html-a" html:id="note3">3</g>: People will not be able to translate your labels and descriptions into other national languages. If this is a problem, please contact the Bloom team.</source>
        <target />
        <note>ID: template.starter.labelsnottranslatable</note>
      </trans-unit>
      <trans-unit id="template.starter.editrawhtml">
        <source xml:lang="en"><g id="genid-2" ctype="x-html-a" html:id="note4">4</g>: If you want the Add Page screen to also provide a short description of the page, you'll need to quit Bloom and edit the template's html in Notepad, like this: <x id="genid-3" ctype="image" html:src="ReadMeImages/pageDescription.png" html:alt="pageDescription image" /></source>
        <target />
        <note>ID: template.starter.editrawhtml</note>
      </trans-unit>
    </body>
  </file>
</xliff>
*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestNamespaceFixing2 did not validate against schema: {0}");

			var xdoc = TestWriteXliff(xmlDoc);
			ValidateXliffOutput(xdoc.OuterXml, "Cleaned up Xliff for TestNamespaceFixing2 did not validate against schema: {0}");

			var nsmgr = HtmlXliff.CreateNamespaceManager(xdoc);
			var body = xdoc.SelectSingleNode("/x:xliff/x:file/x:body", nsmgr);
			Assert.IsNotNull(body);
			VerifyTransUnitLayout(body, 2);

			// Test that two HTML namespace attributes in a single element are handled correctly, using the global html: prefix
			// instead of having a (shared) namespace attribute with a distinct prefix.
			var xel = body.SelectSingleNode("./x:trans-unit[@id='template.starter.editrawhtml']/x:source/x:x[@id='genid-3']", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.AreEqual(4, xel.Attributes.Count);
			Assert.AreEqual("id", xel.Attributes[0].Name);
			Assert.AreEqual("", xel.Attributes[0].Prefix);
			Assert.AreEqual("genid-3", xel.Attributes[0].Value);
			Assert.AreEqual("ctype", xel.Attributes[1].Name);
			Assert.AreEqual("", xel.Attributes[1].Prefix);
			Assert.AreEqual("image", xel.Attributes[1].Value);
			Assert.AreEqual("html:src", xel.Attributes[2].Name);
			Assert.AreEqual("html", xel.Attributes[2].Prefix);
			Assert.AreEqual("ReadMeImages/pageDescription.png", xel.Attributes[2].Value);
			Assert.AreEqual("html:alt", xel.Attributes[3].Name);
			Assert.AreEqual("html", xel.Attributes[3].Prefix);
			Assert.AreEqual("pageDescription image", xel.Attributes[3].Value);
			Assert.AreEqual("", xel.InnerXml);

			// Test that the <source> content does not start with indentation whitespace.
			xel = body.SelectSingleNode("./x:trans-unit[@id='template.starter.editrawhtml']/x:source", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.IsTrue(xel.InnerXml.StartsWith("<g id=", StringComparison.Ordinal));
		}

		/// <summary>
		/// Third test that only the global html: namespace tag is used for extracted elements instead of local
		/// per-element namepace declaration and prefix imposed by XmlNode.InnerXml.  This explicitly tests the
		/// following.
		/// 1) The &lt;source&gt; content does not end with indentation whitespace, but internal newlines
		///    and whitespace are preserved.
		/// 2) A &lt;g&gt; element inside &lt;source&gt; with an attribute in the HTML namespace uses the global
		///    html: prefix for that attribute and does not have a local HTML namespace declaration attribute.
		/// 3) A &lt;g&gt; element nested inside the previously mentioned one that has an attribute in the HTML
		///    namespace uses the global html: prefix for that attribute, not the prefix invented for its parent
		///    &lt;g&gt; element when it had a local HTML namespace declaration attribute.
		/// </summary>
		/// <remarks>
		/// The sil: namespace should never be encountered in extracting XLIFF from HTML.
		/// </remarks>
		[Test]
		public void TestNamespaceFixing3()
		{
			var extractor = HtmlXliff.Parse(@"<html>
  <body>
    <h1 id=""Predictability"" i18n=""leveled.reader.predictability"">Predictability</h1>
    <p i18n=""leveled.reader.repeat.patterns""><em>Predictability</em> in a text means that the reader can guess what would come next. You can increase predictability by using repeated patterns. Here are some patterns you can use:</p>
    <ul>
      <li i18n=""leveled.reader.repetition"">Repetition - repeating parts of the text, for example, using the same sentence with each page and just changing one word in the sentence</li>
      <li i18n=""leveled.reader.sequencing"">Sequencing - a story with a known sequence such as the days of the week or that uses numbers in a pattern</li>
      <li i18n=""leveled.reader.building.sequence"">Building Sequence - a story with a pattern that is repeated and added to with each new page</li>
      <li i18n=""leveled.reader.rhyme"">Rhyme - a story with a pattern or sequence that also includes rhyme. For example:
        <blockquote class=""poetry"">
          <pre>Brown Bear, Brown Bear, What do you see?
I see a red bird looking at me.
Red Bird, Red Bird, What do you see?
I see a yellow duck looking at me.
Yellow Duck, Yellow Duck, What do you see?</pre>
          <div class=""author"">- Bill Martin, Jr. and Eric Carle</div>
        </blockquote>
      </li>
    </ul>
  </body>
</html>");
			/* Expected output (ignore extraneous whitespace)
			<?xml version="1.0" encoding="utf-8"?>
			<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:html="http://www.w3.org/TR/html" xmlns:sil="http://sil.org/software/XLiff">
			  <file original="foo.html" datatype="html" source-language="en">
				<body>
				  <trans-unit id="leveled.reader.predictability">
					<source xml:lang="en">Predictability</source>
					<target />
					<note>ID: leveled.reader.predictability</note>
				  </trans-unit>
				  <trans-unit id="leveled.reader.repeat.patterns">
					<source xml:lang="en"><g id="genid-1" ctype="x-html-em">Predictability</g> in a text means that the reader can guess what would come next. You can increase predictability by using repeated patterns. Here are some patterns you can use:</source>
					<target />
					<note>ID: leveled.reader.repeat.patterns</note>
				  </trans-unit>
				  <trans-unit id="leveled.reader.repetition">
					<source xml:lang="en">Repetition - repeating parts of the text, for example, using the same sentence with each page and just changing one word in the sentence</source>
					<target />
					<note>ID: leveled.reader.repetition</note>
				  </trans-unit>
				  <trans-unit id="leveled.reader.sequencing">
					<source xml:lang="en">Sequencing - a story with a known sequence such as the days of the week or that uses numbers in a pattern</source>
					<target />
					<note>ID: leveled.reader.sequencing</note>
				  </trans-unit>
				  <trans-unit id="leveled.reader.building.sequence">
					<source xml:lang="en">Building Sequence - a story with a pattern that is repeated and added to with each new page</source>
					<target />
					<note>ID: leveled.reader.building.sequence</note>
				  </trans-unit>
				  <trans-unit id="leveled.reader.rhyme">
					<source xml:lang="en">Rhyme - a story with a pattern or sequence that also includes rhyme. For example:
					<g id="genid-2" ctype="x-html-blockquote" html:class="poetry">
					  <g id="genid-3" ctype="x-html-pre">Brown Bear, Brown Bear, What do you see?
			I see a red bird looking at me.
			Red Bird, Red Bird, What do you see?
			I see a yellow duck looking at me.
			Yellow Duck, Yellow Duck, What do you see?</g>
					  <g id="genid-4" ctype="x-html-div" html:class="author">- Bill Martin, Jr. and Eric Carle</g>
					</g></source>
					<target />
					<note>ID: leveled.reader.rhyme</note>
				  </trans-unit>
				</body>
			  </file>
			</xliff>
			*/
			var xmlDoc = extractor.Extract();
			Assert.IsNotNull(xmlDoc);
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestNamespaceFixing3 did not validate against schema: {0}");

			var xdoc = TestWriteXliff(xmlDoc);
			ValidateXliffOutput(xdoc.OuterXml, "Cleaned up Xliff for TestNamespaceFixing3 did not validate against schema: {0}");

			var nsmgr = HtmlXliff.CreateNamespaceManager(xdoc);
			var body = xdoc.SelectSingleNode("/x:xliff/x:file/x:body", nsmgr);
			Assert.IsNotNull(body);
			VerifyTransUnitLayout(body, 6);

			// Check for no leading or trailing whitespace, but also preserving internal whitespace.
			var xel = body.SelectSingleNode("./x:trans-unit[@id='leveled.reader.rhyme']/x:source", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.IsTrue(xel.InnerXml.StartsWith("Rhyme - a story with a pattern or sequence that also includes rhyme. For example:" + Environment.NewLine + "        <g id=", StringComparison.Ordinal));
			Assert.IsTrue(xel.InnerXml.EndsWith(" Carle</g>" + Environment.NewLine + "        </g>", StringComparison.Ordinal));

			// Test that two HTML namespace attributes in nested elements are handled correctly, using the global html: prefix
			// instead of having either shared or separate namespace attribute(s) with distinct prefix(es).
			xel = body.SelectSingleNode("./x:trans-unit[@id='leveled.reader.rhyme']/x:source/x:g[@id='genid-2']", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.AreEqual(3, xel.Attributes.Count);
			Assert.AreEqual("id", xel.Attributes[0].Name);
			Assert.AreEqual("", xel.Attributes[0].Prefix);
			Assert.AreEqual("genid-2", xel.Attributes[0].Value);
			Assert.AreEqual("ctype", xel.Attributes[1].Name);
			Assert.AreEqual("", xel.Attributes[1].Prefix);
			Assert.AreEqual("x-html-blockquote", xel.Attributes[1].Value);
			Assert.AreEqual("html:class", xel.Attributes[2].Name);
			Assert.AreEqual("html", xel.Attributes[2].Prefix);
			Assert.AreEqual("poetry", xel.Attributes[2].Value);
			Assert.IsTrue(xel.InnerXml.StartsWith(Environment.NewLine + "          <g id=", StringComparison.Ordinal));
			Assert.IsTrue(xel.InnerXml.Contains("</g>" + Environment.NewLine + "          <g id="));
			Assert.IsTrue(xel.InnerXml.EndsWith(" Carle</g>" + Environment.NewLine + "        ", StringComparison.Ordinal));
			xel = body.SelectSingleNode("./x:trans-unit[@id='leveled.reader.rhyme']/x:source/x:g[@id='genid-2']/x:g[@id='genid-3']", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.AreEqual(2, xel.Attributes.Count);
			Assert.AreEqual("id", xel.Attributes[0].Name);
			Assert.AreEqual("", xel.Attributes[0].Prefix);
			Assert.AreEqual("genid-3", xel.Attributes[0].Value);
			Assert.AreEqual("ctype", xel.Attributes[1].Name);
			Assert.AreEqual("", xel.Attributes[1].Prefix);
			Assert.AreEqual("x-html-pre", xel.Attributes[1].Value);
			Assert.AreEqual("Brown Bear, Brown Bear, What do you see?" + Environment.NewLine +
"I see a red bird looking at me." + Environment.NewLine +
"Red Bird, Red Bird, What do you see?" + Environment.NewLine +
"I see a yellow duck looking at me." + Environment.NewLine +
"Yellow Duck, Yellow Duck, What do you see?", xel.InnerXml);
			Assert.AreEqual("Brown Bear, Brown Bear, What do you see?" + Environment.NewLine +
"I see a red bird looking at me." + Environment.NewLine +
"Red Bird, Red Bird, What do you see?" + Environment.NewLine +
"I see a yellow duck looking at me." + Environment.NewLine +
"Yellow Duck, Yellow Duck, What do you see?", xel.InnerText);
			xel = body.SelectSingleNode("./x:trans-unit[@id='leveled.reader.rhyme']/x:source/x:g[@id='genid-2']//x:g[@id='genid-4']", nsmgr) as XmlElement;
			Assert.IsNotNull(xel);
			Assert.AreEqual(3, xel.Attributes.Count);
			Assert.AreEqual("id", xel.Attributes[0].Name);
			Assert.AreEqual("", xel.Attributes[0].Prefix);
			Assert.AreEqual("genid-4", xel.Attributes[0].Value);
			Assert.AreEqual("ctype", xel.Attributes[1].Name);
			Assert.AreEqual("", xel.Attributes[1].Prefix);
			Assert.AreEqual("x-html-div", xel.Attributes[1].Value);
			Assert.AreEqual("html:class", xel.Attributes[2].Name);
			Assert.AreEqual("html", xel.Attributes[2].Prefix);
			Assert.AreEqual("author", xel.Attributes[2].Value);
			Assert.AreEqual("- Bill Martin, Jr. and Eric Carle", xel.InnerXml);
			Assert.AreEqual("- Bill Martin, Jr. and Eric Carle", xel.InnerText);
		}

		/// <summary>
		/// Verify the expected basic layout of the XLIFF trans-unit elements.
		/// (This may be redundant with VerifyXliffOutput() in some respects.)
		/// </summary>
		private static void VerifyTransUnitLayout(XmlNode body, int elementCount)
		{
			// We have whitespace before and after (and thus between) the trans-unit elements
			// in the body of the xliff document.
			// elementCount XmlElement children and (elementCount + 1) XmlWhitespace children
			Assert.AreEqual(elementCount * 2 + 1, body.ChildNodes.Count);
			foreach (XmlNode n0 in body.ChildNodes)
			{
				if (n0 is XmlWhitespace)
					continue;
				Assert.AreEqual("trans-unit", n0.Name);
				Assert.AreEqual(1, n0.Attributes.Count);
				Assert.AreEqual(3 + 4, n0.ChildNodes.Count);
				Assert.IsTrue(n0.ChildNodes[0] is XmlWhitespace);
				var source = n0.ChildNodes[1];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				Assert.IsTrue(n0.ChildNodes[2] is XmlWhitespace);
				var target = n0.ChildNodes[3];
				Assert.AreEqual("target", target.Name);
				Assert.AreEqual(0, target.Attributes.Count);
				Assert.AreEqual(0, target.ChildNodes.Count);
				Assert.AreEqual("", target.InnerXml);
				Assert.IsTrue(n0.ChildNodes[4] is XmlWhitespace);
				var note = n0.ChildNodes[5];
				Assert.AreEqual("note", note.Name);
				Assert.AreEqual(0, note.Attributes.Count);
				Assert.AreEqual(1, note.ChildNodes.Count);
				Assert.AreEqual("ID: " + n0.Attributes["id"].Value, note.InnerText);
				Assert.IsTrue(n0.ChildNodes[6] is XmlWhitespace);
			}
		}

		/// <summary>
		/// Use a StringBuilder instead of a filesystem file for storing the XLIFF output.
		/// For testing, it preloads and returns another XmlDocument with the generated output.
		/// </summary>
		private static XmlDocument TestWriteXliff(XmlDocument xmlDoc)
		{
			var xbldr = new StringBuilder(10000);
			HtmlXliff.WriteXliffToStringBuilder(xmlDoc, xbldr);
			var xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(xbldr.ToString());
			return xdoc;
		}
	}
}

