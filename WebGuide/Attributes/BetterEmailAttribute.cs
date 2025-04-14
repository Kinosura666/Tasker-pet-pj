using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebGuide.Attributes
{
    public class BetterEmailAttribute : ValidationAttribute
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override bool IsValid(object value)
        {
            if (value == null) return false;
            var email = value.ToString();
            return EmailRegex.IsMatch(email);
        }
    }
}
