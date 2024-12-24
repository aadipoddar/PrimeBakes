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
		statusLabel = new Label();
		statusComboBox = new ComboBox();
		itemComboBox = new ComboBox();
		saveButton = new Button();
		codeLabel = new Label();
		codeTextBox = new TextBox();
		nameLabel = new Label();
		itemNameTextBox = new TextBox();
		SuspendLayout();
		// 
		// statusLabel
		// 
		statusLabel.AutoSize = true;
		statusLabel.Font = new Font("Segoe UI", 15F);
		statusLabel.Location = new Point(16, 152);
		statusLabel.Name = "statusLabel";
		statusLabel.Size = new Size(65, 28);
		statusLabel.TabIndex = 43;
		statusLabel.Text = "Status";
		// 
		// statusComboBox
		// 
		statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		statusComboBox.FlatStyle = FlatStyle.System;
		statusComboBox.Font = new Font("Segoe UI", 15F);
		statusComboBox.FormattingEnabled = true;
		statusComboBox.Items.AddRange(new object[] { "Active", "Inactive" });
		statusComboBox.Location = new Point(185, 149);
		statusComboBox.Name = "statusComboBox";
		statusComboBox.Size = new Size(271, 36);
		statusComboBox.TabIndex = 38;
		// 
		// itemComboBox
		// 
		itemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		itemComboBox.FlatStyle = FlatStyle.System;
		itemComboBox.Font = new Font("Segoe UI", 15F);
		itemComboBox.FormattingEnabled = true;
		itemComboBox.Location = new Point(93, 12);
		itemComboBox.Name = "itemComboBox";
		itemComboBox.Size = new Size(271, 36);
		itemComboBox.TabIndex = 41;
		// 
		// saveButton
		// 
		saveButton.Font = new Font("Segoe UI", 15F);
		saveButton.Location = new Point(176, 210);
		saveButton.Name = "saveButton";
		saveButton.Size = new Size(118, 38);
		saveButton.TabIndex = 39;
		saveButton.Text = "SAVE";
		saveButton.UseVisualStyleBackColor = true;
		// 
		// codeLabel
		// 
		codeLabel.AutoSize = true;
		codeLabel.Font = new Font("Segoe UI", 15F);
		codeLabel.Location = new Point(16, 112);
		codeLabel.Name = "codeLabel";
		codeLabel.Size = new Size(58, 28);
		codeLabel.TabIndex = 42;
		codeLabel.Text = "Code";
		// 
		// codeTextBox
		// 
		codeTextBox.Font = new Font("Segoe UI", 15F);
		codeTextBox.Location = new Point(185, 109);
		codeTextBox.Name = "codeTextBox";
		codeTextBox.PasswordChar = '*';
		codeTextBox.PlaceholderText = "Code";
		codeTextBox.Size = new Size(271, 34);
		codeTextBox.TabIndex = 37;
		// 
		// nameLabel
		// 
		nameLabel.AutoSize = true;
		nameLabel.Font = new Font("Segoe UI", 15F);
		nameLabel.Location = new Point(16, 72);
		nameLabel.Name = "nameLabel";
		nameLabel.Size = new Size(64, 28);
		nameLabel.TabIndex = 40;
		nameLabel.Text = "Name";
		// 
		// itemNameTextBox
		// 
		itemNameTextBox.Font = new Font("Segoe UI", 15F);
		itemNameTextBox.Location = new Point(185, 69);
		itemNameTextBox.Name = "itemNameTextBox";
		itemNameTextBox.PlaceholderText = "Item Name";
		itemNameTextBox.Size = new Size(271, 34);
		itemNameTextBox.TabIndex = 36;
		// 
		// ItemForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(484, 268);
		Controls.Add(statusLabel);
		Controls.Add(statusComboBox);
		Controls.Add(itemComboBox);
		Controls.Add(saveButton);
		Controls.Add(codeLabel);
		Controls.Add(codeTextBox);
		Controls.Add(nameLabel);
		Controls.Add(itemNameTextBox);
		Name = "ItemForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "ItemForm";
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private Label statusLabel;
	private ComboBox statusComboBox;
	private ComboBox itemComboBox;
	private Button saveButton;
	private Label codeLabel;
	private TextBox codeTextBox;
	private Label nameLabel;
	private TextBox itemNameTextBox;
}