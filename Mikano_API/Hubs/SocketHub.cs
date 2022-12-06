using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Mikano_API.Models;
using static Mikano_API.Models.KMSEnums;
using System.Configuration;
using System.Collections.Specialized;

namespace Mikano_API
{

    public class SocketHub : Hub
    {
        private KMSSectionRepository sectionRpstry = new KMSSectionRepository();
        private AdministratorRepository userRpstry = new AdministratorRepository();
        private dblinqDataContext db = new dblinqDataContext();

        private string customerDirectory = "account";

        private NameValueCollection ProjectKeysConfiguration = (NameValueCollection)ConfigurationManager.GetSection("ProjectKeysConfig");



        //do not remove this
        public void DummyCall() { }


        //  public void BroadcastOrderDetails(EKomBasket entry)
        // {
        //var hubContext = GlobalHost.ConnectionManager.GetHubContext<SocketHub>();
        //if (db.SignalRUsers.Any())
        //{
        //    var config = confRpstry.GetFirstOrDefault();

        //    int ipending = (int)ExcutionState.Pending,
        //        iprocessing = (int)ExcutionState.Processing,
        //        ischeduled = (int)ExcutionState.Scheduled;
        //    string soverdue = TimingState.overdue.ToString(),
        //        slate = TimingState.late.ToString(),
        //        son_time = TimingState.on_time.ToString().Replace("_", "-"),
        //        sscheduled = TimingState.scheduled.ToString(),
        //        sbasketfeedback = KMessageTypes.basketfeedback.ToString();
        //    var currentDate = DateTime.Now;

        //    var entryToBroadcast = new
        //    {
        //        id = entry.id,
        //        orderNb = entry.id + "-" + entry.numberOfTry,
        //        timing = entry.eKomExecutionStateId == ischeduled ? sscheduled : entry.timingStatus,
        //        status = entry.EKomExecutionState.title,
        //        eKomExecutionStateId = entry.eKomExecutionStateId,
        //        isPending = entry.eKomExecutionStateId == ipending,
        //        isProcessing = entry.eKomExecutionStateId == iprocessing,
        //        isScheduled = entry.eKomExecutionStateId == ischeduled,
        //        canceledByUser = entry.canceledByUser,
        //        canceledDate = entry.canceledDate,
        //        date = entry.orderDate,
        //        //rating = entry.rating,
        //        pendingDate = entry.pendingDate,
        //        deliveryDate = entry.deliveryDate,
        //        fullName = entry.AspNetUser.firstName + " " + entry.AspNetUser.lastName,
        //        itemsCount = entry.EKomProductBaskets.Where(e => !e.parentId.HasValue).Sum(e => e.quantity),
        //        price = entry.pricePaid,
        //        lastAction = basketRpstry.GetLastActionParams(entry.id),
        //        feedbacks = basketRpstry.db.fnGetOrderFeedbacksCount(entry.id + "", sbasketfeedback),
        //        feedbacksareread = basketRpstry.db.fnGetOrderHasUnreadFeedbacks(entry.id + "", sbasketfeedback),
        //        address = entry.shippingAddress,
        //        haspendingOrders = basketRpstry.GetAllOrders(null, true).Any(d => d.eKomExecutionStateId == (int)ExcutionState.Pending),
        //        totalOrders = basketRpstry.GetAllOrders(null, true).Count(),
        //        totalOrdersToday = basketRpstry.GetAllOrders(null, false).Where(d => d.orderDate.Value.Date == DateTime.Now.Date).Count(),
        //        averageResponseTime = basketRpstry.GetAverageResponseTime(null, DateTime.Now, DateTime.Now),
        //        serviceQuality = basketRpstry.GetServiceQuality(null, DateTime.Now, DateTime.Now),
        //    };

        //    var users = db.SignalRUsers.Select(d => d.username).Distinct().ToList();
        //    hubContext.Clients.Users(users).BroadcastOrderDetails(entryToBroadcast);
        //    hubContext.Clients.Users(users).BroadcastOrderNotification(entryToBroadcast);
        //}
        // }

        // public void BroadcastOrderFeedback(KMessage entry, EKomBasket order)
        // {
        //var hubContext = GlobalHost.ConnectionManager.GetHubContext<SocketHub>();
        //if (db.SignalRUsers.Any())
        //{
        //    var entryToBroadcast = new
        //    {
        //        isNewMessage = (DateTime.Now - entry.dateCreated).TotalSeconds < 15,
        //        messageId = entry.id,
        //        id = entry.id,
        //        message = entry.message,
        //        name = order.AspNetUser.firstName + " " + order.AspNetUser.lastName,
        //        orderId = order.id,
        //        orderNb = order.id + "-" + order.numberOfTry,
        //        isProcessing = order.eKomExecutionStateId == (int)ExcutionState.Processing,
        //        processedById = order.aspNetUsersOperatorId,
        //        processedByName = order.AspNetUser1 != null ? order.AspNetUser1.firstName + " " + order.AspNetUser1.lastName : "",
        //        itsme = false,
        //        picture = entry.AspNetUser.imgSrc == "" || entry.AspNetUser.imgSrc == null ? (entry.AspNetUser.firstName == null && entry.AspNetUser.lastName == null ? entry.AspNetUser.UserName[0] + "" + entry.AspNetUser.UserName[1] : entry.AspNetUser.firstName[0] + "" + entry.AspNetUser.lastName[0]) : projectConfigKeys.apiUrl + "/content/uploads/" + customerDirectory + "/" + entry.AspNetUser.imgSrc,
        //        date = entry.dateCreated
        //    };

        //    var users = db.SignalRUsers.Select(d => d.username).Distinct().ToList();
        //    hubContext.Clients.Users(users).BroadcastOrderFeedback(entryToBroadcast);
        //    hubContext.Clients.Users(users).BroadcastOrderDetailsFeedback(entryToBroadcast);
        //}
        //  }

        public void BroadcastRemovedFeedback(int orderId)
        {
            //var hubContext = GlobalHost.ConnectionManager.GetHubContext<SocketHub>();
            //if (db.SignalRUsers.Any())
            //{
            //    var users = db.SignalRUsers.Select(d => d.username).Distinct().ToList();
            //    hubContext.Clients.Users(users).BroadcastRemovedFeedback(orderId);
            //}
        }



        public override System.Threading.Tasks.Task OnConnected()
        {
            string username = GetClientId();
            string connectionId = GetConnectionId();

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(connectionId))
            {
                var userEntry = userRpstry.GetByUserName(username);
                if (userEntry != null)
                {
                    if (db.SignalRUsers.Any(d => d.username == username && d.connectionId == connectionId))
                    {
                        db.ExecuteCommand("update SignalRUser set datecreated={0} where username like {1} and connectionid like {2}", DateTime.Now, username, connectionId);
                    }
                    else
                    {
                        db.ExecuteCommand("insert into SignalRUser (username,connectionid) VALUES ({0},{1})", username, connectionId);
                    }
                }
            }

            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnReconnected()
        {
            string username = GetClientId();
            string connectionId = GetConnectionId();

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(connectionId))
            {
                var userEntry = userRpstry.GetByUserName(username);
                if (userEntry != null)
                {
                    if (db.SignalRUsers.Any(d => d.username == username && d.connectionId == connectionId))
                    {
                        db.ExecuteCommand("update SignalRUser set datecreated={0} where username like {1} and connectionid like {2}", DateTime.Now, username, connectionId);
                    }
                    else
                    {
                        db.ExecuteCommand("insert into SignalRUser (username,connectionid) VALUES ({0},{1})", username, connectionId);
                    }
                }
            }
            return base.OnReconnected();
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled = true)
        {
            string username = GetClientId();
            string connectionId = GetConnectionId();
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(connectionId))
            {
                db.ExecuteCommand("delete from SignalRUser where username like {0} and connectionid like {1}", username, connectionId);
            }
            return base.OnDisconnected(stopCalled);
        }

        private string GetClientId()
        {
            try
            {
                return Context.User.Identity.Name;
            }
            catch (Exception e)
            {
                return "";
            }
        }

        private string GetConnectionId()
        {
            try
            {
                return Context.ConnectionId;
            }
            catch (Exception e)
            {
                return "";
            }
        }

    }
}