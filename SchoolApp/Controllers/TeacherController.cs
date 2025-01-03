using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using SchoolApp.Data;
using SchoolApp.Data.Migrations;
using SchoolApp.Interfaces;
using SchoolApp.Models;
using SchoolApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SchoolApp.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPhotoService _photoService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IPhotoService photoService, IWebHostEnvironment environment, ILogger<TeacherController> logger)
        {
            _context = context;
            _userManager = userManager;
            _photoService = photoService;
            _environment = environment;
            _logger = logger;
        }
        public IActionResult Index()
        {
            var mostRecentAnnouncement = _context.Announcements.Include(a => a.AppUser)
               .OrderByDescending(item => item.PostDate)
               .Take(3) // Retrieve the top 3 most recent announcements
               .ToList();
            if (mostRecentAnnouncement == null)
            {
                ViewBag.MostRecentAnnouncement = null;
            }
            ViewBag.MostRecentAnnouncement = mostRecentAnnouncement;
            var mostRecentProject = _context.Projects
            .OrderByDescending(item => item.DateCompleted)
            .Take(3) // Retrieve the top 3 most recent announcements
            .ToList();
            ViewBag.MostRecentProject = mostRecentProject;
            return View();
        }
        public async Task<IActionResult> AnnouncementIndex(string searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            
            if (_context == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database context is null.");
            }
            var myAnnouncement = from s in _context.Announcements
                           select s;                      
            if (!String.IsNullOrEmpty(searchString))
            {
                myAnnouncement = myAnnouncement.Where(s => s.AnnouncementTitle!.Contains(searchString) ||
                                                           s.AnnouncementDescription != null && s.AnnouncementDescription.Contains(searchString) ||
                                                           s.AppUser != null && s.AppUser.FirstName != null && s.AppUser.FirstName.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "AnnouncementTitle":
                    myAnnouncement = myAnnouncement.OrderBy(p => p.AnnouncementTitle);
                    break;
                case "PostDate":
                    myAnnouncement = myAnnouncement.OrderBy(p => p.PostDate);
                    break;
                default:
                    break;
            }
            return View(myAnnouncement
            .Where(announcement => announcement.AnnTearcherId == userId)
            .ToList());        
        }
        public IActionResult CreateAnnouncement()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(Announcement announcementr)
        {            
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;

            if (ModelState.IsValid)
            {
                if (announcementr == null)
                {
                    NotFound();
                }                
                var announce = new Announcement
                    {
                        AnnouncementTitle = announcementr.AnnouncementTitle,
                        AnnouncementDescription = announcementr?.AnnouncementDescription,
                        PostDate = DateTime.Now,                        
                        AnnTearcherId = userId,
                    };
                
                if (announcementr.ImageFile != null)
                {
                    string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                    string uniqueName = Guid.NewGuid() + "-" + announcementr.ImageFile.FileName;
                    string filepath = Path.Combine(uploadedto, uniqueName);
                    await announcementr.ImageFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                    announce.ImageUrl = announcementr.ImageUrl = "Images/" + uniqueName;
                }
                if (announcementr.AnnouncementFile != null)
                {
                    string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                    string uniqueName = Guid.NewGuid() + "-" + announcementr.AnnouncementFile.FileName;
                    string filepath = Path.Combine(uploadedto, uniqueName);
                    await announcementr.AnnouncementFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                    announce.AnnouncementDocFile = announcementr.AnnouncementDocFile = "Images/" + uniqueName;
                }
                _context.Add(announce);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("AnnouncementIndex");              
            }            
            return View(announcementr);
        }
        public async Task<IActionResult> AnnouncementEdit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Announcements == null)
            {
                return NotFound();
            }
            var announcement = await _context.Announcements.FindAsync(id);
            
            if (announcement == null || announcement.AnnTearcherId != userId)
            {
                return NotFound();
            }
            return View(announcement);
        }        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnouncementEdit(int id, Announcement annocuncement)
        {
            if (id != annocuncement.AnnouncementId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                var data = _context.Announcements.FirstOrDefault(x => x.AnnouncementId == id);                                
                if (data != null)
                {                    
                    data.AnnouncementTitle = annocuncement.AnnouncementTitle;
                    data.AnnouncementDescription = annocuncement.AnnouncementDescription;
                    data.PostDate = annocuncement.PostDate = DateTime.Now;
                    //data.AnnouncementPhoto = result != null ? result.Url?.ToString() : null;
                    if (annocuncement.ImageFile != null)
                    {
                        string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                        string uniqueName = Guid.NewGuid() + "-" + annocuncement.ImageFile.FileName;
                        string filepath = Path.Combine(uploadedto, uniqueName);
                        await annocuncement.ImageFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                        data.ImageUrl = annocuncement.ImageUrl = "Images/" + uniqueName;
                    }
                    if (annocuncement.AnnouncementFile != null)
                    {
                        string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                        string uniqueName = Guid.NewGuid() + "-" + annocuncement.AnnouncementFile.FileName;
                        string filepath = Path.Combine(uploadedto, uniqueName);
                        await annocuncement.AnnouncementFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                        data.AnnouncementDocFile = annocuncement.AnnouncementDocFile = "Images/" + uniqueName;
                    }                   
                }               
                await _context.SaveChangesAsync();                
                return RedirectToAction(nameof(AnnouncementIndex));
            }
            return View(annocuncement);
        }
        public async Task<IActionResult> AnnouncementDelete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Announcements == null)
            {
                return NotFound();
            }
            var announcement = await _context.Announcements
                .FindAsync(id);
            if (announcement == null || announcement.AnnTearcherId != userId)
            {
                return NotFound();
            }
            return View(announcement);
        }
        [HttpPost, ActionName("AnnouncementDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnouncementDeleteConfirmed(int id)
        {
            if (_context.Announcements == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Announcement' is null.");
            }
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AnnouncementIndex));
        }
        public async Task<IActionResult> CourseIndex(string searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;

            var courses = from m in _context.Courses
                         select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(s => s.CourseName!.Contains(searchString) ||
                                             s.CourseDescription!.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "CourseName":
                    courses = courses.OrderBy(p => p.CourseName);
                    break;
                case "StartDate":
                    courses = courses.OrderBy(p => p.StartDate);
                    break;
                case "EndDate":
                    courses = courses.OrderBy(p => p.EndDate);
                    break;
                default:
                    break;
            }
            return View(courses.Where(course => course.CTearcherId == userId)
            .ToList());
        }
        public IActionResult CreateCourse()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (!ModelState.IsValid)
            {
                return View(course);
            }
            if (course.CourseFile != null)
            {
                string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                string uniqueName = Guid.NewGuid() + "-" + course.CourseFile.FileName;
                string filepath = Path.Combine(uploadedto, uniqueName);
                await course.CourseFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                course.CourseDocFile = "Images/" + uniqueName;
            }
            
            course.StartDate = DateTime.Now;
            
            if (course.EndDate <= course.StartDate)
            {
                ModelState.AddModelError("EndDate", "Please select a date-time in the future.");
                return View(course);
            }
            course.CTearcherId = userId;
            _context.Add(course);
            await _context.SaveChangesAsync();
            return RedirectToAction("CourseIndex");
        }
        public async Task<IActionResult> CourseEdit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.CTearcherId != userId)
            {
                return NotFound();
            }
            return View(course);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseEdit(int id,Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var data = _context.Courses.FirstOrDefault(x => x.CourseId == id);

                    // Checking if any such record exist
                    if (data != null)
                    {
                        
                        data.CourseName = course.CourseName;
                        data.CourseDescription = course.CourseDescription;
                        
                        if (course.EndDate <= data.StartDate )
                        {
                            ModelState.AddModelError("EndDate", $"Please select a date-time after {data.StartDate}");
                            return View(course);
                        }
                        data.EndDate = course.EndDate;
                        if (course.CourseFile != null)
                        {
                            string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                            string uniqueName = Guid.NewGuid() + "-" + course.CourseFile.FileName;
                            string filepath = Path.Combine(uploadedto, uniqueName);
                            await course.CourseFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                            data.CourseDocFile = course.CourseDocFile = "Images/" + uniqueName;
                        }
                        await _context.SaveChangesAsync();
                    }
                        
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(CourseIndex));
            }
            return View(course);
        }
        public async Task<IActionResult> CourseDelete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }
            var course = await _context.Courses
                .FindAsync(id);
            if (course == null || course.CTearcherId != userId)
            {
                return NotFound();
            }
            return View(course);
        }
        [HttpPost, ActionName("CourseDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseDeleteConfirmed(int id)
        {
            if (_context.Courses == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Course' is null.");
            }
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CourseIndex));
        }
        public async Task<IActionResult> AssignmentIndex(string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            
            var myAssignment = from s in _context.Assignments
                               select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                myAssignment = myAssignment.Where(s => s.AssignmentTitle != null && s.AssignmentTitle.Contains(searchString) ||
                                                       s.AssignmentDescription != null && s.AssignmentDescription.Contains(searchString) ||
                                                       s.Course != null && s.Course.CourseName != null && s.Course.CourseName.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "AssignmentTitle":
                    myAssignment = myAssignment.OrderBy(p => p.AssignmentTitle);
                    break;
                case "Deadline":
                    myAssignment = myAssignment.OrderBy(p => p.Deadline);
                    break;
                default:
                    break;
            }
            return View(await myAssignment.Where(assignment => assignment.ATearcherId == userId)           
            .Include(a => a.Course)
            .Select(a => new
            {
                a.AssignmentId,
                a.AssignmentTitle,
                a.AssignmentDescription,
                a.Deadline,
                a.AssignmentDocFile,
                CourseName = a.Course != null ? a.Course.CourseName : null,
            })
            .ToListAsync());
        }
        public async Task<IActionResult> CreateAssignment()
        {            
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;

            List<Course> courses = _context.Courses.Where(a=> a.CTearcherId == userId).ToList();
            courses.Insert(0, new Course { CourseId = 0, CourseName = "--Select Course Name--" });
            ViewBag.message = new SelectList(courses, "CourseId", "CourseName");
            TempData["CourseList"] = new SelectList(courses, "CourseId", "CourseName");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(Assignment assignment)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (!ModelState.IsValid)
            {
                await CreateAssignment();
                return View(assignment);                
            }            
            if (assignment.AssignmentFile != null)
            {
                string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                string uniqueName = Guid.NewGuid() + "-" + assignment.AssignmentFile.FileName;
                string filepath = Path.Combine(uploadedto, uniqueName);
                await assignment.AssignmentFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                assignment.AssignmentDocFile = "Images/" + uniqueName;
            }
            
            DateTime currentDateTime = DateTime.Now;
            if (assignment.Deadline < currentDateTime)
            {
                ModelState.AddModelError("Deadline", "Please select a date-time in the future.");
                await CreateAssignment();
                return View(assignment);
            }
            if (assignment.CourseId <= 0)
            {
                // Add a server-side validation error
                ModelState.AddModelError("CourseId", "Course Name is required.");

                // Repopulate the SelectList and return to the form
                await CreateAssignment();
                return View(assignment);
            }
            assignment.ATearcherId = userId;
            _context.Add(assignment);
            await _context.SaveChangesAsync();
            return RedirectToAction("AssignmentIndex");
        }
        public async Task<IActionResult> AssignmentEdit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Assignments == null)
            {
                return NotFound();
            }
            var assignment = await _context.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);
            if (assignment == null || assignment.ATearcherId != userId)
            {
                return NotFound();
            }
            var courseNames = await _context.Courses.Where(a => a.CTearcherId == userId)
                .Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = c.CourseName
                })
                .ToListAsync();
            ViewBag.CourseNames = courseNames;
            return View(assignment);                       
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignmentEdit(int id, Assignment assignment)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id != assignment.AssignmentId)
            {
                return NotFound();
            }                        
            try
            {
                var data = _context.Assignments.FirstOrDefault(x => x.AssignmentId == id);
                DateTime currentDateTime = DateTime.Now;

                // Checking if any such record exist
                if (data != null)
                {                    
                    data.AssignmentTitle = assignment.AssignmentTitle;
                    data.AssignmentDescription = assignment.AssignmentDescription;
                    if (assignment.Deadline < currentDateTime)
                    {
                        ModelState.AddModelError("Deadline", "Please select a date-time in the future.");
                        await AssignmentEdit(id);                        
                        return View(assignment);
                    }
                    data.Deadline = assignment.Deadline;
                    if (assignment.CourseId == null)
                    {
                        // Add a server-side validation error
                        ModelState.AddModelError("CourseId", "Course Name is required.");

                        // Repopulate the SelectList and return to the form
                        await AssignmentEdit(id);
                        return View(assignment);
                    }
                    if (!ModelState.IsValid)
                    {
                        await AssignmentEdit(id);
                        return View(assignment);
                    }
                    if (assignment.AssignmentFile != null)
                    {
                        string uploadedto = Path.Combine(_environment.WebRootPath, "Images");
                        string uniqueName = Guid.NewGuid() + "-" + assignment.AssignmentFile.FileName;
                        string filepath = Path.Combine(uploadedto, uniqueName);
                        await assignment.AssignmentFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                        data.AssignmentDocFile = assignment.AssignmentDocFile = "Images/" + uniqueName;
                    }
                    await AssignmentEdit(id);
                    
                    data.CourseId = assignment.CourseId;
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(assignment.AssignmentId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(AssignmentIndex));            
        }
        public async Task<IActionResult> AssignmentDelete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null)
            {
                return NotFound();
            }
            var assignment = _context.Assignments
            .Include(a => a.Course)
            .FirstOrDefault(a => a.AssignmentId == id);
            if (assignment == null || assignment.ATearcherId != userId)
            {
                return NotFound();
            }

            return View(assignment);
        }
        [HttpPost, ActionName("AssignmentDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignmentDeleteConfirmed(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
            }
            else
            {
                return Problem("Entity set is null.");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AssignmentIndex));
        }
        public async Task<IActionResult> ListOfEnrollements(string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacherId = user.Id;
            var coursesCreated = from m in _context.Enrollments
                          select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                coursesCreated = coursesCreated.Where(s => s.Course != null && s.Course.CourseName != null && s.Course.CourseName.Contains(searchString) ||
                                              s.AppUser != null && s.AppUser.UserName != null && s.AppUser.UserName.Contains(searchString) ||
                                              s.AppUser != null && s.AppUser.FirstName != null && s.AppUser.FirstName.Contains(searchString) ||
                                              s.AppUser != null && s.AppUser.LastName != null && s.AppUser.LastName.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "UserName":
                    coursesCreated = coursesCreated.OrderBy(p => p.AppUser.UserName);
                    break;
                case "FirstName":
                    coursesCreated = coursesCreated.OrderBy(p => p.AppUser.FirstName);
                    break;
                case "LastName":
                    coursesCreated = coursesCreated.OrderBy(p => p.AppUser.LastName);
                    break;
                default:
                    break;
            }
            return View(coursesCreated.Where(a =>  a.Course != null && a.Course.CTearcherId == teacherId)
                .Include(a => a.Course)
                .Include(a => a.AppUser).ToList());
        }
        public async Task<IActionResult> ListOfAssignSubmissions(int? id, string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacherId = user.Id;
            var submissionsQuery = _context.AssignmentSubmissions
        .Include(submission => submission.AppUser)
        .Include(submission => submission.Assignment)
        .Where(submission => submission.AssignmentId == id);

            // Apply search filter if searchString is provided
            if (!string.IsNullOrEmpty(searchString))
            {
                submissionsQuery = submissionsQuery
                    .Where(submission =>
                        submission.AppUser != null && submission.AppUser.UserName != null && submission.AppUser.UserName.Contains(searchString) ||
                        submission.SubmissionText != null && submission.SubmissionText.Contains(searchString));
            }
            var submissions = await submissionsQuery.ToListAsync();
            foreach (var submission in submissions)
            {
                if (submission.Assignment != null && submission.Assignment.ATearcherId != teacherId)
                {
                    return NotFound();
                }
            }
            switch (sortOrder)
            {
                case "UserName":
                    submissionsQuery = submissionsQuery.OrderBy(p => p.AppUser.UserName);
                    break;
                case "FirstName":
                    submissionsQuery = submissionsQuery.OrderBy(p => p.AppUser.FirstName);
                    break;
                case "LastName":
                    submissionsQuery = submissionsQuery.OrderBy(p => p.AppUser.LastName);
                    break;
                default:
                    break;
            }
            return View(submissions);
        }
        public async Task<IActionResult> Evaluate(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.AssignmentSubmissions == null)
            {
                return NotFound();
            }
            var submission = await _context.AssignmentSubmissions.FindAsync(id);

            if (submission == null)
            {
                return NotFound();
            }
            return View(submission);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluate(int id, AssignmentSubmission assignmentSubmission)
        {
            if (id != assignmentSubmission.SubmissionId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                var data = _context.AssignmentSubmissions.FirstOrDefault(x => x.SubmissionId == id);
                if (data != null)
                {
                    data.score = assignmentSubmission.score;
                    _context.AssignmentSubmissions.Update(data);
                    await _context.SaveChangesAsync();
                }
                //return RedirectToAction("ListOfAssignSubmissions");
                return RedirectToAction(nameof(ListOfAssignSubmissions), new { id = data.AssignmentId });
            }
            return View();
        }
        public async Task<IActionResult> RemoveStudent(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null)
            {
                return NotFound();
            }
            var student = _context.Enrollments
            .Include(a => a.Course).Include(a => a.AppUser)
            .FirstOrDefault(a => a.EnrollmentId == id);
            if (student == null || student.Course.CTearcherId != userId)
            {
                return NotFound();
            }
            return View(student);
        }
        [HttpPost, ActionName("RemoveStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudentConfirmed(int id)
        {
            var student = await _context.Enrollments.FindAsync(id);
            if (student != null)
            {
                _context.Enrollments.Remove(student);
            }
            else
            {
                return Problem("Entity set is null.");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListOfEnrollements));
        }
        
        private bool AnnouncementExists(int id)
        {
            return (_context.Announcements?.Any(e => e.AnnouncementId == id)).GetValueOrDefault();
        }
        private bool CourseExists(int id)
        {
            return (_context.Courses?.Any(e => e.CourseId == id)).GetValueOrDefault();
        }
        private bool AssignmentExists(int id)
        {
            return (_context.Assignments?.Any(e => e.AssignmentId == id)).GetValueOrDefault();
        }
    }
}
