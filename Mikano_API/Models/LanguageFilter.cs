using System;
namespace Mikano_API.Models
{
    public class LanguageFilter
    {
        public string GetProperty(string name, string language)
        {
            language = language + "" == "en" ? "" : (language + "").ToLower();
            if (!string.IsNullOrEmpty(language))
            {
                name = Char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
            var x = this.GetType();
            return GetType().GetProperty(language + name).GetValue(this, null) != null ? GetType().GetProperty(language + name).GetValue(this, null).ToString() : "";
        }


    }
}