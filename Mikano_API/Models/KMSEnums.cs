namespace Mikano_API.Models
{
    public static class DirectiveStatus
    {
        public static string hidden { get { return "$hidden$"; } }
        public static string disabled { get { return "$disabled$"; } }
        public static string notrequired { get { return "$notrequired$"; } }
        public static string required { get { return "$required$"; } }
    }

    public class KMSEnums
    {

        public enum KActions
        {
            create = 2,
            read = 3,
            update = 4,
            delete = 5,
            publish = 6,
            sort = 8,
        }

        public enum UserActivation
        {
            Active = 1,
            Deactivated = 2,
            Disabled = 3,
            Stopped_Financial = 4
        }

        public enum SubmissionOptions
        {
            saveAndGoBack,
            saveAndClose,
            saveAndStayHere,
            saveAndPublish,
            saveAndGoToNext,
            saveAsNew
        }

        public enum SmsProviders
        {
            eBeirut = 1,
            Etisalat = 2
        }

        public enum UserStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3
        }

        public enum RetailerItemRequestStatus
        {
            Pending = 1,
            Processed = 2,
            Rejected = 3
        }

        public enum MediaType
        {
            Image = 1,
            Video = 2,
            File = 3,
        }

        #region Ecommerce Section
        public enum EKomWorkflowStep
        {
            basket = 0,
            order = 1
        }

        public enum ShippingWorkflowStep
        {
            order_Confirmation = 0,
            order_Preparation = 1,
            package_departure = 2,
            flow_sorting_center = 3,
            out_for_delivery = 4,
            delivered = 5
        }

        public enum EnumExecutionState
        {
            Ordered = 1,
            Accepted = 2,
            Packaged = 3,
            Shipped = 4,
            Delivered = 5,
            Canceled = 6
        }

        public enum GenderType
        {
            Male,
            Female
        }

        public enum EnumPaymentMethods
        {
            online_payment = 1,
            cash_on_delivery = 2
        }
        public enum EnumBillingState
        {
            Not_paid = 1,
            Pending = 4,
            Paid = 5,
            Rejected = 6,
        }
        public enum EnumCouponType
        {
            gift_card = 2,
            promotion_code = 3
        }

        public enum EnumThirdPartyCallLogType
        {
            get_all_products = 1,
            get_product_available_quantity = 2,
            get_loyalty_points = 3,
            post_order = 4,
            post_customer_info = 5,
            create_new_gift_card = 6,
            redeem_gift_card = 7,
            aramex_shipping = 8,
            aramex_pickup = 9
        }

        public enum EnumThirdPartyCallLogStatus
        {
            failed = 1,
            success = 2,
            pending = 3,
        }

        public enum EnumExternalChangesType
        {
            External,
            Website,
        }

        public enum EnumGroupBy
        {
            Color,
            Size,
        }
        #endregion

        #region Spoonstream
        public enum weeklyRecurrence
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday,
            Sunday,
            Daily
        }
        #endregion
    }
}