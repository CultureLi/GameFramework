
namespace Framework
{
    public sealed partial class I18nNew : BeanBase
    {
        public I18nNew(ByteBuf _buf)
        {
            Key = _buf.ReadString();
            Value = _buf.ReadString();
        }

        public static I18nNew Deserializei18n(ByteBuf _buf)
        {
            return new I18nNew(_buf);
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

