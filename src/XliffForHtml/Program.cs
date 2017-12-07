﻿// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Collections.Generic;

namespace XliffForHtml
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			bool useFolder = false;
			bool extractToXliff = true;
			bool injectFromXliff = false;
			string htmlFile = null;
			string xliffFile = null;
			string outputFile = null;
			List<string[]> obfuscateData = new List<string[]>();
			bool verboseWarnings = false;
			var langDir = "en";		// unless user changes it.
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i] == "-f" || args[i] == "--folder")
				{
					useFolder = true;
					if (i+1 < args.Length)
					{
						++i;
						langDir = args[i];
					}
				}
				else if (args[i] == "-e" || args[i] == "--extract")
				{
					extractToXliff = true;
					injectFromXliff = false;
				}
				else if (args[i] == "-i" || args[i] == "--inject")
				{
					injectFromXliff = true;
					extractToXliff = false;
				}
				else if (args[i] == "-x" || args[i] == "--xliff")
				{
					if (i+1 < args.Length)
					{
						++i;
						xliffFile = args[i];
					}
				}
				else if (args[i] == "-o" || args[i] == "--output")
				{
					if (i+1 < args.Length)
					{
						++i;
						outputFile = args[i];
					}
				}
				else if (args[i] == "-v" || args[i] == "--verbose")
				{
					verboseWarnings = true;
				}
				else if (args[i] == "-b" || args[i] == "--obfuscate")
				{
					if (i+1 < args.Length)
					{
						++i;
						if (File.Exists(args[i]))
						{
							foreach (var line in File.ReadAllLines(args[i]))
							{
								var pieces = line.Split('\t');
								if (pieces.Length == 2)
									obfuscateData.Add(pieces);
							}
						}
					}
				}
				else if (htmlFile == null)
				{
					htmlFile = args[i];
				}
				else
				{
					Usage();
					return;
				}
			}
			if (htmlFile == null)
			{
				Usage();
				return;
			}
			if (!File.Exists(htmlFile))
			{
				Console.WriteLine("The input HTML file \"{0}\" does not exist!", htmlFile);
				Usage();
				return;
			}
			if (xliffFile == null)
			{
				if (useFolder)
				{
					xliffFile = Path.Combine(Path.GetDirectoryName(htmlFile), langDir, Path.ChangeExtension(Path.GetFileName(htmlFile),".xlf"));
				}
				else
				{
					xliffFile = Path.ChangeExtension(htmlFile, "xlf");
				}
			}
			if (extractToXliff)
			{
				HtmlXliff extractor = HtmlXliff.Load(htmlFile);
				if (obfuscateData.Count > 0)
					extractor.ObfuscateEmailAddresses(obfuscateData);
				var xdoc = extractor.Extract();
				if (outputFile == null)
					outputFile = xliffFile;
				EnsureOutputDirectoryExists(Path.GetDirectoryName(outputFile));
				if (xdoc != null)
					xdoc.Save(outputFile);
				else
					Console.WriteLine("Nothing was tagged with a data-i18n or i18n attribute in the input file \"{0}\"", htmlFile);
			}
			else if (injectFromXliff)
			{
				HtmlXliff injector = HtmlXliff.Load(htmlFile);
				if (obfuscateData.Count > 0)
					injector.ObfuscateEmailAddresses(obfuscateData);
				var hdoc = injector.InjectTranslations(xliffFile, verboseWarnings);
				if (outputFile == null)
					outputFile = Path.ChangeExtension(xliffFile, ".html");
				EnsureOutputDirectoryExists(Path.GetDirectoryName(outputFile));
				if (outputFile == htmlFile)
				{
					Console.Write("Replace the input html file? [y/N] ");
					var inline = Console.ReadLine();
					if (inline.StartsWith("Y") || inline.StartsWith("y"))
						hdoc.Save(outputFile, System.Text.Encoding.UTF8);
				}
				else
				{
					hdoc.Save(outputFile, System.Text.Encoding.UTF8);
				}
			}
		}

		private static void EnsureOutputDirectoryExists(string directory)
		{
			if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}

		private static void Usage()
		{
			Console.WriteLine("Usage: HtmlXliff [options] htmlfile");
			Console.WriteLine("       -e|--extract = extract strings to translate from html file [default]");
			Console.WriteLine("       -i|--inject = inject translations from xliff file to create translated html file");
			Console.WriteLine("       -v|--verbose = display warnings for missing translations (ignored if extracting)");
			Console.WriteLine("       -f|--folder <lang> = the xliff file is in a subfolder named <lang> (ignored if -x or -o specified)");
			Console.WriteLine("       -x|--xliff <file> = specify xliff file path (ignored if extracting and -o specified)");
			Console.WriteLine("       -o|--output <file> = specify output file path");
			Console.WriteLine("       -b|--obfuscate <file> = specify file containing list of tab separated email addresses and obfuscations");
			Console.WriteLine("By default whether input or output, the xliff file has the same name as the");
			Console.WriteLine("html file, but with a .xlf extension.  -f causes the xliff file to be written");
			Console.WriteLine("to or read from a specified subfolder relative to the html file.");
		}
	}
}
