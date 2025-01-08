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
		itemComboBox = new ComboBox();
		saveButton = new Button();
		codeLabel = new Label();
		codeTextBox = new TextBox();
		nameLabel = new Label();
		nameTextBox = new TextBox();
		statusCheckBox = new CheckBox();
		categoryComboBox = new ComboBox();
		label1 = new Label();
		SuspendLayout();
		// 
		// itemComboBox
		// 
		itemComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
		itemComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		itemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		itemComboBox.FlatStyle = FlatStyle.System;
		itemComboBox.Font = new Font("Segoe UI", 15F);
		itemComboBox.FormattingEnabled = true;
		itemComboBox.Location = new Point(52, 12);
		itemComboBox.Name = "itemComboBox";
		itemComboBox.Size = new Size(271, 36);
		itemComboBox.TabIndex = 6;
		itemComboBox.SelectedIndexChanged += itemComboBox_SelectedIndexChanged;
		// 
		// saveButton
		// 
		saveButton.Font = new Font("Segoe UI", 15F);
		saveButton.Location = new Point(169, 208);
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
		codeLabel.Location = new Point(12, 111);
		codeLabel.Name = "codeLabel";
		codeLabel.Size = new Size(58, 28);
		codeLabel.TabIndex = 42;
		codeLabel.Text = "Code";
		// 
		// codeTextBox
		// 
		codeTextBox.CharacterCasing = CharacterCasing.Upper;
		codeTextBox.Font = new Font("Segoe UI", 15F);
		codeTextBox.Location = new Point(120, 108);
		codeTextBox.Name = "codeTextBox";
		codeTextBox.PlaceholderText = "Code";
		codeTextBox.Size = new Size(271, 34);
		codeTextBox.TabIndex = 2;
		// 
		// nameLabel
		// 
		nameLabel.AutoSize = true;
		nameLabel.Font = new Font("Segoe UI", 15F);
		nameLabel.Location = new Point(12, 151);
		nameLabel.Name = "nameLabel";
		nameLabel.Size = new Size(64, 28);
		nameLabel.TabIndex = 40;
		nameLabel.Text = "Name";
		// 
		// nameTextBox
		// 
		nameTextBox.Font = new Font("Segoe UI", 15F);
		nameTextBox.Location = new Point(120, 148);
		nameTextBox.Name = "nameTextBox";
		nameTextBox.PlaceholderText = "Name";
		nameTextBox.Size = new Size(271, 34);
		nameTextBox.TabIndex = 3;
		// 
		// statusCheckBox
		// 
		statusCheckBox.AutoSize = true;
		statusCheckBox.Font = new Font("Segoe UI", 15F);
		statusCheckBox.Location = new Point(29, 208);
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
		categoryComboBox.Location = new Point(120, 66);
		categoryComboBox.Name = "categoryComboBox";
		categoryComboBox.Size = new Size(271, 36);
		categoryComboBox.TabIndex = 1;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Font = new Font("Segoe UI", 15F);
		label1.Location = new Point(12, 69);
		label1.Name = "label1";
		label1.Size = new Size(92, 28);
		label1.TabIndex = 44;
		label1.Text = "Category";
		// 
		// ItemForm
		// 
		AcceptButton = saveButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(426, 268);
		Controls.Add(label1);
		Controls.Add(categoryComboBox);
		Controls.Add(statusCheckBox);
		Controls.Add(itemComboBox);
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
	private ComboBox itemComboBox;
	private Button saveButton;
	private Label codeLabel;
	private TextBox codeTextBox;
	private Label nameLabel;
	private TextBox nameTextBox;
	private CheckBox statusCheckBox;
	private ComboBox categoryComboBox;
	private Label label1;
}