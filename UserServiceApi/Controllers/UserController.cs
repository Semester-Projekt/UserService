//usings
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Model;
using Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
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

[ApiController] // api controller to handle api calls
[Route("[controller]")] // controller name set as default http endpoint name
public class UserController : ControllerBase
{
    // creates 3 instances, 1 for a logger, one for a config, 1 for an instance of the userRepository.cs class
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _config;
    private UserRepository _userRepository;
    
    public UserController(ILogger<UserController> logger, IConfiguration config, UserRepository userRepository)
    {
        // initializes the controllers constructor with the 3 specified private objects
        _config = config;
        _logger = logger;
        _userRepository = userRepository;


        // Logger host information
        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Auth service responding from {_ipaddr}");

    }



    [HttpGet("version")] // version endpoint
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
    [HttpGet("getuser/{userId}"), DisableRequestSizeLimit] // getuser endpoint to retreive a specific user from the db
    public async Task<IActionResult> GetUserById(int userId)
    {
        _logger.LogInformation("userController - getUser function hit");

        _logger.LogInformation("userController - userId: " + userId);

        var user = await _userRepository.GetUserById(userId); // gets user from collection using repository method

        if (user == null)
        {
            return BadRequest("userController - user does not exist");
        }

        _logger.LogInformation("userController - after loading user: " + user.UserName);

        return Ok(user); // returns an OK statuscode, along with the entire user object
    }






    //POST
    [HttpPost("addNewUser"), DisableRequestSizeLimit] //addnewuser endpoint for adding a new user to the db
    public async Task<IActionResult> AddNewUser([FromBody] User? user)
    {
        _logger.LogInformation("userController - AddNewUser funk ramt");

        var allUsers = await _userRepository.GetAllUsers(); //gets all current users from the db
        
        _logger.LogInformation("UserService - total users: " + allUsers.Count);

        int latestID = await _userRepository.GetNextUserId(); // Gets latest ID in _users + 1

        var newUser = new User // extracts the new user from the request body
        {
            UserId = latestID, // sets the newUser ID as the next one in the collection
            UserName = user!.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };
        _logger.LogInformation("userController - Nyt user objekt lavet, name: " + user.UserName);

        _logger.LogInformation("userController - user mongo id: " + user.MongoId);
        _logger.LogInformation("userController - newuser mongo id: " + newUser.MongoId);


        bool userNameTaken = false; // creates a new bool which will change in case the userName is taken

        foreach (var bruger in allUsers) // loops through all current users
        {
            if (bruger.UserName == user.UserName) // if any of them have the requested UserName, converts the bool to true
            {
                userNameTaken = true;
                break;
            }
        }

        if (newUser.UserId == null) // validates the new users id
        {
            return BadRequest("userController - UserId is null");
        }
        else if (userNameTaken)
        {
            return BadRequest("userController - UserName is taken"); // checks if userName is taken
        }
        else
        {
            _userRepository.AddNewUser(newUser); // adds the new user object to _users
        }

        _logger.LogInformation("userController - new user object added to _users");

        return Ok(newUser); // returns the newUser
    }






    //PUT
    [HttpPut("updateUser/{userId}"), DisableRequestSizeLimit] // updateUser endpoint for updating desired user
    public async Task<IActionResult> UpdateUser(int userId, User? user)
    {
        _logger.LogInformation("userController - UpdateUser function hit");

        var updatedUser = _userRepository.GetUserById(userId); // retreives the desired user from the collection

        if (updatedUser == null)
        {
            return BadRequest("userController - User does not exist");
        }
        _logger.LogInformation("userController - User for update: " + updatedUser.Result.UserName);

        var allUsers = _userRepository.GetAllUsers().Result.ToList();

        bool userNameTaken = false; // creates a new bool which will change in case the userName is taken

        foreach (var bruger in allUsers) // loops through all current users
        {
            if (bruger.UserName == user.UserName) // if any of them have the requested UserName, converts the bool to true
            {
                userNameTaken = true;
                break;
            }
        }
        
        if (userNameTaken) // checks if userName is taken
        {
            return BadRequest($"userController - Cannot change UserName to {user.UserName}. UserName is already taken");
        }
        else
        {
            await _userRepository.UpdateUser(userId, user!); // updates the user with the provided info

            var newUpdatedUser = await _userRepository.GetUserById(userId); // creates an object containing the updatedUser info

            return Ok(newUpdatedUser); // returns an OK statuscode along with the newUpdatedUser object
        }
    }






    //DELETE
    [HttpDelete("deleteUser/{userId}"), DisableRequestSizeLimit] // deleteUser endpoint for deleting a user
    public async Task<IActionResult> DeleteUser(int userId)
    {
        _logger.LogInformation("userController - DeleteUser function hit");

        var deletedUser = _userRepository.GetUserById(userId); // retreives the specified user

        using (HttpClient client = new HttpClient())
        {
            //string catalogueServiceUrl = "http://localhost:4000";
            //string catalogueServiceUrl = "http://catalogue:80";
            string catalogueServiceUrl = Environment.GetEnvironmentVariable("CATALOGUE_SERVICE_URL"); // retreives URL environment variable from docker-compose.yml file
            string getCatalogueEndpoint = "/catalogue/getAllArtifacts"; // specifies with endpoint in CatalogueService to retreive data from

            _logger.LogInformation(catalogueServiceUrl + getCatalogueEndpoint);

            HttpResponseMessage response = await client.GetAsync(catalogueServiceUrl + getCatalogueEndpoint); // creates the endpoint to retreive data from
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "userController - Failed to retrieve UserId from UserService");
            }

            var catalogueResponse = await response.Content.ReadFromJsonAsync<List<ArtifactDTO>>(); // deserializes the data from the endpoint

            List<ArtifactDTO> nonDeletedArtifacts = (List<ArtifactDTO>)catalogueResponse.Where(s => s.Status != "Deleted").ToList(); // filters and retreives the Artifacts where the status is NOT equel to "Deleted"
            _logger.LogInformation("" + nonDeletedArtifacts.Count);

            List<ArtifactDTO> usersArtifacts = new List<ArtifactDTO>(); // initializes a new list of Artifacts to add the specified users Artifacts to

            for (int i = 0; i < nonDeletedArtifacts.Count(); i++) // loops through nonDeletedArtifacts to check if the user owns any
            {
                if (nonDeletedArtifacts[i].ArtifactOwner.UserName == deletedUser.Result.UserName)
                {
                    usersArtifacts.Add(nonDeletedArtifacts[i]); // adds any Artifacts owned by the User to the list
                }
            }
            _logger.LogInformation("userController - UsersArtifactsCount: " + usersArtifacts.Count);


            

            if (deletedUser == null) // validates specified user
            {
                return BadRequest("userController - User does not exist");
            }
            else if (usersArtifacts.Count > 0) // checks whether the specified user owns any Artifacts
            {
                return BadRequest("userController - You have active artifacts in the database and there cannot delete your user");
            }

            else
            {
                _logger.LogInformation("userController - User for deletion: " + deletedUser.Result.UserName);

                await _userRepository.DeleteUser(userId); // deletes the user from the db

                return Ok("userController - User deleted");
            }
        }
    }

}