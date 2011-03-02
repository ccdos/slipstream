﻿
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.SqlTree
{
    public class SetClause : IClause, IExpressionCollection
    {
        public SetClause(IEnumerable<IBinaryExpression> exps)
        {
            foreach (var exp in exps)
            {
                var binExp = exp as IBinaryExpression;
                if (binExp == null || binExp.Operator != ExpressionOperator.EqualOperator)
                {
                    throw new System.ArgumentException("exps");
                }
            }

            this.Expressions = new List<IExpression>(exps);
        }

        public IList<IExpression> Expressions { get; private set; }

        #region INode 成员

        public void Traverse(IVisitor visitor)
        {            
            visitor.VisitBefore(this);
            visitor.VisitOn(this);
            foreach (var exp in this.Expressions)
            {
                exp.Traverse(visitor);
            }
            visitor.VisitAfter(this);
        }

        #endregion

        #region ICloneable 成员

        public object Clone()
        {
            throw new System.NotImplementedException();
        }

        #endregion

    }
}