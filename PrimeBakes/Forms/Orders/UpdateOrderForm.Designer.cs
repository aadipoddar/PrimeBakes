namespace PrimeBakes.Forms.Orders;

partial class UpdateOrderForm
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
		itemsDataGridView = new DataGridView();
		quantityTextBox = new TextBox();
		quantityLabel = new Label();
		saveButton = new Button();
		addButton = new Button();
		printPDFButton = new Button();
		statusCheckBox = new CheckBox();
		categoryLabel = new Label();
		categoryComboBox = new ComboBox();
		brandingLabel = new Label();
		richTextBoxFooter = new RichTextBox();
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
		itemComboBox.Location = new Point(28, 156);
		itemComboBox.Name = "itemComboBox";
		itemComboBox.Size = new Size(271, 36);
		itemComboBox.TabIndex = 2;
		// 
		// itemNameLabel
		// 
		itemNameLabel.AutoSize = true;
		itemNameLabel.Font = new Font("Segoe UI", 15F);
		itemNameLabel.Location = new Point(107, 125);
		itemNameLabel.Name = "itemNameLabel";
		itemNameLabel.Size = new Size(108, 28);
		itemNameLabel.TabIndex = 43;
		itemNameLabel.Text = "Item Name";
		// 
		// itemsDataGridView
		// 
		itemsDataGridView.AllowUserToAddRows = false;
		itemsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		itemsDataGridView.Location = new Point(350, 56);
		itemsDataGridView.Name = "itemsDataGridView";
		itemsDataGridView.ReadOnly = true;
		itemsDataGridView.Size = new Size(747, 382);
		itemsDataGridView.TabIndex = 48;
		itemsDataGridView.CellDoubleClick += itemsDataGridView_CellDoubleClick;
		// 
		// quantityTextBox
		// 
		quantityTextBox.Font = new Font("Segoe UI", 15F);
		quantityTextBox.Location = new Point(187, 198);
		quantityTextBox.Name = "quantityTextBox";
		quantityTextBox.PlaceholderText = "Quantity";
		quantityTextBox.Size = new Size(83, 34);
		quantityTextBox.TabIndex = 3;
		quantityTextBox.Text = "1";
		quantityTextBox.TextAlign = HorizontalAlignment.Right;
		quantityTextBox.KeyPress += quantityTextBox_KeyPress;
		// 
		// quantityLabel
		// 
		quantityLabel.AutoSize = true;
		quantityLabel.Font = new Font("Segoe UI", 15F);
		quantityLabel.Location = new Point(55, 201);
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
		addButton.Location = new Point(97, 253);
		addButton.Name = "addButton";
		addButton.Size = new Size(118, 38);
		addButton.TabIndex = 4;
		addButton.Text = "ADD";
		addButton.UseVisualStyleBackColor = true;
		addButton.Click += addButton_Click;
		// 
		// printPDFButton
		// 
		printPDFButton.Font = new Font("Segoe UI", 15F);
		printPDFButton.Location = new Point(677, 12);
		printPDFButton.Name = "printPDFButton";
		printPDFButton.Size = new Size(118, 38);
		printPDFButton.TabIndex = 53;
		printPDFButton.Text = "Print PDF";
		printPDFButton.UseVisualStyleBackColor = true;
		printPDFButton.Click += printPDFButton_Click;
		// 
		// statusCheckBox
		// 
		statusCheckBox.AutoSize = true;
		statusCheckBox.Font = new Font("Segoe UI", 15F);
		statusCheckBox.Location = new Point(115, 345);
		statusCheckBox.Name = "statusCheckBox";
		statusCheckBox.Size = new Size(84, 32);
		statusCheckBox.TabIndex = 55;
		statusCheckBox.Text = "Status";
		statusCheckBox.UseVisualStyleBackColor = true;
		// 
		// categoryLabel
		// 
		categoryLabel.AutoSize = true;
		categoryLabel.Font = new Font("Segoe UI", 15F);
		categoryLabel.Location = new Point(107, 12);
		categoryLabel.Name = "categoryLabel";
		categoryLabel.Size = new Size(92, 28);
		categoryLabel.TabIndex = 57;
		categoryLabel.Text = "Category";
		// 
		// categoryComboBox
		// 
		categoryComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
		categoryComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
		categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		categoryComboBox.FlatStyle = FlatStyle.System;
		categoryComboBox.Font = new Font("Segoe UI", 15F);
		categoryComboBox.FormattingEnabled = true;
		categoryComboBox.Location = new Point(30, 43);
		categoryComboBox.Name = "categoryComboBox";
		categoryComboBox.Size = new Size(271, 36);
		categoryComboBox.TabIndex = 56;
		categoryComboBox.SelectedIndexChanged += categoryComboBox_SelectedIndexChanged;
		// 
		// brandingLabel
		// 
		brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		brandingLabel.AutoSize = true;
		brandingLabel.BackColor = Color.White;
		brandingLabel.Location = new Point(1033, 461);
		brandingLabel.Name = "brandingLabel";
		brandingLabel.Size = new Size(76, 15);
		brandingLabel.TabIndex = 59;
		brandingLabel.Text = "© AADISOFT";
		// 
		// richTextBoxFooter
		// 
		richTextBoxFooter.Dock = DockStyle.Bottom;
		richTextBoxFooter.Location = new Point(0, 454);
		richTextBoxFooter.Name = "richTextBoxFooter";
		richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
		richTextBoxFooter.Size = new Size(1109, 26);
		richTextBoxFooter.TabIndex = 58;
		richTextBoxFooter.Text = "Version 0.0.0.0";
		// 
		// UpdateOrderForm
		// 
		AcceptButton = addButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(1109, 480);
		Controls.Add(brandingLabel);
		Controls.Add(richTextBoxFooter);
		Controls.Add(categoryLabel);
		Controls.Add(categoryComboBox);
		Controls.Add(statusCheckBox);
		Controls.Add(printPDFButton);
		Controls.Add(addButton);
		Controls.Add(saveButton);
		Controls.Add(quantityLabel);
		Controls.Add(quantityTextBox);
		Controls.Add(itemsDataGridView);
		Controls.Add(itemNameLabel);
		Controls.Add(itemComboBox);
		Name = "UpdateOrderForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Update Order";
		Load += OrderForm_Load;
		((System.ComponentModel.ISupportInitialize)itemsDataGridView).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private ComboBox itemComboBox;
	private Label itemNameLabel;
	private DataGridView itemsDataGridView;
	private TextBox quantityTextBox;
	private Label quantityLabel;
	private Button saveButton;
	private Button addButton;
	private Button printPDFButton;
	private CheckBox statusCheckBox;
	private Label categoryLabel;
	private ComboBox categoryComboBox;
	private Label brandingLabel;
	private RichTextBox richTextBoxFooter;
}