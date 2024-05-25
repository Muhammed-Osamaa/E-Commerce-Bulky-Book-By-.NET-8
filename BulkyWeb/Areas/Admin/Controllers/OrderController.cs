using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;
using Utility;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int id)
        {
			OrderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == id, "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(u => u.OrderHeaderId == id, "Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderDb = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == OrderVM.OrderHeader.OrderHeaderId);
            orderHeaderDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderDb.phoneNumber = OrderVM.OrderHeader.phoneNumber;
            orderHeaderDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderDb.City = OrderVM.OrderHeader.City;
            orderHeaderDb.State = OrderVM.OrderHeader.State;
            orderHeaderDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if (!String.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!String.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderDb.Carrier = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.orderHeader.Update(orderHeaderDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new {id=OrderVM.OrderHeader.OrderHeaderId});
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.orderHeader.UpdateStatus(OrderVM.OrderHeader.OrderHeaderId, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated.";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.OrderHeaderId });

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == OrderVM.OrderHeader.OrderHeaderId);
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _unitOfWork.orderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated.";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.OrderHeaderId });

        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == OrderVM.OrderHeader.OrderHeaderId);
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                service.Create(options);
                _unitOfWork.orderHeader.UpdateStatus(orderHeader.OrderHeaderId, SD.StatusCancelled, SD.StatusRefunded);
            }else
            {
                _unitOfWork.orderHeader.UpdateStatus(orderHeader.OrderHeaderId, SD.StatusCancelled, SD.StatusCancelled);

            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancel Updated.";
            return RedirectToAction(nameof(Details), new { id = OrderVM.OrderHeader.OrderHeaderId });
        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.orderHeader
                .Get(u => u.OrderHeaderId == OrderVM.OrderHeader.OrderHeaderId, includePropties: "ApplicationUser");
            OrderVM.OrderDetails = _unitOfWork.OrderDetails
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.OrderHeaderId, includeProperties: "Product");

            //stripe logic
            var domain = "https://localhost:7157";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"/Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.OrderHeaderId}",
                CancelUrl = domain + $"/admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.orderHeader.UpdateStripPaymentId(OrderVM.OrderHeader.OrderHeaderId, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader orderHeader = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.orderHeader.UpdateStripPaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.orderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }


            }


            return View(orderHeaderId);
        }

        #region API CALLS
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> obj;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                obj = _unitOfWork.orderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                obj = _unitOfWork.orderHeader.GetAll(u => u.Id == UserId,"ApplicationUser").ToList();
            }

            switch (status)
            {
                case "pending": obj =  obj.Where(u=> u.PaymentStatus == SD.PaymentStatusDelayedPayment);break;
                case "inprocess": obj =  obj.Where(u=> u.OrderStatus == SD.StatusInProcess); break;
                case "completed": obj = obj.Where(u=> u.OrderStatus == SD.StatusShipped); break;
                case "approved": obj = obj.Where(u=> u.OrderStatus ==  SD.StatusApproved); break;
                default: _unitOfWork.orderHeader.GetAll(includeProperties: "ApplicationUser").ToList(); break;
            }
            return Json(new { data = obj });
        }
        #endregion
    }
}
