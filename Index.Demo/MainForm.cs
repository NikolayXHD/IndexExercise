using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IndexExercise.Index.FileSystem;

namespace IndexExercise.Index.Demo
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		public MainForm(Func<DemoApplication> demoApplicationFactory)
			: this()
		{
			_demoApplicationFactory = demoApplicationFactory;
			Load += load;
			Closing += closing;
			_textBoxDirectoryStructure.MouseClick += directoryStructureClick;
			_textBoxSearchResult.MouseClick += searchResultClick;

			StartPosition = FormStartPosition.CenterScreen;
			KeyPreview = true;

			KeyDown += keyDown;
			KeyUp += keyUp;

			DockPadding.All = 0;
			_split.DockPadding.All = 0;
			_split.Panel1.DockPadding.All = 0;
			_split.Panel2.DockPadding.All = 0;

			_comboBoxExamples.Items.AddRange(new object[]
			{
				"              // basic examples",
				"sauron eye    // contains either sauron OR eye",
				"\"as the\"      // contains both words in the same order",
				"large AND distance // contains both words in any order",
				"火垂るの        // contains ideographs in the same order",
				"              // syntax",
				"(eye OR fox) AND the // boolean operators and nesting",
				"NOT войн?     // negation WARNING: requres full scan",
				"\"Llanowar elves\" // search the entire phrase",
				"2005 1824     // same as 2005 OR 1824",
				"planet*       // wildcard * means 0 or more characters",
				"войн?         // wildcard ? means any one character",
				"est???*       // starts with est and at least 6 chars",
				"*nowar        // postfix WARNING: requres full scan",
				"/.*лась/      // regular expression in lucene's dialect",
				"война~        // search by approximate spelling",
				"trefork~0.3   // choose min similarity, default is 0.5",
				"[ааа TO яяя]  // alphanumerical range search",
				"[b TO c}      // [] inclusive, {} non-inclusive bounds"
			});
		}

		private void keyUp(object sender, KeyEventArgs e)
		{
			updateCursor();
		}

		private void keyDown(object sender, KeyEventArgs e)
		{
			updateCursor();
		}

		private void updateCursor()
		{
			var cursor = ModifierKeys == Keys.Control
				? Cursors.Hand
				: Cursors.Default;

			_textBoxSearchResult.Cursor = cursor;
			_textBoxDirectoryStructure.Cursor = cursor;
		}



		private void load(object sender, EventArgs e)
		{
			_demoApplication = _demoApplicationFactory.Invoke();
			_demoApplication.IndexChanged += indexChanged;
			_textBoxFileNameRegex.Text = _demoApplication.FileNameRegex;

			_searchStringSubsystem = new SearchStringSubsystem(this, _searchInput, _demoApplication);
			_searchStringSubsystem.SearchResultChanged += searchResultChanged;
			_searchStringSubsystem.SubscribeToEvents();

			_demoApplication.Start();
			_searchStringSubsystem.StartThread();
			_searchStringSubsystem.FocusSearch();
		}

		private void indexChanged(object sender, EventArgs e)
		{
			this.Invoke(updateDirectoryStructure);
		}

		private void searchResultChanged(object sender, FixedSearchResult searchResult)
		{
			this.Invoke(delegate
			{
				var lines = searchResult.HasSyntaxErrors
					? (IEnumerable<string>) searchResult.SyntaxErrors
					: searchResult.FileNames.OrderBy(_ => _, PathString.Comparer);

				_textBoxSearchResult.Text = string.Join(Environment.NewLine, lines);

				updateDirectoryStructure();
			});
		}

		private void updateDirectoryStructure()
		{
			_directoryStructure.Clear();

			var searchResult = _searchStringSubsystem.SearchResult;

			_textBoxDirectoryStructure.Text = _demoApplication.PrintDirectoryStructure(
					(builder, entry) =>
					{
						_directoryStructure.Add(entry);
						if (searchResult.FileNames.Contains(entry.GetPath()))
							builder.Append(" *match*");
					})
				.Replace("\t", "  ");
		}

		private void directoryStructureClick(object sender, MouseEventArgs e)
		{
			if (ModifierKeys != Keys.Control)
				return;

			var charIndex = _textBoxDirectoryStructure.GetCharIndexFromPosition(e.Location);
			var lineIndex = _textBoxDirectoryStructure.GetLineFromCharIndex(charIndex);

			if (0 <= lineIndex && lineIndex < _directoryStructure.Count)
			{
				var clickedEntry = _directoryStructure[lineIndex];

				var path = clickedEntry.GetPath();

				showPathInExplorer(path);
			}
		}

		private void searchResultClick(object sender, MouseEventArgs e)
		{
			if (ModifierKeys != Keys.Control)
				return;

			var charIndex = _textBoxSearchResult.GetCharIndexFromPosition(e.Location);
			var lineIndex = _textBoxSearchResult.GetLineFromCharIndex(charIndex);

			var lines = _textBoxSearchResult.Lines;

			if (0 <= lineIndex && lineIndex < lines.Length)
				showPathInExplorer(lines[lineIndex]);
		}

		private void buttonConfigClick(object sender, EventArgs e)
		{
			showPathInExplorer(Path.GetFullPath("etc\\config.xml"));
		}

		private void buttonLogClick(object sender, EventArgs e)
		{
			showPathInExplorer(Path.GetFullPath("logs"));
		}

		private static void showPathInExplorer(string path)
		{
			if (path == null)
				return;

			if (File.Exists(path))
				Process.Start(new ProcessStartInfo("explorer.exe", $@"/select,""{path}"""));
			else if (Directory.Exists(path))
				Process.Start(new ProcessStartInfo("explorer.exe", path));
		}

		private void querySyntaxLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start(new ProcessStartInfo(
				"https://lucene.apache.org/core/4_0_0/queryparser/org/apache/lucene/queryparser/classic/" +
				"package-summary.html#package_description"));
		}

		private void examplesSelectionCommitted(object sender, EventArgs e)
		{
			var selectedExample = (string) _comboBoxExamples.SelectedItem;

			if (string.IsNullOrEmpty(selectedExample))
				return;

			selectedExample = selectedExample.Split(new[] { "//" }, StringSplitOptions.None)[0].TrimEnd();

			if (string.IsNullOrEmpty(selectedExample))
				return;

			_searchStringSubsystem.SetSearchText(selectedExample);
		}

		private void splitDoubleClick(object sender, MouseEventArgs e)
		{
			_split.Panel2Collapsed = !_split.Panel2Collapsed;
		}

		private void panel1DoubleClick(object sender, EventArgs e)
		{
			_split.Panel2Collapsed = !_split.Panel2Collapsed;
		}

		private void panel2DoubleClick(object sender, MouseEventArgs e)
		{
			_split.Panel2Collapsed = !_split.Panel2Collapsed;
		}


		private void closing(object sender, CancelEventArgs e)
		{
			_searchStringSubsystem.AbortThread();
			_searchStringSubsystem.UnsubscribeFromEvents();
			_demoApplication.Dispose();
		}



		private DemoApplication _demoApplication;
		private SearchStringSubsystem _searchStringSubsystem;

		private readonly Func<DemoApplication> _demoApplicationFactory;
		private readonly List<Entry<Metadata>> _directoryStructure = new List<Entry<Metadata>>();
	}
}