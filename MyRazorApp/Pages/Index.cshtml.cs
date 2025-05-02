using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Data;
using Microsoft.EntityFrameworkCore;
using MyRazorApp.Helpers;
using System.Text;

namespace MyRazorApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SchoolDbContext _context;

        public IndexModel(SchoolDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ClassInformationModel NewClass { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty]
        public string? SelectedColumns { get; set; }

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public List<ClassInformationModel> FilteredClasses { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!IsAuthenticated())
                return RedirectToPage("/Login");

            ApplyFilteringAndPaging();
            return Page();
        }

        private void ApplyFilteringAndPaging()
        {
            var query = _context.Classes.Where(c => c.IsActive).AsQueryable(); 

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var lowerSearch = SearchTerm?.ToLower() ?? string.Empty;
                query = query.Where(c =>
                    c.ClassName.ToLower().Contains(lowerSearch) ||
                    c.Description.ToLower().Contains(lowerSearch) ||
                    c.StudentCount.ToString().Contains(lowerSearch));
            }

            TotalPages = (int)Math.Ceiling(query.Count() / (double)PageSize);

            FilteredClasses = query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new ClassInformationModel
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    StudentCount = c.StudentCount,
                    Description = c.Description
                }).ToList();
        }

        public IActionResult OnPostAdd()
        {
            if (!IsAuthenticated()) return RedirectToPage("/Login");

            if (NewClass == null || !ModelState.IsValid)
                return Page();

            var classToAdd = new Class
            {
                ClassName = NewClass.ClassName,
                StudentCount = NewClass.StudentCount,
                Description = NewClass.Description,
                IsActive = true
            };

            _context.Classes.Add(classToAdd);
            _context.SaveChanges();

            NewClass = new();
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            if (!IsAuthenticated()) return RedirectToPage("/Login");
            var classToDelete = _context.Classes.Find(id);
            if (classToDelete != null)
            {
                classToDelete.IsActive = false;
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostEdit(int id)
        {
            if (!IsAuthenticated()) return RedirectToPage("/Login");

            var classToEdit = _context.Classes.Find(id);
            if (classToEdit != null && classToEdit.IsActive)
            {
                NewClass = new ClassInformationModel
                {
                    Id = classToEdit.Id,
                    ClassName = classToEdit.ClassName,
                    StudentCount = classToEdit.StudentCount,
                    Description = classToEdit.Description
                };
            }

            ApplyFilteringAndPaging();
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            if (!IsAuthenticated()) return RedirectToPage("/Login");

            var classToUpdate = _context.Classes.Find(NewClass.Id);
            if (classToUpdate != null && classToUpdate.IsActive)
            {
                classToUpdate.ClassName = NewClass.ClassName;
                classToUpdate.StudentCount = NewClass.StudentCount;
                classToUpdate.Description = NewClass.Description;
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExportJson(bool isFiltered)
        {
            if (!IsAuthenticated()) return RedirectToPage("/Login");

            var columnIndexes = new List<int>();
            if (!string.IsNullOrWhiteSpace(SelectedColumns))
            {
                columnIndexes = SelectedColumns
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .ToList();
            }

            List<ClassInformationModel> dataToExport;

            if (isFiltered)
            {
                var query = _context.Classes.Where(c => c.IsActive).AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var lowerSearch = SearchTerm?.ToLower() ?? string.Empty;
                    query = query.Where(c =>
                        c.ClassName.ToLower().Contains(lowerSearch) ||
                        c.Description.ToLower().Contains(lowerSearch) ||
                        c.StudentCount.ToString().Contains(lowerSearch));
                }

                dataToExport = query
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .Select(c => new ClassInformationModel
                    {
                        Id = c.Id,
                        ClassName = c.ClassName,
                        StudentCount = c.StudentCount,
                        Description = c.Description
                    }).ToList();
            }
            else
            {
                dataToExport = _context.Classes
                    .Where(c => c.IsActive)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .Select(c => new ClassInformationModel
                    {
                        Id = c.Id,
                        ClassName = c.ClassName,
                        StudentCount = c.StudentCount,
                        Description = c.Description
                    }).ToList();
            }

            var reducedData = dataToExport
                .Where(item => item != null)
                .Select(item =>
                {
                    var dict = new Dictionary<string, object>();
                    if (columnIndexes.Count == 0 || columnIndexes.Contains(0))
                        dict["ClassName"] = item.ClassName;
                    if (columnIndexes.Count == 0 || columnIndexes.Contains(1))
                        dict["StudentCount"] = item.StudentCount;
                    if (columnIndexes.Count == 0 || columnIndexes.Contains(2))
                        dict["Description"] = item.Description;
                    return dict;
                }).ToList();

            var json = Util.Instance.SerializeToJson(reducedData);
            var fileName = isFiltered ? "filtered_classes.json" : "all_classes.json";
            var fileBytes = Encoding.UTF8.GetBytes(json);
            return File(fileBytes, "application/json", fileName);
        }

        public IActionResult OnPostLogout()
        {
            Response.Cookies.Delete("username");
            Response.Cookies.Delete("token");
            Response.Cookies.Delete("session_id");

            HttpContext.Session.Clear();

            return RedirectToPage("/Login");
        }

        private bool IsAuthenticated()
        {
            var sessionUsername = HttpContext.Session.GetString("username") ?? string.Empty;
            var sessionToken = HttpContext.Session.GetString("token") ?? string.Empty;
            var sessionId = HttpContext.Session.GetString("session_id") ?? string.Empty;

            var cookieUsername = Request.Cookies["username"] ?? string.Empty;
            var cookieToken = Request.Cookies["token"] ?? string.Empty;
            var cookieSessionId = Request.Cookies["session_id"] ?? string.Empty;

            return sessionUsername == cookieUsername && sessionToken == cookieToken && sessionId == cookieSessionId;
        }
    }
}
