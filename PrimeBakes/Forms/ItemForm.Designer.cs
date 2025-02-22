namespace PrimeBakes.Forms;

partial class ItemForm
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
		saveButton = new Button();
		codeLabel = new Label();
		codeTextBox = new TextBox();
		nameLabel = new Label();
		nameTextBox = new TextBox();
		statusCheckBox = new CheckBox();
		categoryComboBox = new ComboBox();
		label1 = new Label();
		brandingLabel = new Label();
		richTextBoxFooter = new RichTextBox();
		userCategoryComboBox = new ComboBox();
		categoryLabel = new Label();
		itemListBox = new ListBox();
		codeSearchTextBox = new TextBox();
		nameSearchTextBox = new TextBox();
		SuspendLayout();
		// 
		// saveButton
		// 
		saveButton.Font = new Font("Segoe UI", 15F);
		saveButton.Location = new Point(547, 243);
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
		codeLabel.Location = new Point(402, 109);
		codeLabel.Name = "codeLabel";
		codeLabel.Size = new Size(58, 28);
		codeLabel.TabIndex = 42;
		codeLabel.Text = "Code";
		// 
		// codeTextBox
		// 
		codeTextBox.CharacterCasing = CharacterCasing.Upper;
		codeTextBox.Font = new Font("Segoe UI", 15F);
		codeTextBox.Location = new Point(510, 106);
		codeTextBox.MaxLength = 100;
		codeTextBox.Name = "codeTextBox";
		codeTextBox.PlaceholderText = "Code";
		codeTextBox.Size = new Size(271, 34);
		codeTextBox.TabIndex = 2;
		// 
		// nameLabel
		// 
		nameLabel.AutoSize = true;
		nameLabel.Font = new Font("Segoe UI", 15F);
		nameLabel.Location = new Point(402, 149);
		nameLabel.Name = "nameLabel";
		nameLabel.Size = new Size(64, 28);
		nameLabel.TabIndex = 40;
		nameLabel.Text = "Name";
		// 
		// nameTextBox
		// 
		nameTextBox.Font = new Font("Segoe UI", 15F);
		nameTextBox.Location = new Point(510, 146);
		nameTextBox.MaxLength = 100;
		nameTextBox.Name = "nameTextBox";
		nameTextBox.PlaceholderText = "Name";
		nameTextBox.Size = new Size(271, 34);
		nameTextBox.TabIndex = 3;
		// 
		// statusCheckBox
		// 
		statusCheckBox.AutoSize = true;
		statusCheckBox.Font = new Font("Segoe UI", 15F);
		statusCheckBox.Location = new Point(407, 243);
		statusCheckBox.Name = "statusCheckBox";
		statusCheckBox.Size = new Size(84, 32);
		statusCheckBox.TabIndex = 4;
		statusCheckBox.Text = "Status";
		statusCheckBox.UseVisualStyleBackColor = true;
		// 
		// categoryComboBox
		// 
		categoryComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
		categoryComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		categoryComboBox.FlatStyle = FlatStyle.System;
		categoryComboBox.Font = new Font("Segoe UI", 15F);
		categoryComboBox.FormattingEnabled = true;
		categoryComboBox.Location = new Point(510, 64);
		categoryComboBox.Name = "categoryComboBox";
		categoryComboBox.Size = new Size(271, 36);
		categoryComboBox.TabIndex = 1;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Font = new Font("Segoe UI", 15F);
		label1.Location = new Point(402, 67);
		label1.Name = "label1";
		label1.Size = new Size(92, 28);
		label1.TabIndex = 44;
		label1.Text = "Category";
		// 
		// brandingLabel
		// 
		brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		brandingLabel.AutoSize = true;
		brandingLabel.BackColor = Color.White;
		brandingLabel.Location = new Point(714, 427);
		brandingLabel.Name = "brandingLabel";
		brandingLabel.Size = new Size(76, 15);
		brandingLabel.TabIndex = 46;
		brandingLabel.Text = "© AADISOFT";
		// 
		// richTextBoxFooter
		// 
		richTextBoxFooter.Dock = DockStyle.Bottom;
		richTextBoxFooter.Location = new Point(0, 422);
		richTextBoxFooter.Name = "richTextBoxFooter";
		richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
		richTextBoxFooter.Size = new Size(793, 26);
		richTextBoxFooter.TabIndex = 45;
		richTextBoxFooter.Text = "";
		// 
		// userCategoryComboBox
		// 
		userCategoryComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
		userCategoryComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		userCategoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		userCategoryComboBox.FlatStyle = FlatStyle.System;
		userCategoryComboBox.Font = new Font("Segoe UI", 15F);
		userCategoryComboBox.FormattingEnabled = true;
		userCategoryComboBox.Location = new Point(510, 186);
		userCategoryComboBox.Name = "userCategoryComboBox";
		userCategoryComboBox.Size = new Size(271, 36);
		userCategoryComboBox.TabIndex = 47;
		// 
		// categoryLabel
		// 
		categoryLabel.AutoSize = true;
		categoryLabel.Font = new Font("Segoe UI", 10F);
		categoryLabel.Location = new Point(402, 198);
		categoryLabel.Name = "categoryLabel";
		categoryLabel.Size = new Size(97, 19);
		categoryLabel.TabIndex = 48;
		categoryLabel.Text = "User Category";
		// 
		// itemListBox
		// 
		itemListBox.FormattingEnabled = true;
		itemListBox.ItemHeight = 15;
		itemListBox.Location = new Point(12, 52);
		itemListBox.Name = "itemListBox";
		itemListBox.Size = new Size(384, 349);
		itemListBox.TabIndex = 49;
		itemListBox.SelectedIndexChanged += itemListBox_SelectedIndexChanged;
		// 
		// codeSearchTextBox
		// 
		codeSearchTextBox.CharacterCasing = CharacterCasing.Upper;
		codeSearchTextBox.Font = new Font("Segoe UI", 10F);
		codeSearchTextBox.Location = new Point(219, 12);
		codeSearchTextBox.MaxLength = 100;
		codeSearchTextBox.Name = "codeSearchTextBox";
		codeSearchTextBox.PlaceholderText = "Code";
		codeSearchTextBox.Size = new Size(177, 25);
		codeSearchTextBox.TabIndex = 50;
		codeSearchTextBox.TextChanged += codeSearchTextBox_TextChanged;
		// 
		// nameSearchTextBox
		// 
		nameSearchTextBox.Font = new Font("Segoe UI", 10F);
		nameSearchTextBox.Location = new Point(12, 12);
		nameSearchTextBox.MaxLength = 100;
		nameSearchTextBox.Name = "nameSearchTextBox";
		nameSearchTextBox.PlaceholderText = "Name";
		nameSearchTextBox.Size = new Size(201, 25);
		nameSearchTextBox.TabIndex = 51;
		nameSearchTextBox.TextChanged += nameSearchTextBox_TextChanged;
		// 
		// ItemForm
		// 
		AcceptButton = saveButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(793, 448);
		Controls.Add(nameSearchTextBox);
		Controls.Add(codeSearchTextBox);
		Controls.Add(itemListBox);
		Controls.Add(userCategoryComboBox);
		Controls.Add(categoryLabel);
		Controls.Add(brandingLabel);
		Controls.Add(richTextBoxFooter);
		Controls.Add(label1);
		Controls.Add(categoryComboBox);
		Controls.Add(statusCheckBox);
		Controls.Add(saveButton);
		Controls.Add(codeLabel);
		Controls.Add(codeTextBox);
		Controls.Add(nameLabel);
		Controls.Add(nameTextBox);
		Name = "ItemForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Item";
		Load += ItemForm_Load;
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion
	private Button saveButton;
	private Label codeLabel;
	private TextBox codeTextBox;
	private Label nameLabel;
	private TextBox nameTextBox;
	private CheckBox statusCheckBox;
	private ComboBox categoryComboBox;
	private Label label1;
	private Label brandingLabel;
	private RichTextBox richTextBoxFooter;
	private ComboBox userCategoryComboBox;
	private Label categoryLabel;
	private ListBox itemListBox;
	private TextBox codeSearchTextBox;
	private TextBox nameSearchTextBox;
}