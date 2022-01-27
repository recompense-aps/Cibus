using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cibus
{
	public class CibusDatabaseContext : DbContext
	{
		public DbSet<RecipeData>? Recipes { get; set; }
		public string DbPath { get; }

		public CibusDatabaseContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "cibus.db");
        }

		protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");
	}
}