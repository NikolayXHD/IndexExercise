using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Lucene.Net.Contrib;

namespace IndexExercise.Index.Demo
{
	public class SearchStringSubsystem
	{
		public SearchStringSubsystem(
			Form parent,
			RichTextBox findEditor,
			DemoApplication searcher)
		{
			_parent = parent;
			_findEditor = findEditor;

			_searcher = searcher;
			_highligter = new SearchStringHighlighter(_findEditor);
			_highligter.Highlight();
		}

		public void SubscribeToEvents()
		{
			_findEditor.KeyDown += findKeyDown;
			_findEditor.KeyUp += findKeyUp;
			_findEditor.TextChanged += findTextChanged;
			
			_parent.KeyDown += parentKeyDown;
		}

		public void UnsubscribeFromEvents()
		{
			_findEditor.KeyDown -= findKeyDown;
			_findEditor.KeyUp -= findKeyUp;
			_findEditor.TextChanged -= findTextChanged;
			_parent.KeyDown -= parentKeyDown;
		}

		public void StartThread()
		{
			_idleInputMonitoringThread = new Thread(idleInputMonitoringThread);
			_idleInputMonitoringThread.Start();
		}

		public void AbortThread()
		{
			_idleInputMonitoringThread.Abort();
		}

		private void idleInputMonitoringThread()
		{
			const int delay = 1000;

			try
			{
				while (true)
				{
					updateBackgroundColor();

					int deltaMs;
					if (!_lastUserInput.HasValue || _currentText == _appliedText && _appliedIndexChangeTime == _searcher.IndexChangeTime)
						deltaMs = delay;
					else
						deltaMs = delay - (int) (DateTime.Now - _lastUserInput.Value).TotalMilliseconds;

					if (deltaMs > 0)
						Thread.Sleep(deltaMs + 100);
					else
						applyFind();

				}
			}
			catch (ThreadAbortException)
			{
			}
		}

		private void updateBackgroundColor()
		{
			_findEditor.Invoke(delegate
			{
				Color requiredColor;

				if (SearchResult.HasSyntaxErrors)
					requiredColor = Color.LavenderBlush;
				else if (_currentText != _appliedText)
					requiredColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
				else
					requiredColor = Color.White;

				if (_findEditor.BackColor != requiredColor)
				{
					_findEditor.BackColor = _findEditor.Parent.BackColor = requiredColor;
					_highligter.Highlight();
				}
			});
		}

		private void findTextChanged(object sender, EventArgs e)
		{
			if (_highligter.HighlightingInProgress)
				return;

			_currentText = _findEditor.Text;

			_highligter.Highlight();
		}

		private void parentKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == (Keys.Control | Keys.F))
			{
				FocusSearch();
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private void findKeyDown(object sender, KeyEventArgs e)
		{
			updateBackgroundColor();
			_lastUserInput = DateTime.Now;

			switch (e.KeyData)
			{
				case Keys.Enter:
					applyFind();
					e.Handled = true;
					break;

				case Keys.Control | Keys.X:
				case Keys.Shift | Keys.Delete:
					if (!string.IsNullOrEmpty(_findEditor.SelectedText))
					{
						Clipboard.SetText(SearchStringMark + _findEditor.SelectedText);

						var prefix = _findEditor.Text.Substring(0, _findEditor.SelectionStart);
						int suffixStart = _findEditor.SelectionStart + _findEditor.SelectionLength;
						var suffix = suffixStart < _findEditor.Text.Length
							? _findEditor.Text.Substring(suffixStart)
							: string.Empty;

						setFindText(prefix + suffix, prefix.Length);
					}

					e.Handled = true;
					e.SuppressKeyPress = true;
					break;
				
				case Keys.Control | Keys.C:
				case Keys.Control | Keys.Insert:
					if (!string.IsNullOrEmpty(_findEditor.SelectedText))
						Clipboard.SetText(SearchStringMark + _findEditor.SelectedText);

					e.Handled = true;
					e.SuppressKeyPress = true;
					break;
				
				case Keys.Control | Keys.V:
				case Keys.Shift | Keys.Insert:
					if (Clipboard.ContainsText())
					{
						string searchQuery = clipboardTextToQuery(Clipboard.GetText());
						pasteSearchQuery(searchQuery);
					}

					e.Handled = true;
					e.SuppressKeyPress = true;
					break;

				case Keys.Control | Keys.Shift | Keys.Right:
					break;

				case Keys.Control | Keys.Shift | Keys.Left:
					break;
			}
		}

		private void pasteSearchQuery(string searchQuery)
		{
			int selectionStart = _findEditor.SelectionStart;
			int suffixStart = selectionStart + _findEditor.SelectionLength;
			string text = _findEditor.Text;

			var builder = new StringBuilder();
			if (selectionStart >= 0)
			{
				string prefix = text.Substring(0, selectionStart);
				builder.Append(prefix);
			}

			builder.Append(searchQuery);

			int length = builder.Length;

			if (suffixStart < text.Length)
			{
				string suffix = text.Substring(suffixStart);
				builder.Append(suffix);
			}

			_findEditor.Text = builder.ToString();
			_findEditor.SelectionStart = length;
		}

		private static string clipboardTextToQuery(string text)
		{
			if (text.StartsWith(SearchStringMark))
				return _endLineRegex.Replace(text.Substring(SearchStringMark.Length), " ");

			return getValueQuery(text);
		}

		private static string getValueQuery(string text)
		{
			var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			if (lines.Length == 0)
				return string.Empty;

			var builder = new StringBuilder();

			if (lines.Length == 1 && lines[0].IndexOf(' ') < 0)
				builder.Append(StringEscaper.Escape(lines[0]));
			else if (lines.Length >= 1)
			{
				builder.Append('"');

				for (int i = 0; i < lines.Length; i++)
				{
					if (i > 0)
						builder.Append(' ');

					builder.Append(StringEscaper.Escape(lines[i]));
				}

				builder.Append('"');
			}

			return builder.ToString();
		}

		private void findKeyUp(object sender, KeyEventArgs e)
		{
			updateBackgroundColor();
			_lastUserInput = DateTime.Now;
		}



		private void setFindText(string text, int editedRight)
		{
			_findEditor.Text = text;
			_findEditor.SelectionStart = editedRight;
			_findEditor.SelectionLength = 0;
		}

		public void FocusSearch()
		{
			if (_findEditor.ContainsFocus)
				return;

			int originalSelectionStart = _findEditor.SelectionStart;
			int originalSelectionLength = _findEditor.SelectionLength;

			_findEditor.Focus();
			_findEditor.SelectionStart = originalSelectionStart;
			_findEditor.SelectionLength = originalSelectionLength;
		}

		public void SetSearchText(string value)
		{
			_findEditor.Text = value;
			_appliedText = value;

			applyFind();
		}

		private void applyFind()
		{
			_appliedText = _currentText;
			_appliedIndexChangeTime = _searcher.IndexChangeTime;

			updateSearchResult();

			_findEditor.Invoke(delegate
			{
				updateBackgroundColor();

				if (!SearchResult.HasSyntaxErrors)
					_findEditor.ResetForeColor();
				else
					_findEditor.ForeColor = Color.DarkRed;
			});
		}

		private void updateSearchResult()
		{
			if (!string.IsNullOrWhiteSpace(_currentText))
			{
				var query = _searcher.GetQuery(_currentText);
				var searchResult = _searcher.Search(query);
				SearchResult = new FixedSearchResult(searchResult);
			}
			else
				SearchResult = FixedSearchResult.Empty;

			SearchResultChanged?.Invoke(this, SearchResult);
		}

		private const string SearchStringMark = "search: ";
		private static readonly Regex _endLineRegex = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled | RegexOptions.Singleline);

		public FixedSearchResult SearchResult { get; private set; } = FixedSearchResult.Empty;

		public event EventHandler<FixedSearchResult> SearchResultChanged;

		private DateTime? _lastUserInput;
		private Thread _idleInputMonitoringThread;

		private string _appliedText = string.Empty;
		private string _currentText = string.Empty;

		private DateTime _appliedIndexChangeTime;

		private readonly Form _parent;
		private readonly RichTextBox _findEditor;
		private readonly DemoApplication _searcher;
		private readonly SearchStringHighlighter _highligter;
	}
}