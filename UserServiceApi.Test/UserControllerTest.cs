using NUnit.Framework;
using Moq;
using UserServiceApi;
using Controllers;
using Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UserServiceApi.Test;

public class UserControllerTests : ControllerBase
{
    private ILogger<UserController> _logger = null;
    private IConfiguration _configuration = null;
    private UserController _userController;
    private UserRepository _userRepository;
    
    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<UserController>>().Object;
        /*_configuration = new ConfigurationBuilder().
            AddInMemoryCollection(myConfiguration)
            .Build();*/
    }

    [Test]
    public void Test1()
    {
        NUnit.Framework.Assert.Pass();
    }


    





    private User CreateUser(DateTime requestTime)
    {
        var user = new User()
        {
            UserId = 1,
            UserName = "TestUserName",
            UserPassword = "TestUserPassword",
            UserEmail = "TestUserEmail",
            UserPhone = 11111111,
            UserAddress = "TestUserAddress"
        };
        return user;
    }

    public IActionResult AddUser(User user)
    {
        var res = _userRepository.AddNewUser(user);

        if (res.IsFaulted)
        {
            return BadRequest();
        }

        return CreatedAtAction("Get", new { id = user.UserId }, user);
    }

    


}