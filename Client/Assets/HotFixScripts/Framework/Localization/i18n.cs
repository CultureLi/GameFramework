
namespace Framework
{
    public sealed partial class i18n : BeanBase
    {
        public i18n(ByteBuf _buf)
        {
            Key = _buf.ReadString();
            Value = _buf.ReadString();
        }

        public static i18n Deserializei18n(ByteBuf _buf)
        {
            return new i18n(_buf);
        }

        public readonly string Key;
        public readonly string Value;

        public const int __ID__ = 3176990;
        public override int GetTypeId() => __ID__;

        public override string ToString()
        {
            return "{ "
            + "key:" + Key + ","
            + "value:" + Value + ","
            + "}";
        }
    }
}

