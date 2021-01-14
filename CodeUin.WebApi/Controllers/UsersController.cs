using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CodeUin.Dapper.Entities;
using CodeUin.Dapper.IRepository;
using CodeUin.Helpers;
using CodeUin.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CodeUin.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersController(ILogger<UsersController> logger, IUserRepository userRepository, IMapper mapper, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _userRepository = userRepository;
            _mapper = mapper;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var userInfo = await _userRepository.GetUserDetail(userId);

            if (userInfo == null)
            {
                return Json(new { Code = 200, Msg = "未找到该用户的信息" });
            }

            var outputModel = _mapper.Map<UserModel>(userInfo);

            return Json(new { Code = 200, Data = outputModel }); ;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Login([FromBody] UserLoginModel user)
        {
            // 查询用户信息
            var data = await _userRepository.GetUserDetailByEmail(user.Email);

            // 账号不存在
            if (data == null)
            {
                return Json(new { Code = 200, Msg = "账号或密码错误" });
            }

            user.Password = Encrypt.Md5(data.Salt + user.Password);

            // 密码不一致
            if (!user.Password.Equals(data.Password))
            {
                return Json(new { Code = 200, Msg = "账号或密码错误" });
            }

            var userModel = _mapper.Map<UserModel>(data);

            // 生成token
            var token = GenerateJwtToken(userModel);

            // 存入Redis
            await new RedisHelper().StringSetAsync($"token:{data.Id}", token);

            return Json(new
            {
                Code = 200,
                Msg = "登录成功",
                Data = userModel,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Register([FromBody] UserRegisterModel user)
        {
            // 查询用户信息
            var data = await _userRepository.GetUserDetailByEmail(user.Email);

            if (data != null)
            {
                return Json(new { Code = 200, Msg = "该邮箱已被注册" });
            }

            var salt = Guid.NewGuid().ToString("N");

            user.Password = Encrypt.Md5(salt + user.Password);

            var users = new Users
            {
                Email = user.Email,
                Password = user.Password,
                UserName = user.UserName
            };

            var model = _mapper.Map<Users>(user);

            model.Salt = salt;

            await _userRepository.AddUser(model);

            return Json(new { Code = 200, Msg = "注册成功" });
        }

        /// <summary>
        /// 生成Token
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns></returns>
        private string GenerateJwtToken(UserModel user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Gender, user.Gender.ToString()),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.MobilePhone,user.Mobile??""),
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}