﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;

using ObjectServer.Client.Model;

namespace ObjectServer.Client
{
    public interface IRemoteService
    {

        void GetVersion(Action<Version> resultCallback);
        void ListDatabases(Action<string[], Exception> resultCallback);
        Task<string[]> ListDatabasesAsync();
        void CreateDatabase(string serverPasswordHash, string dbName, string adminPassword, Action resultCallback);
        void DeleteDatabase(string serverPasswordHash, string dbName, Action resultCallback);

        void LogOn(
           string dbName, string userName, string password, Action<string> resultCallback);
        void LogOff(Action resultCallback);
        void Execute(
            string objectName, string method, object[] parameters, Action<object> resultCallback);

        //辅助方法
        void CountModel(
            string objectName, object[][] constraints, Action<long> resultCallback);

        void SearchModel(
            string objectName, object[][] constraints,
            object[][] order, long offset, long limit, Action<long[]> resultCallback);

        void ReadModel(
            string objectName, IEnumerable<long> ids, IEnumerable<string> fields,
            Action<Dictionary<string, object>[]> resultCallback);

        void CreateModel(
             string objectName, IDictionary<string, object> fields, Action<long> resultCallback);

        void WriteModel(string objectName, long id, IDictionary<string, object> fields, Action resultCallback);

        void DeleteModel(string objectName, long[] ids, Action resultCallback);

        void ReadAllMenus(Action<MenuModel[]> resultCallback);
    }
}
