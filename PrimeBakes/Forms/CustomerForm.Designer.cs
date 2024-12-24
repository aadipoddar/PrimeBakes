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
			statusLabel = new Label();
			statusComboBox = new ComboBox();
			customerComboBox = new ComboBox();
			saveButton = new Button();
			codeLabel = new Label();
			codeTextBox = new TextBox();
			nameLabel = new Label();
			customerNameTextBox = new TextBox();
			SuspendLayout();
			// 
			// statusLabel
			// 
			statusLabel.AutoSize = true;
			statusLabel.Font = new Font("Segoe UI", 15F);
			statusLabel.Location = new Point(12, 152);
			statusLabel.Name = "statusLabel";
			statusLabel.Size = new Size(65, 28);
			statusLabel.TabIndex = 35;
			statusLabel.Text = "Status";
			// 
			// statusComboBox
			// 
			statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			statusComboBox.FlatStyle = FlatStyle.System;
			statusComboBox.Font = new Font("Segoe UI", 15F);
			statusComboBox.FormattingEnabled = true;
			statusComboBox.Items.AddRange(new object[] { "Active", "Inactive" });
			statusComboBox.Location = new Point(181, 149);
			statusComboBox.Name = "statusComboBox";
			statusComboBox.Size = new Size(271, 36);
			statusComboBox.TabIndex = 30;
			// 
			// customerComboBox
			// 
			customerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			customerComboBox.FlatStyle = FlatStyle.System;
			customerComboBox.Font = new Font("Segoe UI", 15F);
			customerComboBox.FormattingEnabled = true;
			customerComboBox.Location = new Point(89, 12);
			customerComboBox.Name = "customerComboBox";
			customerComboBox.Size = new Size(271, 36);
			customerComboBox.TabIndex = 33;
			// 
			// saveButton
			// 
			saveButton.Font = new Font("Segoe UI", 15F);
			saveButton.Location = new Point(172, 210);
			saveButton.Name = "saveButton";
			saveButton.Size = new Size(118, 38);
			saveButton.TabIndex = 31;
			saveButton.Text = "SAVE";
			saveButton.UseVisualStyleBackColor = true;
			// 
			// codeLabel
			// 
			codeLabel.AutoSize = true;
			codeLabel.Font = new Font("Segoe UI", 15F);
			codeLabel.Location = new Point(12, 112);
			codeLabel.Name = "codeLabel";
			codeLabel.Size = new Size(58, 28);
			codeLabel.TabIndex = 34;
			codeLabel.Text = "Code";
			// 
			// codeTextBox
			// 
			codeTextBox.Font = new Font("Segoe UI", 15F);
			codeTextBox.Location = new Point(181, 109);
			codeTextBox.Name = "codeTextBox";
			codeTextBox.PasswordChar = '*';
			codeTextBox.PlaceholderText = "Code";
			codeTextBox.Size = new Size(271, 34);
			codeTextBox.TabIndex = 29;
			// 
			// nameLabel
			// 
			nameLabel.AutoSize = true;
			nameLabel.Font = new Font("Segoe UI", 15F);
			nameLabel.Location = new Point(12, 72);
			nameLabel.Name = "nameLabel";
			nameLabel.Size = new Size(64, 28);
			nameLabel.TabIndex = 32;
			nameLabel.Text = "Name";
			// 
			// customerNameTextBox
			// 
			customerNameTextBox.Font = new Font("Segoe UI", 15F);
			customerNameTextBox.Location = new Point(181, 69);
			customerNameTextBox.Name = "customerNameTextBox";
			customerNameTextBox.PlaceholderText = "Customer Name";
			customerNameTextBox.Size = new Size(271, 34);
			customerNameTextBox.TabIndex = 28;
			// 
			// CustomerForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(482, 276);
			Controls.Add(statusLabel);
			Controls.Add(statusComboBox);
			Controls.Add(customerComboBox);
			Controls.Add(saveButton);
			Controls.Add(codeLabel);
			Controls.Add(codeTextBox);
			Controls.Add(nameLabel);
			Controls.Add(customerNameTextBox);
			Name = "CustomerForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "CustomerForm";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label statusLabel;
		private ComboBox statusComboBox;
		private ComboBox customerComboBox;
		private Button saveButton;
		private Label codeLabel;
		private TextBox codeTextBox;
		private Label nameLabel;
		private TextBox customerNameTextBox;
	}
}