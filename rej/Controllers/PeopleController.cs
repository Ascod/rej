using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using rej.Data;
using rej.Models;
using rej.ModelViews;

namespace rej.Controllers
{
    public class PeopleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PeopleController(ApplicationDbContext context, IAuthorizationService authorizationService, IWebHostEnvironment environment)
        {
            _context = context;
            _authorizationService = authorizationService;
            _webHostEnvironment = environment;
        }

        // GET: People
        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            ViewData["LocationSortParm"] = String.IsNullOrEmpty(sortOrder) || sortOrder=="location_asc" ? "location_desc" : "location_asc";
            ViewData["SexSortParm"] = sortOrder == "sex_desc" ? "sex_asc" : "sex_desc";
            ViewData["CurrentFilter"] = searchString;
            var applicationDbContext = _context.People.Include(p => p.Owner);
            var people = from ppl in applicationDbContext select ppl;
            if (!String.IsNullOrEmpty(searchString))
            {
                people = people.Where(p => p.Name.Contains(searchString)
                                       || p.Surname.Contains(searchString)
                                       || p.LastSeenLocation.Contains(searchString)
                                       || p.Description.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "location_desc":
                    people = people.OrderByDescending(p => p.LastSeenLocation);
                    break;
                case "location_asc":
                    people = people.OrderBy(p => p.LastSeenLocation);
                    break;
                case "sex_desc":
                    people = people.OrderByDescending(p => p.IsWoman);
                    break;
                case "sex_asc":
                    people = people.OrderBy(p => p.IsWoman);
                    break;
            }
            return View(await people.AsNoTracking().ToListAsync());
        }

        // GET: People/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.People
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // GET: People/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: People/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(PersonViewModel personViewModel)
        {
            if (ModelState.IsValid)
            {
                Person person = new Person
                {
                    Name = personViewModel.Name,
                    Surname = personViewModel.Surname,
                    IsWoman = personViewModel.IsWoman,
                    Description = personViewModel.Description,
                    LastSeenLocation = personViewModel.LastSeenLocation,
                };
                person.OwnerId = QueryForOwnerId();
                person.Image = UploadImageFile(personViewModel.Image);
                _context.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        // GET: People/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var person = await _context.People.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            } else if (this.QueryForOwnerId() != person.OwnerId && ! User.IsInRole(Constants.ADMINISTRATOR_ROLE))
            {
                return Redirect($"/Identity/Account/AccessDenied");
            }
            if ((await _authorizationService.AuthorizeAsync(User, person, "EditPolicy")).Succeeded)
            {
                return View(person);
            }
            else
            {
                return new ChallengeResult();
            }
        }

        // POST: People/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Image,IsWoman,Surname,Name,Description,LastSeenLocation")] Person person)
        {
            if (id != person.Id)
            {
                return NotFound();
            }
            Person editedPerson = _context.People.Find(id);
            editedPerson.IsWoman = person.IsWoman;
            editedPerson.Surname = person.Surname;
            editedPerson.Name = person.Name;
            editedPerson.Description = person.Description;
            editedPerson.LastSeenLocation = person.LastSeenLocation;
            if (this.QueryForOwnerId() != editedPerson.OwnerId && !User.IsInRole(Constants.ADMINISTRATOR_ROLE))
            {
                return Redirect($"/Identity/Account/AccessDenied");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(editedPerson);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            if ((await _authorizationService.AuthorizeAsync(User, editedPerson, "EditPolicy")).Succeeded)
            {
                return View(person);
            }
            else
            {
                return new ChallengeResult();
            }
        }

        // GET: People/Delete/5
        [Authorize(Roles ="Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.People
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // POST: People/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var person = await _context.People.FindAsync(id);
            _context.People.Remove(person);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PersonExists(int id)
        {
            return _context.People.Any(e => e.Id == id);
        }

        private int QueryForOwnerId()
        {
            Models.User owner = _context.Users.Where(b => b.Email.Equals(this.User.Identity.Name)).FirstOrDefault();
            return owner.Id;
        }

        private string UploadImageFile(IFormFile image)
        {
            string uniqueFileName = null;

            if (image != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString()+ "_" + image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }
    }
}
