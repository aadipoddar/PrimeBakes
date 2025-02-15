﻿namespace PrimeBakesLibrary.Models;

public class CustomerModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public bool Status { get; set; }

	public string DisplayName => $"{Name} ({Code})";
}