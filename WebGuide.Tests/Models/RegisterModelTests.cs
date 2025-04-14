using Xunit;
using WebGuide.Models;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebGuide.Tests.Models
{
    public class RegisterModelTests
    {
        private List<ValidationResult> ValidateModel(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void Should_Fail_When_Username_IsMissing()
        {
            var model = new Register
            {
                Email = "user@mail.com",
                Password = "pass123",
                confirmPassword = "pass123"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Username"));
        }

        [Fact]
        public void Should_Fail_When_Email_IsInvalid()
        {
            var model = new Register
            {
                Username = "test",
                Email = "invalid_email",
                Password = "pass123",
                confirmPassword = "pass123"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Should_Fail_When_Passwords_DoNotMatch()
        {
            var model = new Register
            {
                Username = "test",
                Email = "user@mail.com",
                Password = "pass123",
                confirmPassword = "wrongpass"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("confirmPassword"));
        }

        [Fact]
        public void Should_Pass_When_Model_IsValid()
        {
            var model = new Register
            {
                Username = "validuser",
                Email = "user@mail.com",
                Password = "pass123456",
                confirmPassword = "pass123456"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }


    }
}
