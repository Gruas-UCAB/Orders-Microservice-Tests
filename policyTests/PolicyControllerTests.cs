using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersMicroservice.core.Application;
using OrdersMicroservice.core.Common;
using OrdersMicroservice.Core.Common;
using OrdersMicroservice.src.contract.application.commands.create_policy.types;
using OrdersMicroservice.src.contract.application.commands.update_contract;
using OrdersMicroservice.src.contract.application.commands.update_contract.types;
using OrdersMicroservice.src.contract.application.repositories;
using OrdersMicroservice.src.contract.application.repositories.dto;
using OrdersMicroservice.src.contract.application.repositories.exceptions;
using OrdersMicroservice.src.contract.domain.entities.policy;
using OrdersMicroservice.src.contract.domain.entities.policy.value_objects;
using OrdersMicroservice.src.contract.infrastructure;
using OrdersMicroservice.src.contract.infrastructure.dto;
using OrdersMicroservice.src.contract.infrastructure.validators;
using OrdersMicroservice.src.order.application.commands.create_extra_cost;
using OrdersMicroservice.src.policy.application.commands.create_policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestOrderMicoservice.policyTests
{
    public class PolicyControllerTests
    {
        private readonly Mock<IPolicyRepository> _mockPolicyRepository;
        private readonly Mock<IIdGenerator<string>> _mockIdGenerator;
        private readonly PolicyController _controller;

        public PolicyControllerTests()
        {
            _mockPolicyRepository = new Mock<IPolicyRepository>();
            _mockIdGenerator = new Mock<IIdGenerator<string>>();
            _controller = new PolicyController(_mockPolicyRepository.Object, _mockIdGenerator.Object);
        }

        [Fact]
        public async Task CreatePolicy_ReturnsCreated_WhenExecutionSucceeds()
        {
            // Arrange
            var command = new CreatePolicyCommand("Policy Name", 1000, 100, 10);
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var policy = new Policy(
                             new PolicyId(policyId),
                             new PolicyName("Policy Name"),
                             new PolicyMonetaryCoverage(1000),
                             new PolicyKmCoverage(100),
                             new PolicyBaseKmPrice(10)
                          );
            var validator = new CreatePolicyCommandValidator();
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(policyId);
            _mockPolicyRepository.Setup(r => r.SavePolicy(It.IsAny<Policy>())).ReturnsAsync(policy);
            var service = new CreatePolicyCommandHandler(_mockIdGenerator.Object, _mockPolicyRepository.Object);


            // Act
            var validationResult = validator.Validate(command);
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            var response = await service.Execute(command);
            var result = await _controller.CreatePolicy(command);

            // Assert
            Assert.True(response.IsSuccessful);
            Assert.True(result is CreatedResult);


        }

        [Fact]
        public void CreatePolicy_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var command = new CreatePolicyCommand("Policy Name", 0, 0, 0);
            var validator = new CreatePolicyCommandValidator();
            
            // Act
            var validationResult = validator.Validate(command);
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            // Assert
            Assert.False(validationResult.IsValid);
        }

        [Fact]
        public async Task GetPolicyById_ReturnsOk_WhenPolicyFound()
        {
            // Arrange
            var command = new CreatePolicyCommand("Policy Name", 1000, 100, 10);
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var policy = new Policy(
                             new PolicyId(policyId),
                             new PolicyName("Policy Name"),
                             new PolicyMonetaryCoverage(1000),
                             new PolicyKmCoverage(100),
                             new PolicyBaseKmPrice(10)
                          );
            _mockPolicyRepository.Setup(r => r.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(_Optional<Policy>.Of(policy));

            // Act
            var result = await _controller.GetPolicyById(policyId);

            // Assert
            Assert.True(result is OkObjectResult);
        }
        [Fact]
        public async Task GetPolicyById_ReturnsNotFound_WhenPolicyNotFound()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            _mockPolicyRepository.Setup(r => r.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(_Optional<Policy>.Empty());

            // Act
            var result = await _controller.GetPolicyById(policyId);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task GetAllPolicies_ReturnsOk_WhenPoliciesFound()
        {
            // Arrange
            var data = new GetAllPolicesDto();
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var policies = new List<Policy>
        {
            new Policy(new PolicyId(policyId), 
            new PolicyName("Policy 1"), 
            new PolicyMonetaryCoverage(1000), 
            new PolicyKmCoverage(100), 
            new PolicyBaseKmPrice(10)),
            new Policy(new PolicyId(policyId), 
            new PolicyName("Policy 2"), 
            new PolicyMonetaryCoverage(2000), 
            new PolicyKmCoverage(200), 
            new PolicyBaseKmPrice(20))
        };
            _mockPolicyRepository.Setup(r => r.GetAllPolicies(data)).ReturnsAsync(_Optional<List<Policy>>.Of(policies));

            // Act
            var result = await _controller.GetAllPolicies(data);

            // Assert
            Assert.True(result is OkObjectResult);

        }

        [Fact]
        public async Task GetAllPolicies_ReturnsNotFound_WhenNoPoliciesFound()
        {
            // Arrange
            var data = new GetAllPolicesDto();
            _mockPolicyRepository.Setup(r => r.GetAllPolicies(data)).ReturnsAsync(_Optional<List<Policy>>.Empty());

            // Act
            var result = await _controller.GetAllPolicies(data);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }


        [Fact]
        public async Task UpdatePolicyById_ReturnsOk_WhenPolicyIsUpdated()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var data = new UpdatePolicyByIdDto("Updated Name", 2000, 200, 20);
            var command = new UpdatePolicyByIdCommand(policyId, data.Name, data.MonetaryCoverage, data.KmCoverage, data.BaseKmPrice);
            var policy = new Policy(
                            new PolicyId(policyId),
                            new PolicyName("Policy Name"),
                            new PolicyMonetaryCoverage(1000),
                            new PolicyKmCoverage(100),
                            new PolicyBaseKmPrice(10)
                         );
            _mockPolicyRepository.Setup(r => r.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(_Optional<Policy>.Of(policy));
            _mockPolicyRepository.Setup(r => r.UpdatePolicy(It.IsAny<Policy>())).ReturnsAsync(new PolicyId(policyId));
            var service = new UpdatePolicyByIdCommandHandler( _mockPolicyRepository.Object);

            // Act
            var result = await _controller.UpdatePolicyById(data, policyId);

            // Assert
            Assert.True(result is OkObjectResult);



        }

        [Fact]
        public async Task UpdatePolicyById_ReturnsBadRequest_WhenInvalidUpdatePolicyByIdCommand()
        {
           
            // Act
            var result = await _controller.UpdatePolicyById(null, "c4b13887-0e46-4b28-a24b-9b74c6001324");

            // Assert
            Assert.True(result is BadRequestObjectResult);


        }



    }
}
