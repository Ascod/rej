using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace rej.Models
{
    public class User : IdentityUser<int>
    {
        [PersonalData]
        public ICollection<Person> Entries { get; set; }
    }
}
