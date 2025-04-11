using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorApp.Models;
using MyRazorApp.Helpers;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MyRazorApp.Pages
{
    public class IndexModel : PageModel
    {
        public static List<ClassInformationModel> ClassList { get; set; } = new();
        public static int _idCounter = 1;

        [BindProperty]
        public ClassInformationModel NewClass { get; set; } = new ClassInformationModel();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public List<ClassInformationModel> FilteredClasses { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        [BindProperty]
        public string selectedColumns { get; set; } = string.Empty;

        public void OnGet()
        {
            if (ClassList.Count == 0)
            {
                GenerateDummyData();
            }

            var query = ClassList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var lowerSearch = SearchTerm.ToLower();
                query = query.Where(c =>
                    (!string.IsNullOrEmpty(c.ClassName) && c.ClassName.ToLower().Contains(lowerSearch)) ||
                    (!string.IsNullOrEmpty(c.Description) && c.Description.ToLower().Contains(lowerSearch)) ||
                    c.StudentCount.ToString().Contains(lowerSearch));
            }

            TotalPages = (int)Math.Ceiling(query.Count() / (double)PageSize);

            FilteredClasses = query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private void GenerateDummyData()
        {
            var random = new Random();
            string[] classNames = { "Math", "Science", "History", "Physics", "Chemistry", "Biology", "Music", "Art", "Computer Science", "English" };

            for (int i = 1; i <= 100; i++)
            {
                ClassList.Add(new ClassInformationModel
                {
                    Id = _idCounter++,
                    ClassName = classNames[random.Next(classNames.Length)] + $" {i}",
                    StudentCount = random.Next(1, 101),
                    Description = $"This is a description part for class {i}."
                });
            }
        }

        public IActionResult OnPostAdd()
        {
            if (NewClass == null)
                return Page();

            if (NewClass.Id == 0)
            {
                NewClass.Id = _idCounter++;
                ClassList.Add(new ClassInformationModel
                {
                    Id = NewClass.Id,
                    ClassName = NewClass.ClassName,
                    StudentCount = NewClass.StudentCount,
                    Description = NewClass.Description
                });
            }
            else
            {
                var existingClass = ClassList.FirstOrDefault(c => c.Id == NewClass.Id);
                if (existingClass != null)
                {
                    existingClass.ClassName = NewClass.ClassName;
                    existingClass.StudentCount = NewClass.StudentCount;
                    existingClass.Description = NewClass.Description;
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var classToDelete = ClassList.FirstOrDefault(c => c.Id == id);
            if (classToDelete != null)
            {
                ClassList.Remove(classToDelete);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostEdit(int id)
        {
            var classToEdit = ClassList.FirstOrDefault(c => c.Id == id);
            if (classToEdit != null)
            {
                NewClass = new ClassInformationModel
                {
                    Id = classToEdit.Id,
                    ClassName = classToEdit.ClassName,
                    StudentCount = classToEdit.StudentCount,
                    Description = classToEdit.Description
                };
            }

            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            var existingClass = ClassList.FirstOrDefault(c => c.Id == NewClass.Id);
            if (existingClass != null)
            {
                existingClass.ClassName = NewClass.ClassName;
                existingClass.StudentCount = NewClass.StudentCount;
                existingClass.Description = NewClass.Description;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExportJson(bool isFiltered)
        {
            var query = ClassList.AsQueryable();

            if (isFiltered && !string.IsNullOrWhiteSpace(SearchTerm))
            {
                var lowerSearch = SearchTerm.ToLower();
                query = query.Where(c =>
                    (!string.IsNullOrEmpty(c.ClassName) && c.ClassName.ToLower().Contains(lowerSearch)) ||
                    (!string.IsNullOrEmpty(c.Description) && c.Description.ToLower().Contains(lowerSearch)) ||
                    c.StudentCount.ToString().Contains(lowerSearch));
            }

            // Sadece aktif sayfa verisi
            var pagedData = query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var columnIndexes = !string.IsNullOrWhiteSpace(selectedColumns)
                ? selectedColumns.Split(',').Select(int.Parse).ToList()
                : new List<int> { 0, 1, 2 };

            var reducedData = pagedData.Select(item =>
            {
                var dict = new Dictionary<string, object>();
                if (columnIndexes.Contains(0)) dict["ClassName"] = item.ClassName;
                if (columnIndexes.Contains(1)) dict["StudentCount"] = item.StudentCount;
                if (columnIndexes.Contains(2)) dict["Description"] = item.Description;
                return dict;
            }).ToList();

            var json = Util.Instance.SerializeToJson(reducedData);
            var fileName = isFiltered ? "filtered_classes.json" : "all_classes.json";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(fileBytes, "application/json", fileName);
        }
    }
}
