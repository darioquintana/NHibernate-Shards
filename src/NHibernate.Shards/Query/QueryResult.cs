using System;
using System.Collections;
using System.Collections.Generic;

namespace NHibernate.Shards.Query
{
    public class QueryResult
    {
        private readonly Dictionary<IShard, IList> resultMap = new Dictionary<IShard, IList>();

        private readonly IList<object> entityList = (IList<object>)new ArrayList();//Lists.newArrayList();

        public Dictionary<IShard, IList> GetResultMap()
        {
            return resultMap; //TODO: see how to do resultMap AsReadonly()
        }

        public void Add(IShard shard, List<Object> list)
        {
            resultMap.Add(shard, list);
            entityList.Add(list); //.addAll
        }

        public void Add(QueryResult result)
        {
            //resultMap.Add(result.GetResultMap()); //TODO: see what happens here
            entityList.Add(result.GetEntityList());
        }

        public List<Object> GetEntityList()
        {
            return (List<object>)entityList; //TODO: the cast doesn't goes there
        }

    }
}
