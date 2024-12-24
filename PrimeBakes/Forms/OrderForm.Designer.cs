namespace PrimeBakes.Forms;

partial class OrderForm
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
		itemNameLabel = new Label();
		customerNameLabel = new Label();
		customerComboBox = new ComboBox();
		customerCodeTextBox = new TextBox();
		itemCodeTextBox = new TextBox();
		dataGridView1 = new DataGridView();
		Item = new DataGridViewTextBoxColumn();
		Code = new DataGridViewTextBoxColumn();
		Quantity = new DataGridViewTextBoxColumn();
		quantityTextBox = new TextBox();
		quantityLabel = new Label();
		saveButton = new Button();
		addButton = new Button();
		((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
		SuspendLayout();
		// 
		// itemComboBox
		// 
		itemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		itemComboBox.FlatStyle = FlatStyle.System;
		itemComboBox.Font = new Font("Segoe UI", 15F);
		itemComboBox.FormattingEnabled = true;
		itemComboBox.Location = new Point(28, 139);
		itemComboBox.Name = "itemComboBox";
		itemComboBox.Size = new Size(271, 36);
		itemComboBox.TabIndex = 42;
		// 
		// itemNameLabel
		// 
		itemNameLabel.AutoSize = true;
		itemNameLabel.Font = new Font("Segoe UI", 15F);
		itemNameLabel.Location = new Point(107, 97);
		itemNameLabel.Name = "itemNameLabel";
		itemNameLabel.Size = new Size(108, 28);
		itemNameLabel.TabIndex = 43;
		itemNameLabel.Text = "Item Name";
		// 
		// customerNameLabel
		// 
		customerNameLabel.AutoSize = true;
		customerNameLabel.Font = new Font("Segoe UI", 15F);
		customerNameLabel.Location = new Point(324, 9);
		customerNameLabel.Name = "customerNameLabel";
		customerNameLabel.Size = new Size(96, 28);
		customerNameLabel.TabIndex = 45;
		customerNameLabel.Text = "Customer";
		// 
		// customerComboBox
		// 
		customerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		customerComboBox.FlatStyle = FlatStyle.System;
		customerComboBox.Font = new Font("Segoe UI", 15F);
		customerComboBox.FormattingEnabled = true;
		customerComboBox.Location = new Point(57, 40);
		customerComboBox.Name = "customerComboBox";
		customerComboBox.Size = new Size(271, 36);
		customerComboBox.TabIndex = 44;
		// 
		// customerCodeTextBox
		// 
		customerCodeTextBox.Font = new Font("Segoe UI", 15F);
		customerCodeTextBox.Location = new Point(442, 42);
		customerCodeTextBox.Name = "customerCodeTextBox";
		customerCodeTextBox.PasswordChar = '*';
		customerCodeTextBox.PlaceholderText = "Code";
		customerCodeTextBox.Size = new Size(271, 34);
		customerCodeTextBox.TabIndex = 46;
		// 
		// itemCodeTextBox
		// 
		itemCodeTextBox.Font = new Font("Segoe UI", 15F);
		itemCodeTextBox.Location = new Point(28, 190);
		itemCodeTextBox.Name = "itemCodeTextBox";
		itemCodeTextBox.PasswordChar = '*';
		itemCodeTextBox.PlaceholderText = "Code";
		itemCodeTextBox.Size = new Size(271, 34);
		itemCodeTextBox.TabIndex = 47;
		// 
		// dataGridView1
		// 
		dataGridView1.AllowUserToAddRows = false;
		dataGridView1.AllowUserToDeleteRows = false;
		dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Item, Code, Quantity });
		dataGridView1.Location = new Point(367, 131);
		dataGridView1.Name = "dataGridView1";
		dataGridView1.ReadOnly = true;
		dataGridView1.Size = new Size(421, 307);
		dataGridView1.TabIndex = 48;
		// 
		// Item
		// 
		Item.HeaderText = "Item Name";
		Item.Name = "Item";
		Item.ReadOnly = true;
		// 
		// Code
		// 
		Code.HeaderText = "Item Code";
		Code.Name = "Code";
		Code.ReadOnly = true;
		// 
		// Quantity
		// 
		Quantity.HeaderText = "Quantity";
		Quantity.Name = "Quantity";
		Quantity.ReadOnly = true;
		// 
		// quantityTextBox
		// 
		quantityTextBox.Font = new Font("Segoe UI", 15F);
		quantityTextBox.Location = new Point(136, 249);
		quantityTextBox.Name = "quantityTextBox";
		quantityTextBox.PasswordChar = '*';
		quantityTextBox.PlaceholderText = "Quantity";
		quantityTextBox.RightToLeft = RightToLeft.Yes;
		quantityTextBox.Size = new Size(163, 34);
		quantityTextBox.TabIndex = 49;
		// 
		// quantityLabel
		// 
		quantityLabel.AutoSize = true;
		quantityLabel.Font = new Font("Segoe UI", 15F);
		quantityLabel.Location = new Point(28, 252);
		quantityLabel.Name = "quantityLabel";
		quantityLabel.Size = new Size(88, 28);
		quantityLabel.TabIndex = 50;
		quantityLabel.Text = "Quantity";
		// 
		// saveButton
		// 
		saveButton.Font = new Font("Segoe UI", 15F);
		saveButton.Location = new Point(97, 400);
		saveButton.Name = "saveButton";
		saveButton.Size = new Size(118, 38);
		saveButton.TabIndex = 51;
		saveButton.Text = "SAVE";
		saveButton.UseVisualStyleBackColor = true;
		// 
		// addButton
		// 
		addButton.Font = new Font("Segoe UI", 15F);
		addButton.Location = new Point(97, 329);
		addButton.Name = "addButton";
		addButton.Size = new Size(118, 38);
		addButton.TabIndex = 52;
		addButton.Text = "ADD";
		addButton.UseVisualStyleBackColor = true;
		// 
		// OrderForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(800, 450);
		Controls.Add(addButton);
		Controls.Add(saveButton);
		Controls.Add(quantityLabel);
		Controls.Add(quantityTextBox);
		Controls.Add(dataGridView1);
		Controls.Add(itemCodeTextBox);
		Controls.Add(customerCodeTextBox);
		Controls.Add(customerNameLabel);
		Controls.Add(customerComboBox);
		Controls.Add(itemNameLabel);
		Controls.Add(itemComboBox);
		Name = "OrderForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "OrderForm";
		((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private ComboBox itemComboBox;
	private Label itemNameLabel;
	private Label customerNameLabel;
	private ComboBox customerComboBox;
	private TextBox customerCodeTextBox;
	private TextBox itemCodeTextBox;
	private DataGridView dataGridView1;
	private DataGridViewTextBoxColumn Item;
	private DataGridViewTextBoxColumn Code;
	private DataGridViewTextBoxColumn Quantity;
	private TextBox quantityTextBox;
	private Label quantityLabel;
	private Button saveButton;
	private Button addButton;
}