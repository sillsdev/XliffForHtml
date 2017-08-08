﻿// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Xml;
using NUnit.Framework;
using HtmlAgilityPack;
using XliffForHtml;

namespace XliffForHtmlTests
{
	[TestFixture]
	public class TestInjectXliff
	{
		[Test]
		public void TestBareAmpersand()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""1 & 2 & 3"">One & two & three & etc.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""es"">
  <body>
   <trans-unit id=""1 &amp; 2 &amp; 3"">
    <source xml:lang=""en"">One &amp; two &amp; three &amp; etc.</source>
    <target xml:lang=""es"">Uno &amp; dos &amp; tres &amp; etc.</target>
    <note>ID: 1 &amp; 2 &amp; 3</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("es", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("es", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("1 & 2 & 3", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual(1, paras[0].ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, paras[0].FirstChild.NodeType);
			Assert.AreEqual("Uno & dos & tres & etc.", paras[0].InnerText);
			Assert.AreEqual("Uno & dos & tres & etc.", paras[0].InnerHtml);
		}

		[Test]
		public void TestBrBetweenParas()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Test 1"">This is a test.</p>
<br/>
<p data-i18n=""Test 2"">This is only a test.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""be"">
  <body>
   <trans-unit id=""Test 1"">
    <source xml:lang=""en"">This is a test.</source>
    <target xml:lang=""be"">Гэта тэст.</target>
    <note>ID: Test 1</note>
   </trans-unit>
   <trans-unit id=""Test 2"">
    <source xml:lang=""en"">This is only a test.</source>
	<target xml:lang=""be"">Гэта толькі тэст.</target>
    <note>ID: Test 2</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.AreEqual(2, paras.Count);
			Assert.AreEqual("be", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Гэта тэст.", paras[0].InnerHtml);
			Assert.AreEqual("Test 1", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("be", paras[1].Attributes["lang"].Value);
			Assert.AreEqual("be", paras[1].Attributes["xml:lang"].Value);
			Assert.AreEqual("Test 2", paras[1].Attributes["data-i18n"].Value);
			Assert.AreEqual("Гэта толькі тэст.", paras[1].InnerHtml);
		}

		[Test]
		public void TestBrInText()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Two.Lines"">First line<br>second line</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""es"">
  <body>
   <trans-unit id=""Two.Lines"">
    <source xml:lang=""en"">First line<x id=""genid-1"" ctype=""lb"" equiv-text=""&#xA;"" />second line</source>
    <target xml:lang=""es"">Primera linea<x id=""genid-1"" ctype=""lb"" equiv-text=""&#xA;"" />segunda linea</target>
    <note>ID: Two.Lines</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual("es", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("es", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Two.Lines", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("Primera linea<br>segunda linea", paras[0].InnerHtml);
			Assert.AreEqual(3, paras[0].ChildNodes.Count);
			Assert.AreEqual("#text", paras[0].ChildNodes[0].Name);
			Assert.AreEqual("br", paras[0].ChildNodes[1].Name);
			Assert.AreEqual("#text", paras[0].ChildNodes[2].Name);
		}

		[Test]
		public void TestClassAttribute()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<h2 class=""article-title"" i18n=""article-title"">Life and Habitat of the Marmot</h2>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""de"">
  <body>
   <trans-unit id=""article-title"">
    <source xml:lang=""en"">Life and Habitat of the Marmot</source>
    <target xml:lang=""de"">Leben und Lebensraum des Murmeltieres</target>
    <note>ID: article-title</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.IsNull(paras);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h2");
			Assert.AreEqual(1, headings.Count);
			Assert.AreEqual("de", headings[0].Attributes["lang"].Value);
			Assert.AreEqual("de", headings[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("article-title", headings[0].Attributes["class"].Value);
			Assert.AreEqual("article-title", headings[0].Attributes["i18n"].Value);
			Assert.AreEqual("Leben und Lebensraum des Murmeltieres", headings[0].InnerHtml);
			Assert.AreEqual("Leben und Lebensraum des Murmeltieres", headings[0].InnerText);
		}


		[Test]
		public void TestDuplicateI18nStrings()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""ga"">
  <body>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.ThisPage"">
    <source xml:lang=""en"">This Page</source>
    <target xml:lang=""ga"">seo Leathanach</target>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.Max"">
    <source xml:lang=""en"">Maximum</source>
    <target xml:lang=""ga"">uasmhéid</target>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.LeveledReaderTool.ThisBook"">
    <source xml:lang=""en"">This Book</source>
    <target xml:lang=""ga"">seo Leabhar</target>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var cells = translatedHtml.DocumentNode.SelectNodes("/html/body/table/tr/td");
			Assert.AreEqual(4, cells.Count);
			Assert.AreEqual("ga", cells[0].Attributes["lang"].Value);
			Assert.AreEqual("ga", cells[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("tableTitle thisPageSection", cells[0].Attributes["class"].Value);
			Assert.AreEqual("EditTab.Toolbox.LeveledReaderTool.ThisPage", cells[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("seo Leathanach", cells[0].InnerHtml);
			Assert.AreEqual("seo Leathanach", cells[0].InnerText);
			Assert.AreEqual("ga", cells[1].Attributes["lang"].Value);
			Assert.AreEqual("ga", cells[1].Attributes["xml:lang"].Value);
			Assert.AreEqual("statistics-max", cells[1].Attributes["class"].Value);
			Assert.AreEqual("EditTab.Toolbox.LeveledReaderTool.Max", cells[1].Attributes["data-i18n"].Value);
			Assert.AreEqual("uasmhéid", cells[1].InnerHtml);
			Assert.AreEqual("uasmhéid", cells[1].InnerText);
			Assert.AreEqual("ga", cells[2].Attributes["lang"].Value);
			Assert.AreEqual("ga", cells[2].Attributes["xml:lang"].Value);
			Assert.AreEqual("tableTitle", cells[2].Attributes["class"].Value);
			Assert.AreEqual("EditTab.Toolbox.LeveledReaderTool.ThisBook", cells[2].Attributes["data-i18n"].Value);
			Assert.AreEqual("seo Leabhar", cells[2].InnerHtml);
			Assert.AreEqual("seo Leabhar", cells[2].InnerText);
			Assert.AreEqual("ga", cells[3].Attributes["lang"].Value);
			Assert.AreEqual("ga", cells[3].Attributes["xml:lang"].Value);
			Assert.AreEqual("statistics-max", cells[3].Attributes["class"].Value);
			Assert.AreEqual("EditTab.Toolbox.LeveledReaderTool.Max", cells[3].Attributes["data-i18n"].Value);
			Assert.AreEqual("uasmhéid", cells[3].InnerHtml);
			Assert.AreEqual("uasmhéid", cells[3].InnerText);
		}

		[Test]
		public void TestFormInDiv()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""cy"">
  <body>
   <trans-unit id=""EditTab.Toolbox.Settings.UnlockShellBookIntroductionText"">
    <source xml:lang=""en"">Bloom normally prevents most changes to shellbooks. If you need to add pages, change images, etc., tick the box below.</source>
    <target xml:lang=""cy"">Fel arfer, Blodau yn atal y rhan fwyaf o newidiadau i'r shellbooks. Os oes angen i ychwanegu tudalennau, newid delweddau, ac ati, ticiwch y blwch isod.</target>
   </trans-unit>
   <trans-unit id=""EditTab.Toolbox.Settings.Unlock"">
    <source xml:lang=""en"">Allow changes to this shellbook</source>
    <target xml:lang=""cy"">Caniatáu newidiadau i'r shellbook hwn</target>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/div/form/div/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("cy", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("cy", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("EditTab.Toolbox.Settings.UnlockShellBookIntroductionText", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("Fel arfer, Blodau yn atal y rhan fwyaf o newidiadau i'r shellbooks. Os oes angen i ychwanegu tudalennau, newid delweddau, ac ati, ticiwch y blwch isod.", paras[0].InnerHtml);
			var labels = translatedHtml.DocumentNode.SelectNodes("/html/body/div/form/div/label");
			Assert.AreEqual(1, labels.Count);
			Assert.AreEqual(3, labels[0].Attributes.Count);
			Assert.AreEqual("cy", labels[0].Attributes["lang"].Value);
			Assert.AreEqual("cy", labels[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("EditTab.Toolbox.Settings.Unlock", labels[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("Caniatáu newidiadau i'r shellbook hwn", labels[0].InnerHtml);
		}

		[Test]
		public void TestHtmlFragment()
		{
			var injector = HtmlXliff.Parse(@"
<div class=""bloom-ui bloomDialogContainer"" id=""text-properties-dialog"" style=""visibility: hidden;"">
  <div class=""bloomDialogTitleBar"" data-i18n=""EditTab.TextBoxProperties.Title"">Text Box Properties</div>
  <div class=""hideWhenFormattingEnabled bloomDialogMainPage"">
    <p data-i18n=""BookEditor.FormattingDisabled"">Sorry, Reader Templates do not allow changes to formatting.</p>
  </div>
</div>
");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""eu"">
  <body>
    <trans-unit id=""EditTab.TextBoxProperties.Title"">
     <source xml:lang=""en"">Text Box Properties</source>
     <target xml:lang=""eu"">Testu-koadroko propietateak</target>
    </trans-unit>
    <trans-unit id=""BookEditor.FormattingDisabled"">
     <source xml:lang=""en"">Sorry, Reader Templates do not allow changes to formatting.</source>
     <target xml:lang=""eu"">Barkatu, Reader Templates-ek ez du formatua aldatzeko baimenik.</target>
    </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var divs = translatedHtml.DocumentNode.SelectNodes("/div/div");
			Assert.AreEqual(2, divs.Count);
			Assert.AreEqual(4, divs[0].Attributes.Count);
			Assert.AreEqual("eu", divs[0].Attributes["lang"].Value);
			Assert.AreEqual("eu", divs[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("bloomDialogTitleBar", divs[0].Attributes["class"].Value);
			Assert.AreEqual("EditTab.TextBoxProperties.Title", divs[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("Testu-koadroko propietateak", divs[0].InnerHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/div/div/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("eu", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("eu", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("BookEditor.FormattingDisabled", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual("Barkatu, Reader Templates-ek ez du formatua aldatzeko baimenik.", paras[0].InnerHtml);
		}

		[Test]
		public void TestImg()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p i18n='Mount Hood'>This is Mount Hood: <img src=""mthood.jpg""></p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""fr"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">This is Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" /></source>
    <target xml:lang=""fr"">C'est Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" /></target>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("fr", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("fr", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Mount Hood", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("C'est Mount Hood: ", paras[0].InnerText);
			Assert.AreEqual("C'est Mount Hood: <img src=\"mthood.jpg\">", paras[0].InnerHtml);
		}

		[Test]
		public void TestImgWithAlt()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p data-i18n=""Mount Hood"">My picture,
<img src=""mthood.jpg"" alt=""This is a shot of Mount Hood"" />
and there you have it.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""ko"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">My picture,
<x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""This is a shot of Mount Hood"" />
and there you have it.</source>
    <target xml:lang=""en"">내 그림,
<x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""This is a shot of Mount Hood"" />
거기에 당신이 그것을 가지고 있습니다.</target>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("ko", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("ko", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Mount Hood", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual(@"내 그림,

거기에 당신이 그것을 가지고 있습니다.", paras[0].InnerText);
			Assert.AreEqual(@"내 그림,
<img src=""mthood.jpg"" alt=""This is a shot of Mount Hood"">
거기에 당신이 그것을 가지고 있습니다.", paras[0].InnerHtml);
		}

		[Test]
		public void TestImgWithTitleAndAlt()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p title='Information about Mount Hood' i18n=""Mount Hood"">This is Mount Hood: <img src=""mthood.jpg"" alt=""Mount Hood with its snow-covered top""></p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""pt"">
  <body>
   <trans-unit id=""Mount Hood"">
    <source xml:lang=""en"">This is Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""Mount Hood with its snow-covered top"" /></source>
    <target xml:lang=""pt"">Este é Mount Hood: <x id=""genid-1"" ctype=""image"" html:src=""mthood.jpg"" html:alt=""Mount Hood with its snow-covered top"" /></target>
    <note>ID: Mount Hood</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(4, paras[0].Attributes.Count);
			Assert.AreEqual("pt", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("pt", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Information about Mount Hood", paras[0].Attributes["title"].Value);
			Assert.AreEqual("Mount Hood", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("Este é Mount Hood: ", paras[0].InnerText);
			Assert.AreEqual("Este é Mount Hood: <img src=\"mthood.jpg\" alt=\"Mount Hood with its snow-covered top\">", paras[0].InnerHtml);
		}

		[Test]
		public void TestInlineElements()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p i18n='Portland mountain, river, & sea'>In Portland, Oregon one may <i>ski</i> on the mountain, <b>wind surf</b> in the gorge, and <i>surf</i> in the ocean, all on the same day.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""af"">
  <body>
   <trans-unit id=""Portland mountain, river, &amp; sea"" restype=""x-html-p"">
    <source xml:lang=""en"">In Portland, Oregon one may <g id=""genid-1"" ctype=""italic"">ski</g> on the mountain, <g id=""genid-2"" ctype=""bold"">wind surf</g> in the gorge, and <g id=""genid-3"" ctype=""italic"">surf</g> in the ocean, all on the same day.</source>
    <target xml:lang=""af"">In Portland, Oregon kan mens op die berg <g id=""genid-1"" ctype=""italic"">skiet</g>, <g id=""genid-2"" ctype=""bold"">windswaai</g> in die kloof, en op dieselfde dag in die see <g id=""genid-3"" ctype=""italic"">rondbeweeg</g>.</target>
    <note>ID: Portland mountain, river, &amp; sea</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("af", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("af", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Portland mountain, river, & sea", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("In Portland, Oregon kan mens op die berg skiet, windswaai in die kloof, en op dieselfde dag in die see rondbeweeg.", paras[0].InnerText);
			Assert.AreEqual("In Portland, Oregon kan mens op die berg <i>skiet</i>, <b>windswaai</b> in die kloof, en op dieselfde dag in die see <i>rondbeweeg</i>.", paras[0].InnerHtml);
		}

		[Test]
		public void TestInlineElementWithLang()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<P i18n=""Mixed Language Motto"">The words <Q lang=""fr"">Je me souviens</Q> are the motto of Québec.</P>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""it"">
  <body>
   <trans-unit id=""Mixed Language Motto"">
    <source xml:lang=""en"">The words <g id=""genid-1"" ctype=""x-html-q"" xml:lang=""fr"">Je me souviens</g> are the motto of Québec.</source>
    <target xml:lang=""it"">Le parole <g id=""genid-1"" ctype=""x-html-q"" xml:lang=""fr"">Je me souviens</g> sono il motto di Québec.</target>
   <note>ID: </note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("it", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("it", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Mixed Language Motto", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("Le parole Je me souviens sono il motto di Québec.", paras[0].InnerText);
			Assert.AreEqual("Le parole <q lang=\"fr\">Je me souviens</q> sono il motto di Québec.", paras[0].InnerHtml);
		}

		[Test]
		public void TestInlineSpans()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""colorful info"">Questions will appear in <span fontcolor=""#339966"">Green
face</span>, while answers will appear in <span fontcolor=""#333399"">Indigo
face</span>.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""ru"">
  <body>
   <trans-unit id=""colorful info"">
    <source xml:lang=""en"">Questions will appear in <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#339966"">Green
face</g>, while answers will appear in <g id=""genid-2"" ctype=""x-html-span"" html:fontcolor=""#333399"">Indigo
face</g>.</source>
      <target xml:lang=""ru"">Вопросы появятся в <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#339966"">зеленом
Лицо</g>, в то время как ответы появятся в <g id=""genid-2"" ctype=""x-html-span"" html:fontcolor=""#333399"">индиго
лицо</g>.</target>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("ru", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("ru", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("colorful info", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual(@"Вопросы появятся в зеленом
Лицо, в то время как ответы появятся в индиго
лицо.", paras[0].InnerText);
			Assert.AreEqual(@"Вопросы появятся в <span fontcolor=""#339966"">зеленом
Лицо</span>, в то время как ответы появятся в <span fontcolor=""#333399"">индиго
лицо</span>.", paras[0].InnerHtml);
		}

		[Test]
		public void TestSpanWithLang()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<p i18n=""French Quote She Said"">She added that ""<span lang='fr'>je ne sais quoi</span>"" that made her casserole absolutely delicious.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""sv"">
  <body>
   <trans-unit id=""French Quote She Said"">
    <source xml:lang=""en"">She added that ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"">je ne sais quoi</g>"" that made her casserole absolutely delicious.</source>
    <target xml:lang=""se"">Hon lade till att ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"">je ne sais quoi</g>"" som gjorde hennes gryta absolut läckra.</target>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("sv", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("sv", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("French Quote She Said", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual(@"Hon lade till att ""je ne sais quoi"" som gjorde hennes gryta absolut läckra.", paras[0].InnerText);
			Assert.AreEqual(@"Hon lade till att ""<span lang=""fr"">je ne sais quoi</span>"" som gjorde hennes gryta absolut läckra.", paras[0].InnerHtml);
		}

		[Test]
		public void TestTableContent()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""gr"">
  <body>
   <trans-unit id=""Page.Title"">
    <source xml:lang=""en"">Report</source>
    <target xml:lang=""gr"">αναφορά</target>
    <note>ID: Page.Title</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.1"">
    <source xml:lang=""en"">Text in cell r1-c1</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r1-c1</target>
    <note>ID: Table.Cell-1.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.2"">
    <source xml:lang=""en"">Text in cell r1-c2</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r1-c2</target>
    <note>ID: Table.Cell-1.2</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.1"">
    <source xml:lang=""en"">Text in cell r2-c1</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r2-c1</target>
    <note>ID: Table.Cell-2.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.2"">
    <source xml:lang=""en"">Text in cell r2-c2</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r2-c2</target>
    <note>ID: Table.Cell-2.2</note>
   </trans-unit>
   <trans-unit id=""Copyright.Notice"">
    <source xml:lang=""en"">All rights reserved (c) Gandalf Inc.</source>
    <target xml:lang=""gr"">Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.</target>
    <note>ID: Copyright.Notice</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h1");
			Assert.AreEqual(1, headings.Count);
			Assert.AreEqual(4, headings[0].Attributes.Count);
			Assert.AreEqual("gr", headings[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", headings[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("title", headings[0].Attributes["class"].Value);
			Assert.AreEqual("Page.Title", headings[0].Attributes["i18n"].Value);
			Assert.AreEqual("αναφορά", headings[0].InnerHtml);

			var cells = translatedHtml.DocumentNode.SelectNodes("/html/body/table/tr/td");
			Assert.AreEqual(4, cells.Count);
			Assert.AreEqual(4, cells[0].Attributes.Count);
			Assert.AreEqual("gr", cells[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", cells[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Table.Cell-1.1", cells[0].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[0].Attributes["valign"].Value);
			Assert.AreEqual("Κείμενο στο κελί r1-c1", cells[0].InnerHtml);
			Assert.AreEqual(4, cells[1].Attributes.Count);
			Assert.AreEqual("gr", cells[1].Attributes["lang"].Value);
			Assert.AreEqual("gr", cells[1].Attributes["xml:lang"].Value);
			Assert.AreEqual("Table.Cell-1.2", cells[1].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[1].Attributes["valign"].Value);
			Assert.AreEqual("Κείμενο στο κελί r1-c2", cells[1].InnerHtml);
			Assert.AreEqual(4, cells[2].Attributes.Count);
			Assert.AreEqual("gr", cells[2].Attributes["lang"].Value);
			Assert.AreEqual("gr", cells[2].Attributes["xml:lang"].Value);
			Assert.AreEqual("Table.Cell-2.1", cells[2].Attributes["i18n"].Value);
			Assert.AreEqual("#C0C0C0", cells[2].Attributes["bgcolor"].Value);
			Assert.AreEqual("Κείμενο στο κελί r2-c1", cells[2].InnerHtml);
			Assert.AreEqual(3, cells[3].Attributes.Count);
			Assert.AreEqual("gr", cells[3].Attributes["lang"].Value);
			Assert.AreEqual("gr", cells[3].Attributes["xml:lang"].Value);
			Assert.AreEqual("Table.Cell-2.2", cells[3].Attributes["i18n"].Value);
			Assert.AreEqual("Κείμενο στο κελί r2-c2", cells[3].InnerHtml);

			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("gr", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Copyright.Notice", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.", paras[0].InnerHtml);
		}

		[Test]
		public void TestZeroI18nMarks()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""gr"">
  <body>
   <trans-unit id=""Page.Title"">
    <source xml:lang=""en"">Report</source>
    <target xml:lang=""gr"">αναφορά</target>
    <note>ID: Page.Title</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.1"">
    <source xml:lang=""en"">Text in cell r1-c1</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r1-c1</target>
    <note>ID: Table.Cell-1.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.2"">
    <source xml:lang=""en"">Text in cell r1-c2</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r1-c2</target>
    <note>ID: Table.Cell-1.2</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.1"">
    <source xml:lang=""en"">Text in cell r2-c1</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r2-c1</target>
    <note>ID: Table.Cell-2.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-2.2"">
    <source xml:lang=""en"">Text in cell r2-c2</source>
    <target xml:lang=""gr"">Κείμενο στο κελί r2-c2</target>
    <note>ID: Table.Cell-2.2</note>
   </trans-unit>
   <trans-unit id=""Copyright.Notice"">
    <source xml:lang=""en"">All rights reserved (c) Gandalf Inc.</source>
    <target xml:lang=""gr"">Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.</target>
    <note>ID: Copyright.Notice</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h1");
			Assert.AreEqual(1, headings.Count);
			Assert.AreEqual(1, headings[0].Attributes.Count);
			Assert.AreEqual("title", headings[0].Attributes["class"].Value);
			Assert.AreEqual("Report", headings[0].InnerHtml);

			var cells = translatedHtml.DocumentNode.SelectNodes("/html/body/table/tr/td");
			Assert.AreEqual(4, cells.Count);
			Assert.AreEqual(1, cells[0].Attributes.Count);
			Assert.AreEqual("top", cells[0].Attributes["valign"].Value);
			Assert.AreEqual("Text in cell r1-c1", cells[0].InnerHtml);
			Assert.AreEqual(1, cells[1].Attributes.Count);
			Assert.AreEqual("top", cells[1].Attributes["valign"].Value);
			Assert.AreEqual("Text in cell r1-c2", cells[1].InnerHtml);
			Assert.AreEqual(1, cells[2].Attributes.Count);
			Assert.AreEqual("#C0C0C0", cells[2].Attributes["bgcolor"].Value);
			Assert.AreEqual("Text in cell r2-c1", cells[2].InnerHtml);
			Assert.AreEqual(0, cells[3].Attributes.Count);
			Assert.AreEqual("Text in cell r2-c2", cells[3].InnerHtml);

			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(0, paras[0].Attributes.Count);
			Assert.AreEqual("All rights reserved (c) Gandalf Inc.", paras[0].InnerHtml);
		}

		[Test]
		public void TestMissingTranslations()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""gr"">
  <body>
   <trans-unit id=""Page.Title"">
    <source xml:lang=""en"">Report</source>
    <target xml:lang=""gr"">αναφορά</target>
    <note>ID: Page.Title</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.1"">
    <source xml:lang=""en"">Text in cell r1-c1</source>
    <target xml:lang=""gr""></target>
    <note>ID: Table.Cell-1.1</note>
   </trans-unit>
   <trans-unit id=""Table.Cell-1.2"">
    <source xml:lang=""en"">Text in cell r1-c2</source>
    <target xml:lang=""gr""></target>
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
    <target xml:lang=""gr"">Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.</target>
    <note>ID: Copyright.Notice</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc);
			Assert.IsNotNull(translatedHtml);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h1");
			Assert.AreEqual(1, headings.Count);
			Assert.AreEqual(4, headings[0].Attributes.Count);
			Assert.AreEqual("gr", headings[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", headings[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("title", headings[0].Attributes["class"].Value);
			Assert.AreEqual("Page.Title", headings[0].Attributes["i18n"].Value);
			Assert.AreEqual("αναφορά", headings[0].InnerHtml);

			var cells = translatedHtml.DocumentNode.SelectNodes("/html/body/table/tr/td");
			Assert.AreEqual(4, cells.Count);
			Assert.AreEqual(2, cells[0].Attributes.Count);
			Assert.AreEqual("Table.Cell-1.1", cells[0].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[0].Attributes["valign"].Value);
			Assert.AreEqual("Text in cell r1-c1", cells[0].InnerHtml);
			Assert.AreEqual(2, cells[1].Attributes.Count);
			Assert.AreEqual("Table.Cell-1.2", cells[1].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[1].Attributes["valign"].Value);
			Assert.AreEqual("Text in cell r1-c2", cells[1].InnerHtml);
			Assert.AreEqual(2, cells[2].Attributes.Count);
			Assert.AreEqual("Table.Cell-2.1", cells[2].Attributes["i18n"].Value);
			Assert.AreEqual("#C0C0C0", cells[2].Attributes["bgcolor"].Value);
			Assert.AreEqual("Text in cell r2-c1", cells[2].InnerHtml);
			Assert.AreEqual(1, cells[3].Attributes.Count);
			Assert.AreEqual("Table.Cell-2.2", cells[3].Attributes["i18n"].Value);
			Assert.AreEqual("Text in cell r2-c2", cells[3].InnerHtml);

			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("gr", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Copyright.Notice", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.", paras[0].InnerHtml);
		}
	}
}
