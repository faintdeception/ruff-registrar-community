using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<ILogger<PaymentsController>> _mockLogger;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mockPaymentService = new Mock<IPaymentService>();
        _mockLogger = new Mock<ILogger<PaymentsController>>();
        _controller = new PaymentsController(_mockPaymentService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPayments_Should_ReturnOkWithPayments()
    {
        // Arrange
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m, PaymentMethod = "Credit Card", PaymentType = "Course Fee" },
            new() { Id = "2", Amount = 150.00m, PaymentMethod = "Check", PaymentType = "Membership" }
        };

        _mockPaymentService
            .Setup(s => s.GetAllPaymentsAsync())
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPayments();

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetPayment_Should_ReturnOkWithPayment_WhenPaymentExists()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var expectedPayment = new PaymentDto 
        { 
            Id = paymentId.ToString(), 
            Amount = 100.00m, 
            PaymentMethod = "Credit Card" 
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentByIdAsync(paymentId))
            .Returns(Task.FromResult<PaymentDto?>(expectedPayment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayment, okResult.Value);
    }

    [Fact]
    public async Task GetPayment_Should_ReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockPaymentService
            .Setup(s => s.GetPaymentByIdAsync(paymentId))
            .Returns(Task.FromResult<PaymentDto?>(null));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        var actionResult = result.Result;
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task GetPaymentsByAccountHolder_Should_ReturnOkWithPayments()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m, PaymentMethod = "Credit Card" }
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentsByAccountHolderAsync(accountHolderId))
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPaymentsByAccountHolder(accountHolderId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetPaymentsByEnrollment_Should_ReturnOkWithPayments()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m, PaymentMethod = "Credit Card" }
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentsByEnrollmentAsync(enrollmentId))
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPaymentsByEnrollment(enrollmentId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetPaymentsByType_Should_ReturnOkWithPayments()
    {
        // Arrange
        var paymentType = PaymentType.CourseFee;
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m, PaymentType = "Course Fee" }
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentsByTypeAsync(paymentType))
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPaymentsByType(paymentType);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetPaymentHistory_Should_ReturnOkWithPayments_WithDateRange()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var fromDate = DateTime.Now.AddMonths(-3);
        var toDate = DateTime.Now;
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m, PaymentDate = DateTime.Now.AddMonths(-1) }
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentHistoryAsync(accountHolderId, fromDate, toDate))
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPaymentHistory(accountHolderId, fromDate, toDate);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetPaymentHistory_Should_ReturnOkWithPayments_WithoutDateRange()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var expectedPayments = new List<PaymentDto>
        {
            new() { Id = "1", Amount = 100.00m }
        };

        _mockPaymentService
            .Setup(s => s.GetPaymentHistoryAsync(accountHolderId, null, null))
            .Returns(Task.FromResult<IEnumerable<PaymentDto>>(expectedPayments));

        // Act
        var result = await _controller.GetPaymentHistory(accountHolderId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(expectedPayments, okResult.Value);
    }

    [Fact]
    public async Task GetTotalPaidByAccountHolder_Should_ReturnOkWithTotal()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var expectedTotal = 250.00m;

        _mockPaymentService
            .Setup(s => s.GetTotalPaidByAccountHolderAsync(accountHolderId, null))
            .Returns(Task.FromResult(expectedTotal));

        // Act
        var result = await _controller.GetTotalPaidByAccountHolder(accountHolderId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(expectedTotal, okResult.Value);
    }

    [Fact]
    public async Task GetTotalPaidByAccountHolder_Should_ReturnOkWithTotal_WithPaymentType()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var paymentType = PaymentType.CourseFee;
        var expectedTotal = 150.00m;

        _mockPaymentService
            .Setup(s => s.GetTotalPaidByAccountHolderAsync(accountHolderId, paymentType))
            .Returns(Task.FromResult(expectedTotal));

        // Act
        var result = await _controller.GetTotalPaidByAccountHolder(accountHolderId, paymentType);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(expectedTotal, okResult.Value);
    }

    [Fact]
    public async Task GetTotalPaidByEnrollment_Should_ReturnOkWithTotal()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var expectedTotal = 100.00m;

        _mockPaymentService
            .Setup(s => s.GetTotalPaidByEnrollmentAsync(enrollmentId))
            .Returns(Task.FromResult(expectedTotal));

        // Act
        var result = await _controller.GetTotalPaidByEnrollment(enrollmentId);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(expectedTotal, okResult.Value);
    }

    [Fact]
    public async Task CreatePayment_Should_ReturnCreatedPayment_WhenValidData()
    {
        // Arrange
        var createDto = new CreatePaymentDto
        {
            AccountHolderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentType = PaymentType.CourseFee,
            PaymentDate = DateTime.UtcNow
        };

        var createdPayment = new PaymentDto
        {
            Id = Guid.NewGuid().ToString(),
            Amount = createDto.Amount,
            PaymentMethod = "Credit Card",
            PaymentType = "Course Fee",
            PaymentDate = createDto.PaymentDate
        };

        _mockPaymentService
            .Setup(s => s.CreatePaymentAsync(createDto))
            .Returns(Task.FromResult(createdPayment));

        // Act
        var result = await _controller.CreatePayment(createDto);

        // Assert
        var actionResult = result.Result;
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Same(createdPayment, createdResult.Value);
    }

    [Fact]
    public async Task CreatePayment_Should_ReturnBadRequest_WhenServiceThrowsException()
    {
        // Arrange
        var createDto = new CreatePaymentDto
        {
            AccountHolderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentType = PaymentType.CourseFee
        };

        _mockPaymentService
            .Setup(s => s.CreatePaymentAsync(createDto))
            .ThrowsAsync(new Exception("Payment validation failed"));

        // Act
        var result = await _controller.CreatePayment(createDto);

        // Assert
        var actionResult = result.Result;
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal("Error creating payment", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdatePayment_Should_ReturnOkWithUpdatedPayment_WhenPaymentExists()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var updateDto = new UpdatePaymentDto
        {
            Amount = 150.00m,
            PaymentMethod = PaymentMethod.Check
        };

        var updatedPayment = new PaymentDto
        {
            Id = paymentId.ToString(),
            Amount = updateDto.Amount.Value,
            PaymentMethod = "Check"
        };

        _mockPaymentService
            .Setup(s => s.UpdatePaymentAsync(paymentId, updateDto))
            .Returns(Task.FromResult<PaymentDto?>(updatedPayment));

        // Act
        var result = await _controller.UpdatePayment(paymentId, updateDto);

        // Assert
        var actionResult = result.Result;
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(updatedPayment, okResult.Value);
    }

    [Fact]
    public async Task UpdatePayment_Should_ReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var updateDto = new UpdatePaymentDto
        {
            Amount = 150.00m
        };

        _mockPaymentService
            .Setup(s => s.UpdatePaymentAsync(paymentId, updateDto))
            .Returns(Task.FromResult<PaymentDto?>(null));

        // Act
        var result = await _controller.UpdatePayment(paymentId, updateDto);

        // Assert
        var actionResult = result.Result;
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task DeletePayment_Should_ReturnNoContent_WhenPaymentDeletedSuccessfully()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockPaymentService
            .Setup(s => s.DeletePaymentAsync(paymentId))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _controller.DeletePayment(paymentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePayment_Should_ReturnNotFound_WhenPaymentNotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockPaymentService
            .Setup(s => s.DeletePaymentAsync(paymentId))
            .Returns(Task.FromResult(false));

        // Act
        var result = await _controller.DeletePayment(paymentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
