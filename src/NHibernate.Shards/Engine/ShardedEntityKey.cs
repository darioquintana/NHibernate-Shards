namespace NHibernate.Shards.Engine
{
    public struct ShardedEntityKey
    {
        private readonly string entityName;
        private readonly object id;

        /// <summary>
        /// Creates new <see cref="ShardedEntityKey"/> for given entity name and identifier.
        /// </summary>
        /// <param name="entityName">The entity name.</param>
        /// <param name="id">The entity identifier.</param>
        public ShardedEntityKey(string entityName, object id)
        {
            this.entityName = entityName;
            this.id = id;
        }

        /// <summary>
        /// Creates new <see cref="ShardedEntityKey"/> for given entity class and identifier.
        /// </summary>
        /// <param name="entityClass">The entity class.</param>
        /// <param name="id">The entity identifier.</param>
        /// <remarks>The <see cref="EntityName"/> is set to <see cref="System.Type.FullName"/>
        /// of the entity class, in accordance with NHibernate conventions.</remarks>
        public ShardedEntityKey(System.Type entityClass, object id)
            : this(entityClass.FullName, id)
        { }

        /// <summary>
        /// The entity name.
        /// </summary>
        public string EntityName
        {
            get { return entityName; }
        }

        /// <summary>
        /// The entity identifier.
        /// </summary>
        public object Id
        {
            get { return id; }
        }

        /// <summary>
        /// Indicates whether <see cref="Id"/> is <c>null</c>.
        /// </summary>
        public bool IsNull
        {
            get { return this.id == null; }
        }
    }
}
