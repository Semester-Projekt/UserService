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

    [SetUp]
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
    public async Task VALID_TestAddUser_UserName_Not_Taken_ReturnsOkObjectResult()
    {
        // Arrange
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

        var allUsers = new List<User> { seedUser };

        var stubRepo = new Mock<UserRepository>();
        var stubHttp = new Mock<HttpClient>();

        stubRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        stubRepo.Setup(svc => svc.AddNewUser(newUser)).Returns(Task.FromResult<User?>(newUser));

        var controller = new UserController(_logger, _configuration, stubRepo.Object, stubHttp.Object);
        for (int i = 0; i < allUsers.Count; i++)
        {
            Console.WriteLine("existingUserName: " + allUsers[i].UserName);
        }
        Console.WriteLine("newUserName: " + newUser.UserName);
        // Act
        var result = await controller.AddNewUser(newUser);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That((result as OkObjectResult)?.Value, Is.TypeOf<User>());
    }

    [Test]
    public async Task INVALID_TestAddUser_UserName_Taken_ReturnsBadRequestObjectResult()
    {
        // Arrange
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

        var allUsers = new List<User> { seedUser };

        var stubRepo = new Mock<UserRepository>();
        var stubHttp = new Mock<HttpClient>();

        stubRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        stubRepo.Setup(svc => svc.AddNewUser(newUser)).Returns(Task.FromResult<User?>(newUser));

        var controller = new UserController(_logger, _configuration, stubRepo.Object, stubHttp.Object);
        for (int i = 0; i < allUsers.Count; i++)
        {
            Console.WriteLine("existingUserName: " + allUsers[i].UserName);
        }
        Console.WriteLine("newUserName: " + newUser.UserName);


        // Act
        var result = await controller.AddNewUser(newUser);


        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That((result as BadRequestObjectResult)?.Value, Is.TypeOf<string>());
    }






    // UNIT TEST AF DeleteUser
    [Test]
    public async Task VALID_TestDeleteUser_ReturnsOkObjectResult()
    {
        // Arrange
        var seedUser = CreateUser(new User
        {
            UserId = 1,
            UserName = "SeedUserName",
            UserPassword = "SeedUserPassword",
            UserEmail = "SeedUserEmail",
            UserPhone = 11111111,
            UserAddress = "SeedUserAddress"
        });

        var allUsers = new List<User> { seedUser };

        var stubRepo = new Mock<UserRepository>();
        var stubHttp = new Mock<HttpClient>();

        stubRepo.Setup(svc => svc.GetAllUsers()).ReturnsAsync(allUsers);
        stubRepo.Setup(svc => svc.DeleteUser(seedUser.UserId)).Returns(Task.FromResult<string>("userController - User deleted"));
        stubRepo.Setup(repo => repo.GetUserById(seedUser.UserId)).ReturnsAsync(seedUser);

        Console.WriteLine("SeedUserName: " + seedUser.UserName);
        Console.WriteLine("AllUserName.first: " + allUsers.FirstOrDefault()!.UserName);

        var controller = new UserController(_logger, _configuration, stubRepo.Object, stubHttp.Object);

        // Act
        var result = await controller.DeleteUser(seedUser.UserId);


        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That((result as OkObjectResult)?.Value, Is.TypeOf<string>());
    }
}