using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
    class SetBooleanEvent : IQueryEvent
    {
        private enum CtorType
        {
            PositionVal,
            NameVal
        }

        private readonly CtorType ctorType;
        private readonly int position;
        private readonly bool val;
        private readonly String name;

        private SetBooleanEvent(CtorType ctorType, int position, bool val, String name)
        {
            this.ctorType = ctorType;
            this.position = position;
            this.val = val;
            this.name = name;
        }

        public SetBooleanEvent(int position, bool val)
            : this(CtorType.PositionVal, position, val, null)
        {

        }

        public SetBooleanEvent(String name, bool val)
            : this(CtorType.NameVal, -1, val, name)
        {

        }

        public void OnEvent(IQuery query)
        {
            switch (ctorType)
            {
                case CtorType.PositionVal:
                    query.SetBoolean(position, val);
                    break;
                case CtorType.NameVal:
                    query.SetBoolean(name, val);
                    break;
                default:
                    throw new ShardedSessionException(
                        "Unknown ctor type in SetBooleanEvent: " + ctorType);
            }
        }
    }
}
