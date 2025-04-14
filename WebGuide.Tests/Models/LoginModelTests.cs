using Xunit;
using WebGuide.Models;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace WebGuide.Tests.Models
{
    public class LoginModelTests
    {
        private List<ValidationResult> ValidateModel(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void Should_Fail_When_Email_IsMissing()
        {
            var model = new Login
            {
                Password = "password123"
            };

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Should_Fail_When_Password_IsMissing()
        {
            var model = new Login
            {
                Email = "user@mail.com"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Password"));
        }

        [Fact]
        public void Should_Fail_When_Email_IsInvalid()
        {
            var model = new Login
            {
                Email = "abc", 
                Password = "password123"
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Should_Pass_When_Model_IsValid()
        {
            var model = new Login
            {
                Email = "user@mail.com",
                Password = "password123"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }
    }
}
