namespace PrimeBakes.Forms;

partial class ValidateUserForm
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
		userIdTextBox = new TextBox();
		SuspendLayout();
		// 
		// passwordTextBox
		// 
		passwordTextBox.Font = new Font("Segoe UI", 15F);
		passwordTextBox.Location = new Point(53, 85);
		passwordTextBox.Name = "passwordTextBox";
		passwordTextBox.PasswordChar = '*';
		passwordTextBox.PlaceholderText = "Password";
		passwordTextBox.Size = new Size(157, 34);
		passwordTextBox.TabIndex = 2;
		// 
		// goButton
		// 
		goButton.Font = new Font("Segoe UI", 15F);
		goButton.Location = new Point(70, 145);
		goButton.Name = "goButton";
		goButton.Size = new Size(118, 38);
		goButton.TabIndex = 3;
		goButton.Text = "GO";
		goButton.UseVisualStyleBackColor = true;
		goButton.Click += goButton_Click;
		// 
		// userIdTextBox
		// 
		userIdTextBox.Font = new Font("Segoe UI", 15F);
		userIdTextBox.Location = new Point(53, 32);
		userIdTextBox.Name = "userIdTextBox";
		userIdTextBox.PlaceholderText = "User Id";
		userIdTextBox.Size = new Size(157, 34);
		userIdTextBox.TabIndex = 1;
		userIdTextBox.KeyPress += userIdTextBox_KeyPress;
		// 
		// ValidateUserForm
		// 
		AcceptButton = goButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(272, 227);
		Controls.Add(userIdTextBox);
		Controls.Add(goButton);
		Controls.Add(passwordTextBox);
		Name = "ValidateUserForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "ValidateUser";
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private TextBox passwordTextBox;
	private Button goButton;
	private TextBox userIdTextBox;
}