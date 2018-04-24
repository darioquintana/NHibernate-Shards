using System;

namespace NHibernate.Shards.Util
{
    internal struct Logger
    {
#if NH51_PLUS
	    private readonly INHibernateLogger log;
#else
        private readonly IInternalLogger log;
#endif

        public Logger(System.Type type)
        {
#if NH51_PLUS
    	    this.log = NHibernateLogger.For(type);
#else
            this.log = LoggerProvider.LoggerFor(type);
#endif
        }

        public void Debug(string message)
        {
            this.log.Debug(message);
        }

        public void Debug(string format, params object[] args)
        {
#if NH51_PLUS
            this.log.Debug(format, args);
#else
            this.log.DebugFormat(format, args);
#endif
        }

        internal void Warn(string message)
        {
            this.log.Warn(message);
        }

        public void Warn(Exception e, string message)
        {
#if NH51_PLUS
	        this.log.Warn(e, message);
#else
            this.log.Warn(message, e);
#endif
        }

        public void Error(string message)
        {
#if NH51_PLUS
            this.log.Error(message);
#else
            this.log.Error(message);
#endif
        }

        public void Error(Exception e, string message)
        {
#if NH51_PLUS
            this.log.Error(e, message);
#else
            this.log.Error(message, e);
#endif
        }

        public void Info(string format, params object[] args)
        {
#if NH51_PLUS
            this.log.Info(format, args);
#else
            this.log.InfoFormat(format, args);
#endif
        }
    }
}
