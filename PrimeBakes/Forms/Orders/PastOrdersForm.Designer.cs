namespace PrimeBakes.Forms.Orders;

partial class PastOrdersForm
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
		fromDateTimePicker = new DateTimePicker();
		toDateTimePicker = new DateTimePicker();
		orderDataGridView = new DataGridView();
		refreshButton = new Button();
		printButton = new Button();
		showClearedCheckBox = new CheckBox();
		brandingLabel = new Label();
		richTextBoxFooter = new RichTextBox();
		((System.ComponentModel.ISupportInitialize)orderDataGridView).BeginInit();
		SuspendLayout();
		// 
		// fromDateTimePicker
		// 
		fromDateTimePicker.Font = new Font("Segoe UI", 15F);
		fromDateTimePicker.Format = DateTimePickerFormat.Short;
		fromDateTimePicker.Location = new Point(12, 12);
		fromDateTimePicker.Name = "fromDateTimePicker";
		fromDateTimePicker.Size = new Size(143, 34);
		fromDateTimePicker.TabIndex = 0;
		fromDateTimePicker.ValueChanged += dateTimePicker_ValueChanged;
		// 
		// toDateTimePicker
		// 
		toDateTimePicker.Font = new Font("Segoe UI", 15F);
		toDateTimePicker.Format = DateTimePickerFormat.Short;
		toDateTimePicker.Location = new Point(185, 12);
		toDateTimePicker.Name = "toDateTimePicker";
		toDateTimePicker.Size = new Size(143, 34);
		toDateTimePicker.TabIndex = 1;
		toDateTimePicker.ValueChanged += dateTimePicker_ValueChanged;
		// 
		// orderDataGridView
		// 
		orderDataGridView.AllowUserToAddRows = false;
		orderDataGridView.AllowUserToDeleteRows = false;
		orderDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		orderDataGridView.Location = new Point(12, 71);
		orderDataGridView.Name = "orderDataGridView";
		orderDataGridView.ReadOnly = true;
		orderDataGridView.Size = new Size(776, 367);
		orderDataGridView.TabIndex = 2;
		orderDataGridView.CellDoubleClick += orderDataGridView_CellDoubleClick;
		// 
		// refreshButton
		// 
		refreshButton.Location = new Point(521, 12);
		refreshButton.Name = "refreshButton";
		refreshButton.Size = new Size(115, 36);
		refreshButton.TabIndex = 3;
		refreshButton.Text = "Refresh";
		refreshButton.UseVisualStyleBackColor = true;
		refreshButton.Click += refreshButton_Click;
		// 
		// printButton
		// 
		printButton.Location = new Point(673, 12);
		printButton.Name = "printButton";
		printButton.Size = new Size(115, 36);
		printButton.TabIndex = 4;
		printButton.Text = "Print";
		printButton.UseVisualStyleBackColor = true;
		printButton.Click += printButton_Click;
		// 
		// showClearedCheckBox
		// 
		showClearedCheckBox.AutoSize = true;
		showClearedCheckBox.Font = new Font("Segoe UI", 15F);
		showClearedCheckBox.Location = new Point(355, 16);
		showClearedCheckBox.Name = "showClearedCheckBox";
		showClearedCheckBox.Size = new Size(150, 32);
		showClearedCheckBox.TabIndex = 56;
		showClearedCheckBox.Text = "Show Cleared";
		showClearedCheckBox.UseVisualStyleBackColor = true;
		showClearedCheckBox.CheckedChanged += showClearedCheckBox_CheckedChanged;
		// 
		// brandingLabel
		// 
		brandingLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		brandingLabel.AutoSize = true;
		brandingLabel.BackColor = Color.White;
		brandingLabel.Location = new Point(724, 463);
		brandingLabel.Name = "brandingLabel";
		brandingLabel.Size = new Size(76, 15);
		brandingLabel.TabIndex = 58;
		brandingLabel.Text = "© AADISOFT";
		// 
		// richTextBoxFooter
		// 
		richTextBoxFooter.Dock = DockStyle.Bottom;
		richTextBoxFooter.Location = new Point(0, 456);
		richTextBoxFooter.Name = "richTextBoxFooter";
		richTextBoxFooter.ScrollBars = RichTextBoxScrollBars.Horizontal;
		richTextBoxFooter.Size = new Size(800, 26);
		richTextBoxFooter.TabIndex = 57;
		richTextBoxFooter.Text = "Version 0.0.0.0";
		// 
		// PastOrdersForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(800, 482);
		Controls.Add(brandingLabel);
		Controls.Add(richTextBoxFooter);
		Controls.Add(showClearedCheckBox);
		Controls.Add(printButton);
		Controls.Add(refreshButton);
		Controls.Add(orderDataGridView);
		Controls.Add(toDateTimePicker);
		Controls.Add(fromDateTimePicker);
		Name = "PastOrdersForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "PastOrders";
		Load += PastOrdersForm_Load;
		((System.ComponentModel.ISupportInitialize)orderDataGridView).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private DateTimePicker fromDateTimePicker;
	private DateTimePicker toDateTimePicker;
	private DataGridView orderDataGridView;
	private Button refreshButton;
	private Button printButton;
	private CheckBox showClearedCheckBox;
	private Label brandingLabel;
	private RichTextBox richTextBoxFooter;
}