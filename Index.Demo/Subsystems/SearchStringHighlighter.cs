using System.Drawing;
using System.Windows.Forms;
using Lucene.Net.Contrib;

namespace IndexExercise.Index.Demo
{
	public class SearchStringHighlighter
	{
		private readonly RichTextBox _findEditor;
		public bool HighlightingInProgress { get; private set; }

		public SearchStringHighlighter(RichTextBox findEditor)
		{
			_findEditor = findEditor;
		}

		public void Highlight()
		{
			HighlightingInProgress = true;

			var start = _findEditor.SelectionStart;
			var len = _findEditor.SelectionLength;
			
			var tokenizer = new TolerantTokenizer(_findEditor.Text);
			tokenizer.Parse();

			setColor(0, _findEditor.TextLength, _findEditor.BackColor, Color.Black, false);

			foreach (var token in tokenizer.Tokens)
			{
				if (token.Type.IsAny(TokenType.FieldValue))
					setColor(token.Position, token.Value.Length, _findEditor.BackColor, null, true);
				else if (token.Type.IsAny(TokenType.Field | TokenType.Colon))
					setColor(token.Position, token.Value.Length, null, Color.Teal, false);
				else if (token.Type.IsAny(TokenType.RegexBody))
					setColor(token.Position, token.Value.Length, null, Color.DarkRed, false);
				else
					setColor(token.Position, token.Value.Length, null, Color.MediumBlue, false);
			}

			_findEditor.SelectionStart = start;
			_findEditor.SelectionLength = len;

			HighlightingInProgress = false;
		}

		private void setColor(int from, int len, Color? backColor, Color? foreColor, bool underline)
		{
			_findEditor.SelectionStart = from;
			_findEditor.SelectionLength = len;
			if (backColor.HasValue)
				_findEditor.SelectionBackColor = backColor.Value;
			if (foreColor.HasValue)
				_findEditor.SelectionColor = foreColor.Value;

			if (underline && !_findEditor.SelectedText.IsCjk())
				_findEditor.SelectionFont = new Font(_findEditor.Font, FontStyle.Underline);
			else
				_findEditor.SelectionFont = new Font(_findEditor.Font, FontStyle.Regular);
		}
	}
}