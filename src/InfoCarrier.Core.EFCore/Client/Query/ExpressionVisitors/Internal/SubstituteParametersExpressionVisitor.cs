﻿namespace InfoCarrier.Core.Client.Query.ExpressionVisitors.Internal
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Extensions.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
    using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
    using Remote.Linq;

    internal class SubstituteParametersExpressionVisitor : ExpressionVisitorBase
    {
        private readonly QueryContext queryContext;
        private readonly IModel model;

        public SubstituteParametersExpressionVisitor(QueryContext queryContext, IModel model)
        {
            this.queryContext = queryContext;
            this.model = model;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression maybeInlineEntityProperty = this.TryVisitInlineEntityProperty(node);
            if (maybeInlineEntityProperty != null)
            {
                return maybeInlineEntityProperty;
            }

            if (node.Method.MethodIsClosedFormOf(DefaultQueryExpressionVisitor.GetParameterValueMethodInfo))
            {
                Type paramType = node.Method.GetGenericArguments().Single();
                object paramValue =
                    InfoCarrier.Core.MethodInfoExtensions.GetMethodInfo(() => this.GetParameterValue<object>(node))
                        .GetGenericMethodDefinition()
                        .MakeGenericMethod(paramType)
                        .ToDelegate<Func<MethodCallExpression, object>>(this)
                        .Invoke(node);

                return Expression.Property(
                    Expression.Constant(paramValue),
                    paramValue.GetType(),
                    nameof(VariableQueryArgument<object>.Value));
            }

            return base.VisitMethodCall(node);
        }

        private Expression TryVisitInlineEntityProperty(MethodCallExpression node)
        {
            if (!EntityQueryModelVisitor.IsPropertyMethod(node.Method))
            {
                return null;
            }

            var propertyNameExpression = node.Arguments[1] as ConstantExpression;
            string propertyName = propertyNameExpression?.Value as string;
            if (propertyName == null)
            {
                return null;
            }

            object entity = null;

            var maybeConstant = node.Arguments[0] as ConstantExpression;
            if (maybeConstant != null)
            {
                entity = maybeConstant.Value;
            }

            var maybeMethodCall = node.Arguments[0] as MethodCallExpression;
            if (maybeMethodCall != null
                && maybeMethodCall.Method.MethodIsClosedFormOf(DefaultQueryExpressionVisitor.GetParameterValueMethodInfo))
            {
                entity =
                    Expression.Lambda<Func<QueryContext, object>>(maybeMethodCall, EntityQueryModelVisitor.QueryContextParameter)
                        .Compile()
                        .Invoke(this.queryContext);
            }

            if (entity == null)
            {
                return null;
            }

            IEntityType efType = this.model.FindEntityType(entity.GetType());
            IProperty efProperty = efType?.FindProperty(propertyName);
            if (efProperty == null)
            {
                return null;
            }

            object paramValue =
                InfoCarrier.Core.MethodInfoExtensions.GetMethodInfo(() => Wrap<object>(null))
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(efProperty.ClrType)
                    .Invoke(null, new object[] { efProperty.GetGetter().GetClrValue(entity) });

            Expression result = Expression.Property(
                Expression.Constant(paramValue),
                paramValue.GetType(),
                nameof(VariableQueryArgument<object>.Value));

            if (result.Type != node.Type)
            {
                result = Expression.Convert(result, node.Type);
            }

            return result;
        }

        private object GetParameterValue<T>(MethodCallExpression node) => Wrap(
            Expression
                .Lambda<Func<QueryContext, T>>(node, EntityQueryModelVisitor.QueryContextParameter)
                .Compile()
                .Invoke(this.queryContext));

        private static object Wrap<T>(T value) => new VariableQueryArgument<T>(value);
    }
}