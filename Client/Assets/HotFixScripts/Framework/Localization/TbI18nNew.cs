namespace Framework
{
    public partial class TbI18nNew : TableBase
    {
        private System.Collections.Generic.Dictionary<string, I18nNew> _dataMap;
        private System.Collections.Generic.List<I18nNew> _dataList;

        public override void Initialize(ByteBuf _buf)
        {
            _dataMap = new System.Collections.Generic.Dictionary<string, I18nNew>();
            _dataList = new System.Collections.Generic.List<I18nNew>();

            for (int n = _buf.ReadSize(); n > 0; --n)
            {
                I18nNew _v;
                _v = I18nNew.Deserializei18n(_buf);
                _dataList.Add(_v);
                _dataMap.Add(_v.Key, _v);
            }
        }

        public System.Collections.Generic.Dictionary<string, I18nNew> DataMap => _dataMap;
        public System.Collections.Generic.List<I18nNew> DataList => _dataList;

        public I18nNew GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : null;
        public I18nNew Get(string key) => _dataMap[key];
        public I18nNew this[string key] => _dataMap[key];

    }
}

