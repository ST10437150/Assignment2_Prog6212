
using RaceDayApi.Services;
using Xunit;

namespace RaceDayApi.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Hash_ShouldNotStorePlainTextPassword()
        {
            // Arrange
            var passwordService = new PasswordService();
            var password = "Password123!";

            // Act
            var hashedPassword = passwordService.Hash(password);

            // Assert
            Assert.NotEqual(password, hashedPassword);
            Assert.True(passwordService.Verify(password, hashedPassword));
            Assert.False(passwordService.Verify("WrongPassword123!", hashedPassword));
        }
    }
}
