using System;
using System.IO;

namespace Framework
{
    public partial class TbI18n : TableBase
    {
        private System.Collections.Generic.Dictionary<string, I18n> _dataMap;
        private System.Collections.Generic.List<I18n> _dataList;

        private Func<string, MemoryStream> _streamLoader;
        private string _fileName;
        public override void Initialize(string name, Func<string, MemoryStream> streamLoader)
        {
            _fileName = name;
            _streamLoader = streamLoader;
            _dataMap = new System.Collections.Generic.Dictionary<string, I18n>();
            _dataList = new System.Collections.Generic.List<I18n>();
            LoadAll();
        }

        private void LoadAll()
        {
            var stream = _streamLoader?.Invoke($"{_fileName}.bytes");
            var byteBuf = new ByteBuf(stream.ToArray());
            for (int n = byteBuf.ReadSize(); n > 0; --n)
            {
                I18n v;
                v = I18n.Deserialize(byteBuf);
                _dataList.Add(v);
                _dataMap.Add(v.Key, v);
            }
        }

        public System.Collections.Generic.Dictionary<string, I18n> DataMap => _dataMap;
        public System.Collections.Generic.List<I18n> DataList => _dataList;

        public I18n Get(string key) => _dataMap.TryGetValue(key, out var v) ? v : default;

    }
}

