/*
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

namespace UserServiceApi.Test;

public class UserControllerTests
{
    private ILogger<UserController> _logger = null!;
    private IConfiguration _configuration = null!;
    private Mock<UserRepository> _userRepository;
    
    
    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<UserRepository>();
        _logger = new Mock<ILogger<UserController>>().Object;

        var myConfiguration = new Dictionary<string, string?>
    {
        {"MONGO_CONNECTION_STRING", "mongodb://admin:1234@mongodb:27017/?authSource=admin"}
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


    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
    


    [Test]
    public async Task TestUserEndpoint_valid_userAsync()
    {
        // Arrange
        var user = CreateUser();

        var stubRepo = new Mock<UserRepository>();

        stubRepo.Setup(svc => svc.AddNewUser(user))
            .Returns(Task.FromResult<User?>(user));

        var controller = new UserController(_logger, _configuration, stubRepo.Object);


        // Act
        var result = await controller.AddNewUser(user);


        // Assert
        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        Assert.That((result as CreatedAtActionResult)?.Value, Is.TypeOf<User>());

    }


    // helper method to create User
    private User CreateUser()
    {
        var user = new User()
        {
            UserId = null,
            UserName = "TestUserName",
            UserPassword = "TestUserPassword",
            UserEmail = "TestUserEmail",
            UserPhone = 12345678,
            UserAddress = "TestUserAddress"
        };

        return user;
    }


}

*/