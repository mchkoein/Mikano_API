using System;
using Mikano_API.Models;
using System.Web.Http;
using System.Runtime.Caching;
using System.Web;

namespace Mikano_API.Helpers
{
    public class ProjectKeysModel
    {

        #region Project Info
        public string projectName { get; set; }
        public bool enableTrackingErrors { get; set; }
        public string defaultGiftCardImage { get; set; }
        public string defaultProductImage { get; set; }
        public bool validationBySmsIsEnabled { get; set; }
        public string requestOriginHeaderKey { get; set; }
        public int pushNotificationAppId { get; set; }
        #endregion

        #region Project Urls
        public string apiUrl { get; set; }
        public string kmsUrl { get; set; }
        public string frontUrl { get; set; }
        public string frontOrderUrl { get; set; }
        #endregion

        #region KMS Customization

        #region Primary Color
        public string colorPrimary { get; set; }
        public string colorPrimaryTextOver { get; set; }
        public string colorPrimaryHover { get; set; }
        public string colorPrimaryHoverTextOver { get; set; }
        #endregion

        #region Secondary Color
        public string colorSecondary { get; set; }
        public string colorSecondaryTextOver { get; set; }
        public string colorSecondaryHover { get; set; }
        public string colorSecondaryHoverTextOver { get; set; }
        #endregion

        #region Tertiary Color
        public string colorTertiary { get; set; }
        public string colorTertiaryTextOver { get; set; }
        public string colorTertiaryHover { get; set; }
        public string colorTertiaryHoverTextOver { get; set; }
        #endregion

        public string boardBackgroundColor { get; set; }

        public string kmsLogo { get; set; }
        #endregion

        #region Display Ins
        public int specialOfferTypeId { get; set; }
        public int showInMenuTypeId { get; set; }
        public int saleTypeId { get; set; }
        public int newTypeId { get; set; }
        public int loyaltyProgramTypId { get; set; }
        public int giftWrapTypeId { get; set; }
        public int giftCardTypeId { get; set; }
        public int featuredTypeId { get; set; }
        #endregion

        #region MPGS
        public string mpgs_PaymentServerURL { get; set; }
        public string mpgs_PaymentCheckoutSessionOperation { get; set; }
        public string mpgs_PaymentPassword { get; set; }
        public string mpgs_PaymentUsername { get; set; }
        public string mpgs_PaymentMerchant { get; set; }
        public string mpgs_PaymentCurrency { get; set; }
        #endregion

        #region Email SMTP
        public string smtpUsername { get; set; }
        public string smtpPassword { get; set; }
        public string smtpHostname { get; set; }
        public int smtpPort { get; set; }
        public bool isSSLRequired { get; set; }
        #endregion

        #region SMS
        public string SendSMSURL { get; set; }
        public string SendSMSUsername { get; set; }
        public string SendSMSPassword { get; set; }
        #endregion

        #region Shipping
        public int shippingCompanyAramexId { get; set; }

        #endregion

        #region Email Templates
        public int emailTpWelcomeId { get; set; }
        public int emailTpPostUserFailureId { get; set; }
        public int emailTpPostOrderFailureId { get; set; }
        public int emailTpResetpasswordId { get; set; }
        public int emailTpProductReviewAdminId { get; set; }
        public int emailTpOrderStatusId { get; set; }
        public int emailTpOrderReceiptId { get; set; }
        public int emailTpOrderReceiptAdminId { get; set; }
        public int emailTpOrderInvoiceId { get; set; }
        public int emailTpProductInquiryId { get; set; }
        public int emailTpContactUsId { get; set; }
        public int emailTpContactUsAdminId { get; set; }
        public int emailTpFranchiseInquiryId { get; set; }
        public int emailTpFranchiseInquiryAdminId { get; set; }
        #endregion

        #region Corporate Pages

        #region Image
        public bool hasImageCaption { get; set; }
        public bool hasImageSubCaption { get; set; }
        public bool hasImageDescription { get; set; }
        public bool hasImageLink { get; set; }
        #endregion

        #region Image Secondary
        public bool hasImageSecondaryCaption { get; set; }
        public bool hasImageSecondarySubCaption { get; set; }
        public bool hasImageSecondaryDescription { get; set; }
        public bool hasImageSecondaryLink { get; set; }
        #endregion

        #region Video
        public bool hasVideoCaption { get; set; }
        public bool hasVideoSubCaption { get; set; }
        public bool hasVideoDescription { get; set; }
        public bool hasVideoLink { get; set; }
        #endregion

        #region File
        public bool hasFileCaption { get; set; }
        public bool hasFileSubCaption { get; set; }
        public bool hasFileDescription { get; set; }
        public bool hasFileLink { get; set; }
        #endregion

        #region Image Gallery
        public bool hasImageGalleryCaption { get; set; }
        public bool hasImageGallerySubCaption { get; set; }
        public bool hasImageGalleryDescription { get; set; }
        public bool hasImageGalleryLink { get; set; }
        #endregion

        #region Video Gallery
        public bool hasVideoGalleryCaption { get; set; }
        public bool hasVideoGallerySubCaption { get; set; }
        public bool hasVideoGalleryDescription { get; set; }
        public bool hasVideoGalleryLink { get; set; }
        #endregion

        #region File Gallery
        public bool hasFileGalleryCaption { get; set; }
        public bool hasFileGallerySubCaption { get; set; }
        public bool hasFileGalleryDescription { get; set; }
        public bool hasFileGalleryLink { get; set; }
        #endregion

        #endregion

    }

    public class ProjectKeysHelper : ApiController
    {
        internal KConfigRepository kConfigRpstry = new KConfigRepository();

        public ProjectKeysModel GetKeys()
        {
            MemoryCache memoryCache = MemoryCache.Default;
            var cahedObject = memoryCache.Get("ConfigurationKeys");
            if (cahedObject == null)
            {
                ProjectKeysModel entry = new ProjectKeysModel();

                #region Project Info
                entry.projectName = kConfigRpstry.db.fnConfigKey("name", "project info");
                entry.defaultProductImage = kConfigRpstry.db.fnConfigKey("default-product-image", "project info");
                entry.defaultGiftCardImage = kConfigRpstry.db.fnConfigKey("default-giftCard-image", "project info");
                entry.enableTrackingErrors = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("EnableTrackingErrors", "project info") ?? "false");
                entry.validationBySmsIsEnabled = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("validation-by-sms-is-enabled", "project info") ?? "false");
                entry.requestOriginHeaderKey = kConfigRpstry.db.fnConfigKey("RequestOriginHeaderKey", "project info");
                entry.pushNotificationAppId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("PushNotificationAppId", "project info") ?? "-1");
                #endregion

                #region Project Urls
                entry.apiUrl = kConfigRpstry.db.fnConfigKey("ApiUrl", "project urls");
                entry.kmsUrl = kConfigRpstry.db.fnConfigKey("KmsUrl", "project urls");
                entry.frontUrl = kConfigRpstry.db.fnConfigKey("FrontUrl", "project urls");
                entry.frontOrderUrl = kConfigRpstry.db.fnConfigKey("FrontOrderDetails", "project urls");
                #endregion

                #region Project Customization

                #region Primary Color
                entry.colorPrimary = kConfigRpstry.db.fnConfigKey("colorPrimary", "kms customization");
                entry.colorPrimaryTextOver = kConfigRpstry.db.fnConfigKey("colorPrimaryTextOver", "kms customization");
                entry.colorPrimaryHover = kConfigRpstry.db.fnConfigKey("colorPrimaryHover", "kms customization");
                entry.colorPrimaryHoverTextOver = kConfigRpstry.db.fnConfigKey("colorPrimaryHoverTextOver", "kms customization");
                #endregion

                #region Secondary Color
                entry.colorSecondary = kConfigRpstry.db.fnConfigKey("colorSecondary", "kms customization");
                entry.colorSecondaryTextOver = kConfigRpstry.db.fnConfigKey("colorSecondaryTextOver", "kms customization");
                entry.colorSecondaryHover = kConfigRpstry.db.fnConfigKey("colorSecondaryHover", "kms customization");
                entry.colorSecondaryHoverTextOver = kConfigRpstry.db.fnConfigKey("colorSecondaryHoverTextOver", "kms customization");
                #endregion

                #region Tertiary Color
                entry.colorTertiary = kConfigRpstry.db.fnConfigKey("colorTertiary", "kms customization");
                entry.colorTertiaryTextOver = kConfigRpstry.db.fnConfigKey("colorTertiaryTextOver", "kms customization");
                entry.colorTertiaryHover = kConfigRpstry.db.fnConfigKey("colorTertiaryHover", "kms customization");
                entry.colorTertiaryHoverTextOver = kConfigRpstry.db.fnConfigKey("colorTertiaryHoverTextOver", "kms customization");
                #endregion

                entry.boardBackgroundColor = kConfigRpstry.db.fnConfigKey("boardBackgroundColor", "kms customization");

                entry.kmsLogo = kConfigRpstry.db.fnConfigKey("kmsLogo", "kms customization");
                #endregion

                #region Display Ins
                entry.giftCardTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("giftcard-type-id", "display ins") ?? "-1");
                entry.giftWrapTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("giftwrap-type-id", "display ins") ?? "-1");
                entry.showInMenuTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("show-in-menu-type-id", "display ins") ?? "-1");
                entry.specialOfferTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("special-offer-type-id", "display ins") ?? "-1");
                entry.loyaltyProgramTypId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("loyalty-program-type-id", "display ins") ?? "-1");
                entry.saleTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("sale-type-id", "display ins") ?? "-1");
                entry.newTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("new-type-id", "display ins") ?? "-1");
                entry.featuredTypeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("featured-type-id", "display ins") ?? "-1");
                #endregion

                #region MPGS
                entry.mpgs_PaymentServerURL = kConfigRpstry.db.fnConfigKey("mpgs_PaymentServerURL", "payment - mpgs");
                entry.mpgs_PaymentCheckoutSessionOperation = kConfigRpstry.db.fnConfigKey("mpgs_paymentcheckoutsessionoperation", "payment - mpgs");
                entry.mpgs_PaymentPassword = kConfigRpstry.db.fnConfigKey("mpgs_paymentpassword", "payment - mpgs");
                entry.mpgs_PaymentUsername = kConfigRpstry.db.fnConfigKey("mpgs_paymentusername", "payment - mpgs");
                entry.mpgs_PaymentMerchant = kConfigRpstry.db.fnConfigKey("mpgs_paymentmerchant", "payment - mpgs");
                entry.mpgs_PaymentCurrency = kConfigRpstry.db.fnConfigKey("mpgs_paymentcurrency", "payment - mpgs");
                #endregion

                #region Email SMTP
                entry.smtpUsername = kConfigRpstry.db.fnConfigKey("smtpUsername", "Email SMTP");
                entry.smtpPassword = kConfigRpstry.db.fnConfigKey("smtpPassword", "Email SMTP");
                entry.smtpHostname = kConfigRpstry.db.fnConfigKey("smtpHostname", "Email SMTP");
                entry.smtpPort = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("smtpPort", "Email SMTP") ?? "-1");
                entry.isSSLRequired = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("smtpSSL", "Email SMTP") ?? "-1");
                #endregion

                #region SMS
                entry.SendSMSURL = kConfigRpstry.db.fnConfigKey("SendSMSURL", "SMS");
                entry.SendSMSUsername = kConfigRpstry.db.fnConfigKey("SendSMSUsername", "SMS");
                entry.SendSMSPassword = kConfigRpstry.db.fnConfigKey("SendSMSPassword", "SMS");
                #endregion

                #region Shipping
                entry.shippingCompanyAramexId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("ShippingCompany_AramexId", "shipping") ?? "-1");
                #endregion

                #region Email Templates
                entry.emailTpWelcomeId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-welcome-id", "Email Templates") ?? "-1");
                entry.emailTpOrderStatusId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-orderstatus-id", "Email Templates") ?? "-1");
                entry.emailTpOrderReceiptId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-orderreceipt-id", "Email Templates") ?? "-1");
                entry.emailTpOrderReceiptAdminId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-order-receipt-admin-id", "Email Templates") ?? "-1");
                entry.emailTpOrderInvoiceId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-orderinvoice-id", "Email Templates") ?? "-1");
                entry.emailTpProductInquiryId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-productInquiry-id", "Email Templates") ?? "-1");
                entry.emailTpProductReviewAdminId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-product-review-admin-id", "Email Templates") ?? "-1");
                entry.emailTpResetpasswordId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-resetpassword-id", "Email Templates") ?? "-1");
                entry.emailTpPostOrderFailureId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-tp-post-order-failure-id", "Email Templates") ?? "-1");
                entry.emailTpPostUserFailureId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-tp-post-user-failure-id", "email templates") ?? "-1");
                entry.emailTpContactUsId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-contact-us-id", "email templates") ?? "-1");
                entry.emailTpContactUsAdminId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-contact-us-admin-id", "email templates") ?? "-1");
                entry.emailTpFranchiseInquiryId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-franchise-inquiry-id", "email templates") ?? "-1");
                entry.emailTpFranchiseInquiryAdminId = Convert.ToInt32(kConfigRpstry.db.fnConfigKey("email-franchise-inquiry-admin-id", "email templates") ?? "-1");
                #endregion

                #region Corporate Pages

                #region Image
                entry.hasImageCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageSubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageSubCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageDescription", "corporate pages media kms config") ?? "false");
                entry.hasImageLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region Image Secondary
                entry.hasImageSecondaryCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageSecondaryCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageSecondarySubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageSecondarySubCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageSecondaryDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageSecondaryDescription", "corporate pages media kms config") ?? "false");
                entry.hasImageSecondaryLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageSecondaryLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region Video
                entry.hasVideoCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoCaption", "corporate pages media kms config") ?? "false");
                entry.hasVideoSubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoSubCaption", "corporate pages media kms config") ?? "false");
                entry.hasVideoDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoDescription", "corporate pages media kms config") ?? "false");
                entry.hasVideoLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region File
                entry.hasFileCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileCaption", "corporate pages media kms config") ?? "false");
                entry.hasFileSubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileSubCaption", "corporate pages media kms config") ?? "false");
                entry.hasFileDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileDescription", "corporate pages media kms config") ?? "false");
                entry.hasFileLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region ImageGallery
                entry.hasImageGalleryCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageGalleryCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageGallerySubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageGallerySubCaption", "corporate pages media kms config") ?? "false");
                entry.hasImageGalleryDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageGalleryDescription", "corporate pages media kms config") ?? "false");
                entry.hasImageGalleryLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasImageGalleryLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region VideoGallery
                entry.hasVideoGalleryCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoGalleryCaption", "corporate pages media kms config") ?? "false");
                entry.hasVideoGallerySubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoGallerySubCaption", "corporate pages media kms config") ?? "false");
                entry.hasVideoGalleryDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoGalleryDescription", "corporate pages media kms config") ?? "false");
                entry.hasVideoGalleryLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasVideoGalleryLink", "corporate pages media kms config") ?? "false");
                #endregion

                #region FileGallery
                entry.hasFileGalleryCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileGalleryCaption", "corporate pages media kms config") ?? "false");
                entry.hasFileGallerySubCaption = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileGallerySubCaption", "corporate pages media kms config") ?? "false");
                entry.hasFileGalleryDescription = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileGalleryDescription", "corporate pages media kms config") ?? "false");
                entry.hasFileGalleryLink = Convert.ToBoolean(kConfigRpstry.db.fnConfigKey("hasFileGalleryLink", "corporate pages media kms config") ?? "false");
                #endregion

                #endregion

                memoryCache.Add("ConfigurationKeys", entry, DateTime.Now.AddDays(1));

                return entry;
            }
            else
            {
                return (ProjectKeysModel)cahedObject;
            }
        }

        //public string GetKey(string key)
        //{
        //	switch (key.ToLower().Trim())
        //	{
        //		//case "apiurl":
        //		//	return kConfigRpstry.db.fnConfigKey("ApiUrl", "project urls");
        //		//	break;
        //		//case "fronturl":
        //		//	return kConfigRpstry.db.fnConfigKey("FrontUrl", "project urls");
        //		//	break;
        //		//case "frontorderurl":
        //		//	return kConfigRpstry.db.fnConfigKey("FrontOrderDetails", "project urls");
        //		//	break;
        //		//case "kmsurl":
        //		//	return kConfigRpstry.db.fnConfigKey("KmsUrl", "project urls");
        //		//	break;
        //		//case "projectname":
        //		//	return kConfigRpstry.db.fnConfigKey("name", "project info");
        //		//	break;


        //		//case "defaultgiftcardimage":
        //		//	return kConfigRpstry.db.fnConfigKey("default-giftCard-image", "project info");
        //		//	break;
        //		//case "defaultproductimage":
        //		//	return kConfigRpstry.db.fnConfigKey("default-product-image", "project info");
        //		//	break;


        //		//case "giftcardtypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("giftcard-type-id", "display ins");
        //		//	break;
        //		//case "giftwraptypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("giftwrap-type-id", "display ins");
        //		//	break;
        //		//case "showinmenutypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("show-in-menu-type-id", "display ins");
        //		//	break;
        //		//case "specialoffertypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("special-offer-type-id", "display ins");
        //		//	break;
        //		//case "loyaltyprogramtypid":
        //		//	return kConfigRpstry.db.fnConfigKey("loyalty-program-type-id", "display ins");
        //		//	break;
        //		//case "saletypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("sale-type-id", "display ins");
        //		//	break;
        //		//case "newtypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("new-type-id", "display ins");
        //		//	break;
        //		//case "featuredtypeid":
        //		//	return kConfigRpstry.db.fnConfigKey("featured-type-id", "display ins");
        //		//	break;


        //		//case "enabletrackingerrors":
        //		//	return kConfigRpstry.db.fnConfigKey("EnableTrackingErrors", "project info");
        //		//	break;


        //		//case "validationbysmsisenabled":
        //		//	return kConfigRpstry.db.fnConfigKey("validation-by-sms-is-enabled", "project info");
        //		//	break;
        //		//case "requestoriginheaderkey":
        //		//	return kConfigRpstry.db.fnConfigKey("RequestOriginHeaderKey", "project info");
        //		//	break;
        //		//case "pushnotificationappid":
        //		//	return kConfigRpstry.db.fnConfigKey("PushNotificationAppId", "project info");
        //		//	break;
        //		//case "shippingcompanyaramexid":
        //		//	return kConfigRpstry.db.fnConfigKey("ShippingCompany_AramexId", "shipping");
        //		//	break;


        //		//case "emailtpwelcomeid":
        //		//	return kConfigRpstry.db.fnConfigKey("email-welcome-id", "email templates");
        //		//	break;
        //		//case "emailtporderstatusid":
        //		//	return kConfigRpstry.db.fnConfigKey("email-orderstatus-id", "email templates");
        //		//	break;
        //		//case "emailtpproductreviewadminid":
        //		//	return kConfigRpstry.db.fnConfigKey("email-product-review-admin-id", "email templates");
        //		//	break;
        //		//case "emailtpresetpasswordid":
        //		//	return kConfigRpstry.db.fnConfigKey("email-resetpassword-id", "email templates");
        //		//	break;
        //		//case "emailtppostuserfailureid":
        //		//	return kConfigRpstry.db.fnConfigKey("email-tp-post-user-failure-id", "email templates");
        //		//	break;




        //		//case "mpgs_paymentserverurl":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentServerURL", "payment - mpgs");
        //		//	break;
        //		//case "mpgs_paymentcheckoutsessionoperation":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentCheckoutSessionOperation", "payment - mpgs");
        //		//	break;
        //		//case "mpgs_paymentpassword":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentPassword", "payment - mpgs");
        //		//	break;
        //		//case "mpgs_paymentusername":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentUsername", "payment - mpgs");
        //		//	break;
        //		//case "mpgs_paymentmerchant":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentMerchant", "payment - mpgs");
        //		//	break;
        //		//case "mpgs_paymentcurrency":
        //		//	return kConfigRpstry.db.fnConfigKey("mpgs_PaymentCurrency", "payment - mpgs");
        //		//	break;


        //		//case "sendsmsurl":
        //		//	return kConfigRpstry.db.fnConfigKey("SendSMSURL", "SMS");
        //		//	break;
        //		//case "sendsmsusername":
        //		//	return kConfigRpstry.db.fnConfigKey("SendSMSUsername", "SMS");
        //		//	break;
        //		//case "sendsmspassword":
        //		//	return kConfigRpstry.db.fnConfigKey("SendSMSPassword", "SMS");
        //		//	break;



        //		//case "smtpusername":
        //		//	return kConfigRpstry.db.fnConfigKey("smtpUsername", "Email SMTP");
        //		//	break;
        //		//case "smtppassword":
        //		//	return kConfigRpstry.db.fnConfigKey("smtpPassword", "Email SMTP");
        //		//	break;
        //		//case "smtphostname":
        //		//	return kConfigRpstry.db.fnConfigKey("smtpHostname", "Email SMTP");
        //		//	break;
        //		//case "smtpport":
        //		//	return kConfigRpstry.db.fnConfigKey("smtpPort", "Email SMTP");
        //		//	break;
        //		//case "smtpssl":
        //		//	return kConfigRpstry.db.fnConfigKey("smtpSSL", "Email SMTP");
        //		//	break;


        //		default: return "";
        //	}
        //}

    }
}