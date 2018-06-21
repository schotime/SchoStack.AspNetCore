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
        IBuilderActions<TResponse, IActionResult> For<TResponse>(IRequest<TResponse> request);
    }

    public interface IAsyncViewComponentResultBuilder
    {
        IBuilderActions<TResponse, IViewComponentResult> For<TResponse>(IRequest<TResponse> request);
    }

    public interface IBuilderActions<TResponse, TResult>
    {
        IBuilderActions<TResponse, TResult> On(Func<TResponse, bool> condition, Func<TResponse, Task<TResult>> result);
        IBuilderActions<TResponse, TResult> On(Func<TResponse, bool> condition, Func<TResponse, TResult> result);
        IBuilderActions<TResponse, TResult> Error(Func<Task<TResult>> result);
        IBuilderActions<TResponse, TResult> Error(Func<TResult> result);
        ISendableActions<TResult> Success(Func<TResponse, Task<TResult>> result);
        ISendableActions<TResult> Success(Func<TResponse, TResult> result);
    }

    public interface ISendableActions<T>
    {
        Task<T> Send(CancellationToken cancellationToken = default(CancellationToken));
    }

    public class MediatrResultBuilder : IAsyncActionResultBuilder, IAsyncViewComponentResultBuilder
    {
        private readonly IMediator _mediator;
        private readonly IActionContextAccessor _actionContextAccessor;

        public MediatrResultBuilder(IMediator mediator, IActionContextAccessor actionContextAccessor)
        {
            _mediator = mediator;
            _actionContextAccessor = actionContextAccessor;
        }
        
        IBuilderActions<TResponse, IActionResult> IAsyncActionResultBuilder.For<TResponse>(IRequest<TResponse> request)
        {
            return new BuilderActions<TResponse, IActionResult>(request, _mediator, _actionContextAccessor);
        }

        IBuilderActions<TResponse, IViewComponentResult> IAsyncViewComponentResultBuilder.For<TResponse>(IRequest<TResponse> request)
        {
            return new BuilderActions<TResponse, IViewComponentResult>(request, _mediator, _actionContextAccessor);
        }
        
        public class BuilderActions<TResponse, TResult> : IBuilderActions<TResponse, TResult>, ISendableActions<TResult> where TResult : class
        {
            private readonly IMediator _mediator;
            private readonly IActionContextAccessor _actionContextAccessor;
            private readonly IRequest<TResponse> _request;

            private readonly List<ConditionResult<TResponse, TResult>> _conditionResults  = new List<ConditionResult<TResponse, TResult>>();
            private Func<Task<TResult>> _errorResult;
            private Func<TResponse, Task<TResult>> _successResult;

            public BuilderActions(IRequest<TResponse> request, IMediator mediator, IActionContextAccessor actionContextAccessor)
            {
                _mediator = mediator;
                _actionContextAccessor = actionContextAccessor;
                _request = request;
            }

            public IBuilderActions<TResponse, TResult> On(Func<TResponse, bool> condition, Func<TResponse, Task<TResult>> result)
            {
                _conditionResults.Add(new ConditionResult<TResponse, TResult>(condition, result));
                return this;
            }

            public IBuilderActions<TResponse, TResult> On(Func<TResponse, bool> condition, Func<TResponse, TResult> result)
            {
                _conditionResults.Add(new ConditionResult<TResponse, TResult>(condition, resp => Task.FromResult(result.Invoke(resp))));
                return this;
            }

            public IBuilderActions<TResponse, TResult> Error(Func<Task<TResult>> result)
            {
                _errorResult = result;
                return this;
            }

            public IBuilderActions<TResponse, TResult> Error(Func<TResult> result)
            {
                _errorResult = () => Task.FromResult(result.Invoke());
                return this;
            }

            public ISendableActions<TResult> Success(Func<TResponse, Task<TResult>> result)
            {
                _successResult = result;
                return this;
            }

            public ISendableActions<TResult> Success(Func<TResponse, TResult> result)
            {
                _successResult = resp => Task.FromResult(result.Invoke(resp));
                return this;
            }

            public async Task<TResult> Send(CancellationToken cancellationToken = default(CancellationToken))
            {
                if (_errorResult != null && !_actionContextAccessor.ActionContext.ModelState.IsValid)
                    return await _errorResult();

                var result = await _mediator.Send(_request, cancellationToken);

                var conditionResult = _conditionResults.FirstOrDefault(x => x.Condition(result))?.Result;
                if (conditionResult != null)
                    return await conditionResult.Invoke(result);
                
                return await (_successResult?.Invoke(result) ?? Task.FromResult((TResult) null));
            }
        }

        public class ConditionResult<TResponse, TResult>
        {
            public ConditionResult(Func<TResponse, bool> condition, Func<TResponse, Task<TResult>> result)
            {
                Condition = condition;
                Result = result;
            }

            public Func<TResponse, bool> Condition { get; }
            public Func<TResponse, Task<TResult>> Result { get; }
        }
    }
}