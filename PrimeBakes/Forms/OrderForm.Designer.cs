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
		itemsDataGridView = new DataGridView();
		Item = new DataGridViewTextBoxColumn();
		Code = new DataGridViewTextBoxColumn();
		Quantity = new DataGridViewTextBoxColumn();
		quantityTextBox = new TextBox();
		quantityLabel = new Label();
		saveButton = new Button();
		addButton = new Button();
		((System.ComponentModel.ISupportInitialize)itemsDataGridView).BeginInit();
		SuspendLayout();
		// 
		// itemComboBox
		// 
		itemComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
		itemComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		itemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		itemComboBox.FlatStyle = FlatStyle.System;
		itemComboBox.Font = new Font("Segoe UI", 15F);
		itemComboBox.FormattingEnabled = true;
		itemComboBox.Location = new Point(28, 151);
		itemComboBox.Name = "itemComboBox";
		itemComboBox.Size = new Size(271, 36);
		itemComboBox.TabIndex = 42;
		// 
		// itemNameLabel
		// 
		itemNameLabel.AutoSize = true;
		itemNameLabel.Font = new Font("Segoe UI", 15F);
		itemNameLabel.Location = new Point(107, 109);
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
		customerComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
		customerComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		customerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		customerComboBox.FlatStyle = FlatStyle.System;
		customerComboBox.Font = new Font("Segoe UI", 15F);
		customerComboBox.FormattingEnabled = true;
		customerComboBox.Location = new Point(235, 40);
		customerComboBox.Name = "customerComboBox";
		customerComboBox.Size = new Size(271, 36);
		customerComboBox.TabIndex = 44;
		// 
		// itemsDataGridView
		// 
		itemsDataGridView.AllowUserToAddRows = false;
		itemsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		itemsDataGridView.Columns.AddRange(new DataGridViewColumn[] { Item, Code, Quantity });
		itemsDataGridView.Location = new Point(367, 109);
		itemsDataGridView.Name = "itemsDataGridView";
		itemsDataGridView.ReadOnly = true;
		itemsDataGridView.Size = new Size(421, 329);
		itemsDataGridView.TabIndex = 48;
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
		quantityTextBox.Location = new Point(187, 246);
		quantityTextBox.Name = "quantityTextBox";
		quantityTextBox.PlaceholderText = "Quantity";
		quantityTextBox.Size = new Size(83, 34);
		quantityTextBox.TabIndex = 49;
		quantityTextBox.Text = "1";
		quantityTextBox.TextAlign = HorizontalAlignment.Right;
		quantityTextBox.KeyPress += quantityTextBox_KeyPress;
		// 
		// quantityLabel
		// 
		quantityLabel.AutoSize = true;
		quantityLabel.Font = new Font("Segoe UI", 15F);
		quantityLabel.Location = new Point(55, 249);
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
		saveButton.Click += saveButton_Click;
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
		addButton.Click += addButton_Click;
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
		Controls.Add(itemsDataGridView);
		Controls.Add(customerNameLabel);
		Controls.Add(customerComboBox);
		Controls.Add(itemNameLabel);
		Controls.Add(itemComboBox);
		Name = "OrderForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "OrderForm";
		Load += OrderForm_Load;
		((System.ComponentModel.ISupportInitialize)itemsDataGridView).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private ComboBox itemComboBox;
	private Label itemNameLabel;
	private Label customerNameLabel;
	private ComboBox customerComboBox;
	private DataGridView itemsDataGridView;
	private DataGridViewTextBoxColumn Item;
	private DataGridViewTextBoxColumn Code;
	private DataGridViewTextBoxColumn Quantity;
	private TextBox quantityTextBox;
	private Label quantityLabel;
	private Button saveButton;
	private Button addButton;
}