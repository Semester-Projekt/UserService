using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Model;
using Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Channels;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _config;
    private UserRepository _userRepository;

//docker build test

    public UserController(ILogger<UserController> logger, IConfiguration config, UserRepository userRepository)
    {
        _config = config;
        _logger = logger;
        _userRepository = userRepository;


        //Logger host information
        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Auth service responding from {_ipaddr}");

    }



    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = typeof(Program).Assembly;


        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

        var versionInfo = new
        {
            InformationalVersion = informationalVersion,
            Description = description
        };

        return Ok(versionInfo);
    }




    //GET
    [HttpGet("getuser/{userId}"), DisableRequestSizeLimit]
    public async Task<IActionResult> GetUserById(int userId)
    {
        _logger.LogInformation("getUser function hit");
        
        _logger.LogInformation("still in getUser func");

        _logger.LogInformation("userController - userId: " + userId);

        var user = await _userRepository.GetUserById(userId);

        _logger.LogInformation("after loading user: " + user.UserName);

        return Ok(user);
    }






    //POST
    [HttpPost("addNewUser"), DisableRequestSizeLimit]
    public IActionResult AddNewUser([FromBody] User? user)
    {
        _logger.LogInformation("AddNewUser funk ramt");

        int latestID = _userRepository.GetNextUserId(); // Gets latest ID in _artifacts + 1

        var newUser = new User
        {
            UserId = latestID,
            UserName = user!.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };
        _logger.LogInformation("Nyt user objekt lavet, name: " + user.UserName);

        if (user.UserId == null)
        {
            return BadRequest("UserId is null");
        }
        else
        {
            _userRepository.AddNewUser(newUser);
        }

        _logger.LogInformation("nyt user objekt added til User list");


        return Ok(newUser);
    }






    //PUT
    [HttpPut("updateUser/{userId}"), DisableRequestSizeLimit]
    public async Task<IActionResult> UpdateUser(int userId, User? user)
    {
        _logger.LogInformation("UpdateUser function hit");

        var updatedUser = _userRepository.GetUserById(userId);

        if (updatedUser == null)
        {
            return BadRequest("User does not exist");
        }
        _logger.LogInformation("User for update: " + updatedUser.Result.UserName);

        await _userRepository.UpdateUser(userId, user!);

        var newUpdatedUser = await _userRepository.GetUserById(userId);

        return Ok(newUpdatedUser);
    }






    //DELETE
    [HttpDelete("deleteUser/{userId}"), DisableRequestSizeLimit]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        _logger.LogInformation("DeleteUser function hit");

        var deletedUser = _userRepository.GetUserById(userId);

        if (deletedUser == null)
        {
            return BadRequest("User does not exist");
        }
        _logger.LogInformation("User for deletion: " + deletedUser.Result.UserName);

        await _userRepository.DeleteUser(userId);

        return Ok("User deleted");
    }

}