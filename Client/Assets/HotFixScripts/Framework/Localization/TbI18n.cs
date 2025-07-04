namespace Framework
{
    public partial class TbI18n : TableBase
    {
        private System.Collections.Generic.Dictionary<string, I18n> _dataMap;
        private System.Collections.Generic.List<I18n> _dataList;

        public override void Initialize(ByteBuf _buf)
        {
            _dataMap = new System.Collections.Generic.Dictionary<string, I18n>();
            _dataList = new System.Collections.Generic.List<I18n>();

            for (int n = _buf.ReadSize(); n > 0; --n)
            {
                I18n _v;
                _v = I18n.Deserializei18n(_buf);
                _dataList.Add(_v);
                _dataMap.Add(_v.Key, _v);
            }
        }

        public System.Collections.Generic.Dictionary<string, I18n> DataMap => _dataMap;
        public System.Collections.Generic.List<I18n> DataList => _dataList;

        public I18n GetOrDefault(string key) => _dataMap.TryGetValue(key, out var v) ? v : null;
        public I18n Get(string key) => _dataMap[key];
        public I18n this[string key] => _dataMap[key];

    }
}

