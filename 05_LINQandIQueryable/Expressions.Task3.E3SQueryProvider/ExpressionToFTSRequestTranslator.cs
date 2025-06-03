using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        private List<string> _collectedQueries;

        public ExpressionToFtsRequestTranslator()
        {
            _collectedQueries = new List<string>();
        }

        public List<string> Translate(Expression exp)
        {
            _collectedQueries.Clear();
            Visit(exp);
            return _collectedQueries;
        }

        #region protected methods

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        private string GetMemberName(Expression expression)
        {
            expression = StripQuotes(expression);
            if (expression is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            return null;
        }

        private string GetConstantValue(Expression expression)
        {
            expression = StripQuotes(expression);
            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value?.ToString();
            }
            return null;
        }
        
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Visit(node.Body);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
            {
                var predicate = StripQuotes(node.Arguments[1]);
                Visit(predicate);
                return node;
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                string propertyName = GetMemberName(node.Object);
                if (propertyName == null)
                {
                    throw new NotSupportedException("String method call must be on a member property for FTS translation.");
                }

                if (node.Arguments.Count > 0 && node.Arguments[0] is ConstantExpression constantArg)
                {
                    string value = GetConstantValue(constantArg);
                    if (value == null) throw new NotSupportedException("String method argument must be a non-null constant.");

                    string queryString;
                    switch (node.Method.Name)
                    {
                        case "StartsWith":
                            queryString = $"{propertyName}:({value}*)";
                            break;
                        case "EndsWith":
                            queryString = $"{propertyName}:(*{value})";
                            break;
                        case "Contains":
                            queryString = $"{propertyName}:(*{value}*)";
                            break;
                        case "Equals":
                            queryString = $"{propertyName}:({value})";
                            break;
                        default:
                            throw new NotSupportedException($"String method '{node.Method.Name}' is not supported.");
                    }
                    _collectedQueries.Add(queryString);
                    return node; 
                }
                else
                {
                    throw new NotSupportedException("Unsupported arguments for string method. Expecting a single constant argument.");
                }
            }

            return base.VisitMethodCall(node); 
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    Visit(node.Right);
                    break; 

                case ExpressionType.Equal:
                    string leftMemberName = GetMemberName(node.Left);
                    string rightConstValue = GetConstantValue(node.Right);

                    string propertyName;
                    string value;

                    if (leftMemberName != null && rightConstValue != null)
                    {
                        propertyName = leftMemberName;
                        value = rightConstValue;
                    }
                    else
                    {
                        string rightMemberName = GetMemberName(node.Right);
                        string leftConstValue = GetConstantValue(node.Left);

                        if (rightMemberName != null && leftConstValue != null)
                        {
                            propertyName = rightMemberName;
                            value = leftConstValue;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported Equal expression structure. Operands: Left='{node.Left}', Right='{node.Right}'. Ensure one side is a member and the other is a constant.");
                        }
                    }
                    _collectedQueries.Add($"{propertyName}:({value})");
                    break;

                default:
                    throw new NotSupportedException($"Binary operation '{node.NodeType}' is not supported.");
            }
            return node; 
        }

        #endregion
    }
}
