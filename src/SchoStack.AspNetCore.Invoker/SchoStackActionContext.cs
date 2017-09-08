using Microsoft.AspNetCore.Mvc;

namespace SchoStack.AspNetCore.Invoker
{
    public interface IActionContext
    {
        bool IsValidModel { get; }
        ActionContext Context { get; }
    }

    public class SchoStackActionContext : IActionContext
    {
        private readonly ActionContext _context;

        public SchoStackActionContext(ActionContext context)
        {
            _context = context;
        }

        public ActionContext Context { get { return _context; } }

        public bool IsValidModel
        {
            get
            {
                return _context.ModelState.IsValid;
            }
        }
    }
}