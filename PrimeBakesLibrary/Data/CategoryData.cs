﻿namespace PrimeBakesLibrary.Data;

public static class CategoryData
{
	public static async Task InsertCategory(CategoryModel category) =>
		await SqlDataAccess.SaveData("Insert_Category", new
		{
			category.Id,
			category.Code,
			category.Name,
			category.Status
		});

	public static async Task UpdateCategory(CategoryModel category) =>
		await SqlDataAccess.SaveData("Update_Category", new
		{
			category.Id,
			category.Code,
			category.Name,
			category.Status
		});
}