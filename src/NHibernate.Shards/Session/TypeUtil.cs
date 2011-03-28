using System.Collections.Generic;
using NHibernate.Shards.Util;
using NHibernate.Type;

namespace NHibernate.Shards.Session
{
    using Metadata;

    internal static class TypeUtil
    {
        public static IEnumerable<KeyValuePair<IAssociationType, object>> GetAssociations(
            IClassMetadata classMetadata, object entity, EntityMode entityMode)
        {
            var propertyTypes = classMetadata.PropertyTypes;
            var propertyValues = classMetadata.GetPropertyValues(entity, entityMode);
            return GetAssociations(propertyTypes, propertyValues);
            
        }

        public static IEnumerable<KeyValuePair<IAssociationType, object>> GetAssociations(
            IType[] propertyTypes, object[] propertyValues)
        {
            // we assume types and current state are the same length
            Preconditions.CheckState(propertyTypes.Length == propertyValues.Length);

            for (int i = 0; i < propertyTypes.Length; i++)
            {
                if (propertyTypes[i] != null &&
                    propertyValues[i] != null &&
                    propertyTypes[i].IsAssociationType)
                {
                    yield return new KeyValuePair<IAssociationType, object>((IAssociationType)propertyTypes[i], propertyValues[i]);
                }
            }
        }
    }
}
