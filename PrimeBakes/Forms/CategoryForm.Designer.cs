namespace PrimeBakes.Forms;

partial class CategoryForm
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
		statusCheckBox = new CheckBox();
		categoryComboBox = new ComboBox();
		saveButton = new Button();
		codeLabel = new Label();
		codeTextBox = new TextBox();
		nameLabel = new Label();
		nameTextBox = new TextBox();
		SuspendLayout();
		// 
		// statusCheckBox
		// 
		statusCheckBox.AutoSize = true;
		statusCheckBox.Font = new Font("Segoe UI", 15F);
		statusCheckBox.Location = new Point(29, 167);
		statusCheckBox.Name = "statusCheckBox";
		statusCheckBox.Size = new Size(84, 32);
		statusCheckBox.TabIndex = 47;
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
		categoryComboBox.Location = new Point(52, 12);
		categoryComboBox.Name = "categoryComboBox";
		categoryComboBox.Size = new Size(271, 36);
		categoryComboBox.TabIndex = 49;
		categoryComboBox.SelectedIndexChanged += categoryComboBox_SelectedIndexChanged;
		// 
		// saveButton
		// 
		saveButton.Font = new Font("Segoe UI", 15F);
		saveButton.Location = new Point(169, 167);
		saveButton.Name = "saveButton";
		saveButton.Size = new Size(118, 38);
		saveButton.TabIndex = 48;
		saveButton.Text = "SAVE";
		saveButton.UseVisualStyleBackColor = true;
		saveButton.Click += saveButton_Click;
		// 
		// codeLabel
		// 
		codeLabel.AutoSize = true;
		codeLabel.Font = new Font("Segoe UI", 15F);
		codeLabel.Location = new Point(12, 70);
		codeLabel.Name = "codeLabel";
		codeLabel.Size = new Size(58, 28);
		codeLabel.TabIndex = 51;
		codeLabel.Text = "Code";
		// 
		// codeTextBox
		// 
		codeTextBox.CharacterCasing = CharacterCasing.Upper;
		codeTextBox.Font = new Font("Segoe UI", 15F);
		codeTextBox.Location = new Point(120, 67);
		codeTextBox.Name = "codeTextBox";
		codeTextBox.PlaceholderText = "Code";
		codeTextBox.Size = new Size(271, 34);
		codeTextBox.TabIndex = 45;
		// 
		// nameLabel
		// 
		nameLabel.AutoSize = true;
		nameLabel.Font = new Font("Segoe UI", 15F);
		nameLabel.Location = new Point(12, 110);
		nameLabel.Name = "nameLabel";
		nameLabel.Size = new Size(64, 28);
		nameLabel.TabIndex = 50;
		nameLabel.Text = "Name";
		// 
		// nameTextBox
		// 
		nameTextBox.Font = new Font("Segoe UI", 15F);
		nameTextBox.Location = new Point(120, 107);
		nameTextBox.Name = "nameTextBox";
		nameTextBox.PlaceholderText = "Name";
		nameTextBox.Size = new Size(271, 34);
		nameTextBox.TabIndex = 46;
		// 
		// CategoryForm
		// 
		AcceptButton = saveButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(410, 221);
		Controls.Add(statusCheckBox);
		Controls.Add(categoryComboBox);
		Controls.Add(saveButton);
		Controls.Add(codeLabel);
		Controls.Add(codeTextBox);
		Controls.Add(nameLabel);
		Controls.Add(nameTextBox);
		Name = "CategoryForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Catgeory";
		Load += CatgeoryForm_Load;
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion
	private CheckBox statusCheckBox;
	private ComboBox categoryComboBox;
	private Button saveButton;
	private Label codeLabel;
	private TextBox codeTextBox;
	private Label nameLabel;
	private TextBox nameTextBox;
}