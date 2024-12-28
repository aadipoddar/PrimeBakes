namespace PrimeBakes.Forms;

partial class ValidateUser
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
		passwordTextBox = new TextBox();
		goButton = new Button();
		userComboBox = new ComboBox();
		SuspendLayout();
		// 
		// passwordTextBox
		// 
		passwordTextBox.Font = new Font("Segoe UI", 15F);
		passwordTextBox.Location = new Point(53, 85);
		passwordTextBox.Name = "passwordTextBox";
		passwordTextBox.PasswordChar = '*';
		passwordTextBox.PlaceholderText = "Password";
		passwordTextBox.Size = new Size(271, 34);
		passwordTextBox.TabIndex = 2;
		// 
		// goButton
		// 
		goButton.Font = new Font("Segoe UI", 15F);
		goButton.Location = new Point(120, 161);
		goButton.Name = "goButton";
		goButton.Size = new Size(118, 38);
		goButton.TabIndex = 3;
		goButton.Text = "GO";
		goButton.UseVisualStyleBackColor = true;
		goButton.Click += goButton_Click;
		// 
		// userComboBox
		// 
		userComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
		userComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		userComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		userComboBox.FlatStyle = FlatStyle.System;
		userComboBox.Font = new Font("Segoe UI", 15F);
		userComboBox.FormattingEnabled = true;
		userComboBox.Location = new Point(53, 28);
		userComboBox.Name = "userComboBox";
		userComboBox.Size = new Size(271, 36);
		userComboBox.TabIndex = 1;
		// 
		// ValidateUser
		// 
		AcceptButton = goButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(397, 242);
		Controls.Add(userComboBox);
		Controls.Add(goButton);
		Controls.Add(passwordTextBox);
		Name = "ValidateUser";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "ValidateUser";
		Load += ValidateUser_Load;
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private TextBox passwordTextBox;
	private Button goButton;
	private ComboBox userComboBox;
}