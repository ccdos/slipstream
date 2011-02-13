﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using Npgsql;

namespace ObjectServer.Backend
{
    public class Database : IDisposable
    {
        private IDbConnection conn = null;

        public Database(string dbName)
        {
            string connectionString =
              "Server=localhost;" +
              "Database=objectserver;" +
              "Encoding=UNICODE;" +
              "User ID=objectserver;" +
              "Password=objectserver;";
            this.conn = new NpgsqlConnection(connectionString);
            this.DatabaseName = dbName;
        }

        public Database()
            : this("template1")
        {
        }

        public void Open()
        {
            this.conn.Open();
        }

        public void Close()
        {
            if (this.Connected)
            {
                this.conn.Close();
            }
        }

        public void Create(string dbName)
        {
            //TODO 检查连接
            var sql = string.Format(
                @"CREATE DATABASE ""{0}"" TEMPLATE template0 ENCODING 'unicode'",
                dbName);
            var cmd = this.Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            this.DatabaseName = dbName;
        }

        public void Delete(string dbName)
        {
            var sql = string.Format(
                "DROP DATABASE \"{0}\"", dbName);
            var cmd = this.Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }


        public string[] List()
        {
            var dbUser = "objectserver"; //TODO: 改成可配置的
            var sql = @"
                select datname from pg_database  
                    where datdba = (select distinct usesysid from pg_user where usename=:dbUser) 
                        and datname not in ('template0', 'template1', 'postgres')  
	                order by datname asc;";
            using (var cmd = (NpgsqlCommand)this.conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("dbUser", dbUser);
                var result = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
                return result.ToArray();
            }
        }

        public IDbConnection Connection { get { return this.conn; } }

        public bool Connected
        {
            get
            {
                return this.conn.State != ConnectionState.Closed &&
                    this.conn.State != ConnectionState.Broken;
            }
        }

        public string DatabaseName { get; private set; }

        #region IDisposable 成员

        public void Dispose()
        {
            this.Close();

        }

        #endregion
    }
}
