using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Controllers;
using Model;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client.Exceptions;
using Moq.Protected;
using System.Net;

namespace UserServiceApi.Test;

public class UserControllerTests
{
    private ILogger<UserController> _logger = null!;
    private IConfiguration _configuration = null!;

    [SetUp] // Setup method that initializes before each test method
    public void Setup()
    {
        // Mock ILogger for UserController
        _logger = new Mock<ILogger<UserController>>().Object;
        
        // Mock IConfiguration using in-memory configuration values
        var myConfiguration = new Dictionary<string, string?>
    {
        {"Issuer", "megalangsuperdupertestSecret"},
        {"Secret", "megalangsuperdupertestIssuer"},
        {"AUCTION_SERVICE_URL", "http://localhost:4000" },
        {"AUTH_SERVICE_URL", "http://localhost:4000"},
        {"BID_SERVICE_URL", "http://localhost:4000"},
        {"CATALOGUE_SERVICE_URL", "http://localhost:4000"}
    };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        // Print the configuration for debugging
        Console.WriteLine("Configuration values:");
        foreach (var config in _configuration.AsEnumerable())
        {
            Console.WriteLine($"{config.Key}: {config.Value}");
        }
    }






    // HELPER METHOD FOR CREATING A NEW USER
    private User CreateUser(User user)
    {
        var newUser = new User()
        {
            UserId = user.UserId,
            UserName = user.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };

        return newUser;
    }






    // UNIT TEST AF AddNewUser
    [Test]
    public async Task VALID_TestAddUser_ReturnsOkObjectResult()
    {
        // Arrange
        // Creates 2 separate users with different values
        var seedUser = CreateUser(new User
        {
            UserId = 1,
            UserName = "SeedUserName",
            UserPassword = "SeedUserPassword",
            UserEmail = "SeedUserEmail",
            UserPhone = 11111111,
            UserAddress = "SeedUserAddress"
        });
        var newUser = CreateUser(new User
        {
            UserId = 2,
            UserName = "TestUserName",
            UserPassword = "TestUserPassword",
            UserEmail = "TestUserEmail",
            UserPhone = 99999999,
            UserAddress = "TestUserAddress"
        });

        var allUsers = new List<User> { seedUser }; // Initializes a new list of users and adds seedUser to the list by default

        // Mocks both the UserRepository and an HttpClient
        var mockRepo = new Mock<UserRepository>();
        var mockHttp = new Mock<HttpClient>();

        // Mocks both the GetAllUsers and the AddNewUser functions and defines the desired return types of those methods
        mockRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        mockRepo.Setup(svc => svc.AddNewUser(newUser)).Returns(Task.FromResult<User?>(newUser));

        // Initializes the controller with the necessary values from the UserController constructor
        var controller = new UserController(_logger, _configuration, mockRepo.Object, mockHttp.Object);

        for (int i = 0; i < allUsers.Count; i++)
        {
            Console.WriteLine("existingUserName: " + allUsers[i].UserName);
        }
        Console.WriteLine("newUserName: " + newUser.UserName);


        // Act
        var result = await controller.AddNewUser(newUser); // Awaits and then calls the 'mocked' AddNewUser function


        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>()); // Asserts that the method returns an OkObjectResult, which is the return type of the AddNewUser method in the Controller
        Assert.That((result as OkObjectResult)?.Value, Is.TypeOf<User>()); // Asserts that the method returns a User object in the OkObjectResult return
    }

    [Test]
    public async Task INVALID_TestAddUser_UserName_Taken_ReturnsBadRequestObjectResult()
    {
        // Arrange
        // Creates 2 separate users with different values, except UserName which is the same. This will cause the test to expect a BadRequestObjectResult
        var seedUser = CreateUser(new User
        {
            UserId = 1,
            UserName = "SeedUserName",
            UserPassword = "SeedUserPassword",
            UserEmail = "SeedUserEmail",
            UserPhone = 11111111,
            UserAddress = "SeedUserAddress"
        });
        var newUser = CreateUser(new User
        {
            UserId = 2,
            UserName = "SeedUserName",
            UserPassword = "TestUserPassword",
            UserEmail = "TestUserEmail",
            UserPhone = 11111111,
            UserAddress = "TestUserAddress"
        });

        var allUsers = new List<User> { seedUser }; // Initializes a list of users and adds seedUser by default

        // Mocks both the UserRepository and an HttpClient
        var mockRepo = new Mock<UserRepository>();
        var mockHttp = new Mock<HttpClient>();

        // Mocks both the GetAllUsers and the AddNewUser functions and defines the desired return types of those methods
        mockRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        mockRepo.Setup(svc => svc.AddNewUser(newUser)).Returns(Task.FromResult<User?>(newUser));

        // Initializes the controller with the necessary values from the UserController constructor
        var controller = new UserController(_logger, _configuration, mockRepo.Object, mockHttp.Object);
        for (int i = 0; i < allUsers.Count; i++)
        {
            Console.WriteLine("existingUserName: " + allUsers[i].UserName);
        }
        Console.WriteLine("newUserName: " + newUser.UserName);


        // Act
        var result = await controller.AddNewUser(newUser); // Awaits and then calls the 'mocked' AddNewUser function


        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>()); // Asserts that the return type is a BadRequestObjectResult which means the method has hit an exception in the controller
        Assert.That((result as BadRequestObjectResult)?.Value, Is.TypeOf<string>()); // Aserts that the return type of the BadRequestObjectResult is a string
    }






    // UNIT TEST AF DeleteUser
    [Test]
    public async Task VALID_TestDeleteUser_ReturnsOkObjectResult()
    {
        // Arrange
        // Creates the seedUser
        var seedUser = CreateUser(new User
        {
            UserId = 1,
            UserName = "SeedUserName",
            UserPassword = "SeedUserPassword",
            UserEmail = "SeedUserEmail",
            UserPhone = 11111111,
            UserAddress = "SeedUserAddress"
        });

        var allUsers = new List<User> { seedUser }; // Initializes a list of users with the seedUser

        // Mocks both the UserRepository and an HttpClient
        var mockRepo = new Mock<UserRepository>();
        var mockHttp = new Mock<HttpClient>();

        // Mocks the both the GetAllUsers, the DeleteUser, and the GetUserById functions and defines the desired return types of those methods
        mockRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        mockRepo.Setup(svc => svc.DeleteUser(seedUser.UserId)).Returns(Task.FromResult<string>("userController - User deleted"));
        mockRepo.Setup(repo => repo.GetUserById(seedUser.UserId)).ReturnsAsync(seedUser);

        Console.WriteLine("SeedUserName: " + seedUser.UserName);
        Console.WriteLine("AllUserName.first: " + allUsers.FirstOrDefault()!.UserName);

        // Initializes the controller with the necessary values from the UserController constructor
        var controller = new UserController(_logger, _configuration, mockRepo.Object, mockHttp.Object);


        // Act
        var result = await controller.DeleteUser(seedUser.UserId); // Awaits and then calls the 'mocked' DeleteUser function


        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>()); // Asserts that the method returns an OkObjectResult, which is the return type of the DeleteUser method in the Controller
        Assert.That((result as OkObjectResult)?.Value, Is.TypeOf<string>()); // Aserts that the return type of the OkObjectResult is a string
    }
}