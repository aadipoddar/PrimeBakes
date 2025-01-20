namespace PrimeBakes.Forms
{
	partial class CustomerForm
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
			customerComboBox = new ComboBox();
			saveButton = new Button();
			codeLabel = new Label();
			codeTextBox = new TextBox();
			statusCheckBox = new CheckBox();
			nameLabel = new Label();
			nameTextBox = new TextBox();
			emailLabel = new Label();
			emailTextBox = new TextBox();
			brandingLabel = new Label();
			richTextBoxFooter = new RichTextBox();
			SuspendLayout();
			// 
			// customerComboBox
			// 
			customerComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			customerComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
			customerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			customerComboBox.FlatStyle = FlatStyle.System;
			customerComboBox.Font = new Font("Segoe UI", 15F);
			customerComboBox.FormattingEnabled = true;
			customerComboBox.Location = new Point(42, 12);
			customerComboBox.Name = "customerComboBox";
			customerComboBox.Size = new Size(271, 36);
			customerComboBox.TabIndex = 6;
			customerComboBox.SelectedIndexChanged += customerComboBox_SelectedIndexChanged;
			// 
			// saveButton
			// 
			saveButton.Font = new Font("Segoe UI", 15F);
			saveButton.Location = new Point(131, 191);
			saveButton.Name = "saveButton";
			saveButton.Size = new Size(118, 38);
			saveButton.TabIndex = 5;
			saveButton.Text = "SAVE";
			saveButton.UseVisualStyleBackColor = true;
			saveButton.Click += saveButton_Click;
			// 
			// codeLabel
			// 
			codeLabel.AutoSize = true;
			codeLabel.Font = new Font("Segoe UI", 15F);
			codeLabel.Location = new Point(12, 71);
			codeLabel.Name = "codeLabel";
			codeLabel.Size = new Size(58, 28);
			codeLabel.TabIndex = 34;
			codeLabel.Text = "Code";
			// 
			// codeTextBox
			// 
			codeTextBox.CharacterCasing = CharacterCasing.Upper;
			codeTextBox.Font = new Font("Segoe UI", 15F);
			codeTextBox.Location = new Point(86, 68);
			codeTextBox.MaxLength = 100;
			codeTextBox.Name = "codeTextBox";
			codeTextBox.PlaceholderText = "Code";
			codeTextBox.Size = new Size(271, 34);
			codeTextBox.TabIndex = 1;
			// 
			// statusCheckBox
			// 
			statusCheckBox.AutoSize = true;
			statusCheckBox.Font = new Font("Segoe UI", 15F);
			statusCheckBox.Location = new Point(12, 195);
			statusCheckBox.Name = "statusCheckBox";
			statusCheckBox.Size = new Size(84, 32);
			statusCheckBox.TabIndex = 4;
			statusCheckBox.Text = "Status";
			statusCheckBox.UseVisualStyleBackColor = true;
			// 
			// nameLabel
			// 
			nameLabel.AutoSize = true;
			nameLabel.Font = new Font("Segoe UI", 15F);
			nameLabel.Location = new Point(12, 111);
			nameLabel.Name = "nameLabel";
			nameLabel.Size = new Size(64, 28);
			nameLabel.TabIndex = 36;
			nameLabel.Text = "Name";
			// 
			// nameTextBox
			// 
			nameTextBox.Font = new Font("Segoe UI", 15F);
			nameTextBox.Location = new Point(86, 108);
			nameTextBox.MaxLength = 100;
			nameTextBox.Name = "nameTextBox";
			nameTextBox.PlaceholderText = "Name";
			nameTextBox.Size = new Size(271, 34);
			nameTextBox.TabIndex = 2;
			// 
			// emailLabel
			// 
			emailLabel.AutoSize = true;
			emailLabel.Font = new Font("Segoe UI", 15F);
			emailLabel.Location = new Point(12, 151);
			emailLabel.Name = "emailLabel";
			emailLabel.Size = new Size(59, 28);
			emailLabel.TabIndex = 38;
			emailLabel.Text = "Email";
			// 
			// emailTextBox
			// 
			emailTextBox.Font = new Font("Segoe UI", 15F);
			emailTextBox.Location = new Point(86, 148);
			emailTextBox.MaxLength = 100;
			emailTextBox.Name = "emailTextBox";
			emailTextBox.PlaceholderText = "Email";
			emailTextBox.Size = new Size(271, 34);
			emailTextBox.TabIndex = 3;
			// 
			// brandingLabel
			// 
			brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			brandingLabel.AutoSize = true;
			brandingLabel.BackColor = Color.White;
			brandingLabel.Location = new Point(300, 243);
			brandingLabel.Name = "brandingLabel";
			brandingLabel.Size = new Size(76, 15);
			brandingLabel.TabIndex = 40;
			brandingLabel.Text = "© AADISOFT";
			// 
			// richTextBoxFooter
			// 
			richTextBoxFooter.Dock = DockStyle.Bottom;
			richTextBoxFooter.Location = new Point(0, 237);
			richTextBoxFooter.Name = "richTextBoxFooter";
			richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
			richTextBoxFooter.Size = new Size(376, 26);
			richTextBoxFooter.TabIndex = 39;
			richTextBoxFooter.Text = "Version 0.0.0.0";
			// 
			// CustomerForm
			// 
			AcceptButton = saveButton;
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(376, 263);
			Controls.Add(brandingLabel);
			Controls.Add(richTextBoxFooter);
			Controls.Add(emailLabel);
			Controls.Add(emailTextBox);
			Controls.Add(nameLabel);
			Controls.Add(nameTextBox);
			Controls.Add(statusCheckBox);
			Controls.Add(customerComboBox);
			Controls.Add(saveButton);
			Controls.Add(codeLabel);
			Controls.Add(codeTextBox);
			Name = "CustomerForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Customer";
			Load += CustomerForm_Load;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private ComboBox customerComboBox;
		private Button saveButton;
		private Label codeLabel;
		private TextBox codeTextBox;
		private CheckBox statusCheckBox;
		private Label nameLabel;
		private TextBox nameTextBox;
		private Label emailLabel;
		private TextBox emailTextBox;
		private Label brandingLabel;
		private RichTextBox richTextBoxFooter;
	}
}