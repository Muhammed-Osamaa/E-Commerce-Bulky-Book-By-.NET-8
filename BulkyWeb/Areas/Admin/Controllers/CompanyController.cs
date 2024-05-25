using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;
using Utility;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            
        }

        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Company());

            }
            else
            {
                Company companyObj = _unitOfWork.company.Get(item => item.CompanyId == id);
                return View(companyObj);

            }
        }
        [HttpPost]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {
             
                if (obj.CompanyId == 0)
                {
                    _unitOfWork.company.Add(obj);

                }
                else
                {
                    _unitOfWork.company.Update(obj);

                }
                _unitOfWork.Save();
                TempData["success"] = "Category Created successfully";
                return RedirectToAction("Index");

            }
            else
            {
                return View(obj);
            }
        }

     

        #region API CALLS
        public IActionResult GetAll()
        {
            List<Company> companies = _unitOfWork.company.GetAll().ToList();
            return Json(new {data = companies });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company company = _unitOfWork.company.Get(u => u.CompanyId == id);
            if(company == null)
            {
                return Json(new { success = false, message = "Error while Deleting" });
            }
          
            _unitOfWork.company.Remove(company);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Success" });
        }
        #endregion
    }
}
