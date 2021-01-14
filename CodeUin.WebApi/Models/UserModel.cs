using System;
using System.ComponentModel.DataAnnotations;

namespace CodeUin.WebApi.Models
{
    /// <summary>
    /// 用户实体类
    /// </summary>
    public class UserModel
    {
        public int Id { get; set; }

        public string Email { get; set; }
        public string UserName { get; set; }

        public string Mobile { get; set; }

        public int Gender { get; set; }

        public int Age { get; set; }

        public string Avatar { get; set; }
    }

    public class UserLoginModel
    {
        [Required(ErrorMessage = "请输入邮箱")]
        public string Email { get; set; }

        [Required(ErrorMessage = "请输入密码")]
        public string Password { get; set; }
    }

    public class UserRegisterModel
    {
        [Required(ErrorMessage = "请输入邮箱")]
        [EmailAddress(ErrorMessage = "请输入正确的邮箱地址")]
        public string Email { get; set; }

        [Required(ErrorMessage = "请输入用户名")]
        [MaxLength(length: 12, ErrorMessage = "用户名最大长度不能超过12")]
        [MinLength(length: 2, ErrorMessage = "用户名最小长度不能小于2")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "请输入密码")]
        [MaxLength(length: 20, ErrorMessage = "密码最大长度不能超过20")]
        [MinLength(length: 6, ErrorMessage = "密码最小长度不能小于6")]
        public string Password { get; set; }
    }
}
