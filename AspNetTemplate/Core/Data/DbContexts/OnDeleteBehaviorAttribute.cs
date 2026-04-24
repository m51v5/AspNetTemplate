using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Core.Data.DbContexts
{
    // Note: use it with the navigators
    [AttributeUsage(AttributeTargets.Property)]
    public class OnDeleteBehaviorAttribute : Attribute
    {
        public DeleteBehavior DeleteBehavior { get; }
        public OnDeleteBehaviorAttribute(DeleteBehavior behavior)
        {
            DeleteBehavior = behavior;
        }
    }

}
