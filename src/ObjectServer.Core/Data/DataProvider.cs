﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Data
{
    //重构此类
    //此类最终会被去掉
    public static class DataProvider
    {
        private static object dataProviderLock = new object();
        private static IDataProvider concreteDataProvider;

        private static readonly Dictionary<string, Type>
            dbTypeMapping = new Dictionary<string, Type>()
        {
            { "postgres",   typeof(Postgresql.PgDataProvider) },
            { "mssql",      typeof(Mssql.MssqlDataProvider) },
        };

        static DataProvider()
        {
            LoggerProvider.EnvironmentLogger.Info("Initializing DataProvider...");
            string dbTypeName;
            if (SlipstreamEnvironment.Initialized)
            {
                dbTypeName = SlipstreamEnvironment.Settings.DbType;
            }
            else
            {
                dbTypeName = "postgres";
            }

            var instance = CreateDataProvider(dbTypeName);

            lock (dataProviderLock)
            {
                concreteDataProvider = instance;
            }
        }

        public static IDataProvider CreateDataProvider(string dbTypeName)
        {
            if (!dbTypeMapping.ContainsKey(dbTypeName))
            {
                var msg = String.Format("Unsupported database type: [{0}]", dbTypeName);
                throw new NotSupportedException(msg);
            }

            var providerType = dbTypeMapping[dbTypeName];
            LoggerProvider.EnvironmentLogger.Info(
                String.Format("Concrete DataProvider: [{0}]", providerType.FullName));
            var instance = Activator.CreateInstance(providerType) as IDataProvider;
            concreteDataProvider = instance;
            return instance;
        }

        public static IDataContext OpenDataContext(string dbName)
        {
            if (dbName == null)
            {
                throw new ArgumentNullException("_dbName");
            }

            return concreteDataProvider.OpenDataContext(dbName);
        }

        public static IDataContext CreateDataContext()
        {
            return concreteDataProvider.OpenDataContext();
        }

        public static string[] ListDatabases()
        {
            return concreteDataProvider.ListDatabases();
        }

        public static NHibernate.Dialect.Dialect Dialect
        {
            get
            {
                return concreteDataProvider.Dialect;
            }
        }

        public static bool IsSupportProcedure
        {
            get
            {
                return concreteDataProvider.IsSupportProcedure;
            }
        }

    }
}
