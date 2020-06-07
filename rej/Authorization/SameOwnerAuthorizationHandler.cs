using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using rej.Data;
using rej.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace rej.Authorization
{
    public class SameOwnerAuthorizationHandler : AuthorizationHandler<SameOwnerRequirement, Person>
    {
        ApplicationDbContext _dbContext;

        public SameOwnerAuthorizationHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameOwnerRequirement requirement, Person resource)
        {
            if (context.User == null)
            {
                return Task.CompletedTask;
            }
            if (context.User.IsInRole(Constants.ADMINISTRATOR_ROLE))
            {
                context.Succeed(requirement);
            }
            else if(resource.OwnerId == QueryForOwnerId(context))
            {
                context.Succeed(requirement);
            }
            
            return Task.CompletedTask;
        }

        private int QueryForOwnerId(AuthorizationHandlerContext context)
        {
            Models.User owner = _dbContext.Users.Where(b => b.Email.Equals(context.User.Identity.Name)).FirstOrDefault();
            return owner.Id;
        }
    }
}
public class SameOwnerRequirement : IAuthorizationRequirement { }