// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using HtmlAgilityPack;
using XliffForHtml;

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
			/* Expected output (ignor extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""1 & 2 & 3"">
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
/* Expected output (ignor extraneous whitespace)
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
/* expected output (ignor extraneous whitespace)
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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("First line", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("x", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("lb", n0.Attributes["ctype"].Value);
					Assert.AreEqual("\n", n0.Attributes["equiv-text"].Value);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("second line", n0.InnerText);
					break;
				}
				++index;
			}
			CheckNoteElement(tu);
		}

		[Test]
		public void TestClassAttribute()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<h2 class=""article-title"" i18n=""article-title"">Life and Habitat of the Marmot</h2>
</body>
</html>");
/* Expected output (ignor extraneous whitespace)
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
/*expected output (ignor extraneous whitespace)
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
/* Expected output (ignor extraneous whitespace)
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
/* Expected output (ignor extraneous whitespace)
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
/* Expected output (ignor extraneous whitespace)
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
			ValidateXliffOutput(xmlDoc.OuterXml, "Xliff for TestImgWithTitleandAlt did not validate against schema: {0}");

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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("This is Mount Hood: ", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("x", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("image", n0.Attributes["ctype"].Value);
					Assert.AreEqual("mthood.jpg", n0.Attributes["src", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
					break;
				}
				++index;
			}
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
/* Expected output (ignor extraneous whitespace)
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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(@"My picture,
", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("x", n0.Name);
					Assert.AreEqual(4, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("image", n0.Attributes["ctype"].Value);
					Assert.AreEqual("mthood.jpg", n0.Attributes["src", HtmlXliff.kHtmlNamespace].Value);
					Assert.AreEqual("This is a shot of Mount Hood", n0.Attributes["alt", HtmlXliff.kHtmlNamespace].Value);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(@"
and there you have it.", n0.InnerText);
					break;
				}
				++index;
			}
			CheckNoteElement(tu);
		}

		[Test]
		public void TestImgWithTitleAndAlt()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p title='Information about Mount Hood' i18n=""Mount Hood"">This is Mount Hood: <img src=""mthood.jpg"" alt=""Mount Hood with its snow-covered top""></p>
</body>
</html>");
/* Expected output (ignor extraneous whitespace)
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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("This is Mount Hood: ", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("x", n0.Name);
					Assert.AreEqual(4, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("image", n0.Attributes["ctype"].Value);
					Assert.AreEqual("mthood.jpg", n0.Attributes["src", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
					Assert.AreEqual("Mount Hood with its snow-covered top", n0.Attributes["alt", XliffForHtml.HtmlXliff.kHtmlNamespace].Value);
					break;
				}
				++index;
			}
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
/* Expected output (ignor extraneous whitespace)
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
/* Expected output (ignor extraneous whitespace)
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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("The words ", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("x-html-q", n0.Attributes["ctype"].Value);
					Assert.AreEqual("fr", n0.Attributes["xml:lang"].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("Je me souviens", n0.FirstChild.InnerText);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual(" are the motto of Québec.", n0.InnerText);
					break;
				}
				++index;
			}
			CheckNoteElement(tu);
		}

		[Test]
		public void TestInlineSpans()
		{
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""colorful info"">Questions will appear in <span fontcolor=""#339966"">Green
face</span>, while answers will appear in <span fontcolor=""#333399"">Indigo
face</span>.</p>
</body>
</html>");
/* Expected output (ignor extraneous whitespace)
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
					Assert.AreEqual(@"Green
face", n0.FirstChild.InnerText);
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
					Assert.AreEqual(@"Indigo
face", n0.FirstChild.InnerText);
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
			var extractor = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""French Quote She Said"">She added that ""<span lang='fr'>je ne sais quoi</span>"" that made her casserole absolutely delicious.</p>
</body>
</html>");
/* Expected output (ignor extraneous whitespace)
<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"">
  <body>
   <trans-unit id=""French Quote She Said"">
    <source xml:lang=""en"">She added that ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"">je ne sais quoi</g>"" that made her casserole absolutely delicious.</source>
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
			int index = 0;
			foreach (XmlNode n0 in source.ChildNodes)
			{
				switch (index)
				{
				case 0:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("She added that \"", n0.InnerText);
					break;
				case 1:
					Assert.AreEqual("g", n0.Name);
					Assert.AreEqual(3, n0.Attributes.Count);
					Assert.AreEqual("genid-1", n0.Attributes["id"].Value);
					Assert.AreEqual("x-html-span", n0.Attributes["ctype"].Value);
					Assert.AreEqual("fr", n0.Attributes["xml:lang"].Value);
					Assert.AreEqual(1, n0.ChildNodes.Count);
					Assert.AreEqual(XmlNodeType.Text, n0.FirstChild.NodeType);
					Assert.AreEqual("je ne sais quoi", n0.FirstChild.InnerText);
					break;
				case 2:
					Assert.AreEqual(XmlNodeType.Text, n0.NodeType);
					Assert.AreEqual("\" that made her casserole absolutely delicious.", n0.InnerText);
					break;
				}
				++index;
			}
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
/* Expected output (ignor extraneous whitespace)
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
				Assert.AreEqual(2, tu.ChildNodes.Count);

				var source = tu.ChildNodes[0];
				Assert.AreEqual("source", source.Name);
				Assert.AreEqual(1, source.Attributes.Count);
				Assert.AreEqual("en", source.Attributes["xml:lang"].Value);
				Assert.AreEqual(1, source.ChildNodes.Count);
				Assert.AreEqual(XmlNodeType.Text, source.FirstChild.NodeType);
				Assert.AreEqual(values[index], source.InnerXml);

				CheckNoteElement(tu);
				++index;
			}
		}

		private void CheckNoteElement(XmlNode transUnit)
		{
			var note = transUnit.ChildNodes[1];
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
	}
}

