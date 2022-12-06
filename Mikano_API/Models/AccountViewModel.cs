using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mikano_API.Models
{
    // Models returned by AccountController actions.

    public class ExternalLoginViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string State { get; set; }
    }

    public class GoogleAccountModel
    {
        public string azp { get; set; }
        public string aud { get; set; }
        public string sub { get; set; }
        public string hd { get; set; }
        public string email { get; set; }
        public string email_verified { get; set; }
        public string at_hash { get; set; }
        public string exp { get; set; }
        public string iss { get; set; }
        public string jti { get; set; }
        public string iat { get; set; }
        public string name { get; set; }
        public string picture { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string locale { get; set; }
        public string alg { get; set; }
        public string kid { get; set; }
    }

    public class ExternalAccessTokenCustomLoginModel
    {
        public string accessToken { get; set; }
    }

    public class ExternalCustomLoginPictureDataModel
    {
        public string url { get; set; }
    }
    public class ExternalCustomLoginPictureModel
    {
        public ExternalCustomLoginPictureDataModel data { get; set; }
    }
    public class ExternalCustomLoginModel
    {
        public string id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public DateTime? birthday { get; set; }
        public ExternalCustomLoginPictureModel picture { get; set; }
    }


    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; }

        public string Email { get; set; }

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
    }

    public class UserInfoViewModel
    {
        public string Email { get; set; }

        public bool HasRegistered { get; set; }

        public string LoginProvider { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }


    public class UserForgotPasswordViewModel
    {
        [Required]
        public string UserName { get; set; }
    }


    public class UserResetPasswordViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string token { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public class SettingsViewModel
    {
        [Required]
        public decimal lowCreditAlertThreshold { get; set; }


    }

    public class UpdatePinCodeViewModel
    {

        //[Required]
        public bool pinIsRequired { get; set; }

        public string pinCode { get; set; }

        public string pinVerificationCode { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "New pincode")]
        public string newPinCode { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new pincode")]
        [Compare("newPinCode", ErrorMessage = "The new pincode and confirmation new pincode do not match.")]
        public string ConfirmNewPinCode { get; set; }
    }


    public class UpdatePasswordViewModel
    {
        public string Password { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation new password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }



    public class BOBPaymentResponse
    {
        public string mpgs_3DSECI { get; set; }//mc

        public string mpgs_3DSXID { get; set; }
        public string mpgs_3DSenrolled { get; set; }

        public string mpgs_3DSstatus { get; set; }//mc

        public string mpgs_AVSResultCode { get; set; }
        public string mpgs_AcqAVSRespCode { get; set; }
        public string mpgs_AcqCSCRespCode { get; set; }
        public string mpgs_AcqResponseCode { get; set; }
        public string mpgs_Amount { get; set; }
        public string mpgs_BatchNo { get; set; }
        public string mpgs_CSCResultCode { get; set; }
        public string mpgs_Card { get; set; }
        public string mpgs_Command { get; set; }
        public string mpgs_Locale { get; set; }
        public string mpgs_MerchTxnRef { get; set; }
        public string mpgs_Merchant { get; set; }
        public string mpgs_Message { get; set; }
        public string mpgs_OrderInfo { get; set; }
        public string mpgs_ReceiptNo { get; set; }
        public string mpgs_TransactionNo { get; set; }
        public string mpgs_TxnResponseCode { get; set; }
        public string mpgs_VerSecurityLevel { get; set; }
        public string mpgs_VerStatus { get; set; }
        public string mpgs_VerToken { get; set; }//mc
        public string mpgs_VerType { get; set; }
        public string mpgs_Version { get; set; }
        public string mpgs_SecureHash { get; set; }
        public string mpgs_SecureHashType { get; set; }
    }

    public class PinPayPaymentResponse
    {
        public string UserIdentifier { set; get; }
        public string OperationResultCode { get; set; }
        public string PinPayTransactionId { set; get; }
        public Guid RequestId { set; get; }
        public string OrderReference { set; get; }
        public string OrderInfo { set; get; }
        public string SecureHash { get; set; }
    }



    public class DiscountProduct
    {
        public int id { set; get; }
        public string title { get; set; }
        public decimal discount { set; get; }
        public bool isActive { set; get; }
    }

    public class DiscountCategory
    {
        public int id { set; get; }
        public string title { get; set; }
        public decimal discount { set; get; }

        public IEnumerable<DiscountProduct> products { get; set; }
    }

}
