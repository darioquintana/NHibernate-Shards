using System;

namespace NHibernate.Shards.Demo
{
	public class WeatherReport
	{
		public long ReportId { get; set; }

		public string Continent { get; set; }

		public long Latitude { get; set; }

		public long Longitude { get; set; }

		public int Temperature { get; set; }

		public DateTime ReportTime { get; set; }
	}
}