using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;

namespace SubscriptionServiceTests
{
    public class SubscriptionServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new();

        private readonly SubscriptionService _service;

        public SubscriptionServiceTests()
        {
            _service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);
        }

        /// <summary>
        /// Test: Renewing a subscription for a non-existing member should throw ArgumentException.
        /// </summary>
        [Fact]
        public void RenewSubscription_MemberNotFound_ThrowsArgumentException()
        {
            // Arrange
            _repo.Setup(r => r.GetById(1)).Returns((Member?)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.RenewSubscription(1, 100m, 30));
        }

        /// <summary>
        /// Test: Renewing a subscription for a non-existing member should not update repository.
        /// </summary>
        [Fact]
        public void RenewSubscription_MemberNotFound_NeverUpdatesRepository()
        {
            // Arrange
            _repo.Setup(r => r.GetById(1)).Returns((Member?)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.RenewSubscription(1, 100m, 30));
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);
        }

        /// <summary>
        /// Test: Renewing a subscription with valid payment should return true and sends notification.
        /// </summary>
        [Fact]
        public void RenewSubscription_ValidPayment_ReturnsTrueAndSendNotification()
        {
            // Arrange
            var member = new Member { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, 150m)).Returns(true);

            // Act
            var result = _service.RenewSubscription(1, 150m, 30);

            // Assert
            Assert.True(result);
            _notify.Verify(n => n.SendNotification("Subscription renewed!", 1), Times.Once);
        }

        /// <summary>
        /// Test: Renewing a subscription with valid payment should trigger member update once.
        /// </summary>
        [Fact]
        public void RenewSubscription_ValidPayment_UpdatesMemberOnce()
        {
            // Arrange
            var member = new Member { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetById(1)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(1, 150m)).Returns(true);

            // Act
            _service.RenewSubscription(1, 150m, 30);

            // Assert
            _repo.Verify(r => r.Update(It.Is<Member>(m => m.Id == 1 && m.IsActive == true)), Times.Once);
        }

        /// <summary>
        /// Test: Renewing a subscription with invalid payment should return false
        /// and must not update repository and send notifications.
        /// </summary>
        [Fact]
        public void RenewSubscription_InvalidPayment_ReturnsFalseAndNeverUpdatesAndNotifies()
        {
            // Arrange
            var member = new Member { Id = 2, IsActive = false };
            _repo.Setup(r => r.GetById(2)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(2, 50m)).Returns(false);

            // Act
            var result = _service.RenewSubscription(2, 50m, 30);

            // Assert
            Assert.False(result);
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Test: Member with non-expired subscription must not be deactivated.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_NonExpiredMember_RemainsActive()
        {
            // Arrange
            var activeMember = new Member { Id = 11, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(10) };
            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { activeMember });

            // Act
            _service.DeactivateExpiredMembers();

            // Assert
            Assert.True(activeMember.IsActive);
        }

        /// <summary>
        /// Test: Member without SubscriptionEnd date must not be deactivated.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_NullSubscriptionEnd_RemainsActive()
        {
            // Arrange
            var memberWithoutDate = new Member { Id = 12, IsActive = true, SubscriptionEnd = null };
            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { memberWithoutDate });

            // Act
            _service.DeactivateExpiredMembers();

            // Assert
            Assert.True(memberWithoutDate.IsActive);
        }

        /// <summary>
        /// Test: Deactivating expired members successfully turns off active status.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ExpiredMember_SetsActiveToFalse()
        {
            // Arrange
            var expiredMember = new Member { Id = 10, IsActive = true, SubscriptionEnd = DateTime.Now.AddDays(-2) };
            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { expiredMember });

            // Act
            _service.DeactivateExpiredMembers();

            // Assert
            Assert.False(expiredMember.IsActive);
            _repo.Verify(r => r.Update(expiredMember), Times.Once);
            _notify.Verify(n => n.SendNotification("Membership expired", 10), Times.Once);
        }

        /// <summary>
        /// Parameterized test for RenewSubscription with various payment amounts.
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <param name="expectedResult">Expected boolean result</param>
        [Theory]
        [InlineData(100, true)]
        [InlineData(250, true)]
        [InlineData(30, false)]
        public void RenewSubscription_VariousAmounts_ReturnsExpectedResult(decimal amount, bool expectedResult)
        {
            // Arrange
            var member = new Member { Id = 5, IsActive = false };
            _repo.Setup(r => r.GetById(5)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(5, amount)).Returns(expectedResult);

            // Act
            var result = _service.RenewSubscription(5, amount, 30);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
