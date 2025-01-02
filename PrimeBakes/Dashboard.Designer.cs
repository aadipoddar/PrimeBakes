namespace PrimeBakes;

partial class Dashboard
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
		userButton = new Button();
		customerButton = new Button();
		itemButton = new Button();
		orderButton = new Button();
		pastOrdersButton = new Button();
		viewUpdateOrderButton = new Button();
		SuspendLayout();
		// 
		// userButton
		// 
		userButton.Font = new Font("Segoe UI", 15F);
		userButton.Location = new Point(12, 216);
		userButton.Name = "userButton";
		userButton.Size = new Size(253, 62);
		userButton.TabIndex = 4;
		userButton.Text = "Users";
		userButton.UseVisualStyleBackColor = true;
		userButton.Click += userButton_Click;
		// 
		// customerButton
		// 
		customerButton.Font = new Font("Segoe UI", 15F);
		customerButton.Location = new Point(12, 284);
		customerButton.Name = "customerButton";
		customerButton.Size = new Size(253, 62);
		customerButton.TabIndex = 5;
		customerButton.Text = "Customers";
		customerButton.UseVisualStyleBackColor = true;
		customerButton.Click += customerButton_Click;
		// 
		// itemButton
		// 
		itemButton.Font = new Font("Segoe UI", 15F);
		itemButton.Location = new Point(12, 352);
		itemButton.Name = "itemButton";
		itemButton.Size = new Size(253, 62);
		itemButton.TabIndex = 6;
		itemButton.Text = "Items";
		itemButton.UseVisualStyleBackColor = true;
		itemButton.Click += itemButton_Click;
		// 
		// orderButton
		// 
		orderButton.Font = new Font("Segoe UI", 15F);
		orderButton.Location = new Point(12, 12);
		orderButton.Name = "orderButton";
		orderButton.Size = new Size(253, 62);
		orderButton.TabIndex = 1;
		orderButton.Text = "Orders";
		orderButton.UseVisualStyleBackColor = true;
		orderButton.Click += orderButton_Click;
		// 
		// pastOrdersButton
		// 
		pastOrdersButton.Font = new Font("Segoe UI", 15F);
		pastOrdersButton.Location = new Point(12, 80);
		pastOrdersButton.Name = "pastOrdersButton";
		pastOrdersButton.Size = new Size(253, 62);
		pastOrdersButton.TabIndex = 2;
		pastOrdersButton.Text = "Past Orders";
		pastOrdersButton.UseVisualStyleBackColor = true;
		pastOrdersButton.Click += pastOrdersButton_Click;
		// 
		// viewUpdateOrderButton
		// 
		viewUpdateOrderButton.Font = new Font("Segoe UI", 15F);
		viewUpdateOrderButton.Location = new Point(12, 148);
		viewUpdateOrderButton.Name = "viewUpdateOrderButton";
		viewUpdateOrderButton.Size = new Size(253, 62);
		viewUpdateOrderButton.TabIndex = 3;
		viewUpdateOrderButton.Text = "View / Update Order";
		viewUpdateOrderButton.UseVisualStyleBackColor = true;
		viewUpdateOrderButton.Click += viewUpdateOrderButton_Click;
		// 
		// Dashboard
		// 
		AcceptButton = orderButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(277, 433);
		Controls.Add(viewUpdateOrderButton);
		Controls.Add(pastOrdersButton);
		Controls.Add(orderButton);
		Controls.Add(itemButton);
		Controls.Add(customerButton);
		Controls.Add(userButton);
		Name = "Dashboard";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Dashboard";
		Load += Dashboard_Load;
		ResumeLayout(false);
	}

	#endregion

	private Button userButton;
	private Button customerButton;
	private Button itemButton;
	private Button orderButton;
	private Button pastOrdersButton;
	private Button viewUpdateOrderButton;
}