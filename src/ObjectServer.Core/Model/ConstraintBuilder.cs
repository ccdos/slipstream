﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.SqlTree;

namespace ObjectServer.Model
{
    internal sealed class ConstraintBuilder
    {
        public static readonly string[] Operators = new string[]
        {
            "=", "!=", ">", ">=", "<", "<=", "in", "!in", 
            "like", "!like", "childof", "!childof"
        };

        private LeafDomainCollection leaves;

        private string mainTableAlias;

        IModel model;
        IServiceScope serviceScope;

        public ConstraintBuilder(IServiceScope scope, IModel model)
        {
            Debug.Assert(scope != null);
            Debug.Assert(model != null);

            this.serviceScope = scope;
            this.model = model;
            this.mainTableAlias = "_t0";
            this.leaves = new LeafDomainCollection(model.TableName, this.mainTableAlias);
        }

        public IExpression Push(IEnumerable<ConstraintExpression> constraints)
        {

            if (constraints == null)
            {
                throw new ArgumentNullException("constraints");
            }

            this.leaves.ClearLeaves();

            foreach (var o in constraints)
            {
                var isBaseField = IsInheritedField(this.serviceScope, this.model, o.Field);
                string aliasedField;

                if (isBaseField)
                {
                    var baseField = ((InheritedField)this.model.Fields[o.Field]).BaseField;
                    var relatedField = this.model.Inheritances
                        .Where(i1 => i1.BaseModel == baseField.Model.Name)
                        .Select(i2 => i2.RelatedField)
                        .Single();
                    var baseTableAlias = this.leaves.PutInnerJoin(baseField.Model.TableName, relatedField);
                    aliasedField = baseTableAlias + '.' + o.Field;
                }
                else
                {
                    aliasedField = this.mainTableAlias + '.' + o.Field;
                }
                this.ParseDomainExpression(aliasedField, o.Operator, o.Value);
            }

            return this.leaves.GetRestrictionExpression();
        }

        public IExpression Push(IEnumerable<object> constraints)
        {
            if (constraints == null)
            {
                throw new ArgumentNullException("constraints");
            }

            var constraintExps = from o in constraints select new ConstraintExpression(o);
            return this.Push(constraintExps);
        }

        public AliasExpression[] GetAllAliases()
        {
            return this.leaves.GetTableAliases();
        }

        public IExpression GetJoinRestrictionExpression()
        {
            return this.leaves.GetJoinRestrictionExpression();
        }

        private static bool IsInheritedField(IServiceScope scope, IModel mainModel, string field)
        {
            Debug.Assert(scope != null);
            Debug.Assert(mainModel != null);
            Debug.Assert(!string.IsNullOrEmpty(field));

            if (mainModel.Inheritances.Count == 0)
            {
                return false;
            }

            if (AbstractTableModel.SystemReadonlyFields.Contains(field))
            {
                return false;
            }

            if (mainModel.Fields[field] is InheritedField)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ParseDomainExpression(string lhs, string opr, object value)
        {
            Debug.Assert(!string.IsNullOrEmpty(lhs));
            Debug.Assert(!string.IsNullOrEmpty(opr));

            var aliasedField = lhs;

            //如果是引用或者many-to-one类型的字段可以使用 '.' 来访问关联表的字段
            //比如 [["user.organization.code", "=", "main-company"]]
            string selfFieldName = string.Empty;
            var fieldParts = lhs.Split('.');
            if (fieldParts.Length > 2)
            {
                var aliasName = PreprocessReferenceField(opr, value, fieldParts);
                aliasedField = aliasName + '.' + fieldParts.Last();
                selfFieldName = fieldParts[1];
            }
            else
            {
                selfFieldName = fieldParts.Last();
            }
            var selfField = this.model.Fields[selfFieldName];

            var exps = new List<IExpression>();

            switch (opr)
            {
                case "=":
                case ">":
                case ">=":
                case "<":
                case "<=":
                case "!=":
                case "like":
                case "!like":
                case "in":
                case "!in":
                    if (selfField.Type != FieldType.Reference && selfField.Type != FieldType.ManyToOne)
                    {
                        this.leaves.AppendLeaf(aliasedField, opr, value);
                    }
                    break;

                case "childof":
                    this.ParseChildOfOperator(aliasedField, value);
                    break;

                case "!childof":
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();

            }
        }

        private string PreprocessReferenceField(string opr, object value, string[] fieldParts)
        {
            Debug.Assert(!string.IsNullOrEmpty(opr));
            Debug.Assert(fieldParts != null && fieldParts.Length > 2);

            //检查类型
            var selfFieldName = fieldParts[1];
            var selfField = this.model.Fields[selfFieldName];

            if (selfField.Type != FieldType.Reference && selfField.Type != FieldType.ManyToOne)
            {
                throw new NotSupportedException("仅支持 Reference 和 ManyToOne 类型的字段");
            }

            var joinModel = (IModel)this.serviceScope.GetResource(selfField.Relation);
            var joinTableName = joinModel.TableName;

            string aliasName;
            var idExp = new IdentifierExpression(this.mainTableAlias + '.' + selfFieldName);
            if (selfField.IsRequired) //处理内连接
            {
                aliasName = this.leaves.PutInnerJoin(joinTableName, selfFieldName);
            }
            else //处理外连接
            {
                aliasName = this.leaves.PutOuterJoin(joinTableName);
                var fieldExp = new IdentifierExpression(aliasName + '.' + fieldParts.Last());
                var joinIdExp = new IdentifierExpression(aliasName + '.' + AbstractModel.IDFieldName);

                var joinExp1 = new BinaryExpression(idExp, ExpressionOperator.EqualOperator, joinIdExp);
                var joinExp2 = new BinaryExpression(
                    fieldExp, new ExpressionOperator(opr), new ValueExpression(value));
                var lexp = new BinaryExpression(joinExp1, ExpressionOperator.AndOperator, joinExp2);
                var rexp = new BinaryExpression(
                    idExp, ExpressionOperator.IsOperator, ValueExpression.NullExpression);
                var orExp = new BinaryExpression(
                    new BracketedExpression(lexp), ExpressionOperator.OrOperator, new BracketedExpression(rexp));
                this.leaves.AddJoinRestriction(orExp);
            }

            return aliasName;
        }

        private void ParseChildOfOperator(string aliasedField, object value)
        {
            Debug.Assert(!string.IsNullOrEmpty(aliasedField));
            Debug.Assert(aliasedField.Contains('.'));

            //TODO 处理继承字段
            var fieldParts = aliasedField.Split('.');
            var selfField = fieldParts[1];
            var joinTableName = string.Empty;
            if (selfField == AbstractModel.IDFieldName)
            {
                joinTableName = this.model.TableName;
            }
            else
            {
                var fieldInfo = this.model.Fields[selfField];
                var joinModel = (IModel)this.serviceScope.GetResource(fieldInfo.Relation);
                joinTableName = joinModel.TableName;
            }
            //TODO 确认 many2one 类型字段
            var parentAliasName = this.leaves.AddOuterJoin(joinTableName);
            var childAliasName = this.leaves.PutInnerJoin(joinTableName, selfField);


            /* 生成的 SQL 形如：
             * SELECT mainTable._id 
             * FROM mainTable, category _category_parent_0, category AS _category_child_0
             * WHERE _category_child_0._id = mainTable.field AND
             *     _category_parent_0._id = {value} AND
             *     _category_child_0._left > _category_parent_0._left AND
             *     _category_child_0._left < _category_parent_0._right AND ...
             * 
             * */
            //添加约束
            this.leaves.AppendLeaf(parentAliasName + "." + AbstractModel.IDFieldName, "=", value);
            this.leaves.AddJoinRestriction(childAliasName + "._left", ">", parentAliasName + "._left");
            this.leaves.AddJoinRestriction(childAliasName + "._left", "<", parentAliasName + "._right");
        }


    }
}
