﻿namespace PrimeBakes.Forms;

partial class OrderIdForm
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
		orderIdLabel = new Label();
		orderIdTextBox = new TextBox();
		goButton = new Button();
		SuspendLayout();
		// 
		// orderIdLabel
		// 
		orderIdLabel.AutoSize = true;
		orderIdLabel.Font = new Font("Segoe UI", 15F);
		orderIdLabel.Location = new Point(37, 35);
		orderIdLabel.Name = "orderIdLabel";
		orderIdLabel.Size = new Size(85, 28);
		orderIdLabel.TabIndex = 0;
		orderIdLabel.Text = "Order Id";
		// 
		// orderIdTextBox
		// 
		orderIdTextBox.Font = new Font("Segoe UI", 15F);
		orderIdTextBox.Location = new Point(139, 35);
		orderIdTextBox.Name = "orderIdTextBox";
		orderIdTextBox.Size = new Size(100, 34);
		orderIdTextBox.TabIndex = 1;
		orderIdTextBox.KeyPress += orderIdTextBox_KeyPress;
		// 
		// goButton
		// 
		goButton.Font = new Font("Segoe UI", 15F);
		goButton.Location = new Point(95, 107);
		goButton.Name = "goButton";
		goButton.Size = new Size(118, 43);
		goButton.TabIndex = 2;
		goButton.Text = "GO";
		goButton.UseVisualStyleBackColor = true;
		goButton.Click += goButton_Click;
		// 
		// OrderIdForm
		// 
		AcceptButton = goButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(318, 182);
		Controls.Add(goButton);
		Controls.Add(orderIdTextBox);
		Controls.Add(orderIdLabel);
		Name = "OrderIdForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "OrderIdForm";
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private Label orderIdLabel;
	private TextBox orderIdTextBox;
	private Button goButton;
}