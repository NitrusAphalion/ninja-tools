using System;
using System.Xml.Linq;

namespace NinjaTools.Models
{
	public class Document : Interfaces.IDelayedLoading
	{
		public DocumentType DocumentType { get; set; }
		public object File { get; set; }
		public Type FileType { get; set; }
		public bool Loaded { get; private set; } = false;
		public string Path { get; set; }
		public double Size { get; set; }
		public string SyntaxFile { get; set; }

		public Document(string path, Type fileType, DocumentType documentType)
		{
			DocumentType = documentType;
			FileType = fileType;
			Path = path;
			Size = (new System.IO.FileInfo(Path)).Length / 1000.0;
		}

		public void Load()
		{
			if (!Loaded)
			{
				switch (DocumentType)
				{
					case DocumentType.Log:
						SyntaxFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit\\SyntaxHighlighters\\log.xshd");
						break;

					case DocumentType.Trace:
						SyntaxFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit\\SyntaxHighlighters\\trace.xshd");
						break;

					default:
						if (FileType == typeof(XDocument))
							SyntaxFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit\\SyntaxHighlighters\\xml.xshd");
						break;
				}

				if (FileType == typeof(XDocument))
					File = XDocument.Load(Path, LoadOptions.SetLineInfo);
				else if (FileType == typeof(string[]))
					File = System.IO.File.ReadAllLines(Path);
				else if (FileType == typeof(Database))
					File = new Database(Path); // Database is special, we only wrap it in Document to simplify code -> View Model interacts with the Database object primarily

				Loaded = true;
			}
		}

		public string ToEditorText()
		{
			if (!Loaded) // Can be called in case you duplicated to a new tab without loading, then clicked the tab -> Alternatively could consider to just force a load in case of Clone()
				Load();

			if (FileType == typeof(XDocument))
				return File.ToString();
			else if (FileType == typeof(string[]))
				return string.Join(Environment.NewLine, File as string[]);
			else if (FileType == typeof(Database))
				throw new Exception("Database cannot provide Editor text.");
			else return string.Empty;
		}
	}
}