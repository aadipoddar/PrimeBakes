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
			SuspendLayout();
			// 
			// userComboBox
			// 
			userComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			userComboBox.FlatStyle = FlatStyle.System;
			userComboBox.Font = new Font("Segoe UI", 15F);
			userComboBox.FormattingEnabled = true;
			userComboBox.Location = new Point(96, 12);
			userComboBox.Name = "userComboBox";
			userComboBox.Size = new Size(271, 36);
			userComboBox.TabIndex = 5;
			userComboBox.SelectedIndexChanged += userComboBox_SelectedIndexChanged;
			// 
			// saveButton
			// 
			saveButton.Font = new Font("Segoe UI", 15F);
			saveButton.Location = new Point(169, 212);
			saveButton.Name = "saveButton";
			saveButton.Size = new Size(118, 38);
			saveButton.TabIndex = 4;
			saveButton.Text = "SAVE";
			saveButton.UseVisualStyleBackColor = true;
			saveButton.Click += saveButton_Click;
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
			passwordTextBox.TabIndex = 2;
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
			nameTextBox.Location = new Point(188, 69);
			nameTextBox.Name = "nameTextBox";
			nameTextBox.PlaceholderText = "Name";
			nameTextBox.Size = new Size(271, 34);
			nameTextBox.TabIndex = 1;
			// 
			// statusCheckBox
			// 
			statusCheckBox.AutoSize = true;
			statusCheckBox.Font = new Font("Segoe UI", 15F);
			statusCheckBox.Location = new Point(188, 163);
			statusCheckBox.Name = "statusCheckBox";
			statusCheckBox.Size = new Size(84, 32);
			statusCheckBox.TabIndex = 3;
			statusCheckBox.Text = "Status";
			statusCheckBox.UseVisualStyleBackColor = true;
			// 
			// UserForm
			// 
			AcceptButton = saveButton;
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(504, 284);
			Controls.Add(statusCheckBox);
			Controls.Add(userComboBox);
			Controls.Add(saveButton);
			Controls.Add(passwordLabel);
			Controls.Add(passwordTextBox);
			Controls.Add(nameLabel);
			Controls.Add(nameTextBox);
			Name = "UserForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "UserForm";
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
	}
}