using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{


    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _config;
    private UserRepository _userRepository;

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


    // [Authorize] HUSK AT FJERNE KOMMATERING AF DETTE FELT
    [HttpGet("getuser/{id}"), DisableRequestSizeLimit]
    public async Task<IActionResult> GetUser(int id)
    {
        _logger.LogInformation("getUser function hit");
        
        var user = await _userRepository.GetUser(id);

        

        var filteredUser = new
        {
            user.UserName
        };


        return Ok(filteredUser);
    }





    [Authorize]
    [HttpPost("addNewUser"), DisableRequestSizeLimit]
    public async Task<IActionResult> Post([FromBody] User? user)
    {
        _logger.LogInformation("AddNewUser funk ramt");

        var newUser = new User
        {
            UserName = user!.UserName,
            UserPassword = user.UserPassword,
            UserEmail = user.UserEmail,
            UserPhone = user.UserPhone,
            UserAddress = user.UserAddress
        };
        _logger.LogInformation("Nyt user objekt lavet");


        _userRepository.AddNewUser(user);
        _logger.LogInformation("nyt user objekt added til User list");


        return Ok(newUser);
    }
}