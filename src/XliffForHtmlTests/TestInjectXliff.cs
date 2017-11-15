// Copyright (c) 2017 SIL International
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("es", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("es", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("1 & 2 & 3", paras[0].Attributes["data-i18n"].Value);
			Assert.AreEqual(1, paras[0].ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, paras[0].FirstChild.NodeType);
			Assert.AreEqual("Uno &amp; dos &amp; tres &amp; etc.", paras[0].InnerHtml);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var bodyNodes = translatedHtml.DocumentNode.SelectSingleNode("/html/body").ChildNodes;
			Assert.AreEqual(7, bodyNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, bodyNodes[0].NodeType);
			Assert.AreEqual("p", bodyNodes[1].Name);
			Assert.AreEqual(HtmlNodeType.Text, bodyNodes[2].NodeType);
			Assert.AreEqual("br", bodyNodes[3].Name);
			Assert.AreEqual(HtmlNodeType.Text, bodyNodes[4].NodeType);
			Assert.AreEqual("p", bodyNodes[5].Name);
			Assert.AreEqual(HtmlNodeType.Text, bodyNodes[6].NodeType);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
		public void TestClassAttributeIsPreserved()
		{
			var injector = HtmlXliff.Parse(@"<html>
<body>
<h2 class=""Article-Title"" i18n=""article-title"">Life and Habitat of the Marmot</h2>
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.IsNull(paras);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h2");
			Assert.AreEqual(1, headings.Count);
			Assert.AreEqual("de", headings[0].Attributes["lang"].Value);
			Assert.AreEqual("de", headings[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Article-Title", headings[0].Attributes["class"].Value);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
		public void TestTitleAndAltArePreserved()
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
<p i18n=""French Quote She Said"">She added that ""<span lang='fr' fontcolor=""#339966"">je ne sais quoi</span>"" that made her casserole absolutely delicious.</p>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""sv"">
  <body>
   <trans-unit id=""French Quote She Said"">
    <source xml:lang=""en"">She added that ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"" html:fontcolor=""#339966"">je ne sais quoi</g>"" that made her casserole absolutely delicious.</source>
    <target xml:lang=""se"">Hon lade till att ""<g id=""genid-1"" ctype=""x-html-span"" xml:lang=""fr"" html:fontcolor=""#339966"">je ne sais quoi</g>"" som gjorde hennes gryta absolut läckra.</target>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("sv", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("sv", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("French Quote She Said", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual(@"Hon lade till att ""je ne sais quoi"" som gjorde hennes gryta absolut läckra.", paras[0].InnerText);
			Assert.AreEqual(@"Hon lade till att ""<span lang=""fr"" fontcolor=""#339966"">je ne sais quoi</span>"" som gjorde hennes gryta absolut läckra.", paras[0].InnerHtml);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
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
   <trans-unit id=""Copyright.Notice"">
    <source xml:lang=""en"">All rights reserved (c) Gandalf Inc.</source>
    <target xml:lang=""gr"">Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.</target>
    <note>ID: Copyright.Notice</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var headings = translatedHtml.DocumentNode.SelectNodes("/html/body/h1");
			Assert.AreEqual(1, headings.Count);
			// lang attributes added, Russian text substituted for original English
			Assert.AreEqual(4, headings[0].Attributes.Count);
			Assert.AreEqual("gr", headings[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", headings[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("title", headings[0].Attributes["class"].Value);
			Assert.AreEqual("Page.Title", headings[0].Attributes["i18n"].Value);
			Assert.AreEqual("αναφορά", headings[0].InnerHtml);

			var cells = translatedHtml.DocumentNode.SelectNodes("/html/body/table/tr/td");
			Assert.AreEqual(4, cells.Count);
			// original English text retained, English lang attributes added
			Assert.AreEqual(4, cells[0].Attributes.Count);
			Assert.AreEqual("Table.Cell-1.1", cells[0].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[0].Attributes["valign"].Value);
			Assert.AreEqual("en", cells[0].Attributes["lang"].Value);
			Assert.AreEqual("en", cells[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Text in cell r1-c1", cells[0].InnerHtml);
			// original English text retained, English lang attributes added
			Assert.AreEqual(4, cells[1].Attributes.Count);
			Assert.AreEqual("Table.Cell-1.2", cells[1].Attributes["i18n"].Value);
			Assert.AreEqual("top", cells[1].Attributes["valign"].Value);
			Assert.AreEqual("en", cells[1].Attributes["lang"].Value);
			Assert.AreEqual("en", cells[1].Attributes["xml:lang"].Value);
			Assert.AreEqual("Text in cell r1-c2", cells[1].InnerHtml);
			// original English text retained, English lang attributes added
			Assert.AreEqual(4, cells[2].Attributes.Count);
			Assert.AreEqual("Table.Cell-2.1", cells[2].Attributes["i18n"].Value);
			Assert.AreEqual("#C0C0C0", cells[2].Attributes["bgcolor"].Value);
			Assert.AreEqual("en", cells[2].Attributes["lang"].Value);
			Assert.AreEqual("en", cells[2].Attributes["xml:lang"].Value);
			Assert.AreEqual("Text in cell r2-c1", cells[2].InnerHtml);
			// original English text retained, English lang attributes added
			Assert.AreEqual(3, cells[3].Attributes.Count);
			Assert.AreEqual("Table.Cell-2.2", cells[3].Attributes["i18n"].Value);
			Assert.AreEqual("en", cells[3].Attributes["lang"].Value);
			Assert.AreEqual("en", cells[3].Attributes["xml:lang"].Value);
			Assert.AreEqual("Text in cell r2-c2", cells[3].InnerHtml);

			var paras = translatedHtml.DocumentNode.SelectNodes("html/body/p");
			Assert.AreEqual(1, paras.Count);
			// lang attributes added, Russian text substituted for original English
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("gr", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("gr", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("Copyright.Notice", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual("Όλα τα δικαιώματα κατοχυρωμένα (γ) Gandalf Inc.", paras[0].InnerHtml);
		}

		[Test]
		public void TestMultiParagraphUnit()
		{
			var injector = HtmlXliff.Parse(@"<html>
 <body>
  <div i18n=""lots of text"">
   <p>This is the first paragraph.</p>
   <p>This is the second paragraph.  It is even longer than the first paragraph.</p>
  </div>
  <p i18n=""single para"">This paragraph is independent of the others.</p>
 </body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""ru"">
  <body>
   <trans-unit id=""lots of text"">
    <source xml:lang=""en"">
   <g id=""genid-1"" ctype=""x-html-p"">This is the first paragraph.</g>
   <g id=""genid-2"" ctype=""x-html-p"">This is the second paragraph.  It is even longer than the first paragraph.</g>
  </source>
    <target xml:lang=""ru"">
   <g id=""genid-1"" ctype=""x-html-p"">Это первый абзац.</g>
   <g id=""genid-2"" ctype=""x-html-p"">Это второй абзац. Это даже длиннее первого абзаца.</g>
  </target>
    <note>ID: lots of text</note>
   </trans-unit>
   <trans-unit id=""single para"">
    <source xml:lang=""en"">This paragraph is independent of the others.</source>
    <target xml:lang=""ru"">Этот параграф не зависит от других.</target>
    <note>ID: single para</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var divs = translatedHtml.DocumentNode.SelectNodes("/html/body/div");
			Assert.AreEqual(1, divs.Count);
			Assert.AreEqual(3, divs[0].Attributes.Count);
			Assert.AreEqual("ru", divs[0].Attributes["lang"].Value);
			Assert.AreEqual("ru", divs[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("lots of text", divs[0].Attributes["i18n"].Value);
			Assert.AreEqual(2, divs[0].ChildNodes.Count);
			var n0 = divs[0].ChildNodes[0];
			Assert.AreEqual("p", n0.Name);
			Assert.AreEqual(0, n0.Attributes.Count);
			Assert.AreEqual("Это первый абзац.", n0.InnerHtml);
			var n1 = divs[0].ChildNodes[1];
			Assert.AreEqual("p", n1.Name);
			Assert.AreEqual(0, n1.Attributes.Count);
			Assert.AreEqual("Это второй абзац. Это даже длиннее первого абзаца.", n1.InnerHtml);
			var paras = translatedHtml.DocumentNode.SelectNodes("/html/body/p");
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(3, paras[0].Attributes.Count);
			Assert.AreEqual("ru", paras[0].Attributes["lang"].Value);
			Assert.AreEqual("ru", paras[0].Attributes["xml:lang"].Value);
			Assert.AreEqual("single para", paras[0].Attributes["i18n"].Value);
			Assert.AreEqual(1, paras[0].ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, paras[0].ChildNodes[0].NodeType);
			Assert.AreEqual("Этот параграф не зависит от других.", paras[0].ChildNodes[0].InnerHtml);
		}

		[Test]
		public void TestWeirdAttributeChars()
		{
			var injector = HtmlXliff.Parse(@"<html>
 <body>
  <p i18n=""translate this!""><span data1=""' < & >"" data2='"" < & >'>This  is a test of strange characters: &lt; & > "" '.</span></p>
 </body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""fr"">
  <body>
   <trans-unit id=""translate this!"">
    <source xml:lang=""en""><g id=""genid-1"" ctype=""x-html-span"" html:data1=""' &lt; &amp; &gt;"" html:data2=""&quot; &lt; &amp; &gt;"">This  is a test of strange characters: &lt; &amp; &gt; "" '.</g></source>
    <target xml:lang=""fr""><g id=""genid-1"" ctype=""x-html-span"" html:data1=""' &lt; &amp; &gt;"" html:data2=""&quot; &lt; &amp; &gt;"">Il s'agit d'un test de caractères étranges: &lt; &amp; &gt; "" '.</g></target>
    <note>ID: translate this!</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var para = translatedHtml.DocumentNode.SelectSingleNode("/html/body/p");
			Assert.AreEqual(3, para.Attributes.Count);
			Assert.AreEqual("translate this!", para.Attributes["i18n"].Value);
			Assert.AreEqual("fr", para.Attributes["lang"].Value);
			Assert.AreEqual("fr", para.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, para.ChildNodes.Count);
			Assert.AreEqual("span", para.ChildNodes[0].Name);

			var span = para.ChildNodes[0];
			Assert.AreEqual(2, span.Attributes.Count);
			Assert.AreEqual("' &lt; &amp; &gt;", span.Attributes["data1"].Value);
			Assert.AreEqual("&quot; &lt; &amp; &gt;", span.Attributes["data2"].Value);
			Assert.AreEqual(1, span.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, span.ChildNodes[0].NodeType);
			Assert.AreEqual("Il s'agit d'un test de caractères étranges: &lt; &amp; &gt; \" '.", span.InnerHtml);
		}

		[Test]
		public void TestTranslationChangesMarkup()
		{
			var injector = HtmlXliff.Parse(@"<html>
 <body>
  <p i18n=""translate please"">Questions are in <span fontcolor=""#229922"">green</span> to set them off.</span></p>
 </body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""fr"">
  <body>
   <trans-unit id=""translate please"">
    <source xml:lang=""en"">Questions are in <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#229922"">green</g> to set them off.</source>
    <target xml:lang=""fr"">Questions will appear in the <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#229922"">face</g> with the colour <g id=""genid-1"" ctype=""x-html-span"" html:fontcolor=""#229922"">green</g>.</target>
    <note>ID: translate this!</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);
			var para = translatedHtml.DocumentNode.SelectSingleNode("/html/body/p");
			Assert.AreEqual(3, para.Attributes.Count);
			Assert.AreEqual("translate please", para.Attributes["i18n"].Value);
			Assert.AreEqual("fr", para.Attributes["lang"].Value);
			Assert.AreEqual("fr", para.Attributes["xml:lang"].Value);
			Assert.AreEqual(5, para.ChildNodes.Count);

			var text0 = para.ChildNodes[0];
			Assert.AreEqual(HtmlNodeType.Text, text0.NodeType);
			Assert.AreEqual("Questions will appear in the ", text0.InnerHtml);

			var span0 = para.ChildNodes[1];
			Assert.AreEqual("span", span0.Name);
			Assert.AreEqual(1, span0.Attributes.Count);
			Assert.AreEqual("#229922", span0.Attributes["fontcolor"].Value);
			Assert.AreEqual(1, span0.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, span0.ChildNodes[0].NodeType);
			Assert.AreEqual("face", span0.ChildNodes[0].InnerHtml);

			var text1 = para.ChildNodes[2];
			Assert.AreEqual(HtmlNodeType.Text, text1.NodeType);
			Assert.AreEqual(" with the colour ", text1.InnerHtml);

			var span1 = para.ChildNodes[3];
			Assert.AreEqual("span", span1.Name);
			Assert.AreEqual(1, span1.Attributes.Count);
			Assert.AreEqual("#229922", span1.Attributes["fontcolor"].Value);
			Assert.AreEqual(1, span1.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, span1.ChildNodes[0].NodeType);
			Assert.AreEqual("green", span1.ChildNodes[0].InnerHtml);

			var text2 = para.ChildNodes[4];
			Assert.AreEqual(HtmlNodeType.Text, text2.NodeType);
			Assert.AreEqual(".", text2.InnerHtml);
		}

		[Test]
		public void TestNestedHtmlListsFromMarkdownIt()
		{
			var injector = HtmlXliff.Parse(@"<html>
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
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
 <file original=""test.html"" datatype=""html"" source-language=""en"" target-language=""fr"">
  <body>
   <trans-unit id=""integrity.todo.ideas.Reinstall"">
    <source xml:lang=""en"">Run the Bloom installer again, and see if it starts up OK this time.</source>
    <target xml:lang=""fr"">Lancez à nouveau l'installateur Bloom et vérifiez si cette fois, il démarre correctement.</target>
    <note>ID: integrity.todo.ideas.Reinstall</note>
   </trans-unit>
   <trans-unit id=""integrity.todo.ideas.Antivirus"">
    <source xml:lang=""en"">If that doesn't fix it, it's time to talk to your anti-virus program.</source>
    <target xml:lang=""fr"">Si cela ne règle pas le problème, il va vous falloir intervenir au niveau de votre programme antivirus.</target>
    <note>ID: integrity.todo.ideas.Antivirus</note>
   </trans-unit>
   <trans-unit id=""integrity.todo.ideas.AVAST"">
    <source xml:lang=""en"">AVAST: <g id=""genid-1"" ctype=""x-html-a"" html:href=""http://www.getavast.net/support/managing-exceptions"">Instructions</g>.</source>
    <target xml:lang=""fr"">AVAST : <g id=""genid-1"" ctype=""x-html-a"" html:href=""http://www.getavast.net/support/managing-exceptions"">Instructions</g>.</target>
    <note>ID: integrity.todo.ideas.AVAST</note>
   </trans-unit>
   <trans-unit id=""integrity.todo.ideas.Restart"">
    <source xml:lang=""en"">Run the Bloom installer again, and see if it starts up OK this time.</source>
    <target xml:lang=""fr"">Exécutez l'installateur Bloom à nouveau, et voyez s'il démarre OK cette fois.</target>
    <note>ID: integrity.todo.ideas.Restart</note>
   </trans-unit>
   <trans-unit id=""integrity.todo.ideas.Retrieve"">
    <source xml:lang=""en"">You can also try and retrieve the part of Bloom that your anti-virus program took from it.</source>
    <target xml:lang=""fr"">Vous pouvez également essayer de récupérer la partie de Bloom que votre programme anti-virus a retiré.</target>
    <note>ID: integrity.todo.ideas.Retrieve</note>
   </trans-unit>
  </body>
 </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);

			// Without the new code being tested by this method, only the top three list items survived the
			// translation process.  Assuming that the XLIFF has the form above with simple strings for each
			// target element, exactly those strings replaced the entire content of the list items.  The
			// embedded p element markup were lost, and the embedded sublist was lost.

			var elements = translatedHtml.DocumentNode.SelectNodes("/html/body/*");
			Assert.AreEqual(1, elements.Count);

			var ul = elements[0];
			Assert.AreEqual("ul", ul.Name);
			Assert.AreEqual(0, ul.Attributes.Count);
			Assert.AreEqual(7, ul.ChildNodes.Count);

			CheckForEmptyEvenNumberedTextChildNodes(ul);

			var li0 = ul.ChildNodes[1];
			Assert.AreEqual("li", li0.Name);
			Assert.AreEqual(1, li0.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Reinstall", li0.Attributes["i18n"].Value);
			Assert.AreEqual(3, li0.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li0);

			var p0 = li0.ChildNodes[1];
			Assert.AreEqual("p", p0.Name);
			Assert.AreEqual(2, p0.Attributes.Count);
			Assert.AreEqual("fr", p0.Attributes["lang"].Value);
			Assert.AreEqual("fr", p0.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, p0.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, p0.ChildNodes[0].NodeType);
			Assert.AreEqual("Lancez à nouveau l'installateur Bloom et vérifiez si cette fois, il démarre correctement.", p0.InnerHtml);

			var li1 = ul.ChildNodes[3];
			Assert.AreEqual("li", li1.Name);
			Assert.AreEqual(1, li1.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Antivirus", li1.Attributes["i18n"].Value);
			Assert.AreEqual(5, li1.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li1);

			var p1 = li1.ChildNodes[1];
			Assert.AreEqual("p", p1.Name);
			Assert.AreEqual(2, p1.Attributes.Count);
			Assert.AreEqual("fr", p1.Attributes["lang"].Value);
			Assert.AreEqual("fr", p1.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, p1.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, p1.ChildNodes[0].NodeType);
			Assert.AreEqual("Si cela ne règle pas le problème, il va vous falloir intervenir au niveau de votre programme antivirus.", p1.InnerHtml);

			var li1ul = li1.ChildNodes[3];
			Assert.AreEqual("ul", li1ul.Name);
			Assert.AreEqual(0, li1ul.Attributes.Count);
			Assert.AreEqual(5, li1ul.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li1ul);

			var li1a = li1ul.ChildNodes[1];
			Assert.AreEqual("li", li1a.Name);
			Assert.AreEqual(1, li1a.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.AVAST", li1a.Attributes["i18n"].Value);
			CheckForEmptyEvenNumberedTextChildNodes(li1a);

			var p2 = li1a.ChildNodes[1];
			Assert.AreEqual("p", p2.Name);
			Assert.AreEqual(2, p2.Attributes.Count);
			Assert.AreEqual("fr", p2.Attributes["lang"].Value);
			Assert.AreEqual("fr", p2.Attributes["xml:lang"].Value);
			Assert.AreEqual(3, p2.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, p2.ChildNodes[0].NodeType);
			Assert.AreEqual("AVAST : ", p2.ChildNodes[0].InnerHtml);
			var anchor = p2.ChildNodes[1];
			Assert.AreEqual("a", anchor.Name);
			Assert.AreEqual(1, anchor.Attributes.Count);
			Assert.AreEqual("http://www.getavast.net/support/managing-exceptions", anchor.Attributes["href"].Value);
			Assert.AreEqual("Instructions", anchor.InnerText);
			Assert.AreEqual(HtmlNodeType.Text, p2.ChildNodes[2].NodeType);
			Assert.AreEqual(".", p2.ChildNodes[2].InnerHtml);

			var li2a = li1ul.ChildNodes[3];
			Assert.AreEqual("li", li2a.Name);
			Assert.AreEqual(1, li2a.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Restart", li2a.Attributes["i18n"].Value);
			CheckForEmptyEvenNumberedTextChildNodes(li2a);

			var p3 = li2a.ChildNodes[1];
			Assert.AreEqual("p", p3.Name);
			Assert.AreEqual(2, p3.Attributes.Count);
			Assert.AreEqual("fr", p3.Attributes["lang"].Value);
			Assert.AreEqual("fr", p3.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, p3.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, p3.ChildNodes[0].NodeType);
			Assert.AreEqual("Exécutez l'installateur Bloom à nouveau, et voyez s'il démarre OK cette fois.", p3.InnerHtml);

			var li2 = ul.ChildNodes[5];
			Assert.AreEqual("li", li2.Name);
			Assert.AreEqual(1, li2.Attributes.Count);
			Assert.AreEqual("integrity.todo.ideas.Retrieve", li2.Attributes["i18n"].Value);
			Assert.AreEqual(3, li2.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li2);

			var p4 = li2.ChildNodes[1];
			Assert.AreEqual("p", p4.Name);
			Assert.AreEqual(2, p4.Attributes.Count);
			Assert.AreEqual("fr", p4.Attributes["lang"].Value);
			Assert.AreEqual("fr", p4.Attributes["xml:lang"].Value);
			Assert.AreEqual(1, p4.ChildNodes.Count);
			Assert.AreEqual(HtmlNodeType.Text, p4.ChildNodes[0].NodeType);
			Assert.AreEqual("Vous pouvez également essayer de récupérer la partie de Bloom que votre programme anti-virus a retiré.", p4.InnerHtml);
		}

		[Test]
		public void TestPartialHtmlFromMarkdownIt()
		{
			var injector = HtmlXliff.Parse(@"<h2 i18n=""integrity.title"">Bloom cannot find some of its own files, and cannot continue</h2>
<h3 i18n=""integrity.causes"">Possible Causes</h3>
<ol>
<li i18n=""integrity.causes.1"">
<p>Your antivirus may have &quot;quarantined&quot; one or more Bloom files.</p>
</li>
<li i18n=""integrity.causes.2"">
<p>Your computer administrator may have your computer &quot;locked down&quot; to prevent bad things, but in such a way that Bloom could not place these files where they belong. </p>
</li>
</ol>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
  <file original=""IntegrityFailureAdvice-ar.htm"" datatype=""html"" source-language=""en"" target-language=""ar"">
    <body>
      <trans-unit id=""integrity.title"">
        <source xml:lang=""en"">Bloom cannot find some of its own files, and cannot continue</source>
        <target xml:lang=""ar"">يتعذر على Bloom العثور على بعض الملفات الخاصة به، ولا يمكنه المتابعة</target>
        <note>ID: integrity.title</note>
      </trans-unit>
      <trans-unit id=""integrity.causes"">
        <source xml:lang=""en"">Possible Causes</source>
        <target xml:lang=""ar"">الأسباب الممكنة</target>
        <note>ID: integrity.causes</note>
      </trans-unit>
      <trans-unit id=""integrity.causes.1"">
        <source xml:lang=""en"">Your antivirus may have ""quarantined"" one or more Bloom files.</source>
        <target xml:lang=""ar"">ربما قام مكافح الفيروسات بعزل ملف واحد أو أكثر من ملفات Bloom.</target>
        <note>ID: integrity.causes.1</note>
      </trans-unit>
      <trans-unit id=""integrity.causes.2"">
        <source xml:lang=""en"">Your computer administrator may have your computer ""locked down"" to prevent bad things, but in such a way that Bloom could not place these files where they belong. </source>
        <target xml:lang=""ar"">ربما قام مشرف الكمبيوتر بقفل الكمبيوتر لمنع القيام بأنشطة سيئة، وأدى ذلك إلى تعذر Bloom عن وضع الملفات في المكان المناسب.</target>
        <note>ID: integrity.causes.2</note>
      </trans-unit>
    </body>
  </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);

			Assert.AreEqual(1, translatedHtml.DocumentNode.ChildNodes.Count);
			var divDir = translatedHtml.DocumentNode.FirstChild;
			Assert.AreEqual("div", divDir.Name);
			Assert.AreEqual(3, divDir.Attributes.Count);
			Assert.AreEqual("rtl", divDir.Attributes["dir"].Value);
			Assert.AreEqual("ar", divDir.Attributes["lang"].Value);
			Assert.AreEqual("ar", divDir.Attributes["xml:lang"].Value);

			Assert.AreEqual(5, divDir.ChildNodes.Count);

			var h2 = divDir.ChildNodes[0];
			Assert.AreEqual("h2", h2.Name);
			Assert.AreEqual(4, h2.Attributes.Count);
			Assert.AreEqual("rtl", h2.Attributes["dir"].Value);
			Assert.AreEqual("ar", h2.Attributes["lang"].Value);
			Assert.AreEqual("ar", h2.Attributes["xml:lang"].Value);
			Assert.AreEqual("integrity.title", h2.Attributes["i18n"].Value);
			Assert.AreEqual("يتعذر على Bloom العثور على بعض الملفات الخاصة به، ولا يمكنه المتابعة", h2.InnerHtml);

			var tn = divDir.ChildNodes[1];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			var h3 = divDir.ChildNodes[2];
			Assert.AreEqual("h3", h3.Name);
			Assert.AreEqual(4, h3.Attributes.Count);
			Assert.AreEqual("rtl", h3.Attributes["dir"].Value);
			Assert.AreEqual("ar", h3.Attributes["lang"].Value);
			Assert.AreEqual("ar", h3.Attributes["xml:lang"].Value);
			Assert.AreEqual("integrity.causes", h3.Attributes["i18n"].Value);
			Assert.AreEqual("الأسباب الممكنة", h3.InnerHtml);

			tn = divDir.ChildNodes[3];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			var ol = divDir.ChildNodes[4];
			Assert.AreEqual("ol", ol.Name);
			Assert.AreEqual(0, ol.Attributes.Count);
			Assert.AreEqual(5, ol.ChildNodes.Count);

			tn = ol.ChildNodes[0];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			var li = ol.ChildNodes[1];
			Assert.AreEqual("li", li.Name);
			Assert.AreEqual(1, li.Attributes.Count);
			Assert.AreEqual("integrity.causes.1", li.Attributes["i18n"].Value);
			Assert.AreEqual(3, li.ChildNodes.Count);

			tn = li.ChildNodes[0];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			var para = li.ChildNodes[1];
			Assert.AreEqual("p", para.Name);
			Assert.AreEqual(3, para.Attributes.Count);
			Assert.AreEqual("rtl", para.Attributes["dir"].Value);
			Assert.AreEqual("ar", para.Attributes["lang"].Value);
			Assert.AreEqual("ar", para.Attributes["xml:lang"].Value);
			Assert.AreEqual("ربما قام مكافح الفيروسات بعزل ملف واحد أو أكثر من ملفات Bloom.", para.InnerHtml);

			tn = li.ChildNodes[2];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			tn = ol.ChildNodes[2];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			li = ol.ChildNodes[3];
			Assert.AreEqual("li", li.Name);
			Assert.AreEqual(1, li.Attributes.Count);
			Assert.AreEqual("integrity.causes.2", li.Attributes["i18n"].Value);
			Assert.AreEqual(3, li.ChildNodes.Count);

			tn = li.ChildNodes[0];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			para = li.ChildNodes[1];
			Assert.AreEqual("p", para.Name);
			Assert.AreEqual("rtl", para.Attributes["dir"].Value);
			Assert.AreEqual("ar", para.Attributes["lang"].Value);
			Assert.AreEqual("ar", para.Attributes["xml:lang"].Value);
			Assert.AreEqual("ربما قام مشرف الكمبيوتر بقفل الكمبيوتر لمنع القيام بأنشطة سيئة، وأدى ذلك إلى تعذر Bloom عن وضع الملفات في المكان المناسب.", para.InnerHtml);

			tn = li.ChildNodes[2];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));

			tn = ol.ChildNodes[4];
			Assert.AreEqual(HtmlNodeType.Text, tn.NodeType);
			Assert.IsTrue(String.IsNullOrWhiteSpace(tn.InnerHtml));
		}


		[Test]
		public void TestRtlFullHtml()
		{
			var injector = HtmlXliff.Parse(@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body>
<h2 i18n=""integrity.title"">Bloom cannot find some of its own files, and cannot continue</h2>
<h3 i18n=""integrity.causes"">Possible Causes</h3>
<ol>
<li i18n=""integrity.causes.1"">
<p>Your antivirus may have &quot;quarantined&quot; one or more Bloom files.</p>
</li>
<li i18n=""integrity.causes.2"">
<p>Your computer administrator may have your computer &quot;locked down&quot; to prevent bad things, but in such a way that Bloom could not place these files where they belong. </p>
</li>
</ol>
</body>
</html>");
			var xliffDoc = new XmlDocument();
			xliffDoc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""1.2"" xmlns=""urn:oasis:names:tc:xliff:document:1.2"" xmlns:html=""http://www.w3.org/TR/html"" xmlns:sil=""http://sil.org/software/XLiff"">
  <file original=""IntegrityFailureAdvice-ar.htm"" datatype=""html"" source-language=""en"" target-language=""ar"">
    <body>
      <trans-unit id=""integrity.title"">
        <source xml:lang=""en"">Bloom cannot find some of its own files, and cannot continue</source>
        <target xml:lang=""ar"">يتعذر على Bloom العثور على بعض الملفات الخاصة به، ولا يمكنه المتابعة</target>
        <note>ID: integrity.title</note>
      </trans-unit>
      <trans-unit id=""integrity.causes"">
        <source xml:lang=""en"">Possible Causes</source>
        <target xml:lang=""ar"">الأسباب الممكنة</target>
        <note>ID: integrity.causes</note>
      </trans-unit>
      <trans-unit id=""integrity.causes.1"">
        <source xml:lang=""en"">Your antivirus may have ""quarantined"" one or more Bloom files.</source>
        <target xml:lang=""ar"">ربما قام مكافح الفيروسات بعزل ملف واحد أو أكثر من ملفات Bloom.</target>
        <note>ID: integrity.causes.1</note>
      </trans-unit>
      <trans-unit id=""integrity.causes.2"">
        <source xml:lang=""en"">Your computer administrator may have your computer ""locked down"" to prevent bad things, but in such a way that Bloom could not place these files where they belong. </source>
        <target xml:lang=""ar"">ربما قام مشرف الكمبيوتر بقفل الكمبيوتر لمنع القيام بأنشطة سيئة، وأدى ذلك إلى تعذر Bloom عن وضع الملفات في المكان المناسب.</target>
        <note>ID: integrity.causes.2</note>
      </trans-unit>
    </body>
  </file>
</xliff>");
			var translatedHtml = injector.InjectTranslations(xliffDoc, true);
			Assert.IsNotNull(translatedHtml);

			Assert.AreEqual(3, translatedHtml.DocumentNode.ChildNodes.Count);
			var htmlNode = translatedHtml.DocumentNode.ChildNodes[2];
			Assert.AreEqual("html", htmlNode.Name);
			Assert.AreEqual(0, htmlNode.Attributes.Count);
			Assert.AreEqual(5, htmlNode.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(htmlNode);

			var headNode = htmlNode.ChildNodes[1];
			Assert.AreEqual("head", headNode.Name);
			Assert.AreEqual(0, headNode.Attributes.Count);
			Assert.AreEqual(1, headNode.ChildNodes.Count);
			var metaNode = headNode.ChildNodes[0];
			Assert.AreEqual("meta", metaNode.Name);
			Assert.AreEqual(1, metaNode.Attributes.Count);
			Assert.AreEqual("utf-8", metaNode.Attributes["charset"].Value);
			Assert.AreEqual(0, metaNode.ChildNodes.Count);

			var bodyNode = htmlNode.ChildNodes[3];
			Assert.AreEqual("body", bodyNode.Name);
			Assert.AreEqual(3, bodyNode.Attributes.Count);
			Assert.AreEqual("rtl", bodyNode.Attributes["dir"].Value);
			Assert.AreEqual("ar", bodyNode.Attributes["lang"].Value);
			Assert.AreEqual("ar", bodyNode.Attributes["xml:lang"].Value);
			Assert.AreEqual(7, bodyNode.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(bodyNode);

			var h2 = bodyNode.ChildNodes[1];
			Assert.AreEqual("h2", h2.Name);
			Assert.AreEqual(4, h2.Attributes.Count);
			Assert.AreEqual("rtl", h2.Attributes["dir"].Value);
			Assert.AreEqual("ar", h2.Attributes["lang"].Value);
			Assert.AreEqual("ar", h2.Attributes["xml:lang"].Value);
			Assert.AreEqual("integrity.title", h2.Attributes["i18n"].Value);
			Assert.AreEqual("يتعذر على Bloom العثور على بعض الملفات الخاصة به، ولا يمكنه المتابعة", h2.InnerHtml);

			var h3 = bodyNode.ChildNodes[3];
			Assert.AreEqual("h3", h3.Name);
			Assert.AreEqual(4, h3.Attributes.Count);
			Assert.AreEqual("rtl", h3.Attributes["dir"].Value);
			Assert.AreEqual("ar", h3.Attributes["lang"].Value);
			Assert.AreEqual("ar", h3.Attributes["xml:lang"].Value);
			Assert.AreEqual("integrity.causes", h3.Attributes["i18n"].Value);
			Assert.AreEqual("الأسباب الممكنة", h3.InnerHtml);

			var ol = bodyNode.ChildNodes[5];
			Assert.AreEqual("ol", ol.Name);
			Assert.AreEqual(0, ol.Attributes.Count);
			Assert.AreEqual(5, ol.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(ol);

			var li = ol.ChildNodes[1];
			Assert.AreEqual("li", li.Name);
			Assert.AreEqual(1, li.Attributes.Count);
			Assert.AreEqual("integrity.causes.1", li.Attributes["i18n"].Value);
			Assert.AreEqual(3, li.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li);

			var para = li.ChildNodes[1];
			Assert.AreEqual("p", para.Name);
			Assert.AreEqual(3, para.Attributes.Count);
			Assert.AreEqual("rtl", para.Attributes["dir"].Value);
			Assert.AreEqual("ar", para.Attributes["lang"].Value);
			Assert.AreEqual("ar", para.Attributes["xml:lang"].Value);
			Assert.AreEqual("ربما قام مكافح الفيروسات بعزل ملف واحد أو أكثر من ملفات Bloom.", para.InnerHtml);

			li = ol.ChildNodes[3];
			Assert.AreEqual("li", li.Name);
			Assert.AreEqual(1, li.Attributes.Count);
			Assert.AreEqual("integrity.causes.2", li.Attributes["i18n"].Value);
			Assert.AreEqual(3, li.ChildNodes.Count);
			CheckForEmptyEvenNumberedTextChildNodes(li);

			para = li.ChildNodes[1];
			Assert.AreEqual("p", para.Name);
			Assert.AreEqual(3, para.Attributes.Count);
			Assert.AreEqual("rtl", para.Attributes["dir"].Value);
			Assert.AreEqual("ar", para.Attributes["lang"].Value);
			Assert.AreEqual("ar", para.Attributes["xml:lang"].Value);
			Assert.AreEqual("ربما قام مشرف الكمبيوتر بقفل الكمبيوتر لمنع القيام بأنشطة سيئة، وأدى ذلك إلى تعذر Bloom عن وضع الملفات في المكان المناسب.", para.InnerHtml);
		}

		void CheckForEmptyEvenNumberedTextChildNodes(HtmlNode node)
		{
			for (int i = 0; i < node.ChildNodes.Count; i += 2)
			{
				Assert.AreEqual(HtmlNodeType.Text, node.ChildNodes[i].NodeType);
				Assert.IsNotNullOrEmpty(node.ChildNodes[i].InnerText);
				Assert.IsTrue(String.IsNullOrWhiteSpace(node.ChildNodes[i].InnerText));
			}
		}
	}
}
