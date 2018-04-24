using System;

namespace NHibernate.Shards.Util
{
    internal struct Logger
    {
	    private readonly INHibernateLogger log;

        public Logger(System.Type type)
        {
    	    this.log = NHibernateLogger.For(type);
        }

        public void Debug(string message)
        {
            this.log.Debug(message);
        }

        public void Debug(string format, params object[] args)
        {
            this.log.Debug(format, args);
        }

        internal void Warn(string message)
        {
            this.log.Warn(message);
        }

        public void Warn(Exception e, string message)
        {
	        this.log.Warn(e, message);
        }

        public void Error(string message)
        {
            this.log.Error(message);
        }

        public void Error(Exception e, string message)
        {
            this.log.Error(e, message);
        }

        public void Info(string format, params object[] args)
        {
            this.log.Info(format, args);
        }
    }
}
