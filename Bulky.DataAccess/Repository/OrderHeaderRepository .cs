using Bulky.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }



        public void Update(OrderHeader obj)
        {
            _db.Update(obj);
        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var obj = _db.OrderHeaders.FirstOrDefault(u => u.OrderHeaderId == id);
            if (obj != null)
            {
                if (!String.IsNullOrEmpty(orderStatus))
                {
                    obj.OrderStatus = orderStatus;
                }
                if (!String.IsNullOrEmpty(paymentStatus))
                {
                    obj.PaymentStatus = paymentStatus;
                }
            }
        }

        public void UpdateStripPaymentId(int id, string sessionId, string paymentIntentId)
        {
            var obj = _db.OrderHeaders.FirstOrDefault(u => u.OrderHeaderId == id);
            if (obj != null)
            {
                if (!String.IsNullOrEmpty(sessionId))
                {
                    obj.SessionId = sessionId;
                }
                if (!String.IsNullOrEmpty(paymentIntentId))
                {
                    obj.PaymentIntentId = paymentIntentId;
                    obj.PaymentDate = DateTime.Now;
                }
            }
        }
    }
}
