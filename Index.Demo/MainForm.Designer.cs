namespace IndexExercise.Index.Demo
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this._labelDirectoryStructure = new System.Windows.Forms.Label();
			this._labelSearchInput = new System.Windows.Forms.Label();
			this._labelSearchResult = new System.Windows.Forms.Label();
			this._split = new System.Windows.Forms.SplitContainer();
			this._labelSearchInputDetails = new System.Windows.Forms.Label();
			this._labelSearchResultDetails = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this._labelQuerySyntax = new System.Windows.Forms.LinkLabel();
			this._buttonLog = new System.Windows.Forms.Button();
			this._buttonConfig = new System.Windows.Forms.Button();
			this._labelExamples = new System.Windows.Forms.Label();
			this._labelDirectoryStructureDetails = new System.Windows.Forms.Label();
			this._comboBoxExamples = new System.Windows.Forms.ComboBox();
			this._labelFileNamePatternDetails = new System.Windows.Forms.Label();
			this._labelFileNamePattern = new System.Windows.Forms.Label();
			this._textBoxFileNameRegex = new System.Windows.Forms.TextBox();
			this._textBoxSearchResult = new IndexExercise.Index.Demo.FixedRichTextBox();
			this._searchPanel = new IndexExercise.Index.Demo.BorderedPanel();
			this._searchInput = new IndexExercise.Index.Demo.FixedRichTextBox();
			this._textBoxDirectoryStructure = new IndexExercise.Index.Demo.FixedRichTextBox();
			((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
			this._split.Panel1.SuspendLayout();
			this._split.Panel2.SuspendLayout();
			this._split.SuspendLayout();
			this._searchPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelDirectoryStructure
			// 
			this._labelDirectoryStructure.AutoSize = true;
			this._labelDirectoryStructure.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._labelDirectoryStructure.Location = new System.Drawing.Point(3, 163);
			this._labelDirectoryStructure.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelDirectoryStructure.Name = "_labelDirectoryStructure";
			this._labelDirectoryStructure.Size = new System.Drawing.Size(140, 15);
			this._labelDirectoryStructure.TabIndex = 0;
			this._labelDirectoryStructure.Text = "Directory structure";
			// 
			// _labelSearchInput
			// 
			this._labelSearchInput.AutoSize = true;
			this._labelSearchInput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._labelSearchInput.Location = new System.Drawing.Point(3, 3);
			this._labelSearchInput.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelSearchInput.Name = "_labelSearchInput";
			this._labelSearchInput.Size = new System.Drawing.Size(91, 15);
			this._labelSearchInput.TabIndex = 0;
			this._labelSearchInput.Text = "Search input";
			// 
			// _labelSearchResult
			// 
			this._labelSearchResult.AutoSize = true;
			this._labelSearchResult.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._labelSearchResult.Location = new System.Drawing.Point(3, 85);
			this._labelSearchResult.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelSearchResult.Name = "_labelSearchResult";
			this._labelSearchResult.Size = new System.Drawing.Size(98, 15);
			this._labelSearchResult.TabIndex = 0;
			this._labelSearchResult.Text = "Search result";
			// 
			// _split
			// 
			this._split.Dock = System.Windows.Forms.DockStyle.Fill;
			this._split.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this._split.Location = new System.Drawing.Point(0, 0);
			this._split.Margin = new System.Windows.Forms.Padding(0);
			this._split.Name = "_split";
			// 
			// _split.Panel1
			// 
			this._split.Panel1.Controls.Add(this._labelSearchInputDetails);
			this._split.Panel1.Controls.Add(this._labelSearchResultDetails);
			this._split.Panel1.Controls.Add(this._textBoxSearchResult);
			this._split.Panel1.Controls.Add(this._labelSearchInput);
			this._split.Panel1.Controls.Add(this._labelSearchResult);
			this._split.Panel1.Controls.Add(this._searchPanel);
			this._split.Panel1.DoubleClick += new System.EventHandler(this.panel1DoubleClick);
			// 
			// _split.Panel2
			// 
			this._split.Panel2.Controls.Add(this.label1);
			this._split.Panel2.Controls.Add(this._labelQuerySyntax);
			this._split.Panel2.Controls.Add(this._buttonLog);
			this._split.Panel2.Controls.Add(this._buttonConfig);
			this._split.Panel2.Controls.Add(this._labelExamples);
			this._split.Panel2.Controls.Add(this._labelDirectoryStructureDetails);
			this._split.Panel2.Controls.Add(this._comboBoxExamples);
			this._split.Panel2.Controls.Add(this._labelFileNamePatternDetails);
			this._split.Panel2.Controls.Add(this._textBoxDirectoryStructure);
			this._split.Panel2.Controls.Add(this._labelFileNamePattern);
			this._split.Panel2.Controls.Add(this._textBoxFileNameRegex);
			this._split.Panel2.Controls.Add(this._labelDirectoryStructure);
			this._split.Panel2.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.panel2DoubleClick);
			this._split.Size = new System.Drawing.Size(1264, 762);
			this._split.SplitterDistance = 845;
			this._split.SplitterWidth = 8;
			this._split.TabIndex = 2;
			this._split.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.splitDoubleClick);
			// 
			// _labelSearchInputDetails
			// 
			this._labelSearchInputDetails.AutoSize = true;
			this._labelSearchInputDetails.Location = new System.Drawing.Point(3, 21);
			this._labelSearchInputDetails.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelSearchInputDetails.Name = "_labelSearchInputDetails";
			this._labelSearchInputDetails.Size = new System.Drawing.Size(329, 15);
			this._labelSearchInputDetails.TabIndex = 0;
			this._labelSearchInputDetails.Text = "Type and wait 1 second or press Enter to apply";
			// 
			// _labelSearchResultDetails
			// 
			this._labelSearchResultDetails.AutoSize = true;
			this._labelSearchResultDetails.Location = new System.Drawing.Point(3, 103);
			this._labelSearchResultDetails.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelSearchResultDetails.Name = "_labelSearchResultDetails";
			this._labelSearchResultDetails.Size = new System.Drawing.Size(273, 15);
			this._labelSearchResultDetails.TabIndex = 0;
			this._labelSearchResultDetails.Text = "Cltrl + click item to show in explorer";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 21);
			this.label1.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(189, 15);
			this.label1.TabIndex = 0;
			this.label1.Text = "Select from list to search";
			// 
			// _labelQuerySyntax
			// 
			this._labelQuerySyntax.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelQuerySyntax.AutoSize = true;
			this._labelQuerySyntax.Location = new System.Drawing.Point(271, 24);
			this._labelQuerySyntax.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelQuerySyntax.Name = "_labelQuerySyntax";
			this._labelQuerySyntax.Size = new System.Drawing.Size(140, 15);
			this._labelQuerySyntax.TabIndex = 0;
			this._labelQuerySyntax.TabStop = true;
			this._labelQuerySyntax.Text = "Lucene query syntax";
			this._labelQuerySyntax.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.querySyntaxLinkClicked);
			// 
			// _buttonLog
			// 
			this._buttonLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonLog.Location = new System.Drawing.Point(335, 735);
			this._buttonLog.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this._buttonLog.Name = "_buttonLog";
			this._buttonLog.Size = new System.Drawing.Size(76, 24);
			this._buttonLog.TabIndex = 7;
			this._buttonLog.Text = "Logs";
			this._buttonLog.UseVisualStyleBackColor = true;
			this._buttonLog.Click += new System.EventHandler(this.buttonLogClick);
			// 
			// _buttonConfig
			// 
			this._buttonConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonConfig.Location = new System.Drawing.Point(256, 735);
			this._buttonConfig.Name = "_buttonConfig";
			this._buttonConfig.Size = new System.Drawing.Size(76, 24);
			this._buttonConfig.TabIndex = 6;
			this._buttonConfig.Text = "Config";
			this._buttonConfig.UseVisualStyleBackColor = true;
			this._buttonConfig.Click += new System.EventHandler(this.buttonConfigClick);
			// 
			// _labelExamples
			// 
			this._labelExamples.AutoSize = true;
			this._labelExamples.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._labelExamples.Location = new System.Drawing.Point(3, 3);
			this._labelExamples.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelExamples.Name = "_labelExamples";
			this._labelExamples.Size = new System.Drawing.Size(105, 15);
			this._labelExamples.TabIndex = 0;
			this._labelExamples.Text = "Query examples";
			// 
			// _labelDirectoryStructureDetails
			// 
			this._labelDirectoryStructureDetails.AutoSize = true;
			this._labelDirectoryStructureDetails.Location = new System.Drawing.Point(3, 181);
			this._labelDirectoryStructureDetails.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelDirectoryStructureDetails.Name = "_labelDirectoryStructureDetails";
			this._labelDirectoryStructureDetails.Size = new System.Drawing.Size(336, 30);
			this._labelDirectoryStructureDetails.TabIndex = 0;
			this._labelDirectoryStructureDetails.Text = "Select observed directories and files in config\r\nCltrl + click item to show in ex" +
    "plorer";
			// 
			// _comboBoxExamples
			// 
			this._comboBoxExamples.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._comboBoxExamples.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboBoxExamples.FormattingEnabled = true;
			this._comboBoxExamples.Location = new System.Drawing.Point(0, 42);
			this._comboBoxExamples.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._comboBoxExamples.MaxDropDownItems = 20;
			this._comboBoxExamples.Name = "_comboBoxExamples";
			this._comboBoxExamples.Size = new System.Drawing.Size(411, 23);
			this._comboBoxExamples.TabIndex = 3;
			this._comboBoxExamples.SelectionChangeCommitted += new System.EventHandler(this.examplesSelectionCommitted);
			// 
			// _labelFileNamePatternDetails
			// 
			this._labelFileNamePatternDetails.AutoSize = true;
			this._labelFileNamePatternDetails.Location = new System.Drawing.Point(3, 103);
			this._labelFileNamePatternDetails.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelFileNamePatternDetails.Name = "_labelFileNamePatternDetails";
			this._labelFileNamePatternDetails.Size = new System.Drawing.Size(175, 15);
			this._labelFileNamePatternDetails.TabIndex = 0;
			this._labelFileNamePatternDetails.Text = "Can be changed in config";
			// 
			// _labelFileNamePattern
			// 
			this._labelFileNamePattern.AutoSize = true;
			this._labelFileNamePattern.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._labelFileNamePattern.Location = new System.Drawing.Point(3, 85);
			this._labelFileNamePattern.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
			this._labelFileNamePattern.Name = "_labelFileNamePattern";
			this._labelFileNamePattern.Size = new System.Drawing.Size(224, 15);
			this._labelFileNamePattern.TabIndex = 0;
			this._labelFileNamePattern.Text = "Indexed file name regex pattern";
			// 
			// _textBoxFileNameRegex
			// 
			this._textBoxFileNameRegex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxFileNameRegex.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._textBoxFileNameRegex.Location = new System.Drawing.Point(0, 121);
			this._textBoxFileNameRegex.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this._textBoxFileNameRegex.Name = "_textBoxFileNameRegex";
			this._textBoxFileNameRegex.ReadOnly = true;
			this._textBoxFileNameRegex.Size = new System.Drawing.Size(411, 23);
			this._textBoxFileNameRegex.TabIndex = 4;
			// 
			// _textBoxSearchResult
			// 
			this._textBoxSearchResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxSearchResult.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._textBoxSearchResult.Location = new System.Drawing.Point(3, 121);
			this._textBoxSearchResult.Name = "_textBoxSearchResult";
			this._textBoxSearchResult.ReadOnly = true;
			this._textBoxSearchResult.Size = new System.Drawing.Size(839, 638);
			this._textBoxSearchResult.TabIndex = 1;
			this._textBoxSearchResult.Text = "";
			this._textBoxSearchResult.WordWrap = false;
			// 
			// _searchPanel
			// 
			this._searchPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._searchPanel.BackColor = System.Drawing.Color.White;
			this._searchPanel.Controls.Add(this._searchInput);
			this._searchPanel.Location = new System.Drawing.Point(3, 39);
			this._searchPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this._searchPanel.Name = "_searchPanel";
			this._searchPanel.Size = new System.Drawing.Size(839, 27);
			this._searchPanel.TabIndex = 0;
			this._searchPanel.VisibleBorders = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			// 
			// _searchInput
			// 
			this._searchInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._searchInput.BackColor = System.Drawing.Color.White;
			this._searchInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._searchInput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._searchInput.Location = new System.Drawing.Point(6, 6);
			this._searchInput.Margin = new System.Windows.Forms.Padding(6, 6, 6, 1);
			this._searchInput.Multiline = false;
			this._searchInput.Name = "_searchInput";
			this._searchInput.Size = new System.Drawing.Size(827, 20);
			this._searchInput.TabIndex = 0;
			this._searchInput.Text = "";
			this._searchInput.WordWrap = false;
			// 
			// _textBoxDirectoryStructure
			// 
			this._textBoxDirectoryStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDirectoryStructure.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._textBoxDirectoryStructure.Location = new System.Drawing.Point(0, 214);
			this._textBoxDirectoryStructure.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this._textBoxDirectoryStructure.Name = "_textBoxDirectoryStructure";
			this._textBoxDirectoryStructure.ReadOnly = true;
			this._textBoxDirectoryStructure.Size = new System.Drawing.Size(411, 545);
			this._textBoxDirectoryStructure.TabIndex = 5;
			this._textBoxDirectoryStructure.Text = "";
			this._textBoxDirectoryStructure.WordWrap = false;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1264, 762);
			this.Controls.Add(this._split);
			this.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MainForm";
			this.Text = "Index demo application";
			this._split.Panel1.ResumeLayout(false);
			this._split.Panel1.PerformLayout();
			this._split.Panel2.ResumeLayout(false);
			this._split.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
			this._split.ResumeLayout(false);
			this._searchPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label _labelDirectoryStructure;
		private BorderedPanel _searchPanel;
		private System.Windows.Forms.Label _labelSearchInput;
		private System.Windows.Forms.Label _labelSearchResult;
		private System.Windows.Forms.SplitContainer _split;
		private FixedRichTextBox _searchInput;
		private System.Windows.Forms.Label _labelFileNamePattern;
		private System.Windows.Forms.TextBox _textBoxFileNameRegex;
		private FixedRichTextBox _textBoxSearchResult;
		private FixedRichTextBox _textBoxDirectoryStructure;
		private System.Windows.Forms.Button _buttonLog;
		private System.Windows.Forms.Button _buttonConfig;
		private System.Windows.Forms.Label _labelFileNamePatternDetails;
		private System.Windows.Forms.Label _labelDirectoryStructureDetails;
		private System.Windows.Forms.Label _labelSearchResultDetails;
		private System.Windows.Forms.Label _labelSearchInputDetails;
		private System.Windows.Forms.LinkLabel _labelQuerySyntax;
		private System.Windows.Forms.Label _labelExamples;
		private System.Windows.Forms.ComboBox _comboBoxExamples;
		private System.Windows.Forms.Label label1;
	}
}

