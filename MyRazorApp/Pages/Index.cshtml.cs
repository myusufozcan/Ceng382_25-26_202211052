using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using MyRazorApp.Models;

namespace MyRazorApp.Pages
{
    public class IndexModel : PageModel
    {
        public static List<ClassInformationModel> ClassList { get; set; } = new();
        public static int _idCounter = 1;

        [BindProperty]
        public ClassInformationModel NewClass { get; set; } = new ClassInformationModel();

        [BindProperty]
        public int EditId { get; set; }

        public void OnGet()
        {
        }

        public ClassInformationModel GetNewClass()
        {
            return NewClass;
        }

        public IActionResult OnPostAdd()
        {
        if (NewClass == null || !ModelState.IsValid)
        {
            return Page();
        }

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

        NewClass = new ClassInformationModel();

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
    }
}