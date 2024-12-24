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
			statusLabel = new Label();
			statusComboBox = new ComboBox();
			employeeComboBox = new ComboBox();
			saveButton = new Button();
			passwordLabel = new Label();
			passwordTextBox = new TextBox();
			nameLabel = new Label();
			employeeNameTextBox = new TextBox();
			SuspendLayout();
			// 
			// statusLabel
			// 
			statusLabel.AutoSize = true;
			statusLabel.Font = new Font("Segoe UI", 15F);
			statusLabel.Location = new Point(19, 152);
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
			statusComboBox.Location = new Point(188, 149);
			statusComboBox.Name = "statusComboBox";
			statusComboBox.Size = new Size(271, 36);
			statusComboBox.TabIndex = 30;
			// 
			// employeeComboBox
			// 
			employeeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			employeeComboBox.FlatStyle = FlatStyle.System;
			employeeComboBox.Font = new Font("Segoe UI", 15F);
			employeeComboBox.FormattingEnabled = true;
			employeeComboBox.Location = new Point(96, 12);
			employeeComboBox.Name = "employeeComboBox";
			employeeComboBox.Size = new Size(271, 36);
			employeeComboBox.TabIndex = 33;
			// 
			// saveButton
			// 
			saveButton.Font = new Font("Segoe UI", 15F);
			saveButton.Location = new Point(179, 210);
			saveButton.Name = "saveButton";
			saveButton.Size = new Size(118, 38);
			saveButton.TabIndex = 31;
			saveButton.Text = "SAVE";
			saveButton.UseVisualStyleBackColor = true;
			// 
			// passwordLabel
			// 
			passwordLabel.AutoSize = true;
			passwordLabel.Font = new Font("Segoe UI", 15F);
			passwordLabel.Location = new Point(19, 112);
			passwordLabel.Name = "passwordLabel";
			passwordLabel.Size = new Size(93, 28);
			passwordLabel.TabIndex = 34;
			passwordLabel.Text = "Password";
			// 
			// passwordTextBox
			// 
			passwordTextBox.Font = new Font("Segoe UI", 15F);
			passwordTextBox.Location = new Point(188, 109);
			passwordTextBox.Name = "passwordTextBox";
			passwordTextBox.PasswordChar = '*';
			passwordTextBox.PlaceholderText = "Password";
			passwordTextBox.Size = new Size(271, 34);
			passwordTextBox.TabIndex = 29;
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
			// employeeNameTextBox
			// 
			employeeNameTextBox.Font = new Font("Segoe UI", 15F);
			employeeNameTextBox.Location = new Point(188, 69);
			employeeNameTextBox.Name = "employeeNameTextBox";
			employeeNameTextBox.PlaceholderText = "Employee Name";
			employeeNameTextBox.Size = new Size(271, 34);
			employeeNameTextBox.TabIndex = 28;
			// 
			// UserForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(504, 284);
			Controls.Add(statusLabel);
			Controls.Add(statusComboBox);
			Controls.Add(employeeComboBox);
			Controls.Add(saveButton);
			Controls.Add(passwordLabel);
			Controls.Add(passwordTextBox);
			Controls.Add(nameLabel);
			Controls.Add(employeeNameTextBox);
			Name = "UserForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "UserForm";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label statusLabel;
		private ComboBox statusComboBox;
		private ComboBox employeeComboBox;
		private Button saveButton;
		private Label passwordLabel;
		private TextBox passwordTextBox;
		private Label nameLabel;
		private TextBox employeeNameTextBox;
	}
}