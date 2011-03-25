using System;

namespace NHibernate.Shards.Demo
{
	public class WeatherReport
	{
		public virtual string ReportId { get; set; }

		public virtual string Continent { get; set; }

		public virtual long Latitude { get; set; }

		public virtual long Longitude { get; set; }

		public virtual int Temperature { get; set; }

		public virtual DateTime ReportTime { get; set; }
	}
}