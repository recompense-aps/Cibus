using System;

namespace Cibus
{
	public abstract class CibusData
	{
		public DateTimeOffset DateCreated { get; protected set;}
		public DateTimeOffset DateUpdated { get; protected set;}
		public virtual void OnSavingToDatabase() => DateUpdated = DateTimeOffset.Now;
		public virtual void OnCreating() => DateCreated = DateTimeOffset.Now;
	}
}