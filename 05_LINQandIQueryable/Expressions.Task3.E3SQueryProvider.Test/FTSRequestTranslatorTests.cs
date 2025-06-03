using System;
using System.Linq;
using System.Linq.Expressions;
using Expressions.Task3.E3SQueryProvider.Models.Entities;
using Xunit;
using System.Collections.Generic;

namespace Expressions.Task3.E3SQueryProvider.Test
{
    public class FtsRequestTranslatorTests
    {
        #region SubTask 1 : operands order

        [Fact]
        public void TestBinaryBackOrder()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => "EPRUIZHW006" == employee.Workstation;

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(EPRUIZHW006)", translatedQueries[0]);
        }

        #endregion

        #region SubTask 2: inclusion operations

        [Fact]
        public void TestBinaryEqualsQueryable()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<IQueryable<EmployeeEntity>, IQueryable<EmployeeEntity>>> expression
                = query => query.Where(e => e.Workstation == "EPRUIZHW006");

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(EPRUIZHW006)", translatedQueries[0]);
        }

        [Fact]
        public void TestBinaryEquals()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => employee.Workstation == "EPRUIZHW006";

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(EPRUIZHW006)", translatedQueries[0]);
        }

        [Fact]
        public void TestMethodEquals()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => employee.Workstation.Equals("EPRUIZHW006");

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(EPRUIZHW006)", translatedQueries[0]);
        }

        [Fact]
        public void TestStartsWith()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => employee.Workstation.StartsWith("EPRUIZHW006");
            
            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(EPRUIZHW006*)", translatedQueries[0]);
        }

        [Fact]
        public void TestEndsWith()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => employee.Workstation.EndsWith("IZHW0060");

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(*IZHW0060)", translatedQueries[0]);
        }

        [Fact]
        public void TestContains()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<EmployeeEntity, bool>> expression
                = employee => employee.Workstation.Contains("IZHW006");

            List<string> translatedQueries = translator.Translate(expression);
            Assert.NotNull(translatedQueries);
            Assert.Single(translatedQueries);
            Assert.Equal("Workstation:(*IZHW006*)", translatedQueries[0]);
        }

        #endregion
    }
}
