using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.DeclaringType == typeof(string) && node.Method.Name == "Equals")
            {
                if (node.Object?.NodeType == ExpressionType.MemberAccess && node.Arguments[0]?.NodeType == ExpressionType.Constant)
                {
                    Visit(node.Object);
                    _resultStringBuilder.Append("(");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");

                    return node;
                }
            }

            if (node.Method.DeclaringType == typeof(string) && node.Method.Name == "Contains")
            {
                if (node.Object?.NodeType == ExpressionType.MemberAccess && node.Arguments[0]?.NodeType == ExpressionType.Constant)
                {
                    Visit(node.Object);
                    _resultStringBuilder.Append("(*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");

                    return node;
                }
            }

            if (node.Method.DeclaringType == typeof(string) && node.Method.Name == "StartsWith")
            {
                if (node.Object?.NodeType == ExpressionType.MemberAccess && node.Arguments[0]?.NodeType == ExpressionType.Constant)
                {
                    Visit(node.Object);
                    _resultStringBuilder.Append("(");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");

                    return node;
                }
            }

            if (node.Method.DeclaringType == typeof(string) && node.Method.Name == "EndsWith")
            {
                if (node.Object?.NodeType == ExpressionType.MemberAccess && node.Arguments[0]?.NodeType == ExpressionType.Constant)
                {
                    Visit(node.Object);
                    _resultStringBuilder.Append("(*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");

                    return node;
                }
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    Expression memberAccessNode = null;
                    Expression constantNode = null;

                    if (node.Left.NodeType == ExpressionType.MemberAccess && node.Right.NodeType == ExpressionType.Constant)
                    {
                        memberAccessNode = node.Left;
                        constantNode = node.Right;
                    }
                    else if (node.Left.NodeType == ExpressionType.Constant && node.Right.NodeType == ExpressionType.MemberAccess)
                    {
                        memberAccessNode = node.Right;
                        constantNode = node.Left;
                    }
                    else
                    {
                        if (node.Left.NodeType != ExpressionType.MemberAccess && node.Right.NodeType != ExpressionType.MemberAccess)
                        {
                            throw new NotSupportedException($"One of the operands should be property or field: {node.Left.NodeType}, {node.Right.NodeType}");
                        }
                        throw new NotSupportedException($"One of the operands should be constant: {node.Left.NodeType}, {node.Right.NodeType}");
                    }

                    Visit(memberAccessNode);
                    _resultStringBuilder.Append("(");
                    Visit(constantNode);
                    _resultStringBuilder.Append(")");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
