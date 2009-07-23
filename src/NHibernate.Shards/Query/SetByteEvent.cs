using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
    public class SetByteEvent : IQueryEvent
    {
        private /*static*/ enum CtorType
        {
            PositionVal,
            NameVal
        }

        private readonly CtorType ctorType;
        private readonly int position;
        private readonly byte val;
        private readonly String name;

        private SetByteEvent(CtorType ctorType, int position, byte val, String name)
        {
            this.ctorType = ctorType;
            this.position = position;
            this.val = val;
            this.name = name;
        }

        public SetByteEvent(int position, byte val)
            : this(CtorType.PositionVal, position, val, null)
        { }

        public SetByteEvent(String name, byte val)
            : this(CtorType.NameVal, -1, val, name)
        { }

        public void OnEvent(IQuery query)
        {
            switch (ctorType)
            {
                case CtorType.PositionVal:
                    query.SetByte(position, val);
                    break;
                case CtorType.NameVal:
                    query.SetByte(name, val);
                    break;
                default:
                    throw new ShardedSessionException(
                        "Unknown ctor type in SetByteEvent: " + ctorType);
            }
        }
    }
}
