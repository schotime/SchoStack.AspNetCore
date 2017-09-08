namespace SchoStack.AspNetCore.Invoker
{
    public interface IHandler<TInput, TOutput>
    {
        TOutput Handle(TInput input);
    }

    public interface IHandler<TOutput>
    {
        TOutput Handle();
    }
}