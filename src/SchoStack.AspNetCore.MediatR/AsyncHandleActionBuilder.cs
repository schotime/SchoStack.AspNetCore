using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SchoStack.AspNetCore.MediatR
{
    public interface IAsyncActionResultBuilder
    {
        AsyncActionResultBuilder.IActions<TResponse> For<TResponse>(IRequest<TResponse> request);
    }

    public class AsyncActionResultBuilder : IAsyncActionResultBuilder
    {
        private readonly IMediator _mediator;
        private readonly IActionContextAccessor _actionContextAccessor;

        public AsyncActionResultBuilder(IMediator mediator, IActionContextAccessor actionContextAccessor)
        {
            _mediator = mediator;
            _actionContextAccessor = actionContextAccessor;
        }

        public IActions<TResponse> For<TResponse>(IRequest<TResponse> request)
        {
            return new Actions<TResponse>(request, this);
        }

        public interface IActions<TResponse>
        {
            IActions<TResponse> On(Func<TResponse, bool> condition, Func<TResponse, IActionResult> result);
            IActions<TResponse> Error(Func<IActionResult> result);
            ISendableActions Success(Func<TResponse, IActionResult> result);
        }

        public interface ISendableActions
        {
            Task<IActionResult> Send(CancellationToken cancellationToken = default(CancellationToken));
        }

        public class Actions<TResponse> : IActions<TResponse>, ISendableActions
        {
            private readonly AsyncActionResultBuilder _builder;
            private readonly IRequest<TResponse> _request;

            public Actions(IRequest<TResponse> request, AsyncActionResultBuilder builder)
            {
                _builder = builder;
                _request = request;
            }

            private List<ConditionResult<TResponse>> ConditionResults  = new List<ConditionResult<TResponse>>();
            private Func<IActionResult> ErrorResult;
            private Func<TResponse, IActionResult> SuccessResult;

            public IActions<TResponse> On(Func<TResponse, bool> condition, Func<TResponse, IActionResult> result)
            {
                ConditionResults.Add(new ConditionResult<TResponse>(condition, result));
                return this;
            }

            public IActions<TResponse> Error(Func<IActionResult> result)
            {
                ErrorResult = result;
                return this;
            }

            public ISendableActions Success(Func<TResponse, IActionResult> result)
            {
                SuccessResult = result;
                return this;
            }

            public async Task<IActionResult> Send(CancellationToken cancellationToken = default(CancellationToken))
            {
                var result = await _builder._mediator.Send(_request, cancellationToken);

                if (ErrorResult != null && !_builder._actionContextAccessor.ActionContext.ModelState.IsValid)
                    return ErrorResult();

                var conditionResult = ConditionResults.FirstOrDefault(x => x.Condition(result))?.Result;
                if (conditionResult != null)
                    return conditionResult?.Invoke(result);

                return SuccessResult?.Invoke(result);
            }
        }

        public class ConditionResult<TResponse>
        {
            public ConditionResult(Func<TResponse, bool> condition, Func<TResponse, IActionResult> result)
            {
                Condition = condition;
                Result = result;
            }

            public Func<TResponse, bool> Condition { get; }
            public Func<TResponse, IActionResult> Result { get; }
        }
    }
}