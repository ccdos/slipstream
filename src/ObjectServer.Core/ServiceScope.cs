﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Diagnostics;

using ObjectServer.Data;

namespace ObjectServer
{
    /// <summary>
    /// 但凡是需要 RPC 的方法都需要用此 scope 包裹
    /// </summary>
    internal sealed class ServiceScope : IServiceScope
    {
        private bool ownDb;
        /// <summary>
        /// 安全的创建 Context，会检查 session 等
        /// </summary>
        /// <param name="sessionId"></param>
        public ServiceScope(string sessionId)
        {
            var sessStore = Environment.SessionStore;
            var session = sessStore.GetSession(sessionId);
            if (session == null || !session.IsActive)
            {
                throw new UnauthorizedAccessException("Not logged!");
            }

            LoggerProvider.EnvironmentLogger.Debug(() =>
                string.Format("ContextScope is opening for sessionId: [{0}]", sessionId));

            this.Session = session;
            this.SessionStore.Pulse(session.Id);

            this.ownDb = true;
            this.DBProfile = Environment.DBProfiles.GetDBProfile(session.Database);
            this.DBContext.Open();
        }

        /// <summary>
        /// 直接建立  context，忽略 session 、登录等
        /// </summary>
        /// <param name="dbName"></param>
        public ServiceScope(string dbName, string login)
        {
            LoggerProvider.EnvironmentLogger.Debug(() =>
                string.Format("ContextScope is opening for database: [{0}]", dbName));

            this.Session = new Session(dbName);
            this.SessionStore.PutSession(this.Session);
            this.ownDb = true;
            this.DBProfile = Environment.DBProfiles.GetDBProfile(this.Session.Database);
            this.DBContext.Open();
        }

        /// <summary>
        /// 构造一个使用 'system' 用户登录的 ServiceScope
        /// </summary>
        /// <param name="db"></param>
        public ServiceScope(IDBProfile db)
        {
            Debug.Assert(db != null);
            Debug.Assert(db.DBContext != null);

            LoggerProvider.EnvironmentLogger.Debug(() =>
                string.Format("ContextScope is opening for DatabaseContext"));

            this.Session = new Session(db.DBContext.DatabaseName);
            this.SessionStore.PutSession(this.Session);
            this.ownDb = false;
            this.DBProfile = db;
            this.DBContext.Open();
        }

        public IResource GetResource(string resName)
        {
            if (string.IsNullOrEmpty(resName))
            {
                throw new ArgumentNullException("resName");
            }

            return this.DBProfile.GetResource(resName);
        }

        public IDBContext DBContext
        {
            get
            {
                Debug.Assert(this.DBProfile != null);
                return this.DBProfile.DBContext;
            }
        }

        private IDBProfile DBProfile { get; set; }

        public Session Session { get; private set; }


        #region IDisposable 成员

        public void Dispose()
        {
            if (this.ownDb)
            {
                this.DBContext.Close();
            }

            LoggerProvider.EnvironmentLogger.Debug(() => "ScopeContext closed");
        }

        #endregion

        #region IEquatable<IContext> 成员

        public bool Equals(IServiceScope other)
        {
            return this.Session.Id == other.Session.Id;
        }

        #endregion

        public override int GetHashCode()
        {
            return this.Session.Id.GetHashCode();
        }

        private SessionStore SessionStore
        {
            get { return Environment.SessionStore; }
        }
    }
}
