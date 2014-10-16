namespace NHibernate.Shards.Test
{
	using System.Collections.Generic;
	using NHibernate.Mapping.ByCode;
	using NHibernate.Mapping.ByCode.Conformist;
	using NUnit.Framework;

	[TestFixture]
	public class ShardedIntegrationTests : ShardedTestCase
	{
		#region Test SetUp

		protected override void Configure(NHibernate.Cfg.Configuration protoConfig)
		{
			var mapper = new ModelMapper();
			mapper.AddMapping<PersonMap>();
			protoConfig.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
		}

		#endregion

		#region Tests

		[Test]
		public void CanSaveEntityWithComponentList()
		{
			var person = new Person {LegalName = new PersonName {FirstName = "John", LastName = "Doe"}};
			person.Aliases.Add(new PersonName {FirstName = "Johny", LastName = "D"});
			person.Aliases.Add(new PersonName {FirstName = "Don Jon"});

			using (var session = SessionFactory.OpenSession())
			using (session.BeginTransaction())
			{
				session.Save(person);
				session.Flush();
			}

			Assert.That(person.Id, Is.GreaterThan(0));
		}

		#endregion

		#region Domain model and mapping classes

		public class Person
		{
			public int Id { get; set; }
			public PersonName LegalName { get; set; }
			public IList<PersonName> Aliases { get; protected set; }

			public Person()
			{
				this.Aliases = new List<PersonName>();
			}
		}

		public class PersonName
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		public class PersonMap : ClassMapping<Person>
		{
			public PersonMap()
			{
				Id(x => x.Id, m =>
				{
					m.Generator(Generators.Identity);
					m.Column("id");
				});

				Component(x => x.LegalName, c =>
				{
					c.Property(x => x.FirstName, p => p.Column("first_name"));
					c.Property(x => x.LastName, p => p.Column("last_name"));
					c.Lazy(false);
				});

				List(x => x.Aliases, l =>
				{
					l.Table("aliases");
					l.Key(k =>
					{
						k.Column("person_id");
						k.ForeignKey("fk_alias_person");
						k.NotNullable(true);
					});
					l.Index(i => i.Column("item_no"));
				}, e => e.Component(c =>
				{
					c.Property(x => x.FirstName);
					c.Property(x => x.LastName);
					c.Lazy(false);
				}));

				Lazy(false);
				Table("person");
			}
		}

		#endregion
	}
}
