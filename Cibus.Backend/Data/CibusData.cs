using System;

namespace Cibus
{
	public abstract class CibusData
	{
		public long Id { get; set; }
		public DateTimeOffset DateCreated { get; protected set;}
		public DateTimeOffset DateUpdated { get; protected set;}
		public virtual void OnSavingToDatabase() => DateUpdated = DateTimeOffset.Now;
		public virtual void OnCreating()
		{
			DateCreated = DateTimeOffset.Now;
		}
	}
}