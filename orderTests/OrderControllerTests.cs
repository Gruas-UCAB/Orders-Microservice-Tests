using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersMicroservice.core.Application;
using OrdersMicroservice.Core.Common;
using OrdersMicroservice.src.contract.application.repositories;
using OrdersMicroservice.src.contract.domain.value_objects;
using OrdersMicroservice.src.order.application.commands.create_order;
using OrdersMicroservice.src.order.application.commands.create_order.types;
using OrdersMicroservice.src.order.application.repositories;
using OrdersMicroservice.src.order.application.repositories.dto;
using OrdersMicroservice.src.order.domain.value_objects;
using OrdersMicroservice.src.order.domain;
using OrdersMicroservice.src.order.infrastructure;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using OrdersMicroservice.core.Common;
using OrdersMicroservice.src.contract.domain.entities.policy.value_objects;
using OrdersMicroservice.src.contract.domain.entities.vehicle.value_objects;
using OrdersMicroservice.src.contract.domain.entities.vehicle;
using OrdersMicroservice.src.contract.domain;
using OrdersMicroservice.src.contract.domain.entities.policy;
using OrdersMicroservice.src.order.application.state_machine.events;
using System.Net;
using OrdersMicroservice.src.order.application.commands.assign_conductor.types;
using OrdersMicroservice.src.order.application.commands.assign_conductor;
using OrdersMicroservice.src.order.infrastructure.dto;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using OrdersMicroservice.src.order.application.commands.accept_order.types;
using OrdersMicroservice.src.order.application.commands.toggle_accept_order;
using OrdersMicroservice.src.order.application.commands.locate_order.types;
using OrdersMicroservice.src.order.application.commands.locate_order;
using OrdersMicroservice.src.order.application.commands.start_order.types;
using OrdersMicroservice.src.order.application.commands.start_order;
using OrdersMicroservice.src.order.application.commands.finish_order.types;
using OrdersMicroservice.src.order.application.commands.finish_order;
using OrdersMicroservice.src.order.application.commands.pay_order.types;
using OrdersMicroservice.src.order.application.commands.pay_order;
using OrdersMicroservice.src.order.application.commands.add_extra_costs_to_order.types;
using OrdersMicroservice.src.order.application.commands.add_extra_costs_to_order;
using OrdersMicroservice.src.extracost.domain.value_objects;
using OrdersMicroservice.src.order.domain.entities.extraCost;
using OrdersMicroservice.src.order.application.commands.cancel_order.types;
using OrdersMicroservice.src.order.application.commands.cancel_order;

namespace TestOrderMicoservice.orderTests
{
    public class OrderControllerTests
    {
        private readonly Mock<IIdGenerator<string>> _mockIdGenerator;
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IContractRepository> _mockContractRepository;
        private readonly Mock<IExtraCostRepository> _mockExtraCostRepository;
        private readonly Mock<IRestClient> _mockRestClient;
        private readonly Mock<IBus> _mockBus;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            _mockIdGenerator = new Mock<IIdGenerator<string>>();
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockContractRepository = new Mock<IContractRepository>();
            _mockExtraCostRepository = new Mock<IExtraCostRepository>();
            _mockRestClient = new Mock<IRestClient>();
            _mockBus = new Mock<IBus>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _controller = new OrderController(
                _mockIdGenerator.Object,
                _mockOrderRepository.Object,
                _mockContractRepository.Object,
                _mockExtraCostRepository.Object,
                _mockRestClient.Object,
                _mockBus.Object,
                _mockPublishEndpoint.Object
            );
        }


        [Fact]
        public async Task CreateOrder_ReturnsCreated_WhenSuccessful()
        {
            // Arrange
            var token = "Bearer token";
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var command = new CreateOrderCommand(orderDate, "vehiculo accidentado", "40.7128,-74.0060", "39.7128,-71.0060", 
                "0db44d22-19e6-44d0-8f09-8aa0be5a93c4", "f57f0a79-b23b-496b-bcae-f9ef13529232");
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("por asignar"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
    );

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
            var userExistsRequestp = new RestRequest($"https://localhost:5350/user/{command.OrderDispatcherId}", Method.Get);
            userExistsRequestp.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""provider"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });
            var optionalContract = _Optional<Contract>.Of(contract);
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);
            _mockOrderRepository.Setup(repo => repo.GetAllOrders(It.IsAny<GetAllOrdersDto>())).ReturnsAsync(_Optional<List<Order>>.Empty());
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(orderId);
            _mockOrderRepository.Setup(repo => repo.SaveOrder(It.IsAny<Order>())).ReturnsAsync(order);


           var service = new CreateOrderCommandHandler(_mockIdGenerator.Object, _mockOrderRepository.Object, _mockContractRepository.Object, _mockPublishEndpoint.Object);


            // Act

            var response = await service.Execute(command);
            var result = await _controller.CreateOrder(command, token);

            // Assert
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task CreateOrder_ReturnsNotFound_WhenDispatcherNotFound()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            var command = new CreateOrderCommand(orderDate, "vehiculo accidentado", "40.7128,-74.0060", "39.7128,-71.0060",
                "0db44d22-19e6-44d0-8f09-8aa0be5a93c4", "f57f0a79-b23b-496b-bcae-f9ef13529232");
            var token = "Bearer token";
           
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";


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
            var userExistsRequestp = new RestRequest($"https://localhost:5350/user/{command.OrderDispatcherId}", Method.Get);
            userExistsRequestp.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""provider"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });
            var optionalContract = _Optional<Contract>.Of(contract);
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);
            _mockOrderRepository.Setup(repo => repo.GetAllOrders(It.IsAny<GetAllOrdersDto>())).ReturnsAsync(_Optional<List<Order>>.Empty());
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(orderId);
            var service = new CreateOrderCommandHandler(_mockIdGenerator.Object, _mockOrderRepository.Object, _mockContractRepository.Object, _mockPublishEndpoint.Object);


            // Act

            var response = await service.Execute(command);
            var result = await _controller.CreateOrder(command, token);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }

        [Fact]
        public async Task CreateOrder_ReturnsNotFound_WhenContractNotFound()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            var command = new CreateOrderCommand(orderDate, "vehiculo accidentado", "40.7128,-74.0060", "39.7128,-71.0060",
                "0db44d22-19e6-44d0-8f09-8aa0be5a93c4", "f57f0a79-b23b-496b-bcae-f9ef13529232");
            var token = "Bearer token";

            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";


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


            var userExistsRequestp = new RestRequest($"https://localhost:5350/user/{command.OrderDispatcherId}", Method.Get);
            userExistsRequestp.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""provider"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });
            var optionalContract = _Optional<Contract>.Empty;
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).ReturnsAsync(optionalContract);
            _mockOrderRepository.Setup(repo => repo.GetAllOrders(It.IsAny<GetAllOrdersDto>())).ReturnsAsync(_Optional<List<Order>>.Empty());
            _mockIdGenerator.Setup(g => g.GenerateId()).Returns(orderId);
            var service = new CreateOrderCommandHandler(_mockIdGenerator.Object, _mockOrderRepository.Object, _mockContractRepository.Object, _mockPublishEndpoint.Object);


            // Act

            var response = await service.Execute(command);
            var result = await _controller.CreateOrder(command, token);

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }
        [Fact]
        public async Task GetAllOrders_ReturnsOkResult_WithOrdersList()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("por asignar"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );
            var orders = new List<Order>
        {
             order,
        };
            var optionalOrders = _Optional<List<Order>>.Of(orders);
            _mockOrderRepository.Setup(repo => repo.GetAllOrders(It.IsAny<GetAllOrdersDto>()))
                .ReturnsAsync(optionalOrders);

            // Act
            var result = await _controller.GetAllOrders(new GetAllOrdersDto());

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetAllOrders_ReturnsBadRequest_WhenNoOrdersFound()
        {
            // Arrange
            var optionalOrders = _Optional<List<Order>>.Empty();
            _mockOrderRepository.Setup(repo => repo.GetAllOrders(It.IsAny<GetAllOrdersDto>()))
                            .ReturnsAsync(optionalOrders);

            // Act
            var result = await _controller.GetAllOrders(new GetAllOrdersDto());


            // Assert
            Assert.True(result is BadRequestObjectResult);
        }

        [Fact]
        public async Task GetOrderById_ReturnsOkResult_WithOrder()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("por asignar"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task GetOrderById_ReturnsBadRequest_WhenOrderNotFound()
        {
            // Arrange
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var optionalOrder = _Optional<Order>.Empty();
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            Assert.True(result is BadRequestObjectResult);

        }


        [Fact]
        public async Task AssignConductor_ReturnsOk_WhenConductorAssignedSuccessfully()
        {
            // Arrange
            var conductorId = "3b5f42af-36bf-47ea-b759-75c249aac8a7";
            var updateOrderDto = new UpdateOrderDto(conductorId, 150, null, null, 100);
            var token = "Bearer token";
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var userExistsRequestp = new RestRequest($"https://localhost:5350/user/{updateOrderDto.ConductorAssignedId}", Method.Get);
            userExistsRequestp.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""conductor"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;

            var order = Order.Create(
                   new OrderId(orderId),
                   new OrderNumber(orderNumber),
                   new OrderDate(orderDate),
                   new OrderStatus("por asignar"),
                   new IncidentType("vehiculo accidentado"),
                   new OrderDestination("40.7128,-74.0060"),
                   new OrderLocation("39.7128,-71.0060"),
                   new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                   new OrderCost(420),
                   new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                   );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);

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
            _mockContractRepository.Setup(repo => repo.GetContractById(It.IsAny<ContractId>())).
                ReturnsAsync(optionalContract);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
               .ReturnsAsync(order);
            var service = new AssignConductorCommandHandler(_mockOrderRepository.Object, _mockContractRepository.Object, _mockPublishEndpoint.Object);

            // Act
            var response = await service.Execute(new AssignConductorCommand(orderId,conductorId, 100));
            var result = await _controller.AssignConductor(updateOrderDto, "token", orderId );

            // Assert
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task AssignConductor_ReturnsNotFound_WhenConductorNotFound()
        {
            // Arrange
            var updateOrderDto = new UpdateOrderDto("conductorId", null, null, null, 100);
            var token = "Bearer token";
            var userExistsRequestp = new RestRequest($"https://localhost:5350/user/{updateOrderDto.ConductorAssignedId}", Method.Get);
            userExistsRequestp.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""conductor"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });

            // Act
            var result = await _controller.AssignConductor(updateOrderDto, "token", "orderId");

            // Assert
            Assert.True(result is NotFoundObjectResult);
        }


        [Fact]
        public async Task ToggleAcceptOrder_ReturnsOk_ToggleAcceptOrderCommandHandler()
        {
            // Arrange
            var token = "token";
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("por aceptar"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            var data = new AcceptOrderDto(true);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);
            var service = new ToggleAcceptOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);

            var UpdateConductorStatusRequest = new RestRequest($"https://localhost:5250/provider/conductors/conductor/toggle-activity/{order}", 
                Method.Patch);
            UpdateConductorStatusRequest.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""conductor"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });

            // Act
            var response = await service.Execute(new ToggleAcceptOrderCommand(orderId, data.Accepted));
            var result = await _controller.ToggleAcceptOrder(data, token, orderId);

            // Assert
            Assert.True(response.IsSuccessful);

            
        }

        [Fact]
        public async Task LocateOrder_ReturnsOk_Success()
        {
            // Arrange
            var token = "token";
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("aceptado"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);
            var service = new LocateOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);
            var UpdateConductorLocationRequest = new RestRequest($"https://localhost:5250/provider/conductors/conductor/location/{order}", 
                Method.Patch);
            UpdateConductorLocationRequest.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""conductor"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });

            // Act
            var response = await service.Execute(new LocateOrderCommand(orderId));
            var result = await _controller.LocateOrder(token, orderId);

            // Assert
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task LocateOrder_ReturnsNotFound_OrderNotFound()
        {
            // Arrange
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var token = "token";
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(_Optional<Order>.Empty());

            // Act
            var result = await _controller.LocateOrder(token, orderId);

            // Assert
            Assert.True(result is BadRequestObjectResult);

        }

        [Fact]
        public async Task StartOrder_ReturnsOk_Success()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("localizado"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);
            var command = new StartOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);


            // Act
            var result = await _controller.StartOrder(orderId);

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task StartOrder_ReturnsBadRequest_OrderStartFails_()
        {
            // Arrange
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(_Optional<Order>.Empty());

            // Act
            var result = await _controller.StartOrder(orderId);

            // Assert
            Assert.True(result is BadRequestObjectResult);
        }

        [Fact]
        public async Task CancelOrder_ReturnsOk_Success()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("localizado"),//"localizado"
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);

            var service = new CancelOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);
           ;

            // Act
            var result = await _controller.CancelOrder(orderId);

            // Assert
            Assert.True(result is OkObjectResult);

        }

        [Fact]
        public async Task FinishOrder_ReturnsOk_Success()
        {
            // Arrange
            var token = "token";
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("en proceso"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);

            var UpdateConductorLocationRequest = new RestRequest($"https://localhost:5250/provider/conductors/conductor/location/{order}", 
                Method.Patch);
            UpdateConductorLocationRequest.AddHeader("Authorization", token);
            var userExistsRequest = _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                          .ReturnsAsync(new RestResponse
                          {
                              StatusCode = HttpStatusCode.OK,
                              Content = @"{
                                       ""id"": ""0fdceb8f-217f-4cdc-b983-7e9815187dce"",
                                       ""name"": ""test user"",
                                       ""phone"": ""+584242374797"",
                                       ""role"": ""conductor"",
                                       ""department"": ""5fb9be1e-37a6-457b-8718-6a832185b5d3"",
                                        ""isActive"": true
                                        }"

                          });
            var service = new FinishOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);


            // Act
            var response = await service.Execute(new FinishOrderCommand(orderId));
            var result = await _controller.FinishOrder("token", orderId);

            // Assert
            Assert.True(response.IsSuccessful);
        }


        [Fact]
        public async Task FinishOrder_ReturnsBadRequest_FinishFails()
        {
            // Arrange
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(_Optional<Order>.Empty());

            // Act
            var result = await _controller.FinishOrder("token", orderId);

            // Assert
            Assert.True(result is BadRequestObjectResult);
        }


        [Fact]
        public async Task PayOrder_ReturnsOk_Success()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("finalizado"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);


            var service = new PayOrderCommandHandler(_mockOrderRepository.Object, _mockPublishEndpoint.Object);

            // Act
            var result = await _controller.PayOrder(orderId);
            var response = await service.Execute(new PayOrderCommand(orderId));

            // Assert
            Assert.True(result is OkObjectResult);
        }

        [Fact]
        public async Task PayOrder_ReturnsBadRequest_OrderPaymentFails()
        {
           
            // Arrange
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(_Optional<Order>.Empty());
            // Act
            var result = await _controller.PayOrder(orderId);

            // Assert
            Assert.True(result is BadRequestObjectResult);

        }
        

        [Fact]
        public async Task AddExtraCosts_Success_ReturnsOk()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("localizado"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);
            var extraCosts = new List<ExtraCostDto> { new ExtraCostDto("ded942ce-dcbf-4e3b-bb29-13a212d8710e", "Cambio de neumatico", 100) };
            var updateOrderDto = new UpdateOrderDto(null, null, null, extraCosts, null);

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

            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            var extraCost = new ExtraCost(
                                new ExtraCostId(extracdId),
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );
            _mockExtraCostRepository.Setup(r => r.GetExtraCostById(It.IsAny<ExtraCostId>())).ReturnsAsync(_Optional<ExtraCost>.Of(extraCost));



            var service = new AddExtraCostsToOrderCommandHandler(_mockOrderRepository.Object, 
                _mockContractRepository.Object, _mockExtraCostRepository.Object);
            var response = await service.Execute(new AddExtraCostsToOrderCommand(orderId, updateOrderDto.ExtraCosts!));

            // Act
            var result = await _controller.AddExtraCosts(updateOrderDto, orderId);

            // Assert
            Assert.True(result is OkObjectResult);

        }


        [Fact]
        public async Task AddExtraCosts_ReturnsBadRequest_Failure()
        {
            // Arrange
            var orderDate = new DateTime(2025, 12, 31, 23, 59, 59);
            int orderNumber = 1001;
            var orderId = "30d812e4-0437-496e-95e8-2037cf1f2eae";
            var order = Order.Create(
                              new OrderId(orderId),
                              new OrderNumber(orderNumber),
                              new OrderDate(orderDate),
                              new OrderStatus("cancelado"),
                              new IncidentType("vehiculo accidentado"),
                              new OrderDestination("40.7128,-74.0060"),
                              new OrderLocation("39.7128,-71.0060"),
                              new OrderDispatcherId("0db44d22-19e6-44d0-8f09-8aa0be5a93c4"),
                              new OrderCost(420),
                              new ContractId("f57f0a79-b23b-496b-bcae-f9ef13529232")
                              );

            var optionalOrder = _Optional<Order>.Of(order);
            _mockOrderRepository.Setup(repo => repo.GetOrderById(It.IsAny<OrderId>()))
                .ReturnsAsync(optionalOrder);
            _mockOrderRepository.Setup(repo => repo.UpdateOrder(It.IsAny<Order>()))
                 .ReturnsAsync(order);
            var extraCosts = new List<ExtraCostDto> { new ExtraCostDto("ded942ce-dcbf-4e3b-bb29-13a212d8710e", "Cambio de neumatico", 100) };
            var updateOrderDto = new UpdateOrderDto(null, null, null, extraCosts, null);

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

            var extracdId = "ded942ce-dcbf-4e3b-bb29-13a212d8710e";
            var extraCost = new ExtraCost(
                                new ExtraCostId(extracdId),
                                new ExtraCostDescription("Valid Description"),
                                new ExtraCostPrice(10)
                );
            _mockExtraCostRepository.Setup(r => r.GetExtraCostById(It.IsAny<ExtraCostId>())).ReturnsAsync(_Optional<ExtraCost>.Of(extraCost));



            var service = new AddExtraCostsToOrderCommandHandler(_mockOrderRepository.Object,
                _mockContractRepository.Object, _mockExtraCostRepository.Object);
            var response = await service.Execute(new AddExtraCostsToOrderCommand(orderId, updateOrderDto.ExtraCosts!));

            // Act
            var result = await _controller.AddExtraCosts(updateOrderDto, orderId);

            // Assert
           Assert.True(result is BadRequestObjectResult);
          

        }




    }
}
