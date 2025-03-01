﻿namespace PrimeBakesLibrary.Models;

public class ItemModel
{
	public int Id { get; set; }
	public int ItemCategoryId { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public int UserCategoryId { get; set; }
	public bool Status { get; set; }

	public string DisplayName => $"{Name} ({Code})";
}