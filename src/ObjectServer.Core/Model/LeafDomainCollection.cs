﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.SqlTree;

namespace ObjectServer.Model
{
    internal class LeafDomainCollection
    {
        public sealed class TableJoin
        {
            public TableJoin(string t, string a)
            {
                this.Table = t;
                this.Alias = a;
            }

            public string Table { get; private set; }
            public string Alias { get; private set; }
        }

        private int joinCount = 0;
        private string mainTableAlias;
        private IList<TableJoin> innerJoins = new List<TableJoin>();
        private IList<TableJoin> outerJoins = new List<TableJoin>();
        private IList<IExpression> restrictions = new List<IExpression>();
        private IList<IExpression> joinRestrictions = new List<IExpression>();

        public LeafDomainCollection(string mainTable, string mainTableAlias)
        {
            Debug.Assert(!string.IsNullOrEmpty(mainTableAlias));

            this.mainTableAlias = mainTableAlias;

            //这里打了个洞，做的不够好，重构
            this.innerJoins.Add(new TableJoin(mainTable, mainTableAlias));
        }

        public void ClearLeaves()
        {
            this.restrictions.Clear();
        }

        public void AppendLeaf(string lhs, string opr, object value)
        {
            //TODO 检查是否是叶子类型的 DOMAIN 表达式
            //检查重复

            switch (opr)
            {
                case "=":
                case ">":
                case ">=":
                case "<":
                case "<=":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        new ExpressionOperator(opr),
                        new ValueExpression(value)));

                    break;

                case "!=":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        ExpressionOperator.NotEqualOperator,
                        new ValueExpression(value)));
                    break;

                case "like":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        ExpressionOperator.LikeOperator,
                        new ValueExpression(value)));
                    break;

                case "!like":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        ExpressionOperator.NotLikeOperator,
                        new ValueExpression(value)));
                    break;

                case "in":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        ExpressionOperator.InOperator,
                        new ExpressionGroup((IEnumerable<object>)value)));
                    break;

                case "!in":
                    this.restrictions.Add(new BinaryExpression(
                        new IdentifierExpression(lhs),
                        ExpressionOperator.NotInOperator,
                        new ExpressionGroup((IEnumerable<object>)value)));
                    break;

                default:
                    throw new NotSupportedException();
            }

        }

        public void AddJoinRestriction(string lhs, string opr, string rhs)
        {
            if (string.IsNullOrEmpty(lhs))
            {
                throw new ArgumentNullException("lhs");
            }

            if (string.IsNullOrEmpty(opr))
            {
                throw new ArgumentNullException("opr");
            }

            if (string.IsNullOrEmpty(rhs))
            {
                throw new ArgumentNullException("rhs");
            }

            //TODO 检查是否已经存在： 
            this.AddJoinRestriction(new BinaryExpression(
                new IdentifierExpression(lhs),
                new ExpressionOperator(opr),
                new IdentifierExpression(rhs)));
        }

        public void AddJoinRestriction(IExpression exp)
        {
            if (exp == null)
            {
                throw new ArgumentNullException("exp");
            }
            this.joinRestrictions.Add(exp);
        }

        public IExpression GetJoinRestrictionExpression()
        {
            if (this.joinRestrictions.Count > 0)
            {
                return this.joinRestrictions.JoinExpressions(ExpressionOperator.AndOperator);
            }
            else
            {
                return ValueExpression.TrueExpression;
            }
        }

        public IExpression GetRestrictionExpression()
        {
            if (this.restrictions.Count > 0)
            {
                return this.restrictions.JoinExpressions(ExpressionOperator.AndOperator);
            }
            else
            {
                return ValueExpression.TrueExpression;
            }
        }

        public AliasExpression[] GetTableAliases()
        {
            var joins = this.outerJoins.Union(this.innerJoins).Select(j => new AliasExpression(j.Table, j.Alias));
            return joins.ToArray();
        }

        public string PutInnerJoin(string table, string relatedField)
        {
            var joinInfo = this.innerJoins.SingleOrDefault(j => j.Table == table);

            string alias;
            if (joinInfo == null)
            {
                this.joinCount++;
                alias = "_t" + this.joinCount.ToString();
                this.innerJoins.Add(new TableJoin(table, alias));
                this.joinRestrictions.Add(new BinaryExpression(
                    new IdentifierExpression(alias + "." + AbstractModel.IDFieldName),
                    ExpressionOperator.EqualOperator,
                    new IdentifierExpression(this.mainTableAlias + "." + relatedField)));

            }
            else
            {
                alias = joinInfo.Alias;
            }
            return alias;
        }

        public string PutOuterJoin(string table)
        {
            var existedJoin = this.outerJoins.SingleOrDefault(oj => oj.Table == table);
            if (existedJoin != null)
            {
                return existedJoin.Alias;
            }
            else
            {
                this.joinCount++;
                string alias = "_t" + this.joinCount.ToString();
                this.outerJoins.Add(new TableJoin(table, alias));
                return alias;
            }
        }

        public string AddOuterJoin(string table)
        {
            this.joinCount++;
            string alias = "_t" + this.joinCount.ToString();
            this.outerJoins.Add(new TableJoin(table, alias));
            return alias;
        }

    }
}
