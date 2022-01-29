using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Cibus
{
	public static class DAL
	{
		public static void Do(Action<DALContext> action)
		{
			using (var db = new CibusDatabaseContext())
			{
				try
				{
					action(new DALContext(db));
				}
				catch (Exception e)
				{
					throw new DALException(e);
				}

				db.SaveChanges();
			}
		}

		
		public static async Task DoAsync(Func<DALContext, Task> action)
		{
			using (var db = new CibusDatabaseContext())
			{
				try
				{
					await action(new DALContext(db));
				}
				catch (Exception e)
				{
					throw new DALException(e);
				}

				db.SaveChanges();
			}
		}

		public static T Do<T>(Func<DALContext,T> func)
		{
			using (var db = new CibusDatabaseContext())
			{
				try
				{
					T results = func(new DALContext(db));
					db.SaveChanges();
					return results;
				}
				catch (Exception e)
				{
					throw new DALException(e);
				}
			}
		}

		public static async Task<T> DoAsync<T>(Func<DALContext,Task<T>> func)
		{
			using (var db = new CibusDatabaseContext())
			{
				try
				{
					T results = await func(new DALContext(db));
					db.SaveChanges();
					return results;
				}
				catch (Exception e)
				{
					throw new DALException(e);
				}	
			}
		}
	}

	public class DALException : Exception
	{
		public Exception ExecutingException { get; }
		public DALException(Exception executingException) : base($"Unable to complete DAL action. No changes saved - {executingException.Message}")
		{
			ExecutingException = executingException;
		}
	}

	public class DALContext
	{
		private CibusDatabaseContext context { get; }
		public DALContext(CibusDatabaseContext dbContext)
		{
			context = dbContext;
		}

		public T OnSavingToDatabase<T>(T data) where T:CibusData
		{
			data.OnSavingToDatabase();
			return data;
		}

		public DbSet<RecipeData> GetAllRecipeData()
		{
			return context.Recipes;
		}

		public async Task<RecipeData> GetOrCreateRecipe(Guid? recipeGuid = null)
		{
			var recipe = context?.Recipes?.SingleOrDefault(r => r.Guid == recipeGuid);
			if (recipe == null)
			{
				recipe = new RecipeData();
				await context.AddAsync(recipe);
			}
			return recipe;
		}
	}
}