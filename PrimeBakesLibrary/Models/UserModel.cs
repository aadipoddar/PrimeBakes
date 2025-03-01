﻿namespace PrimeBakesLibrary.Models;

public class UserModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public string Password { get; set; }
	public int CustomerId { get; set; }
	public int UserCategoryId { get; set; }
	public bool Status { get; set; }

	public string DisplayName => $"{Name} ({Code})";
}