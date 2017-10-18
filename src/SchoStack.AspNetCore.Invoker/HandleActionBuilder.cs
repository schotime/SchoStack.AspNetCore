using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SchoStack.AspNetCore.Invoker
{
    public class HandleActionBuilder
    {
        private readonly IInvoker _invoker;
        private Func<ActionContext, ActionResult> _successResult;
        private Func<ActionContext, ActionResult> _errorResult;

        public HandleActionBuilder(IInvoker invoker)
        {
            _invoker = invoker;
        }

        public HandleActionBuilder OnSuccess(Func<ActionResult> result)
        {
            _successResult = _ => result();
            return this;
        }

        public HandleActionBuilder OnSuccess(Func<ActionContext, ActionResult> result)
        {
            _successResult = result;
            return this;
        }

        public HandleActionBuilder OnModelError(Func<ActionResult> result)
        {
            _errorResult = _ => result();
            return this;
        }

        public HandleActionBuilder OnModelError(Func<ActionContext, ActionResult> result)
        {
            _errorResult = result;
            return this;
        }

        public HandleActionBuilder<HandleActionBuilder, TRet> Returning<TRet>()
        {
            return new HandleActionBuilder<HandleActionBuilder, TRet>(null, _invoker, _successResult, _errorResult);
        }
    }

    public class HandleActionBuilder<T> : IReturnActionResult
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private Func<ActionContext, ActionResult> _successResult;
        private Func<ActionContext, ActionResult> _errorResult;

        public HandleActionBuilder(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
        }

        public HandleActionBuilder<T, TRet> Returning<TRet>()
        {
            return new HandleActionBuilder<T, TRet>(_inputModel, _invoker, _successResult, _errorResult);
        }

        public HandleActionBuilder<T> OnSuccess(Func<ActionResult> result)
        {
            _successResult = _ => result();
            return this;
        }

        public HandleActionBuilder<T> OnSuccess(Func<ActionContext, ActionResult> result)
        {
            _successResult = result;
            return this;
        }

        public HandleActionBuilder<T> OnModelError(Func<ActionResult> result)
        {
            _errorResult = _ => result();
            return this;
        }

        public HandleActionBuilder<T> OnModelError(Func<ActionContext, ActionResult> result)
        {
            _errorResult = result;
            return this;
        }

        Func<ActionContext, ActionResult> IReturnActionResult.Result()
        {
            return x => new HandleActionResult(this).ExecuteResult(new SchoStackActionContext(x));
        }

        public static implicit operator HandleActionResult(HandleActionBuilder<T> obj)
        {
            return new HandleActionResult(obj);
        }

        public class HandleActionResult : ActionResult, IExecuteResult
        {
            private readonly HandleActionBuilder<T> _builder;

            public HandleActionResult(HandleActionBuilder<T> builder)
            {
                _builder = builder;
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var result = ExecuteResult(new SchoStackActionContext(context));
                return result?.ExecuteResultAsync(context);
            }

            public ActionResult ExecuteResult(IActionContext context)
            {
                if (!context.IsValidModel && _builder._errorResult != null)
                {
                    return _builder._errorResult(context.Context);
                }

                _builder._invoker.Execute(_builder._inputModel);

                return _builder._successResult?.Invoke(context.Context);
            }
        }
    }
    
    public class HandleActionBuilder<T, TRet> : IReturnActionResult
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private readonly List<ConditionResult<TRet>> _conditionResults = new List<ConditionResult<TRet>>();
        private Func<TRet, ActionContext, ActionResult> _successResult;
        private Func<ActionContext, ActionResult> _errorResult;

        public HandleActionBuilder(T inputModel, IInvoker invoker, Func<ActionContext, ActionResult> successResult, Func<ActionContext, ActionResult> errorResult)
        {
            _inputModel = inputModel;
            _invoker = invoker;
            _successResult = (_, x) => successResult(x);
            _errorResult = errorResult;
        }

        public HandleActionBuilder<T, TRet> On(Func<TRet, bool> condition, Func<TRet, ActionResult> result)
        {
            _conditionResults.Add(new ConditionResult<TRet>
            {
                Condition = (x, _) => condition(x),
                Result = (x, _) => result(x)
            });
            return this;
        }

        public HandleActionBuilder<T, TRet> On(Func<TRet, ActionContext, bool> condition, Func<TRet, ActionContext, ActionResult> result)
        {
            _conditionResults.Add(new ConditionResult<TRet>
            {
                Condition = condition,
                Result = result
            });
            return this;
        }

        public HandleActionBuilder<T, TRet> OnSuccess(Func<TRet, ActionResult> result)
        {
            _successResult = (x, cc) => result(x);
            return this;
        }

        public HandleActionBuilder<T, TRet> OnSuccess(Func<TRet, ActionContext, ActionResult> result)
        {
            _successResult = result;
            return this;
        }

        public HandleActionBuilder<T, TRet> OnModelError(Func<ActionResult> result)
        {
            _errorResult = _ => result();
            return this;
        }

        public HandleActionBuilder<T, TRet> OnModelError(Func<ActionContext, ActionResult> result)
        {
            _errorResult = result;
            return this;
        }

        public static implicit operator HandleActionResult(HandleActionBuilder<T, TRet> obj)
        {
            return new HandleActionResult(obj);
        }

        Func<ActionContext, ActionResult> IReturnActionResult.Result()
        {
            return x => new HandleActionResult(this).ExecuteResult(new SchoStackActionContext(x));
        }

        public class HandleActionResult : ActionResult, IExecuteResult
        {
            private readonly HandleActionBuilder<T, TRet> _builder;

            public HandleActionResult(HandleActionBuilder<T, TRet> builder)
            {
                _builder = builder;
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var result = ExecuteResult(new SchoStackActionContext(context));
                return result?.ExecuteResultAsync(context);
            }

            public ActionResult ExecuteResult(IActionContext context)
            {
                if (!context.IsValidModel && _builder._errorResult != null)
                {
                    return _builder._errorResult(context.Context);
                }

                var result = _builder._inputModel == null ? _builder._invoker.Execute<TRet>() : _builder._invoker.Execute<TRet>(_builder._inputModel);
                var conditionResult = _builder._conditionResults.FirstOrDefault(x => x.Condition(result, context.Context));
                if (conditionResult != null)
                {
                    return conditionResult.Result(result, context.Context);
                }
                return _builder._successResult?.Invoke(result, context.Context);
            }
        }
    }

    public interface IReturnActionResult
    {
        Func<ActionContext, ActionResult> Result();
    }

    public interface IExecuteResult
    {
        ActionResult ExecuteResult(IActionContext context);
    }

    public class ConditionResult<TRet>
    {
        public Func<TRet, ActionContext, bool> Condition { get; set; }
        public Func<TRet, ActionContext, ActionResult> Result { get; set; }
    }

    public interface IActionResultBuilder
    {
        ActionResult Build<T>(T input, Func<HandleActionBuilder<T>, IReturnActionResult> action);
        ActionResult Build(Func<HandleActionBuilder, IReturnActionResult> action);
    }

    public class ActionResultBuilder : IActionResultBuilder
    {
        private readonly IInvoker _invoker;
        private readonly IActionContextAccessor _actionContextAccessor;

        public ActionResultBuilder(IInvoker invoker, IActionContextAccessor actionContextAccessor)
        {
            _invoker = invoker;
            _actionContextAccessor = actionContextAccessor;
        }

        public ActionResult Build<T>(T input, Func<HandleActionBuilder<T>, IReturnActionResult> action)
        {
            var builder1 = new HandleActionBuilder<T>(input, _invoker);
            var result = action(builder1);
            return result.Result()(_actionContextAccessor.ActionContext);
        }

        public ActionResult Build(Func<HandleActionBuilder, IReturnActionResult> action)
        {
            var builder1 = new HandleActionBuilder(_invoker);
            var result = action(builder1);
            return result.Result()(_actionContextAccessor.ActionContext);
        }
    }
}