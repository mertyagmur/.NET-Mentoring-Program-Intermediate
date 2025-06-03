using Expressions.Task3.E3SQueryProvider.Helpers;
using Expressions.Task3.E3SQueryProvider.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Expressions.Task3.E3SQueryProvider.QueryProvider
{
    public class E3SLinqProvider : IQueryProvider
    {
        private readonly IE3SSearchService _e3sClient;

        public E3SLinqProvider(IE3SSearchService client)
        {
            _e3sClient = client;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new E3SQuery<TElement>(expression, this);
        }

        public object Execute(Expression expression)
        {
            Type itemType = TypeHelper.GetElementType(expression.Type);
            var translator = new ExpressionToFtsRequestTranslator();
            List<string> queryStrings = translator.Translate(expression);

            return _e3sClient.SearchFts(itemType, queryStrings);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            Type itemType = TypeHelper.GetElementType(expression.Type);

            var translator = new ExpressionToFtsRequestTranslator();
            List<string> queryStrings = translator.Translate(expression);
            
            return (TResult)_e3sClient.SearchFts(itemType, queryStrings);
        }
    }
}
