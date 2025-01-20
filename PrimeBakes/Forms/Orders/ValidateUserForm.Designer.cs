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
		userCodeTextBox = new TextBox();
		brandingLabel = new Label();
		richTextBoxFooter = new RichTextBox();
		SuspendLayout();
		// 
		// passwordTextBox
		// 
		passwordTextBox.Font = new Font("Segoe UI", 15F);
		passwordTextBox.Location = new Point(53, 85);
		passwordTextBox.MaxLength = 100;
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
		// userCodeTextBox
		// 
		userCodeTextBox.CharacterCasing = CharacterCasing.Upper;
		userCodeTextBox.Font = new Font("Segoe UI", 15F);
		userCodeTextBox.Location = new Point(53, 32);
		userCodeTextBox.MaxLength = 100;
		userCodeTextBox.Name = "userCodeTextBox";
		userCodeTextBox.PlaceholderText = "User Code";
		userCodeTextBox.Size = new Size(157, 34);
		userCodeTextBox.TabIndex = 1;
		// 
		// brandingLabel
		// 
		brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		brandingLabel.AutoSize = true;
		brandingLabel.BackColor = Color.White;
		brandingLabel.Location = new Point(192, 204);
		brandingLabel.Name = "brandingLabel";
		brandingLabel.Size = new Size(76, 15);
		brandingLabel.TabIndex = 32;
		brandingLabel.Text = "© AADISOFT";
		// 
		// richTextBoxFooter
		// 
		richTextBoxFooter.Dock = DockStyle.Bottom;
		richTextBoxFooter.Location = new Point(0, 197);
		richTextBoxFooter.Name = "richTextBoxFooter";
		richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
		richTextBoxFooter.Size = new Size(272, 26);
		richTextBoxFooter.TabIndex = 31;
		richTextBoxFooter.Text = "Version 0.0.0.0";
		// 
		// ValidateUserForm
		// 
		AcceptButton = goButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(272, 223);
		Controls.Add(brandingLabel);
		Controls.Add(richTextBoxFooter);
		Controls.Add(userCodeTextBox);
		Controls.Add(goButton);
		Controls.Add(passwordTextBox);
		Name = "ValidateUserForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "ValidateUser";
		Load += ValidateUserForm_Load;
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private TextBox passwordTextBox;
	private Button goButton;
	private TextBox userCodeTextBox;
	private Label brandingLabel;
	private RichTextBox richTextBoxFooter;
}