using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Xml;
using System.Text;
using System.IO;
using System.Configuration;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Mail;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Routing;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Reflection;
using Mikano_API.Models;
using System.Threading.Tasks;

namespace Mikano_API.Helpers
{
    public class UtilsHelper
    {
        private readonly string PasswordHash = ConfigurationManager.AppSettings["PasswordHash"];


        public string sendEmail(string from, string to, string cc, string bcc, string subject, string body, string attachment)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

            string smtpUsername = projectConfigKeys.smtpUsername;
            string smtpPassword = projectConfigKeys.smtpPassword;
            string smtpHostname = projectConfigKeys.smtpHostname;
            int smtpPort = projectConfigKeys.smtpPort;
            bool isSSLRequired = projectConfigKeys.isSSLRequired;

            try
            {
                MailMessage message = new MailMessage();
                if (attachment != "")
                    message.Attachments.Add(new Attachment(attachment));
                message.From = new MailAddress(from);
                if (to.IndexOf(';') != -1)
                {
                    string[] Tos = to.Split(';');
                    foreach (string toRecipient in Tos)
                    {
                        message.To.Add(toRecipient);
                    }
                }
                else
                {
                    message.To.Add(to);
                }
                if (cc != "")
                    message.CC.Add(cc);
                if (bcc != "")
                    message.Bcc.Add(bcc);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient(smtpHostname, smtpPort);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtp.EnableSsl = isSSLRequired;
                smtp.Send(message);

                return "SUCCESS";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task SendEmailAsync(String from, String to, string cc, string bcc, String subject, String body, string attachment)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

            string smtpUsername = projectConfigKeys.smtpUsername;
            string smtpPassword = projectConfigKeys.smtpPassword;
            string smtpHostname = projectConfigKeys.smtpHostname;
            int smtpPort = projectConfigKeys.smtpPort;
            bool isSSLRequired = projectConfigKeys.isSSLRequired;

            try
            {
                var message = new MailMessage();
                message.From = new MailAddress(from);
                message.To.Add(to);

                message.Subject = subject;
                message.Body = body;

                SmtpClient smtp = new SmtpClient(smtpHostname, smtpPort);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtp.EnableSsl = isSSLRequired;
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
            }
        }
        public string safeHTML(string str)
        {
            //variable to hold the returned value
            string strippedString;
            try
            {
                //variable to hold our RegularExpression pattern
                string pattern = "<.*?>";
                //replace all HTML tags
                strippedString = Regex.Replace(str, pattern, string.Empty);
            }
            catch
            {
                strippedString = string.Empty;
            }
            return strippedString;
        }

        public string GetStrippedHTML(string text, int take)
        {
            if (take != -1)
            {
                text = safeHTML(text ?? "");
                string points = text == null ? "" : (text.Length >= take) ? "..." : "";
                return null == text ? "" : (text.Substring(0, (text.Length < take ? text.Length : take))) + points;
            }
            else
            {
                return safeHTML(text ?? "");
            }
        }

        public double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            double unixTime = (date.ToUniversalTime() - origin.ToUniversalTime()).TotalSeconds;
            return unixTime;
        }

        public string EEncryptString(string clearText)
        {
            string EncryptionKey = ConfigurationManager.AppSettings["PasswordHash"];
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 100);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText.Replace("+", "$p$-").Replace("=", "$e$-").Replace("/", "$s$-");
        }

        public string EDecryptString(string cipherText)
        {
            string EncryptionKey = ConfigurationManager.AppSettings["PasswordHash"];
            cipherText = cipherText.Replace("$p$-", "+").Replace("$e$-", "=").Replace("$s$-", "/");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 100);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        public string RandomString(int length, bool isOnlyNumeric = false)
        {
            string valid = isOnlyNumeric ? "1234567890" : "abcdefghijklmnopqrst1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        public string CreateSHA256Signature(SortedList<string, string> list, bool isMobile, bool isResponse = false)
        {


            /////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////SOME DAY I WILL GO TO BOB AND KILL EVERYBODY IN THE DEPART, NO IN THE BANK///////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            var keys = list.Select(d => d.Key).ToArray();
            Array.Sort(keys, StringComparer.Ordinal);

            // Hex Decode the Secure Secret for use in using the HMACSHA256 hasher
            // hex decoding eliminates this source of error as it is independent of the character encoding
            // hex decoding is precise in converting to a byte array and is the preferred form for representing binary values as hex strings. 
            var _secureSecret = ConfigurationManager.AppSettings["mpgs_SecureSecret" + (isMobile ? "_mobile" : "")];

            byte[] convertedHash = new byte[_secureSecret.Length / 2];
            for (int i = 0; i < _secureSecret.Length / 2; i++)
            {
                convertedHash[i] = (byte)Int32.Parse(_secureSecret.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            // Build string from collection in preperation to be hashed
            StringBuilder sb = new StringBuilder();
            foreach (string kvp in keys)
            {
                if (!string.IsNullOrEmpty(list[kvp]) && kvp != "mpgs_SecureHash" && kvp != "mpgs_SecureHashType")
                {
                    sb.Append(kvp + "=" + list[kvp] + "&");
                }
            }


            //if (isResponse)
            //{
            //    //this sorting is important in order to generate the right key
            //    if (list["mpgs_Card"] != "VC") {
            //    sb.Append("mpgs_3DSECI=" + list["mpgs_3DSECI"] + "&");
            //    }
            //    sb.Append("mpgs_3DSXID=" + list["mpgs_3DSXID"] + "&");
            //    sb.Append("mpgs_3DSenrolled=" + list["mpgs_3DSenrolled"] + "&");
            //    if (list["mpgs_Card"] != "VC")
            //    {
            //        sb.Append("mpgs_3DSstatus=" + list["mpgs_3DSstatus"] + "&");
            //    }
            //    sb.Append("mpgs_AVSResultCode=" + list["mpgs_AVSResultCode"] + "&");
            //    sb.Append("mpgs_AcqAVSRespCode=" + list["mpgs_AcqAVSRespCode"] + "&");
            //    sb.Append("mpgs_AcqCSCRespCode=" + list["mpgs_AcqCSCRespCode"] + "&");
            //    sb.Append("mpgs_AcqResponseCode=" + list["mpgs_AcqResponseCode"] + "&");
            //    sb.Append("mpgs_Amount=" + list["mpgs_Amount"] + "&");
            //    sb.Append("mpgs_BatchNo=" + list["mpgs_BatchNo"] + "&");
            //    sb.Append("mpgs_CSCResultCode=" + list["mpgs_CSCResultCode"] + "&");
            //    sb.Append("mpgs_Card=" + list["mpgs_Card"] + "&");
            //    sb.Append("mpgs_Command=" + list["mpgs_Command"] + "&");
            //    sb.Append("mpgs_Locale=" + list["mpgs_Locale"] + "&");
            //    sb.Append("mpgs_MerchTxnRef=" + list["mpgs_MerchTxnRef"] + "&");
            //    sb.Append("mpgs_Merchant=" + list["mpgs_Merchant"] + "&");
            //    sb.Append("mpgs_Message=" + list["mpgs_Message"] + "&");
            //    sb.Append("mpgs_OrderInfo=" + list["mpgs_OrderInfo"] + "&");
            //    sb.Append("mpgs_ReceiptNo=" + list["mpgs_ReceiptNo"] + "&");
            //    sb.Append("mpgs_TransactionNo=" + list["mpgs_TransactionNo"] + "&");
            //    sb.Append("mpgs_TxnResponseCode=" + list["mpgs_TxnResponseCode"] + "&");
            //    sb.Append("mpgs_VerSecurityLevel=" + list["mpgs_VerSecurityLevel"] + "&");
            //    sb.Append("mpgs_VerStatus=" + list["mpgs_VerStatus"] + "&");
            //    if (list["mpgs_Card"] != "VC")
            //    {
            //        sb.Append("mpgs_VerToken=" + list["mpgs_VerToken"] + "&");
            //    }
            //    sb.Append("mpgs_VerType=" + list["mpgs_VerType"] + "&");
            //    sb.Append("mpgs_Version=" + list["mpgs_Version"] + "&");
            //}
            //else
            //{
            //    sb.Append("mpgs_AccessCode=" + list["mpgs_AccessCode"] + "&");
            //    sb.Append("mpgs_Amount=" + list["mpgs_Amount"] + "&");
            //    sb.Append("mpgs_Command=" + list["mpgs_Command"] + "&");
            //    sb.Append("mpgs_Gateway=" + list["mpgs_Gateway"] + "&");
            //    sb.Append("mpgs_MerchTxnRef=" + list["mpgs_MerchTxnRef"] + "&");
            //    sb.Append("mpgs_Merchant=" + list["mpgs_Merchant"] + "&");
            //    sb.Append("mpgs_OrderInfo=" + list["mpgs_OrderInfo"] + "&");
            //    sb.Append("mpgs_ReturnURL=" + list["mpgs_ReturnURL"] + "&");
            //    sb.Append("mpgs_Version=" + list["mpgs_Version"] + "&");
            //}


            sb.Remove(sb.Length - 1, 1);

            // Create secureHash on string
            string hexHash = "";
            using (HMACSHA256 hasher = new HMACSHA256(convertedHash))
            {
                byte[] utf8bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] iso8859bytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("iso-8859-1"), utf8bytes);
                byte[] hashValue = hasher.ComputeHash(iso8859bytes);

                foreach (byte b in hashValue)
                {
                    hexHash += b.ToString("X2");
                }
            }
            return hexHash;
        }

        public string GetRequestRaw(NameValueCollection keys, SortedList<string, string> listEntries, string requestUrl)
        {
            StringBuilder data = new StringBuilder();
            if (keys != null)
            {
                foreach (var kvp in keys.AllKeys)
                {
                    data.Append(kvp + "=" + HttpUtility.UrlEncode(keys[kvp], System.Text.Encoding.GetEncoding("ISO-8859-1")) + "&");
                }
            }

            if (listEntries != null)
            {
                foreach (var kvp in listEntries)
                {
                    data.Append(kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value, System.Text.Encoding.GetEncoding("ISO-8859-1")) + "&");
                }
            }

            return requestUrl + "?" + data.ToString();
        }

        public string MinifyUrl(string link)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    link = webClient.DownloadString(string.Format("http://tinyurl.com/api-create.php?url={0}", HttpUtility.UrlEncode(link)));
                    webClient.Dispose();
                }
            }
            catch (Exception e)
            {

            }

            return link;
        }

        public string CreatePassword(int length, bool isOnlyNumeric = false)
        {
            string valid = isOnlyNumeric ? "1234567890" : "abcdefghijklmnopqrst1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        public string SetCacheData(object dataToCache, string keyPrefix)
        {
            var cache = MemoryCache.Default;
            if (cache == null)
            {
                cache = new MemoryCache("MYMainCache");
            }
            var fileKey = keyPrefix + "~$~" + CreatePassword(10);
            cache.Add(fileKey, dataToCache, DateTime.Now.AddMinutes(2));

            return EEncryptString(fileKey);
        }

        public object GetCacheData(string fileKey)
        {
            var cache = MemoryCache.Default;
            if (cache == null)
            {
                cache = new MemoryCache("MYMainCache");
            }
            fileKey = EDecryptString(fileKey);
            var results = cache.Get(fileKey);

            return results;
        }

        public List<KeyValuePair<string, int>> GetEnumList<T>()
        {
            var list = new List<KeyValuePair<string, int>>();
            foreach (var e in Enum.GetValues(typeof(T)))
            {
                list.Add(new KeyValuePair<string, int>(e.ToString(), (int)e));
            }
            return list;
        }

        public struct DateRange
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
        public DateRange ThisMonth(DateTime date)
        {
            DateRange range = new DateRange();

            range.Start = new DateTime(date.Year, date.Month, 1);
            range.End = range.Start.AddMonths(1).AddSeconds(-1);

            return range;
        }
        public DateRange ThisWeek(DateTime date)
        {
            DateRange range = new DateRange();

            range.Start = date.Date.AddDays(-(int)date.DayOfWeek + 1);
            range.End = range.Start.AddDays(7).AddSeconds(-1);

            return range;
        }


        public string SetFormat(string number, int len)
        {
            string result = "";
            if (len - (int)number.Length > 0)
                for (var i = 0; i < len - (int)number.Length; i++)
                    result += "0";
            return result + number;
        }


        public Dictionary<string, object> ObjectToDictionary(object obj)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string propName = prop.Name;
                var val = obj.GetType().GetProperty(propName).GetValue(obj, null);
                if (val != null)
                {
                    ret.Add(propName, val.ToString());
                }
                else
                {
                    ret.Add(propName, null);
                }
            }

            return ret;
        }


        public string UppercaseFirst(string s)
        {
            string stringToReturn = "";
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            string[] x = s.Split(' ');
            for (int i = 0; i < x.Length; i++)
            {
                stringToReturn += char.ToUpper(x[i][0]) + x[i].Substring(1).ToLower() + " ";
            }
            // Return char and concat substring.
            return stringToReturn;
        }

        /// <summary>
        /// If the string is multiple words, it returns the first letter of each word capitalized
        /// If it is one word, it returns the word with first letter capitalized
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string UppercaseFirstAbbreviate(string s)
        {
            string stringToReturn = "";
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            string[] x = s.Split(' ');
            if (x.Length > 1)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    stringToReturn += char.ToUpper(x[i][0]);
                }
            }
            else
                stringToReturn = char.ToUpper(x[0][0]) + x[0].Substring(1).ToLower();
            // Return char and concat substring.
            return stringToReturn;
        }


        /// <summary>
        /// Used in search results, strips html tags and returns text highlighting the keywords entered in the search
        /// </summary>
        /// <param name="text"></param>
        /// <param name="keyword"></param>
        /// <param name="preText"></param>
        /// <param name="postText"></param>
        /// <param name="trimLength"></param>
        /// <returns></returns>
        public string HighlightKeywords(string text, string keyword, string preText, string postText, int trimLength)
        {
            text = Regex.Replace(text, "<.*?>", string.Empty);
            string output = text;
            int keywordIndex = CultureInfo.CurrentUICulture.CompareInfo.IndexOf(text, keyword, CompareOptions.IgnoreCase);

            if (keywordIndex != -1)
            {
                var tempArray = text.ToLower().Split(new string[] { keyword.ToLower() }, StringSplitOptions.RemoveEmptyEntries);
                string startingText = text.Substring(0, keywordIndex);
                startingText = (startingText.Length > trimLength) ? startingText.Substring(startingText.Length - trimLength) : startingText;
                string endingText = text.Substring(keywordIndex + keyword.Length);
                endingText = (endingText.Length > trimLength) ? endingText.Substring(0, trimLength - 1) : endingText;

                output = startingText + preText + text.Substring(keywordIndex, keyword.Length) + postText + endingText + "...";
            }
            else
            {
                output = (text.Length > trimLength) ? text.Substring(0, trimLength - 1) + "..." : text;
            }
            return output;
        }



        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public int ComputeLevenshteinCost(string searchedString, string keyword)
        {
            if (!string.IsNullOrEmpty(searchedString))
            {
                var words = searchedString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                int localEn = -1;
                foreach (var i in words)
                {
                    int cost;
                    if (i.ToLower().StartsWith(keyword.ToLower()))
                    {
                        cost = 0;
                    }
                    else
                    {
                        cost = ComputeLevenshteinDistance(keyword.ToLower(), i.ToLower());
                    }
                    localEn = (localEn == -1) ? cost
                        : cost < localEn ? cost : localEn;
                }
                return localEn;
            }
            else
            {
                return 100000;
            }
        }

    }
}
