using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersMicroservice.core.Application;
using OrdersMicroservice.Core.Common;
using OrdersMicroservice.src.order.application.commands.create_extra_cost.types;
using OrdersMicroservice.src.order.application.commands.create_extra_cost;
using OrdersMicroservice.src.order.application.repositories;
using OrdersMicroservice.src.order.infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection.Metadata;
using OrdersMicroservice.src.extracost.domain.value_objects;
using OrdersMicroservice.src.order.domain.entities.extraCost;
using OrdersMicroservice.src.order.infrastructure.validators;
using OrdersMicroservice.core.Common;
using OrdersMicroservice.src.order.application.repositories.dto;
using OrdersMicroservice.src.order.application.repositories.exceptions;


namespace TestOrderMicoservice.extraCostTests
{
    public class ExtraCostControllerTests
    {
        private readonly Mock<IExtraCostRepository> _mockExtraCostRepository;
        private readonly Mock<IIdGenerator<string>> _mockIdGenerator;
        private readonly ExtraCostController _controller;

        public ExtraCostControllerTests()
        {
            _mockExtraCostRepository = new Mock<IExtraCostRepository>();
            _mockIdGenerator = new Mock<IIdGenerator<string>>();
            _controller = new ExtraCostController(_mockExtraCostRepository.Object, _mockIdGenerator.Object);
        }

        [Fact]
        public async Task CreateExtraCost_ReturnsCreated_WhenServiceSucceeds()
        {
            // Arrange
            var command = new CreateExtraCostCommand(100, "Valid Description");
            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            var extraCostTest = new ExtraCost(
                                new ExtraCostId(extracdId), 
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );
            var validator = new CreateExtraCostCommandValidator();
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(extracdId);
            _mockExtraCostRepository.Setup(r => r.SaveExtraCost(It.IsAny<ExtraCost>())).ReturnsAsync(extraCostTest);
            var service = new CreateExtraCostCommandHandler(_mockExtraCostRepository.Object, _mockIdGenerator.Object);


            // Act
            var validationResult = validator.Validate(command);
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            var response = await service.Execute(command);
            var result = await _controller.CreateExtraCost(command);

            // Assert
            Assert.True(response.IsSuccessful);
            Assert.True(result is CreatedResult);


        }

        [Fact]
        public void CreateExtraCost_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var command = new CreateExtraCostCommand(0, "");
            var validator = new CreateExtraCostCommandValidator();

            // Act
            var validationResult = validator.Validate(command);
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            // Assert
            Assert.False(validationResult.IsValid);
        }


        [Fact]
        public async Task GetAllExtraCosts_ReturnsOk_WhenExtraCostsFound()
        {
            // Arrange
            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            var extracost1 = new ExtraCost(
                                new ExtraCostId(extracdId),
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );
            var extracost2 = new ExtraCost(
                                new ExtraCostId(extracdId),
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );

            var extracosts = new List<ExtraCost>
            {

                extracost1,
                extracost2,
            };
            var getAllExtracDto = new GetAllExtraCostsDto();
            _mockExtraCostRepository.Setup(repo => repo.GetAllExtraCosts(getAllExtracDto))
            .ReturnsAsync(_Optional<List<ExtraCost>>.Of(extracosts));

            // Act
            var result = await _controller.GetAllExtraCosts(getAllExtracDto);

            // Assert
            Assert.True(result is OkObjectResult);
        }


        [Fact]
        public async Task GetAllExtraCosts_ReturnsNotFound_WhenNoExtraCostsFound()
        {
            // Arrange
            var getAllExtracDto = new GetAllExtraCostsDto();
            _mockExtraCostRepository.Setup(r => r.GetAllExtraCosts(getAllExtracDto)).ReturnsAsync(_Optional<List<ExtraCost>>.Empty());

            // Act
            var result = await _controller.GetAllExtraCosts(getAllExtracDto);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }


        [Fact]
        public async Task GetExtraCostById_ReturnsOk_WhenExtraCostFound()
        {
            // Arrange
            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            var extraCost = new ExtraCost(
                                new ExtraCostId(extracdId),
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );
            _mockExtraCostRepository.Setup(r => r.GetExtraCostById(It.IsAny<ExtraCostId>())).ReturnsAsync(_Optional<ExtraCost>.Of(extraCost));

            // Act
            var result = await _controller.GetExtraCostById(extracdId);

            // Assert
            Assert.True(result is OkObjectResult);

        }

        [Fact]
        public async Task GetExtraCostById_ReturnsNotFound_WhenExtraCostNotFound()
        {
            // Arrange
            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            _mockExtraCostRepository.Setup(r => r.GetExtraCostById(It.IsAny<ExtraCostId>())).ReturnsAsync(_Optional<ExtraCost>.Empty());

            // Act
            var result = await _controller.GetExtraCostById(extracdId );

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }


    }
}
