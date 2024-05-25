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

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> objProductList = _unitOfWork.category.GetAll().Select(item => new SelectListItem()
            {
                Text = item.Name,
                Value = item.Id.ToString()
            });
            ProductVM productVM = new ProductVM()
            {
                CategoryList = objProductList,
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                return View(productVM);

            }
            else
            {
                productVM.Product = _unitOfWork.product.Get(item => item.ProductId == id);
                return View(productVM);

            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string newFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");
                    if (!String.IsNullOrEmpty(obj.Product.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (FileStream fileStream = new FileStream(Path.Combine(productPath, newFileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    };
                    obj.Product.ImageUrl = @"\images\product\" + newFileName;
                }
                if (obj.Product.ProductId == 0)
                {
                    _unitOfWork.product.Add(obj.Product);

                }
                else
                {
                    _unitOfWork.product.Update(obj.Product);

                }
                _unitOfWork.Save();
                TempData["success"] = "Category Created successfully";
                return RedirectToAction("Index");

            }
            else
            {
                obj.CategoryList = _unitOfWork.category.GetAll().Select(item => new SelectListItem()
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
                return View(obj);
            }
        }

     

        #region API CALLS
        public IActionResult GetAll()
        {
            List<Product> products = _unitOfWork.product.GetAll(includeProperties:"Category").ToList();
            return Json(new {data = products});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product product = _unitOfWork.product.Get(u => u.ProductId == id);
            if(product == null)
            {
                return Json(new { success = false, message = "Error while Deleting" });
            }
            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
            _unitOfWork.product.Remove(product);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Success" });
        }
        #endregion
    }
}
