using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
    class SetBinaryEvent : IQueryEvent
    {
        private enum CtorType
        {
            PositionVal,
            NameVal
        }

        private readonly CtorType ctorType;
        private readonly int position;
        private readonly byte[] val;
        private readonly String name;

        private SetBinaryEvent(CtorType ctorType, int position, byte[] val, String name)
        {
            this.ctorType = ctorType;
            this.position = position;
            this.val = val;
            this.name = name;
        }

        public SetBinaryEvent(int position, byte[] val)
            : this(CtorType.PositionVal, position, val, null)
        { }

        public SetBinaryEvent(String name, byte[] val)
            : this(CtorType.NameVal, -1, val, name)
        { }

        public void OnEvent(IQuery query)
        {
            switch (ctorType)
            {
                case CtorType.PositionVal:
                    query.SetBinary(position, val);
                    break;
                case CtorType.NameVal:
                    query.SetBinary(name, val);
                    break;
                default:
                    throw new ShardedSessionException(
                        "Unknown ctor type in SetBinaryEvent: " + ctorType);
            }
        }
    }
}
