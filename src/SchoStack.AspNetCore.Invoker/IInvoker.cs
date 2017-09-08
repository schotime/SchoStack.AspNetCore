namespace SchoStack.AspNetCore.Invoker
{
    public interface IInvoker
    {
        TOutput Execute<TOutput>(object inputModel);
        TOutput Execute<TOutput>();
        void Execute(object inputModel);
    }
}