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
using System.Diagnostics;

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


    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;

        properties.Add("service", "User");
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
        properties.Add("version", ver!);

        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipa = ips.First().MapToIPv4().ToString();
            properties.Add("hosted-at-address", ipa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            properties.Add("hosted-at-address", "Could not resolve IP-address");
        }

        return properties;
    }



    //GET
    [HttpGet("getuser/{userId}"), DisableRequestSizeLimit] // getuser endpoint to retreive a specific user from the db
    public async Task<IActionResult> GetUserById(int userId)
    {
        _logger.LogInformation("UserService - getUser function hit");

        _logger.LogInformation("UserService - userId: " + userId);

        var user = await _userRepository.GetUserById(userId); // gets user from collection using repository method

        if (user == null)
        {
            return BadRequest("UserService - user does not exist");
        }

        _logger.LogInformation("UserService - after loading user: " + user.UserName);

        return Ok(user); // returns an OK statuscode, along with the entire user object
    }






    //POST
    [HttpPost("addNewUser"), DisableRequestSizeLimit] //addnewuser endpoint for adding a new user to the db
    public async Task<IActionResult> AddNewUser([FromBody] User? user)
    {
        _logger.LogInformation("UserService - AddNewUser funk ramt");

        var allUsers = await _userRepository.GetAllUsers(); //gets all current users from the db
        
        _logger.LogInformation("UserService - total users: " + allUsers.Count);

        int? latestID = await _userRepository.GetNextUserId(); // Gets latest ID in _users + 1

        var newUser = new User // extracts the new user from the request body
        {
            UserId = latestID, // sets the newUser ID as the next one in the collection
            UserName = user!.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };
        _logger.LogInformation("UserService - Nyt user objekt lavet, name: " + user.UserName);

        _logger.LogInformation("UserService - user mongo id: " + user.MongoId);
        _logger.LogInformation("UserService - newuser mongo id: " + newUser.MongoId);


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
            return BadRequest("UserService - UserId is null");
        }
        else if (userNameTaken)
        {
            return BadRequest("UserService - UserName is taken"); // checks if userName is taken
        }
        else
        {
            _userRepository.AddNewUser(newUser); // adds the new user object to _users
        }

        _logger.LogInformation("UserService - new user object added to _users");

        return Ok(newUser); // returns the newUser
    }






    //PUT
    [HttpPut("updateUser/{userId}"), DisableRequestSizeLimit] // updateUser endpoint for updating desired user
    public async Task<IActionResult> UpdateUser(int userId, User? user)
    {
        _logger.LogInformation("UserService - UpdateUser function hit");

        var updatedUser = _userRepository.GetUserById(userId); // retreives the desired user from the collection

        if (updatedUser == null)
        {
            return BadRequest("UserService - User does not exist");
        }
        _logger.LogInformation("UserService - User for update: " + updatedUser.Result.UserName);

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
            return BadRequest($"UserService - Cannot change UserName to {user.UserName}. UserName is already taken");
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
        _logger.LogInformation("UserService - DeleteUser function hit");

        var deletedUser = _userRepository.GetUserById(userId); // retreives the specified user

        using (HttpClient client = new HttpClient())
        {
            //string catalogueServiceUrl = "http://localhost:4000";
            //string catalogueServiceUrl = "http://catalogue:80";
            string catalogueServiceUrl = Environment.GetEnvironmentVariable("CATALOGUE_SERVICE_URL"); // retreives URL environment variable from docker-compose.yml file
            string getCatalogueEndpoint = "/catalogue/getAllArtifacts"; // specifies with endpoint in CatalogueService to retreive data from

            _logger.LogInformation($"UserService - {catalogueServiceUrl + getCatalogueEndpoint}");

            HttpResponseMessage response = await client.GetAsync(catalogueServiceUrl + getCatalogueEndpoint); // creates the endpoint to retreive data from
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "UserService - Failed to retrieve UserId from UserService");
            }

            var allArtifacts = await response.Content.ReadFromJsonAsync<List<ArtifactDTO>>(); // deserializes the data from the endpoint and retreives all Artifacts in the endpoints db
            _logger.LogInformation("UserService - Total Artifacts: " + allArtifacts!.Count);

            List<ArtifactDTO> activeArtifacts = (List<ArtifactDTO>)allArtifacts.Where(s => s.Status == "Active").ToList(); // filters and retreives the Artifacts where the status is NOT equel to "Deleted"
            _logger.LogInformation("UserService - Total Active Artifacts: " + activeArtifacts.Count);

            List<ArtifactDTO> usersActiveArtifacts = new List<ArtifactDTO>(); // initializes a new list of Artifacts to add the specified users Artifacts to

            if (deletedUser == null) // validates specified user
            {
                return BadRequest("UserService - User does not exist");
            }

            for (int i = 0; i < activeArtifacts.Count(); i++) // loops through activeArtifacts to check if the user owns any
            {
                if (activeArtifacts[i].ArtifactOwner.UserName == deletedUser.Result.UserName)
                {
                    usersActiveArtifacts.Add(activeArtifacts[i]); // adds any Artifacts owned by the User to the list
                }
            }
            _logger.LogInformation("UserService - UsersActiveArtifactsCount: " + usersActiveArtifacts.Count);
            
            if (usersActiveArtifacts.Count > 0) // checks whether the specified user owns any Artifacts
            {
                return BadRequest("UserService - You have active artifacts in the database and therefore cannot delete your user");
            }
            else
            {
                _logger.LogInformation("UserService - User for deletion: " + deletedUser.Result.UserName);
                
                foreach (var artifact in allArtifacts) // loops through allArtifacts and sets any, with a matching ArtifactOwner to have a status of 'Deleted'
                {
                    if (artifact.ArtifactOwner.UserName == deletedUser.Result.UserName)
                    {
                        _logger.LogInformation("UserService - deletedArtifactName: " + artifact.ArtifactName);
                        string getArtifactDeletionEndpoint = "/catalogue/deleteartifact/" + artifact.ArtifactID; // retreives endpoint to deleteArtifact in CatalogueService
                        _logger.LogInformation($"UserService - {catalogueServiceUrl + getArtifactDeletionEndpoint}");
                        HttpResponseMessage deletArtifactResponse = await client.PutAsync(catalogueServiceUrl + getArtifactDeletionEndpoint, null);
                    }
                }

                await _userRepository.DeleteUser(userId); // deletes the user from the db

                return Ok("userController - User deleted");
            }
        }
    }

}