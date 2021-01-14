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
