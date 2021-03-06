﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Sandwych;

namespace SlipStream.Model
{
    internal sealed class ManyToOneField : AbstractField
    {
        OnDeleteAction onDelete;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="masterModel">主表对象</param>
        public ManyToOneField(IModel model, string name, string masterModel)
            : base(model, name, FieldType.ManyToOne)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrEmpty(masterModel))
            {
                throw new ArgumentNullException("masterModel");
            }

            this.Relation = masterModel;
            this.OnDeleteAction = OnDeleteAction.Cascade;
            this.Required();
        }

        protected override Dictionary<long, object> OnGetFieldValues(
            ICollection<Dictionary<string, object>> rawRecords)
        {
            if (rawRecords == null)
            {
                throw new ArgumentNullException("rawRecords");
            }

            var fields = new string[] { "name" }; //TODO 改成静态变量
            var result = new Dictionary<long, object>(rawRecords.Count());
            dynamic masterModel = this.Model.DbDomain.GetResource(this.Relation);
            if (masterModel.ContainsField("name")) //如果有 name 字段
            {
                var manyToOneFieldValues = rawRecords.ToDictionary(_ => (long)_[AbstractModel.IdFieldName]);

                var availableRecords =
                    from r in rawRecords
                    let mid = r[this.Name]
                    where !mid.IsNull()
                    select new
                    {
                        MasterId = (long)mid,
                        SelfId = (long)r[AbstractModel.IdFieldName]
                    };

                if (availableRecords.Any())
                {
                    var masterRecords = masterModel.ReadInternal(availableRecords.Select(ar => ar.MasterId).ToArray(), fields);
                    var masterNames = new Dictionary<long, string>(masterRecords.Length);
                    foreach (var mr in masterRecords)
                    {
                        masterNames.Add((long)mr[AbstractModel.IdFieldName], (string)mr["name"]);
                    }

                    foreach (var mid in availableRecords)
                    {
                        result[mid.SelfId] = new object[] { mid.MasterId, masterNames[mid.MasterId] };
                    }
                }

                var nullRecords = from r in rawRecords
                                  let mid = r[this.Name]
                                  where mid.IsNull()
                                  select (long)r[AbstractModel.IdFieldName];
                foreach (var mid in nullRecords)
                {
                    result[mid] = null;
                }
            }
            else
            {
                foreach (var r in rawRecords)
                {
                    var id = (long)r[AbstractModel.IdFieldName];
                    var masterId = (long)r[this.Name];
                    result.Add(id, new object[] { masterId, string.Empty });
                }
            }

            return result;
        }


        protected override object OnSetFieldValue(object value)
        {
            return value;
        }

        public override object BrowseField(IDictionary<string, object> record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            object fieldValueObj;
            if (!record.TryGetValue(this.Name, out fieldValueObj) || fieldValueObj == null)
            {
                throw new Exceptions.ResourceException(
                    "Unable to get field value: " + this.Model.Name + "." + this.Name);
            }

            long[] destIds = null;
            if (fieldValueObj is long || fieldValueObj is long?)
            {
                destIds = new long[] { (long)fieldValueObj };
            }
            else if (fieldValueObj is object[])
            {
                var destIdObj = ((object[])fieldValueObj).First();
                destIds = new long[] { (long)destIdObj };
            }
            else
            {
                throw new NotSupportedException();
            }

            var destModelName = this.Relation;
            dynamic destMetaModel = this.Model.DbDomain.GetResource(destModelName);
            //查询 ManyToOne 字段
            var destRecord = destMetaModel.Read(destIds, null)[0];
            return new BrowsableRecord(destMetaModel, destRecord);
        }

        public override bool IsRequired
        {
            get
            {
                return base.IsRequired;
            }
        }

        public override bool IsColumn { get { return !this.IsFunctional; } }

        public override bool IsScalar
        {
            get { return false; }
        }

        public override int Size
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override OnDeleteAction OnDeleteAction
        {
            get
            {
                return this.onDelete;
            }
            set
            {
                this.onDelete = value;
            }
        }

        public override IDictionary<string, string> Options
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void VerifyDefinition()
        {
            base.VerifyDefinition();

            if (this.IsRequired && this.OnDeleteAction == SlipStream.Model.OnDeleteAction.SetNull)
            {
                throw new Exceptions.ResourceException(
                    String.Format("Field [{0}] can not set to be OnDeleteAction.SetNull, because it's required.", this));
            }
        }
    }
}
