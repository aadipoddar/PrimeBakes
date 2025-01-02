namespace PrimeBakes.Forms;

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
		((System.ComponentModel.ISupportInitialize)orderDataGridView).BeginInit();
		SuspendLayout();
		// 
		// fromDateTimePicker
		// 
		fromDateTimePicker.Font = new Font("Segoe UI", 15F);
		fromDateTimePicker.Format = DateTimePickerFormat.Short;
		fromDateTimePicker.Location = new Point(133, 12);
		fromDateTimePicker.Name = "fromDateTimePicker";
		fromDateTimePicker.Size = new Size(143, 34);
		fromDateTimePicker.TabIndex = 0;
		fromDateTimePicker.ValueChanged += dateTimePicker_ValueChanged;
		// 
		// toDateTimePicker
		// 
		toDateTimePicker.Font = new Font("Segoe UI", 15F);
		toDateTimePicker.Format = DateTimePickerFormat.Short;
		toDateTimePicker.Location = new Point(508, 12);
		toDateTimePicker.Name = "toDateTimePicker";
		toDateTimePicker.Size = new Size(143, 34);
		toDateTimePicker.TabIndex = 1;
		toDateTimePicker.ValueChanged += dateTimePicker_ValueChanged;
		// 
		// orderDataGridView
		// 
		orderDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		orderDataGridView.Location = new Point(12, 110);
		orderDataGridView.Name = "orderDataGridView";
		orderDataGridView.Size = new Size(776, 328);
		orderDataGridView.TabIndex = 2;
		orderDataGridView.CellDoubleClick += orderDataGridView_CellDoubleClick;
		// 
		// PastOrdersForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(800, 450);
		Controls.Add(orderDataGridView);
		Controls.Add(toDateTimePicker);
		Controls.Add(fromDateTimePicker);
		Name = "PastOrdersForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "PastOrders";
		Load += PastOrdersForm_Load;
		((System.ComponentModel.ISupportInitialize)orderDataGridView).EndInit();
		ResumeLayout(false);
	}

	#endregion

	private DateTimePicker fromDateTimePicker;
	private DateTimePicker toDateTimePicker;
	private DataGridView orderDataGridView;
}