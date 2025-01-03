using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SchoolApp.Data;
using SchoolApp.Data.Migrations;
using SchoolApp.Models;
using System;
using System.Security.Claims;



namespace SchoolApp.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;       
        private readonly IWebHostEnvironment _hostEnvironment;
        public StudentController(ApplicationDbContext context, UserManager<IdentityUser> userManager,IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }
        public async Task<IActionResult> ListOfNotes(string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var mynotes = from s in _context.MNotes
                            select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                mynotes = mynotes.Where(s => s.Title != null && s.Title.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "Title":
                    mynotes = mynotes.OrderBy(p => p.Title);
                    break;                
                default:
                    break;
            }
            return View(mynotes.Where(project => project.StudentId == userId)
            .ToList());           
        }
        public IActionResult MyNote()
        {            
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyNote(MNote note)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            
            if (ModelState.IsValid)
            {
                note.CreatedDate = DateTime.Now;
                note.StudentId = userId;
                _context.Add(note);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ListOfNotes));
            }
            return View(note);
        }
        public async Task<IActionResult> NoteEdit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }
            var note = await _context.MNotes.FindAsync(id);
            if (note == null || note.StudentId != userId)
            {
                return NotFound();
            }            
            return View(note);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NoteEdit(int id, MNote note)
        {
            if (id != note.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var data = _context.MNotes.FirstOrDefault(x => x.Id == id);
                    if (data != null)
                    {
                        
                        data.Title = note.Title;
                        data.Content = note.Content;
                        data.CreatedDate = DateTime.Now;    
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExit(note.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ListOfNotes));
            }
            return View(note);
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            ViewBag.UserId = userId;
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
        public async Task<IActionResult> ProjectIndex(string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            IQueryable<Project> projects = _context.Projects; // Replace with your entity set

            var myproject = from s in _context.Projects
                                 select s;            
            if (!String.IsNullOrEmpty(searchString))
            {
                myproject = myproject.Where(s => s.ProjectTitle != null && s.ProjectTitle.Contains(searchString) ||
                                                 s.ProjectDescription != null && s.ProjectDescription.Contains(searchString) ||
                                                 s.ProjectArea != null && s.ProjectArea.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "ProjectTitle":
                    myproject = myproject.OrderBy(p => p.ProjectTitle);
                    break;
                case "DateCompleted":
                    myproject = myproject.OrderBy(p => p.DateCompleted);
                    break;
                default:
                    break;
            }
            return View(myproject.Where(project => project.StudentId == userId)
            .ToList());
        }
        public IActionResult CreateProject()
        {            
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project project)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (ModelState.IsValid)
            {
                if (project.ImageFile != null)
                {
                    string uploadedto = Path.Combine(_hostEnvironment.WebRootPath, "Images");
                    string uniqueName = Guid.NewGuid() + "-" + project.ImageFile.FileName;
                    string filepath = Path.Combine(uploadedto, uniqueName);
                    await project.ImageFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                    project.ImageUrl = "Images/" + uniqueName;
                }
                if (project.PhotoFile != null)
                {
                    string uploadedto = Path.Combine(_hostEnvironment.WebRootPath, "Images");
                    string uniqueName = Guid.NewGuid() + "-" + project.PhotoFile.FileName;
                    string filepath = Path.Combine(uploadedto, uniqueName);
                    await project.PhotoFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                    project.MediaUrl = "Images/" + uniqueName;
                }
                DateTime currentDateTime = DateTime.Now;
                if (project.DateCompleted < currentDateTime)
                {
                    ModelState.AddModelError("DateCompleted", "Please select a date-time in the future.");
                    return View(project);
                }
                project.StudentId = userId;
                var originalTitle = project.ProjectTitle;
                int counter = 1;

                while (_context.Projects.Any(x => x.ProjectTitle == project.ProjectTitle))
                {
                    // Note with the same title already exists; regenerate the title
                    project.ProjectTitle = $"{originalTitle} - {counter}";
                    counter++;
                }
                _context.Add(project);
                await _context.SaveChangesAsync();

                return RedirectToAction("ProjectIndex");
            }
            return View(project);
        }
        public async Task<IActionResult> ProjectEdit(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }
            var project = await _context.Projects.FindAsync(id);
            if(project == null || project.StudentId != userId)
            {
                return NotFound();
            }            
            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProjectEdit(int id, Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try { 
                    var data = _context.Projects.FirstOrDefault(x => x.ProjectId == id);                
                    if (data != null)
                    {
                        var originalTitle = project.ProjectTitle;
                        int counter = 1;

                        while (_context.Projects.Any(x => x.ProjectId != id && x.ProjectTitle == project.ProjectTitle))
                        {
                            // Note with the same title already exists; regenerate the title
                            project.ProjectTitle = $"{originalTitle} - {counter}";
                            counter++;
                        }
                        data.ProjectTitle = project.ProjectTitle;
                        data.ProjectDescription = project.ProjectDescription;
                        DateTime currentDateTime = DateTime.Now;
                        if (project.DateCompleted < currentDateTime)
                        {
                            ModelState.AddModelError("DateCompleted", "Please select a date-time in the future.");                           
                            return View(project);
                        }
                        data.DateCompleted = project.DateCompleted;
                        //data.AnnouncementPhoto = result != null ? result.Url?.ToString() : null;
                        if (project.PhotoFile != null)
                        {
                            string uploadedto = Path.Combine(_hostEnvironment.WebRootPath, "Images");
                            string uniqueName = Guid.NewGuid() + "-" + project.PhotoFile.FileName;
                            string filepath = Path.Combine(uploadedto, uniqueName);
                            await project.PhotoFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                            data.MediaUrl = project.MediaUrl = "Images/" + uniqueName;
                        }
                        await _context.SaveChangesAsync();
                    }                                             
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExit(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ProjectIndex));
            }
            return View(project);
        }
        public async Task<IActionResult> ProjectDelete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }
            var project = await _context.Projects
                .FindAsync(id);
            if (project == null || project.StudentId != userId)
            {
                return NotFound();
            }            
            return View(project);
        }
        [HttpPost, ActionName("ProjectDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProjectDeleteConfirmation(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Project' is null.");
            }
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ProjectIndex));
        }
        public async Task<IActionResult> ListOfCourses(string? searchString, string sortOrder)
        {           
            var courses = from s in _context.Courses
                                 select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(s => s.CourseName != null && s.CourseName.Contains(searchString) ||
                                             s.CourseDescription != null && s.CourseDescription.Contains(searchString));
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
            return View(await courses.Include(a=>a.AppUser).ToListAsync());
        }
        public async Task<IActionResult> ListOfEnrollments(string? searchString, string sortOrder)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var enrolled = from s in _context.Enrollments
                          select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                enrolled = enrolled.Where(s =>
                            (s.Course != null && s.Course.CourseName != null && s.Course.CourseName.Contains(searchString)) ||
                            (s.AppUser != null && s.AppUser.UserName != null && s.AppUser.UserName.Contains(searchString)));
            }
            switch (sortOrder)
            {
                case "CourseName":
                    enrolled = enrolled.OrderBy(p => p.Course.CourseName);
                    break;
                case "StartDate":
                    enrolled = enrolled.OrderBy(p => p.Course.StartDate);
                    break;
                case "EndDate":
                    enrolled = enrolled.OrderBy(p => p.Course.EndDate);
                    break;
                default:
                    break;
            }
            return View(enrolled.Where(project => project.StudentId == userId).Include(a => a.Course)
            .ToList());
        }
        public async Task<IActionResult> EnrollInACourse(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }
            var course = await _context.Courses
                .FindAsync(id);            
            if (course == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            ViewBag.UserId = userId;
            return View(course);            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollInACourse(Enrollment enrollment, int id)
        { 
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;            
            var enroll = new Enrollment
            {
                CourseId = id,
                StudentId = userId,
                IsEnrolled = true
            };
            
            if (!ModelState.IsValid )
            {
                return View(enrollment);
            }
            _context.Add(enroll);
            await _context.SaveChangesAsync();
            return RedirectToAction("ListOfEnrollments"); 
        }
        public async Task<IActionResult> EnrollmentDelete(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            if (id == null)
            {
                return NotFound();
            }
            var enrollment = _context.Enrollments
            .Include(a => a.Course)
            .FirstOrDefault(a => a.EnrollmentId == id);
            if (enrollment == null || enrollment.StudentId != userId)
            {
                return NotFound();
            }
            return View(enrollment);
        }
        [HttpPost, ActionName("EnrollmentDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollmentDeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
            }
            else
            {
                return Problem("Entity set is null.");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ListOfAssignments(string? searchString)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;            
            var enrolledCourses = _context.Enrollments
                .Where(e => e.StudentId == userId && e.Course != null && e.Course.Assignments != null)
                .SelectMany(e => e.Course.Assignments)
                .Include(course => course.Course)
                .ToList();
            //var course = _context.Courses.ToListAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                enrolledCourses = enrolledCourses
                    .Where(a => a.AssignmentTitle != null && a.AssignmentTitle.Contains(searchString) ||
                                a.AssignmentDescription != null && a.AssignmentDescription.Contains(searchString) ||
                                a.Course != null && a.Course.CourseName != null && a.Course.CourseName.Contains(searchString))
                                .ToList();
            }
            return View(enrolledCourses);
        }
        public async Task<IActionResult> SubmitAssignment(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            bool flag = false;
            if (id == null)
            {
                return NotFound();
            }
            var assignment = await _context.Assignments
        .Where(a => a.AssignmentId == id)
        .FirstOrDefaultAsync();


            var courseData = _context.Enrollments.Where(a => a.StudentId == userId).ToListAsync();
            foreach (var courser in await courseData)
            {
                if (assignment != null && assignment.CourseId == courser.CourseId)
                {
                    flag = true;
                }
            }
            if (flag == false)
            {
                return NotFound();
            }
            if (assignment == null)
            {
                return NotFound();
            }

            var submission = await _context.AssignmentSubmissions
                .Where(submission => submission.AssignmentId == id && submission.StudentId == userId)
                .FirstOrDefaultAsync();

            return View(Tuple.Create(assignment, submission));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(int? id,AssignmentSubmission assignmentSubmission, string? submissionText, IFormFile? AssignmentFile, int scores)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            ViewBag.IsAssignmentSubmitted = false;
            var submit = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = userId,
                IsSubmmited = true,
                SubmissionText = submissionText,
                SubmissionDate = DateTime.Now
            };
            
            if (id == null)
            {
                return NotFound();
            }
            
            if (AssignmentFile != null)
            {
                string uploadedto = Path.Combine(_hostEnvironment.WebRootPath, "Images");
                string uniqueName = Guid.NewGuid() + "-" + AssignmentFile.FileName;
                string filepath = Path.Combine(uploadedto, uniqueName);
                await AssignmentFile.CopyToAsync(new FileStream(filepath, FileMode.Create));
                submit.SubmissionDocFile = "Images/" + uniqueName;
            }
            if (string.IsNullOrWhiteSpace(submissionText) && AssignmentFile == null)
            {
                ModelState.AddModelError("SubmissionDate", "You must provide either a submission text or a file.");
            }
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(SubmitAssignment));
            }
            TempData["IsAssignmentSubmitted"] = true;
            _context.Add(submit);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(ListOfAssignments));
            
        }
        private bool ProjectExit(int id)
        {
            return (_context.Projects?.Any(e => e.ProjectId == id)).GetValueOrDefault();
        }
    }
    
}
