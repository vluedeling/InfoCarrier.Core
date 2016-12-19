﻿namespace InfoCarrier.Core.Client.Query
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore.Query;
    using Remotion.Linq;
    using Remotion.Linq.Clauses;
    using Remotion.Linq.Clauses.ResultOperators;
    using Utils;

    public class InfoCarrierResultOperatorHandler : ResultOperatorHandler
    {
        public override Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel)
        {
            foreach (var castResultOperator in resultOperator.YieldAs<CastResultOperator>())
            {
                // Don't let ResultOperatorHandler.HandleCast swallow .Cast when upcasting
                return Expression.Call(
                    entityQueryModelVisitor.LinqOperatorProvider
                        .Cast.MakeGenericMethod(castResultOperator.CastItemType),
                    entityQueryModelVisitor.Expression);
            }

            return base.HandleResultOperator(entityQueryModelVisitor, resultOperator, queryModel);
        }
    }
}
