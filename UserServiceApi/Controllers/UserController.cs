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
        _logger.LogInformation("userController - getUser function hit");

        _logger.LogInformation("userController - userId: " + userId);

        var user = await _userRepository.GetUserById(userId); // gets user from collection using repository method

        _logger.LogInformation("after loading user: " + user.UserName);

        return Ok(user); // returns an OK statuscode, along with the entire user object
    }






    //POST
    [HttpPost("addNewUser"), DisableRequestSizeLimit]
    public async Task<IActionResult> AddNewUser([FromBody] User? user)
    {
        _logger.LogInformation("AddNewUser funk ramt");

        int latestID = _userRepository.GetNextUserId(); // Gets latest ID in _artifacts + 1

        var newUser = new User
        {
            UserId = latestID, // sets the newUser ID as the next one in the collection
            UserName = user!.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };
        _logger.LogInformation("Nyt user objekt lavet, name: " + user.UserName);

        var allUsers = await _userRepository.GetAllUsers();

        _logger.LogInformation("UserService - total users: " + allUsers.Count);

        bool userNameTaken = false;

        foreach (var bruger in allUsers)
        {
            if (bruger.UserName == user.UserName)
            {
                userNameTaken = true;
                break;
            }
        }

        if (newUser.UserId == null)
        {
            return BadRequest("UserId is null");
        }
        else if (userNameTaken)
        {
            return BadRequest("UserName is taken");
        }
        else
        {
            _userRepository.AddNewUser(newUser);
        }

        _logger.LogInformation("new user object added to User list");

        return Ok(newUser);
    }






    //PUT
    [HttpPut("updateUser/{userId}"), DisableRequestSizeLimit]
    public async Task<IActionResult> UpdateUser(int userId, User? user)
    {
        _logger.LogInformation("UpdateUser function hit");

        var updatedUser = _userRepository.GetUserById(userId); // retreives the desired user from the collection

        if (updatedUser == null)
        {
            return BadRequest("User does not exist");
        }
        _logger.LogInformation("User for update: " + updatedUser.Result.UserName);

        await _userRepository.UpdateUser(userId, user!);

        var newUpdatedUser = await _userRepository.GetUserById(userId); // creates an object containing the updatedUser info

        return Ok(newUpdatedUser); // returns an OK statuscode along with the newUpdatedUser object
    }






    //DELETE
    [HttpDelete("deleteUser/{userId}"), DisableRequestSizeLimit]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        _logger.LogInformation("DeleteUser function hit");

        var deletedUser = _userRepository.GetUserById(userId);

        using (HttpClient client = new HttpClient())
        {
            // string catalogueServiceUrl = "http://localhost:5004";
            string catalogueServiceUrl = "http://user:80";
            string getCatalogueEndpoint = "/catalogue/getAllArtifacts";

            _logger.LogInformation(catalogueServiceUrl + getCatalogueEndpoint);

            HttpResponseMessage response = await client.GetAsync(catalogueServiceUrl + getCatalogueEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to retrieve UserId from UserService");
            }

            var catalogueResponse = await response.Content.ReadFromJsonAsync<List<ArtifactDTO>>();

            List<ArtifactDTO> nonDeletedArtifacts = (List<ArtifactDTO>)catalogueResponse.Where(s => s.Status != "Deleted").ToList();
            _logger.LogInformation("" + nonDeletedArtifacts.Count);

            List<ArtifactDTO> usersArtifacts = new List<ArtifactDTO>();

            for (int i = 0; i < nonDeletedArtifacts.Count(); i++)
            {
                if (nonDeletedArtifacts[i].ArtifactOwner.UserName == deletedUser.Result.UserName)
                {
                    usersArtifacts.Add(nonDeletedArtifacts[i]);
                }
            }
            _logger.LogInformation("UsersArtifactsCount: " + usersArtifacts.Count);


            

            if (deletedUser == null)
            {
                return BadRequest("User does not exist");
            }
            else if (usersArtifacts.Count > 0)
            {
                return BadRequest("You have active artifacts in the database and there cannot delete your user");
            }

            else
            {
                _logger.LogInformation("User for deletion: " + deletedUser.Result.UserName);

                await _userRepository.DeleteUser(userId);

                return Ok("User deleted");
            }
        }
    }

}