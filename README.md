#前言

我可能有三年没怎么碰C#了，目前的工作是在全职搞前端，最近有时间抽空看了一下Asp.net Core，Core版本号都到了5.0了，也越来越好用了，下面将记录一下这几天以来使用Asp.Net Core WebApi+Dapper+Mysql+Redis+Docker的一次开发过程。

## 项目结构
最终项目结构如下，CodeUin.Dapper数据访问层,CodeUin.WebApi应用层，其中涉及到具体业务逻辑的我将直接写在Controllers中，不再做过多分层。CodeUin.Helpers我将存放一些项目的通用帮助类，如果是只涉及到当前层的帮助类将直接在所在层级种的Helpers文件夹中存储即可。

![项目结构](https://s3.ax1x.com/2021/01/13/st0MGR.jpg)

## 安装环境

### MySQL

```cmd
# 下载镜像
docker pull mysql
# 运行
docker run -itd --name 容器名称 -p 3306:3306 -e MYSQL_ROOT_PASSWORD=你的密码 mysql
```

如果正在使用的客户端工具连接MySQL提示1251，这是因为客户端不支持新的加密方式造成的，解决办法如下。

![1251](https://s3.ax1x.com/2021/01/13/stfMJf.md.jpg)

```shell
# 查看当前运行的容器
docker ps 
# 进入容器
docker exec -it 容器名称 bash
# 访问MySQL
mysql -u root -p
# 查看加密规则
select host,user,plugin,authentication_string from mysql.user;
# 对远程连接进行授权
GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;
# 更改密码加密规则
ALTER USER 'root'@'%' IDENTIFIED WITH mysql_native_password BY '你的密码';
# 刷新权限
flush privileges;
```

最后，使用MySQL客户端工具进行连接测试，我使用的工具是**Navicat Premium**。

![MySQL](https://s3.ax1x.com/2021/01/13/stbPY9.jpg)

### Redis

```shell
# 下载镜像
docker pull redis
# 运行
docker run -itd -p 6379:6379 redis
```

使用Redis客户端工具进行连接测试，我使用的工具是**Another Redis DeskTop Manager**。

![Redis](https://s3.ax1x.com/2021/01/13/stHNGR.md.jpg)

### .NET 环境

服务器我使用的是CentOS 8,使用的NET SDK版本5.0,下面将记录我是如何在CentOS 8中安装.NET SDK和.NET运行时的。

```shell
# 安装SDK
sudo dnf install dotnet-sdk-5.0
# 安装运行时
sudo dnf install aspnetcore-runtime-5.0
```

检查是否安装成功，使用`dotnet --info`命令查看安装信息

![SDK](https://s3.ax1x.com/2021/01/13/stLSPJ.jpg)

## 创建项目

下面将实现一个用户的登录注册，和获取用户信息的小功能。

### 数据服务层

该层设计参考了 [玉龙雪山](https://www.cnblogs.com/wangyulong/p/8960972.html) 的架构，我也比较喜欢这种结构，一看结构就知道是要做什么的，简单清晰。

首先，新建一个项目命名为CodeUin.Dapper，只用来提供接口，为业务层服务。

- Entities
  - 存放实体类
- IRepository
  - 存放仓库接口
- Repository
  - 存放仓库接口实现类
- BaseModel
  - 实体类的基类，用来存放通用字段
- DataBaseConfig
  - 数据访问配置类
- IRepositoryBase
  - 存放最基本的仓储接口 增删改查等
- RepositoryBase
  - 基本仓储接口的具体实现

![Dapper](https://s3.ax1x.com/2021/01/13/stOONF.jpg)

#### 创建BaseModel基类

该类存放在项目的根目录下，主要作用是将数据库实体类中都有的字段独立出来。

```c#
using System;

namespace CodeUin.Dapper
{
    /// <summary>
    /// 基础实体类
    /// </summary>
    public class BaseModel
    {
        /// <summary>
        /// 主键Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
```

#### 创建DataBaseConfig类

该类存放在项目的根目录下，我这里使用的是MySQL，需要安装以下依赖包，如果使用的其他数据库，自行安装对应的依赖包即可。

![依赖](https://s3.ax1x.com/2021/01/13/sNZly6.jpg)

该类具体代码如下：

```c#
using MySql.Data.MySqlClient;
using System.Data;

namespace CodeUin.Dapper
{
    public class DataBaseConfig
    {
        private static string MySqlConnectionString = @"Data Source=数据库地址;Initial Catalog=codeuin;Charset=utf8mb4;User 		ID=root;Password=数据库密码;";
        
        public static IDbConnection GetMySqlConnection(string sqlConnectionString = null)
        {
            if (string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                sqlConnectionString = MySqlConnectionString;
            }
            IDbConnection conn = new MySqlConnection(sqlConnectionString);
            conn.Open();
            return conn;
        }
    }
}
```

#### 创建IRepositoryBase类

该类存放在项目的根目录下，存放常用的仓储接口。

```c#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeUin.Dapper
{
    public interface IRepositoryBase<T>
    {
        Task<int> Insert(T entity, string insertSql);

        Task Update(T entity, string updateSql);

        Task Delete(int Id, string deleteSql);

        Task<List<T>> Select(string selectSql);

        Task<T> Detail(int Id, string detailSql);
    }
}
```

#### 创建RepositoryBase类

该类存放在项目的根目录下，是IRepositoryBase类的具体实现。

```c#
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CodeUin.Dapper
{
    public class RepositoryBase<T> : IRepositoryBase<T>
    {
        public async Task Delete(int Id, string deleteSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                await conn.ExecuteAsync(deleteSql, new { Id });
            }
        }

        public async Task<T> Detail(int Id, string detailSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<T>(detailSql, new { Id });
            }
        }

        public async Task<List<T>> ExecQuerySP(string SPName)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await Task.Run(() => conn.Query<T>(SPName, null, null, true, null, CommandType.StoredProcedure).ToList());
            }
        }

        public async Task<int> Insert(T entity, string insertSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await conn.ExecuteAsync(insertSql, entity);
            }
        }

        public async Task<List<T>> Select(string selectSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await Task.Run(() => conn.Query<T>(selectSql).ToList());
            }
        }

        public async Task Update(T entity, string updateSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                await conn.ExecuteAsync(updateSql, entity);
            }
        }
    }
}
```

好了，基础类基本已经定义完成。下面将新建一个Users类，并定义几个常用的接口。

#### 创建Users实体类

该类存放在Entities文件夹中，该类继承BaseModel。

```c#
namespace CodeUin.Dapper.Entities
{
    /// <summary>
    /// 用户表
    /// </summary>
    public class Users : BaseModel
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 盐
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public int IsDelete { get; set; }
    }
}
```

#### 创建IUserRepository类

该类存放在IRepository文件夹中，继承IRepositoryBase<Users>，并定义了额外的接口。

```c#
using CodeUin.Dapper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeUin.Dapper.IRepository
{
    public interface IUserRepository : IRepositoryBase<Users>
    {
        Task<List<Users>> GetUsers();

        Task<int> AddUser(Users entity);

        Task DeleteUser(int d);

        Task<Users> GetUserDetail(int id);

        Task<Users> GetUserDetailByEmail(string email);
    }
}

```

#### 创建UserRepository类

该类存放在Repository文件夹中，继承RepositoryBase<Users>, IUserRepository ,是IUserRepository类的具体实现。

```c#
using CodeUin.Dapper.Entities;
using CodeUin.Dapper.IRepository;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CodeUin.Dapper.Repository
{
    public class UserRepository : RepositoryBase<Users>, IUserRepository
    {
        public async Task DeleteUser(int id)
        {
            string deleteSql = "DELETE FROM [dbo].[Users] WHERE Id=@Id";
            await Delete(id, deleteSql);
        }


        public async Task<Users> GetUserDetail(int id)
        {
            string detailSql = @"SELECT Id, Email, UserName, Mobile, Password, Age, Gender, CreateTime,Salt, IsDelete FROM Users WHERE Id=@Id";
            return await Detail(id, detailSql);
        }

        public async Task<Users> GetUserDetailByEmail(string email)
        {
            string detailSql = @"SELECT Id, Email, UserName, Mobile, Password, Age, Gender, CreateTime, Salt, IsDelete FROM Users WHERE Email=@email";

            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<Users>(detailSql, new { email });
            }
        }

        public async Task<List<Users>> GetUsers()
        {
            string selectSql = @"SELECT * FROM Users";
            return await Select(selectSql);
        }

        public async Task<int> AddUser(Users entity)
        {
            string insertSql = @"INSERT INTO Users (UserName, Gender, Avatar, Mobile, CreateTime, Password, Salt, IsDelete, Email) VALUES (@UserName, @Gender, @Avatar, @Mobile, now(),@Password, @Salt, @IsDelete,@Email);SELECT @id= LAST_INSERT_ID();";
            return await Insert(entity, insertSql);
        }
    }
}
```

大功告成，接下来需要手动创建数据库和表结构，不能像使用EF那样自动生成了，使用Dapper基本上是要纯写SQL的，如果想像EF那样使用，就要额外的安装一个扩展 [Dapper.Contrib](https://github.com/StackExchange/dapper-dot-net/tree/master/Dapper.Contrib)。

数据库表结构如下，比较简单。

```sql
DROP TABLE IF EXISTS `Users`;
CREATE TABLE `Users` (
  `Id` int(11) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `Email` varchar(255) DEFAULT NULL COMMENT '邮箱',
  `UserName` varchar(20) DEFAULT NULL COMMENT '用户名称',
  `Mobile` varchar(11) DEFAULT NULL COMMENT '手机号',
  `Age` int(11) DEFAULT NULL COMMENT '年龄',
  `Gender` int(1) DEFAULT '0' COMMENT '性别',
  `Avatar` varchar(255) DEFAULT NULL COMMENT '头像',
  `Salt` varchar(255) DEFAULT NULL COMMENT '加盐',
  `Password` varchar(255) DEFAULT NULL COMMENT '密码',
  `IsDelete` int(2) DEFAULT '0' COMMENT '0-正常 1-删除',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `USER_MOBILE_INDEX` (`Mobile`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=10000 DEFAULT CHARSET=utf8mb4 COMMENT='用户信息表';
```

好了，数据访问层大概就这样子了，下面来看看应用层的具体实现方式。

### 应用程序层

创建一个WebApi项目，主要对外提供Api接口服务，具体结构如下。

- Autofac
  - 存放IOC 依赖注入的配置项
- AutoMapper
  - 存放实体对象映射关系的配置项
- Controllers
  - 控制器，具体业务逻辑也将写在这
- Fliters
  - 存放自定义的过滤器
- Helpers
  - 存放本层中用到的一些帮助类
- Models
  - 存放输入/输出/DTO等实体类

![WebApi](https://s3.ax1x.com/2021/01/13/sN0M9A.jpg)

好了，结构大概就是这样。错误优先，先处理程序异常，和集成日志程序吧。

#### 自定义异常处理

在Helpers文件夹中创建一个ErrorHandingMiddleware中间件，添加扩展方法ErrorHandlingExtensions，在Startup中将会使用到。

```c#
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CodeUin.WebApi.Helpers
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            this.next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                var statusCode = 500;

                await HandleExceptionAsync(context, statusCode, ex.Message);
            }
            finally
            {
                var statusCode = context.Response.StatusCode;
                var msg = "";

                if (statusCode == 401)
                {
                    msg = "未授权";
                }
                else if (statusCode == 404)
                {
                    msg = "未找到服务";
                }
                else if (statusCode == 502)
                {
                    msg = "请求错误";
                }
                else if (statusCode != 200)
                {
                    msg = "未知错误";
                }
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    await HandleExceptionAsync(context, statusCode, msg);
                }
            }
        }

        // 异常错误信息捕获，将错误信息用Json方式返回
        private static Task HandleExceptionAsync(HttpContext context, int statusCode, string msg)
        {
            var result = JsonConvert.SerializeObject(new { Msg = msg, Code = statusCode });

            context.Response.ContentType = "application/json;charset=utf-8";

            return context.Response.WriteAsync(result);
        }
    }

    // 扩展方法
    public static class ErrorHandlingExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
```

最后，在 Startup 的 Configure 方法中添加 app.UseErrorHandling() ，当程序发送异常时，会走我们的自定义异常处理。

```c#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();

    // 请求错误提示配置
    app.UseErrorHandling();

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
  		endpoints.MapControllers();
    });
}
```

#### 日志程序

我这里使用的是NLog，需要在项目中先安装依赖包。

![Nlog](https://s3.ax1x.com/2021/01/13/sNcQIK.jpg)

首先在项目根目录创建一个 nlog.config 的配置文件，具体内容如下。

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      internalLogLevel="Info"
      internalLogFile="c:\temp\internal-nlog.txt">

	<!-- enable asp.net core layout renderers -->
	<extensions>
		<add assembly="NLog.Web.AspNetCore"/>
	</extensions>

	<!-- the targets to write to -->
	<targets>

		<target xsi:type="File" name="allfile" fileName="${currentdir}\logs\nlog-all-${shortdate}.log"
				layout="${longdate}|${event-properties:item=EventId_Id}|${uppercase:${level}}|${aspnet-request-ip}|${logger}|${message} ${exception:format=tostring}" />

		<target xsi:type="Console" name="ownFile-web"
				layout="${longdate}|${event-properties:item=EventId_Id}|${uppercase:${level}}|${logger}|${aspnet-request-ip}|${message} ${exception:format=tostring}|url: ${aspnet-request-url}|action: ${aspnet-mvc-action}" />
	</targets>
	<!-- rules to map from logger name to target -->
	<rules>
		<!--All logs, including from Microsoft-->
		<logger name="*" minlevel="Info" writeTo="allfile" />

		<!--Skip non-critical Microsoft logs and so log only own logs-->
		<logger name="Microsoft.*" maxlevel="Info" final="true" />
		<!-- BlackHole without writeTo -->
		<logger name="*" minlevel="Info" writeTo="ownFile-web" />
	</rules>
</nlog>
```

更多配置信息可以直接去官网查看 https://nlog-project.org

最后，在 Program 入口文件中集成 Nlog

```c#
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace CodeUin.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            NLogBuilder.ConfigureNLog("nlog.config");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseNLog();
    }
}
```

现在，我们可以直接使用NLog了，使用方法可以查看上面的 ErrorHandlingMiddleware 类中有使用到。

#### 依赖注入

将使用 Autofac 来管理类之间的依赖关系，Autofac 是一款超级赞的.NET IoC 容器 。首先我们需要安装依赖包。

![Autofac](https://s3.ax1x.com/2021/01/13/sNRGzF.jpg)

在 项目根目录的 Autofac 文件夹中新建一个 CustomAutofacModule 类，用来管理我们类之间的依赖关系。

```c#
using Autofac;
using CodeUin.Dapper.IRepository;
using CodeUin.Dapper.Repository;

namespace CodeUin.WebApi.Autofac
{
    public class CustomAutofacModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UserRepository>().As<IUserRepository>();
        }
    }
}
```

最后，在 Startup 类中添加方法

```c#
public void ConfigureContainer(ContainerBuilder builder)
{
    // 依赖注入
    builder.RegisterModule(new CustomAutofacModule());
}
```

#### 实体映射

将使用 Automapper 帮我们解决对象映射到另外一个对象中的问题，比如这种代码。

```c#
// 如果有几十个属性是相当的可怕的
var users = new Users
{
    Email = user.Email,
    Password = user.Password,
    UserName = user.UserName
};
// 使用Automapper就容易多了
var model = _mapper.Map<Users>(user);
```

先安装依赖包

![Automapper](https://s3.ax1x.com/2021/01/13/sN4hng.jpg)

在项目根目录的 AutoMapper 文件夹中 新建 AutoMapperConfig 类，来管理我们的映射关系。

```c#
using AutoMapper;
using CodeUin.Dapper.Entities;
using CodeUin.WebApi.Models;

namespace CodeUin.WebApi.AutoMapper
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<UserRegisterModel, Users>().ReverseMap();
            CreateMap<UserLoginModel, Users>().ReverseMap();
            CreateMap<UserLoginModel, UserModel>().ReverseMap();
            CreateMap<UserModel, Users>().ReverseMap();
        }
    }
}
```

最后，在 Startup 文件的 ConfigureServices 方法中 添加 services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()) 即可。

#### 使用JWT

下面将集成JWT，来处理授权等信息。首先，需要安装依赖包。

![JWT](https://s3.ax1x.com/2021/01/13/sNItsO.jpg)

修改 appsttings.json 文件，添加 Jwt 配置信息。

```c#
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "AllowedHosts": "*",
    "Jwt": {
        "Key": "e816f4e9d7a7be785a",  // 这个key必须大于16位数，非常生成的时候会报错
        "Issuer": "codeuin.com"
    }
}
```

最后，在 Startup 类的 ConfigureServices 方法中添加 Jwt 的使用。

```c#
     services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),   //缓冲过期时间 默认5分钟
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };
            });
```

好了，最终我们的 Startup 类是这样子的，关于自定义的参数验证后面会讲到。

```c#
using Autofac;
using AutoMapper;
using CodeUin.WebApi.Autofac;
using CodeUin.WebApi.Filters;
using CodeUin.WebApi.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace CodeUin.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // 依赖注入
            builder.RegisterModule(new CustomAutofacModule());
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),   //缓冲过期时间 默认5分钟
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };
            });

            services.AddHttpContextAccessor();

            // 使用AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // 关闭参数自动校验
            services.Configure<ApiBehaviorOptions>((options) =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // 使用自定义验证器
            services.AddControllers(options =>
            {
                options.Filters.Add<ValidateModelAttribute>();
            }).
            AddJsonOptions(options =>
            {
                // 忽略null值
                options.JsonSerializerOptions.IgnoreNullValues = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // 请求错误提示配置
            app.UseErrorHandling();

            // 授权
            app.UseAuthentication();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
```

#### 新建实体类

我将新建三个实体类，分别是 UserLoginModel 用户登录，UserRegisterModel 用户注册，UserModel 用户基本信息。

UserLoginModel 和 UserRegisterModel 将根据我们在属性中配置的特性自动验证合法性，就不需要在控制器中单独写验证逻辑了，极大的节省了工作量。

```c#
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
```

#### 验证器

在项目根目录的 Filters 文件夹中 添加 ValidateModelAttribute 文件夹，将在 Action 请求中先进入我们的过滤器，如果不符合我们定义的规则将直接输出错误项。

具体代码如下。

```c#
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace CodeUin.WebApi.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var item = context.ModelState.Keys.ToList().FirstOrDefault();

                //返回第一个验证参数错误的信息
                context.Result = new BadRequestObjectResult(new
                {
                    Code = 400,
                    Msg = context.ModelState[item].Errors[0].ErrorMessage
                });
            }
        }
    }
}
```

#### 添加自定义验证特性

有时候我们需要自己额外的扩展一些规则，只需要继承 ValidationAttribute 类然后实现 IsValid 方法即可，比如我这里验证了中国的手机号码。

```c# 
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CodeUin.WebApi.Filters
{
    public class ChineMobileAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (!(value is string)) return false;

            var val = (string)value;

            return Regex.IsMatch(val, @"^[1]{1}[2,3,4,5,6,7,8,9]{1}\d{9}$");
        }
    }
}
```

#### 实现登录注册

我们来实现一个简单的业务需求，用户注册，登录，和获取用户信息，其他的功能都大同小异，无非就是CRUD！。

接口我们在数据服务层已经写好了，接下来是处理业务逻辑的时候到了，将直接在 Controllers 中编写。

新建一个控制器 UsersController ，业务很简单，不过多介绍了，具体代码如下。

 

```c#
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
```

最后，来测试一下我们的功能，首先是注册。

先来验证一下我们的传入的参数是否符合我们定义的规则。

输入一个错误的邮箱号试试看！

![注册](https://s3.ax1x.com/2021/01/13/sNHppn.jpg)

ok，没有问题，和我们在 UserRegisterModel 中 添加的验证特性返回结果一致，最后我们测试一下完全符合规则的情况。

![注册成功](https://s3.ax1x.com/2021/01/13/sNHGAe.jpg)

最后，注册成功了，查询下数据库也是存在的。

![user](https://s3.ax1x.com/2021/01/13/sNHhuV.jpg)

我们来试试登录接口，在调用登录接口之前我们先来测试一下我们的配置的权限验证是否已经生效，在不登录的情况下直接访问获取用户信息接口。

![未授权](https://s3.ax1x.com/2021/01/13/sNHju6.jpg)

直接访问会返回未授权，那是因为我们没有登录，自然也就没有 Token，目前来看是没问题的，但要看看我们传入正确的Token 是否能过权限验证。

现在，我们需要调用登录接口，登录成功后会返回一个Token，后面的接口请求都需要用到，不然会无权限访问。

先来测试一下密码错误的情况。

![密码错误](https://s3.ax1x.com/2021/01/13/sNbU54.jpg)

返回正确，符合我们的预期结果，下面将试试正确的密码登录，看是否能够返回我们想要的结果。

![登录成功](https://s3.ax1x.com/2021/01/13/sNbjRs.png)

登录成功，接口也返回了我们预期的结果，最后看看生成的 token 是否按照我们写的逻辑那样，存一份到 redis 当中。

![redis](https://s3.ax1x.com/2021/01/13/sNqVzR.png)

也是没有问题的，和我们预想的一样。

下面将携带正确的 token 请求获取用户信息的接口，看看是否能够正确返回。

获取用户信息的接口不会携带任何参数，只会在请求头的 Headers 中 添加 Authorization ，将我们正确的 token 传入其中。

![获取用户信息](https://s3.ax1x.com/2021/01/13/sNq5Y4.png)

能够正确获取到我们的用户信息，也就是说我们的权限这一块也是没有问题的了，下面将使用 Docker 打包部署到 Linux 服务器中。

## 打包部署

在项目的根目录下添加 Dockerfile 文件，内容如下。

```
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["CodeUin.WebApi/CodeUin.WebApi.csproj", "CodeUin.WebApi/"]
COPY ["CodeUin.Helpers/CodeUin.Helpers.csproj", "CodeUin.Helpers/"]
COPY ["CodeUin.Dapper/CodeUin.Dapper.csproj", "CodeUin.Dapper/"]
RUN dotnet restore "CodeUin.WebApi/CodeUin.WebApi.csproj"
COPY . .
WORKDIR "/src/CodeUin.WebApi"
RUN dotnet build "CodeUin.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeUin.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeUin.WebApi.dll"]
```

在 Dockerfile 文件的目录下运行打包命令

```cmd
# 在当前文件夹（末尾的句点）中查找 Dockerfile
docker build -t codeuin-api .
# 查看镜像
docker images
# 保存镜像到本地
docker save -o codeuin-api.tar codeuin-api
```

最后，将我们保存的镜像通过上传的服务器后导入即可。

通过 ssh 命令 连接服务器，在刚上传包的目录下执行导入命令。

```cmd
# 加载镜像
docker load -i codeuin-api.tar
# 运行镜像
docker run -itd -p 8888:80 --name codeuin-api codeuin-api
# 查看运行状态
docker stats
```

到此为止，我们整个部署工作已经完成了，最后在请求服务器的接口测试一下是否ok。

![服务器请求](https://s3.ax1x.com/2021/01/13/sUKi7V.png)

最终的结果也是ok的，到此为止，我们所有基础的工作都完成了。

