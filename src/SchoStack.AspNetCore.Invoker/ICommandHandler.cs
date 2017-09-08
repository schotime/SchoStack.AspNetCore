namespace SchoStack.AspNetCore.Invoker
{
    public interface ICommandHandler<TInput>
    {
        void Handle(TInput command);
    }
}