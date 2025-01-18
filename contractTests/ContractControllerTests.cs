using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersMicroservice.core.Application;
using OrdersMicroservice.Core.Common;
using OrdersMicroservice.src.contract.application.commands.create_contract.types;
using OrdersMicroservice.src.contract.application.commands.create_contract;
using OrdersMicroservice.src.contract.application.repositories;
using OrdersMicroservice.src.contract.infrastructure.validators;
using OrdersMicroservice.src.contract.infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrdersMicroservice.core.Common;
using OrdersMicroservice.src.contract.domain.entities.policy.value_objects;
using OrdersMicroservice.src.contract.domain.entities.policy;
using System.Diagnostics.Contracts;
using OrdersMicroservice.src.contract.domain;
using Contract = OrdersMicroservice.src.contract.domain.Contract;
using OrdersMicroservice.src.contract.domain.entities.vehicle;
using OrdersMicroservice.src.contract.domain.value_objects;
using OrdersMicroservice.src.contract.domain.entities.vehicle.value_objects;
using OrdersMicroservice.src.contract.application.repositories.dto;
using OrdersMicroservice.src.contract.application.commands.update_contract.types;
using OrdersMicroservice.src.contract.application.commands.update_contract;
using OrdersMicroservice.src.contract.infrastructure.dto;
using OrdersMicroservice.src.user.application.commands.update_contract.types;
using OrdersMicroservice.src.contract.domain.exceptions;




namespace TestOrderMicoservice.contractTests
{
    public class ContractControllerTests
    {
        private readonly Mock<IContractRepository> _mockContractRepository;
        private readonly Mock<IPolicyRepository> _mockPolicyRepository;
        private readonly Mock<IIdGenerator<string>> _mockIdGenerator;
        private readonly ContractController _controller;

        public ContractControllerTests()
        {
            _mockContractRepository = new Mock<IContractRepository>();
            _mockPolicyRepository = new Mock<IPolicyRepository>();
            _mockIdGenerator = new Mock<IIdGenerator<string>>();
            _controller = new ContractController(_mockContractRepository.Object, _mockPolicyRepository.Object, _mockIdGenerator.Object);
        }


        [Fact]
        public async Task CreateContract_ReturnsCreated_WhenExecutionSucceeds()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var command = new CreateContractCommand(
                1001,
                expirationDate,
                "ab143cd",
                "Brand",
                "Model",
                2020,
                "Red",
                10000,
                12345678,
                "Owner",
                policyId
                );
            var policy = new Policy(
                             new PolicyId(policyId),
                             new PolicyName("Policy Name"),
                             new PolicyMonetaryCoverage(1000),
                             new PolicyKmCoverage(100),
                             new PolicyBaseKmPrice(10)
                             );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contratId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var validator = new CreateContractCommandValidator();
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(contratId);
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(vehicleId);
            _mockPolicyRepository.Setup(r => r.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(_Optional<Policy>.Of(policy));
            _mockContractRepository.Setup(r => r.SaveContract(It.IsAny<Contract>())).ReturnsAsync(contract);

            var service = new CreateContractCommandHandler(_mockIdGenerator.Object, _mockContractRepository.Object, _mockPolicyRepository.Object);



            // Act
            var response = await service.Execute(command);
            var result = await _controller.CreateContract(command);

            // Assert
            Assert.True(response.IsSuccessful);
            Assert.True(result is CreatedResult);
        }

        [Fact]
        public async Task CreateContract_ReturnsBadRequest_WhenExecutionFails()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var command = new CreateContractCommand(
                1001,
                expirationDate,
                "ab143cd",
                "Brand",
                "Model",
                2020,
                "Red",
                10000,
                12345678,
                "Owner",
                policyId
                );
            var policy = new Policy(
                             new PolicyId(policyId),
                             new PolicyName("Policy Name"),
                             new PolicyMonetaryCoverage(1000),
                             new PolicyKmCoverage(100),
                             new PolicyBaseKmPrice(10)
                             );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contratId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var validator = new CreateContractCommandValidator();
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(contratId);
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(vehicleId);
            _mockPolicyRepository.Setup(r => r.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(_Optional<Policy>.Empty);


            var service = new CreateContractCommandHandler(_mockIdGenerator.Object, _mockContractRepository.Object, _mockPolicyRepository.Object);



            // Act
            var response = await service.Execute(command);
            var result = await _controller.CreateContract(command);

            // Assert
            Assert.False(response.IsSuccessful);
            ;
        }


        [Fact]
        public async Task CreateContract_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var command = new CreateContractCommand(0, DateTime.Now, "", "", "", 0, "", 0, 0, "", "");
            var validator = new CreateContractCommandValidator();
            var validationResult = validator.Validate(command);
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            // Act
            var result = await _controller.CreateContract(command);

            // Assert
            Assert.False(validationResult.IsValid); ;
        }

        [Fact]
        public async Task GetAllContracts_ReturnsOkResult_WhenContractsExist()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var policy = new Policy(
                 new PolicyId(policyId),
                 new PolicyName("Policy Name"),
                 new PolicyMonetaryCoverage(1000),
                 new PolicyKmCoverage(100),
                 new PolicyBaseKmPrice(10)
                 );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contratId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var contracts = new List<Contract>
        {
            contract,

        };
            var optionalContracts = _Optional<List<Contract>>.Of(contracts);
            _mockContractRepository.Setup(repo => repo.GetAllContracts(It.IsAny<GetAllContractsDto>()))
                .ReturnsAsync(optionalContracts);

            // Act
            var result = await _controller.GetAllContracts(new GetAllContractsDto());

            // Assert
            Assert.NotNull(result);
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetAllContracts_ReturnsNotFound_WhenNoContractsExist()
        {
            // Arrange
            var optionalContracts = _Optional<List<Contract>>.Empty();
            _mockContractRepository.Setup(repo => repo.GetAllContracts(It.IsAny<GetAllContractsDto>())).ReturnsAsync(optionalContracts);

            // Act
            var result = await _controller.GetAllContracts(new GetAllContractsDto());

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task GetContractById_ReturnsOkResult_WhenContractExists()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var policy = new Policy(
                 new PolicyId(policyId),
                 new PolicyName("Policy Name"),
                 new PolicyMonetaryCoverage(1000),
                 new PolicyKmCoverage(100),
                 new PolicyBaseKmPrice(10)
                 );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contratId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var optionalContract = _Optional<Contract>.Of(contract);
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);

            // Act
            var result = await _controller.GetContractById(contratId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetContractById_ReturnsNotFound_WhenContractDoesNotExist()
        {
            // Arrange
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var optionalContract = _Optional<Contract>.Empty();
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);

            // Act
            var result = await _controller.GetContractById(contratId);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task GetContractByContractNumber_ReturnsOkResult_WhenContractExists()
        {
            // Arrange
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var policy = new Policy(
                 new PolicyId(policyId),
                 new PolicyName("Policy Name"),
                 new PolicyMonetaryCoverage(1000),
                 new PolicyKmCoverage(100),
                 new PolicyBaseKmPrice(10)
                 );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contratId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var optionalContract = _Optional<Contract>.Of(contract);
            _mockContractRepository.Setup(repo => repo.GetContractByContractNumber(It.IsAny<NumberContract>())).ReturnsAsync(optionalContract);

            // Act
            var result = await _controller.GetContractByContractNumber(1001);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetContractByContractNumber_ReturnsNotFound_WhenContractDoesNotExist()
        {
            // Arrange

            var optionalContract = _Optional<Contract>.Empty();
            _mockContractRepository.Setup(repo => repo.GetContractByContractNumber(It.IsAny<NumberContract>())).ReturnsAsync(optionalContract);


            // Act
            var result = await _controller.GetContractByContractNumber(1001);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task GetContractVehicleById_ReturnsOkResult_WhenVehicleExists()
        {
            // Arrange
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );
            var optionalVehicle = _Optional<Vehicle>.Of(vehicle);
            _mockContractRepository.Setup(repo => repo.GetContractVehicle(It.IsAny<ContractId>())).ReturnsAsync(optionalVehicle);

            // Act
            var result = await _controller.GetContractVehicleById(contratId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetContractVehicleById_ReturnsNotFound_WhenVehicleDoesNotExist()
        {
            // Arrange
            var contractId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var optionalVehicle = _Optional<Vehicle>.Empty();
            _mockContractRepository.Setup(repo => repo.GetContractVehicle(It.IsAny<ContractId>())).ReturnsAsync(optionalVehicle);

            // Act
            var result = await _controller.GetContractVehicleById(contractId);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task GetContractPolicyById_ReturnsOkResult_WhenVehicleExists()
        {
            // Arrange
            var contratId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var policy = new Policy(
                 new PolicyId(policyId),
                 new PolicyName("Policy Name"),
                 new PolicyMonetaryCoverage(1000),
                 new PolicyKmCoverage(100),
                 new PolicyBaseKmPrice(10)
                 );
            var optionalPolicy = _Optional<Policy>.Of(policy);
            _mockContractRepository.Setup(repo => repo.GetContractPolicy(It.IsAny<ContractId>())).ReturnsAsync(optionalPolicy);

            // Act
            var result = await _controller.GetContractPolicyById(contratId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetContractPolicyById_ReturnsNotFound_WhenVehicleDoesNotExist()
        {
            // Arrange
            var contractId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var optionalPolicy = _Optional<Policy>.Empty();
            _mockContractRepository.Setup(repo => repo.GetContractPolicy(It.IsAny<ContractId>())).ReturnsAsync(optionalPolicy);


            // Act
            var result = await _controller.GetContractPolicyById(contractId);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task UpdateContractById_ReturnsOkResult_WhenUpdateIsSuccessful()
        {
            // Arrange
            var contractId = "fa750e04-c73c-459f-8a5b-f599996d1b59";
            var policyId = "c4b13887-0e46-4b28-a24b-9b74c6001324";
            var vehicleId = "fad74e97-5e31-42ff-8a06-5f1a14687a6e";
            var expirationDate = new DateTime(2033, 12, 31, 23, 59, 59);
            var policy = new Policy(
                 new PolicyId(policyId),
                 new PolicyName("Policy Name"),
                 new PolicyMonetaryCoverage(1000),
                 new PolicyKmCoverage(100),
                 new PolicyBaseKmPrice(10)
                 );
            var vehicle = new Vehicle(
                              new VehicleId(vehicleId),
                              new VehicleLicensePlate("ab143cd"),
                              new VehicleBrand("Brand"),
                              new VehicleModel("Model"),
                              new VehicleYear(2020),
                              new VehicleColor("Red"),
                              new VehicleKm(10000),
                              new VehicleOwnerDni(12345678),
                              new VehicleOwnerName("Owner")
                              );

            var contract = Contract.Create(
                                    new ContractId(contractId),
                                    new NumberContract(1001),
                                    new ContractExpitionDate(expirationDate),
                                    vehicle,
                                    policy

                               );
            var updateDto = new UpdateContractDto(policyId, expirationDate);
            var command = new UpdateContractCommand(contractId, updateDto.ExpirationDate, updateDto.PolicyId);

            var optionalContract = _Optional<Contract>.Of(contract);
            var optionalPolicy = _Optional<Policy>.Of(policy);
            _mockContractRepository.Setup(repo => repo.UpdateContract(It.IsAny<Contract>())).ReturnsAsync(new ContractId(contractId));
            _mockPolicyRepository.Setup(repo => repo.GetPolicyById(It.IsAny<PolicyId>())).ReturnsAsync(optionalPolicy);
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);
            var service = new UpdateContractByIdCommandHandler(_mockContractRepository.Object, _mockPolicyRepository.Object);


            // Act
            var result = await _controller.UpdateContractById(updateDto, contractId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task ToggleActivityContractById_ReturnsOkResult_WhenToggleIsSuccessful()
        {
            // Arrange
            var contractId = "fa750e04-c73c-459f-8a5b-f599996d1b59";

            _mockContractRepository.Setup(repo => repo.ToggleActivityContractById(It.IsAny<ContractId>())).ReturnsAsync(new ContractId(contractId));

            // Act
            var result = await _controller.ToggleActivityContractById(contractId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task ToggleActivityContractById_ReturnsOkResult_WhenToggleFail()
        {
            // Arrange
            var contractId = "";

            // Act
            var result = await _controller.ToggleActivityContractById(contractId);

            // Assert
            Assert.False(result is OkObjectResult);

        }



    }
}
