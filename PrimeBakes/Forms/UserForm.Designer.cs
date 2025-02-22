namespace PrimeBakes.Forms
{
	partial class UserForm
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
			userComboBox = new ComboBox();
			saveButton = new Button();
			passwordLabel = new Label();
			passwordTextBox = new TextBox();
			nameLabel = new Label();
			nameTextBox = new TextBox();
			statusCheckBox = new CheckBox();
			customerLabel = new Label();
			customerComboBox = new ComboBox();
			codeLabel = new Label();
			codeTextBox = new TextBox();
			brandingLabel = new Label();
			richTextBoxFooter = new RichTextBox();
			categoryComboBox = new ComboBox();
			categoryLabel = new Label();
			SuspendLayout();
			// 
			// userComboBox
			// 
			userComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			userComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
			userComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			userComboBox.FlatStyle = FlatStyle.System;
			userComboBox.Font = new Font("Segoe UI", 15F);
			userComboBox.FormattingEnabled = true;
			userComboBox.Location = new Point(96, 12);
			userComboBox.Name = "userComboBox";
			userComboBox.Size = new Size(271, 36);
			userComboBox.TabIndex = 7;
			userComboBox.SelectedIndexChanged += userComboBox_SelectedIndexChanged;
			// 
			// saveButton
			// 
			saveButton.Font = new Font("Segoe UI", 15F);
			saveButton.Location = new Point(140, 283);
			saveButton.Name = "saveButton";
			saveButton.Size = new Size(118, 38);
			saveButton.TabIndex = 6;
			saveButton.Text = "SAVE";
			saveButton.UseVisualStyleBackColor = true;
			saveButton.Click += saveButton_Click;
			// 
			// passwordLabel
			// 
			passwordLabel.AutoSize = true;
			passwordLabel.Font = new Font("Segoe UI", 15F);
			passwordLabel.Location = new Point(19, 152);
			passwordLabel.Name = "passwordLabel";
			passwordLabel.Size = new Size(93, 28);
			passwordLabel.TabIndex = 34;
			passwordLabel.Text = "Password";
			// 
			// passwordTextBox
			// 
			passwordTextBox.Font = new Font("Segoe UI", 15F);
			passwordTextBox.Location = new Point(127, 149);
			passwordTextBox.MaxLength = 100;
			passwordTextBox.Name = "passwordTextBox";
			passwordTextBox.PasswordChar = '*';
			passwordTextBox.PlaceholderText = "Password";
			passwordTextBox.Size = new Size(271, 34);
			passwordTextBox.TabIndex = 3;
			// 
			// nameLabel
			// 
			nameLabel.AutoSize = true;
			nameLabel.Font = new Font("Segoe UI", 15F);
			nameLabel.Location = new Point(19, 72);
			nameLabel.Name = "nameLabel";
			nameLabel.Size = new Size(64, 28);
			nameLabel.TabIndex = 32;
			nameLabel.Text = "Name";
			// 
			// nameTextBox
			// 
			nameTextBox.Font = new Font("Segoe UI", 15F);
			nameTextBox.Location = new Point(127, 69);
			nameTextBox.MaxLength = 100;
			nameTextBox.Name = "nameTextBox";
			nameTextBox.PlaceholderText = "Name";
			nameTextBox.Size = new Size(271, 34);
			nameTextBox.TabIndex = 1;
			// 
			// statusCheckBox
			// 
			statusCheckBox.AutoSize = true;
			statusCheckBox.Font = new Font("Segoe UI", 15F);
			statusCheckBox.Location = new Point(27, 287);
			statusCheckBox.Name = "statusCheckBox";
			statusCheckBox.Size = new Size(84, 32);
			statusCheckBox.TabIndex = 5;
			statusCheckBox.Text = "Status";
			statusCheckBox.UseVisualStyleBackColor = true;
			// 
			// customerLabel
			// 
			customerLabel.AutoSize = true;
			customerLabel.Font = new Font("Segoe UI", 15F);
			customerLabel.Location = new Point(19, 192);
			customerLabel.Name = "customerLabel";
			customerLabel.Size = new Size(96, 28);
			customerLabel.TabIndex = 36;
			customerLabel.Text = "Customer";
			// 
			// customerComboBox
			// 
			customerComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			customerComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
			customerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			customerComboBox.FlatStyle = FlatStyle.System;
			customerComboBox.Font = new Font("Segoe UI", 15F);
			customerComboBox.FormattingEnabled = true;
			customerComboBox.Location = new Point(127, 189);
			customerComboBox.Name = "customerComboBox";
			customerComboBox.Size = new Size(271, 36);
			customerComboBox.TabIndex = 4;
			// 
			// codeLabel
			// 
			codeLabel.AutoSize = true;
			codeLabel.Font = new Font("Segoe UI", 15F);
			codeLabel.Location = new Point(19, 112);
			codeLabel.Name = "codeLabel";
			codeLabel.Size = new Size(58, 28);
			codeLabel.TabIndex = 38;
			codeLabel.Text = "Code";
			// 
			// codeTextBox
			// 
			codeTextBox.CharacterCasing = CharacterCasing.Upper;
			codeTextBox.Font = new Font("Segoe UI", 15F);
			codeTextBox.Location = new Point(127, 109);
			codeTextBox.MaxLength = 100;
			codeTextBox.Name = "codeTextBox";
			codeTextBox.PlaceholderText = "Code";
			codeTextBox.Size = new Size(271, 34);
			codeTextBox.TabIndex = 2;
			// 
			// brandingLabel
			// 
			brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			brandingLabel.AutoSize = true;
			brandingLabel.BackColor = Color.White;
			brandingLabel.Location = new Point(336, 358);
			brandingLabel.Name = "brandingLabel";
			brandingLabel.Size = new Size(76, 15);
			brandingLabel.TabIndex = 40;
			brandingLabel.Text = "© AADISOFT";
			// 
			// richTextBoxFooter
			// 
			richTextBoxFooter.Dock = DockStyle.Bottom;
			richTextBoxFooter.Location = new Point(0, 352);
			richTextBoxFooter.Name = "richTextBoxFooter";
			richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
			richTextBoxFooter.Size = new Size(416, 26);
			richTextBoxFooter.TabIndex = 39;
			richTextBoxFooter.Text = "";
			// 
			// categoryComboBox
			// 
			categoryComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			categoryComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
			categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			categoryComboBox.FlatStyle = FlatStyle.System;
			categoryComboBox.Font = new Font("Segoe UI", 15F);
			categoryComboBox.FormattingEnabled = true;
			categoryComboBox.Location = new Point(127, 231);
			categoryComboBox.Name = "categoryComboBox";
			categoryComboBox.Size = new Size(271, 36);
			categoryComboBox.TabIndex = 41;
			// 
			// categoryLabel
			// 
			categoryLabel.AutoSize = true;
			categoryLabel.Font = new Font("Segoe UI", 15F);
			categoryLabel.Location = new Point(19, 234);
			categoryLabel.Name = "categoryLabel";
			categoryLabel.Size = new Size(92, 28);
			categoryLabel.TabIndex = 42;
			categoryLabel.Text = "Category";
			// 
			// UserForm
			// 
			AcceptButton = saveButton;
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(416, 378);
			Controls.Add(categoryComboBox);
			Controls.Add(categoryLabel);
			Controls.Add(brandingLabel);
			Controls.Add(richTextBoxFooter);
			Controls.Add(codeLabel);
			Controls.Add(codeTextBox);
			Controls.Add(customerComboBox);
			Controls.Add(customerLabel);
			Controls.Add(statusCheckBox);
			Controls.Add(userComboBox);
			Controls.Add(saveButton);
			Controls.Add(passwordLabel);
			Controls.Add(passwordTextBox);
			Controls.Add(nameLabel);
			Controls.Add(nameTextBox);
			Name = "UserForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "User";
			Load += UserForm_Load;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private ComboBox userComboBox;
		private Button saveButton;
		private Label passwordLabel;
		private TextBox passwordTextBox;
		private Label nameLabel;
		private TextBox nameTextBox;
		private CheckBox statusCheckBox;
		private Label customerLabel;
		private ComboBox customerComboBox;
		private Label codeLabel;
		private TextBox codeTextBox;
		private Label brandingLabel;
		private RichTextBox richTextBoxFooter;
		private ComboBox categoryComboBox;
		private Label categoryLabel;
	}
}