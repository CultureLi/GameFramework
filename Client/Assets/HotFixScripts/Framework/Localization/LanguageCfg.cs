
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Framework;


namespace cfg
{
public sealed partial class LanguageCfg : Framework.BeanBase
{
    public LanguageCfg(ByteBuf _buf) 
    {
        Id = _buf.ReadInt();
        Desc = _buf.ReadString();
        Name = _buf.ReadString();
        Code = _buf.ReadString();
    }

    public static LanguageCfg DeserializeLanguageCfg(ByteBuf _buf)
    {
        return new LanguageCfg(_buf);
    }

    /// <summary>
    /// id
    /// </summary>
    public readonly int Id;
    /// <summary>
    /// 描述
    /// </summary>
    public readonly string Desc;
    /// <summary>
    /// 名称
    /// </summary>
    public readonly string Name;
    public readonly string Code;
   
    public const int __ID__ = 491994572;
    public override int GetTypeId() => __ID__;

    public override string ToString()
    {
        return "{ "
        + "id:" + Id + ","
        + "desc:" + Desc + ","
        + "name:" + Name + ","
        + "code:" + Code + ","
        + "}";
    }
}
}

