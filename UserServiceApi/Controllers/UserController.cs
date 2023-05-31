// Usings
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
using System.Net.Http.Headers;

namespace Controllers;

[ApiController] // Api controller to handle api calls
[Route("[controller]")] // Controller name set as default http endpoint name
public class UserController : ControllerBase
{
    // Creates 3 instances, 1 for a logger, one for a config, 1 for an instance of the userRepository.cs class
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _config;
    private UserRepository _userRepository;
    private HttpClient _httpClient = new HttpClient();

    public UserController(ILogger<UserController> logger, IConfiguration config, UserRepository userRepository, HttpClient httpClient)
    {
        // Initializes the controllers constructor with the 3 specified private objects
        _logger = logger;
        _config = config;
        _userRepository = userRepository;
        _httpClient = httpClient;
        
        // Logger host information
        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"UserService - Auth service responding from {_ipaddr}");
    }


    // VERSION_ENDEPUNKT
    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        // Create a dictionary to hold the version properties
        var properties = new Dictionary<string, string>();

        // Get the assembly information of the program
        var assembly = typeof(Program).Assembly;

        // Add the service name to the properties dictionary
        properties.Add("service", "User");

        // Retrieve the product version from the assembly and add it to the properties dictionary
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
        properties.Add("version", ver!);

        try
        {
            // Get the host name of the current machine
            var hostName = System.Net.Dns.GetHostName();

            // Get the IP addresses associated with the host name
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);

            // Retrieve the first IPv4 address and add it to the properties dictionary
            var ipa = ips.First().MapToIPv4().ToString();
            properties.Add("hosted-at-address", ipa);
        }
        catch (Exception ex)
        {
            // Log and handle any exceptions that occurred during IP address retrieval
            _logger.LogError(ex.Message);

            // Add a default message to the properties dictionary if IP address resolution failed
            properties.Add("hosted-at-address", "Could not resolve IP address");
        }

        // Return the populated properties dictionary
        return properties;
    }



    // GET
    [HttpGet("getuser/{userId}"), DisableRequestSizeLimit] // Getuser endpoint to retreive a specific user from the db
    public async Task<IActionResult> GetUserById(int userId)
    {
        _logger.LogInformation("UserService - getUser function hit");

        _logger.LogInformation("UserService - userId: " + userId);

        var user = await _userRepository.GetUserById(userId); // Gets user from collection using repository method

        if (user == null)
        {
            return BadRequest("UserService - user does not exist");
        }

        _logger.LogInformation("UserService - after loading user: " + user.UserName);

        return Ok(user); // Returns an OK statuscode, along with the entire user object
    }






    // POST
    [HttpPost("addNewUser"), DisableRequestSizeLimit]
    public async Task<IActionResult> AddNewUser([FromBody] User? user)
    {
        _logger.LogInformation("UserService - AddNewUser funk ramt");

        var allUsers = await _userRepository.GetAllUsers();
        _logger.LogInformation("UserService - total users: " + allUsers.Count);
        // Find the maximum user ID from the list of all users, or default to 0 if the list is empty, then add 1 to get the latest ID for the new user
        int? latestID = allUsers.DefaultIfEmpty().Max(a => a == null ? 0 : a.UserId) + 1;

        var newUser = new User(); // Initialize new User object and validate the request body input
        if (user != null)
        {
            newUser.UserId = latestID;
            newUser.UserName = user.UserName;
            newUser.UserPassword = user.UserPassword;
            newUser.UserEmail = user.UserEmail;
            newUser.UserPhone = user.UserPhone;
            newUser.UserAddress = user.UserAddress;
        }
        _logger.LogInformation("UserService - New user object created, name: " + newUser.UserName);

        if (newUser.UserId == null)
        {
            return BadRequest("UserService - UserId is null");
        }

        // Check if the new user's username is already taken by comparing it with the usernames of existing users
        bool userNameTaken = allUsers.Any(u => u.UserName == newUser.UserName);
        if (userNameTaken)
        {
            return BadRequest("UserService - UserName is already taken");
        }
        
        await _userRepository.AddNewUser(newUser); // Adds the newUser to _users
        _logger.LogInformation("UserService - New user object added to _users");

        return Ok(newUser);
    }








    // PUT
    [Authorize]
    [HttpPut("updateUser/{userId}"), DisableRequestSizeLimit] // UpdateUser endpoint for updating desired user
    public async Task<IActionResult> UpdateUser(int userId, User? user)
    {
        _logger.LogInformation("UserService - UpdateUser function hit");

        var updatedUser = _userRepository.GetUserById(userId); // Retreives the desired user from the collection

        if (updatedUser == null)
        {
            return BadRequest("UserService - User does not exist");
        }
        _logger.LogInformation("UserService - User for update: " + updatedUser.Result.UserName);

        var allUsers = _userRepository.GetAllUsers().Result.ToList();

        bool userNameTaken = false; // Creates a new bool which will change in case the userName is taken

        foreach (var bruger in allUsers) // Loops through all current users
        {
            if (bruger.UserName == user!.UserName) // If any of them have the requested UserName, converts the bool to true
            {
                userNameTaken = true;
                break;
            }
        }
        
        if (userNameTaken) // Checks if userName is taken
        {
            return BadRequest($"UserService - Cannot change UserName to {user!.UserName}. UserName is already taken");
        }
        else
        {
            await _userRepository.UpdateUser(userId, user!); // Updates the user with the provided info

            var newUpdatedUser = await _userRepository.GetUserById(userId); // Creates an object containing the updatedUser info

            return Ok(newUpdatedUser); // Returns an OK statuscode along with the newUpdatedUser object
        }
    }






    // DELETE
    [Authorize]
    [HttpDelete("deleteUser/{userId}"), DisableRequestSizeLimit] // DeleteUser endpoint for deleting a user
    public async Task<IActionResult> DeleteUser(int? userId)
    {
        _logger.LogInformation("UserService - DeleteUser function hit");

        var deletedUser = _userRepository.GetUserById(userId); // Retreives the specified user

        using (_httpClient = new HttpClient())
        {
            string catalogueServiceUrl = Environment.GetEnvironmentVariable("CATALOGUE_SERVICE_URL")!; // Retreives URL environment variable from docker-compose.yml file
            string getCatalogueEndpoint = "/catalogue/getAllArtifacts"; // Specifies with endpoint in CatalogueService to retreive data from

            _logger.LogInformation($"UserService - {catalogueServiceUrl + getCatalogueEndpoint}");

            // Retrieve the current user's token from the request
            var tokenValue = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            _logger.LogInformation("CatalogueService - token first default: " + tokenValue);
            var token = tokenValue?.Replace("Bearer ", "");
            _logger.LogInformation("CatalogueService - token w/o bearer: " + token);

            // Create a new HttpRequestMessage to include the token
            var request = new HttpRequestMessage(HttpMethod.Get, catalogueServiceUrl + getCatalogueEndpoint);
            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "UserService - Failed to retrieve UserId from UserService");
            }
            
            var allArtifacts = await response.Content.ReadFromJsonAsync<List<ArtifactDTO>>(); // Deserializes the data from the endpoint and retreives all Artifacts in the endpoints db
            _logger.LogInformation("UserService - Total Artifacts: " + allArtifacts!.Count);

            List<ArtifactDTO> activeArtifacts = (List<ArtifactDTO>)allArtifacts.Where(s => s.Status == "Active").ToList(); // Filters and retreives the Artifacts where the status is NOT equel to "Deleted"
            _logger.LogInformation("UserService - Total Active Artifacts: " + activeArtifacts.Count);

            List<ArtifactDTO> usersActiveArtifacts = new List<ArtifactDTO>(); // Initializes a new list of Artifacts to add the specified users Artifacts to

            if (deletedUser == null) // Validates specified user
            {
                return BadRequest("UserService - User does not exist");
            }

            for (int i = 0; i < activeArtifacts.Count(); i++) // Loops through activeArtifacts to check if the user owns any
            {
                if (activeArtifacts[i].ArtifactOwner!.UserName == deletedUser.Result.UserName)
                {
                    usersActiveArtifacts.Add(activeArtifacts[i]); // Adds any Artifacts owned by the User to the list
                }
            }
            _logger.LogInformation("UserService - UsersActiveArtifactsCount: " + usersActiveArtifacts.Count);
            
            if (usersActiveArtifacts.Count > 0) // Checks whether the specified user owns any Artifacts
            {
                return BadRequest("UserService - You have active artifacts in the database and therefore cannot delete your user");
            }
            else
            {
                _logger.LogInformation("UserService - User for deletion: " + deletedUser.Result.UserName);
                
                foreach (var artifact in allArtifacts) // Loops through allArtifacts and sets any, with a matching ArtifactOwner to have a status of 'Deleted'
                {
                    if (artifact.ArtifactOwner!.UserName == deletedUser.Result.UserName)
                    {
                        _logger.LogInformation("UserService - deletedArtifactName: " + artifact.ArtifactName);
                        string getArtifactDeletionEndpoint = "/catalogue/deleteartifact/" + artifact.ArtifactID; // Retreives endpoint to deleteArtifact in CatalogueService
                        _logger.LogInformation($"UserService - {catalogueServiceUrl + getArtifactDeletionEndpoint}");
                        HttpResponseMessage deletArtifactResponse = await _httpClient.PutAsync(catalogueServiceUrl + getArtifactDeletionEndpoint, null);
                    }
                }

                await _userRepository.DeleteUser(userId); // Deletes the specified user from the db

                return Ok("userController - User deleted");
            }
        }
    }

}