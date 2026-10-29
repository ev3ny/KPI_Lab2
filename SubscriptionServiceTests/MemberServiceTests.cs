using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;

namespace SubscriptionServiceTests
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _repo = new();
        private readonly Mock<IPaymentService> _payment = new();
        private readonly Mock<INotificationService> _notify = new();

        private readonly MemberService _service;

        public MemberServiceTests()
        {
            _service = new MemberService(_repo.Object);
        }

        /// <summary>
        /// Test: Retrieving an existing member should return a non-null member object.
        /// </summary>
        [Fact]
        public void GetMember_WhenMemberExists_ReturnsNotNullMember()
        {
            // Arrange
            const string MEMBER_NAME = "Alex";
            var member = new Member { Id = 1, Name = MEMBER_NAME, IsActive = true };
            _repo.Setup(r => r.GetById(It.IsAny<int>())).Returns(member);

            // Act
            var result = _service.GetMember(1);

            // Assert
            Assert.NotNull(result);
        }

        /// <summary>
        /// Test: Retrieving an existing member should return the correct member name.
        /// </summary>
        [Fact]
        public void GetMember_WhenMemberExists_ReturnsCorrectName()
        {
            // Arrange
            const string MEMBER_NAME = "Alex";
            var member = new Member { Id = 1, Name = MEMBER_NAME, IsActive = true };
            _repo.Setup(r => r.GetById(1)).Returns(member);

            // Act
            var result = _service.GetMember(1);

            // Assert
            Assert.Equal(MEMBER_NAME, result?.Name);
        }

        /// <summary>
        /// Test: Retrieving a non-existing member should return null.
        /// </summary>
        [Fact]
        public void GetMember_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            _repo.Setup(r => r.GetById(99)).Returns((Member?)null);

            // Act
            var result = _service.GetMember(99);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test: Checking an active member's status should return true.
        /// </summary>
        [Fact]
        public void IsActive_WhenMemberIsActive_ReturnsTrue()
        {
            // Arrange
            var member = new Member { Id = 1, IsActive = true };
            _repo.Setup(r => r.GetById(1)).Returns(member);

            // Act
            var result = _service.IsActive(1);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Test: Checking an inactive member's status should return false.
        /// </summary>
        [Fact]
        public void IsActive_WhenMemberIsNotActive_ReturnsFalse()
        {
            // Arrange
            var member = new Member { Id = 2, IsActive = false };
            _repo.Setup(r => r.GetById(2)).Returns(member);

            // Act
            var result = _service.IsActive(2);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test: Checking status when member is missing should not return true.
        /// </summary>
        [Fact]
        public void IsActive_WhenMemberIsNull_NotReturnsTrue()
        {
            // Arrange
            _repo.Setup(r => r.GetById(404)).Returns((Member?)null);

            // Act
            var result = _service.IsActive(404);

            // Assert
            Assert.NotEqual(true, result);
        }

        /// <summary>
        /// Parameterized test for GetMember with various IDs.
        /// </summary>
        /// <param name="memberId">The ID of the member to retrieve</param>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public void GetMember_VariousIds_CallsRepositoryGetById(int memberId)
        {
            // Arrange
            _repo.Setup(r => r.GetById(memberId)).Returns(new Member { Id = memberId });

            // Act
            _service.GetMember(memberId);

            // Assert
            _repo.Verify(r => r.GetById(memberId), Times.Once);
        }
    }
}