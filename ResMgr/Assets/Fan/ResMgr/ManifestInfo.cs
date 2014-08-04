using System.Collections.Generic;
using System.Text;

namespace Fan.ResMgr
{
    public class AssetInfo : System.Object
    {
        public AssetInfo(string subPath, long length, long createDate)
        {
            _subPath = subPath;
            _length = length;
            _createDate = createDate;
           
        }

        private string _subPath;

        public string SubPath
        {
            get { return _subPath; }
        }

        private long _createDate;

        public long CreateDate
        {
            get { return _createDate; }
            set { _createDate = value; }
        }

        private long _length;

        public long Length
        {
            get { return _length; }
            set { _length = value; }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();//override　Equals　后必须override GetHashCode
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AssetInfo))
                return false;
           
            AssetInfo assetInfo = obj as AssetInfo;
            return _subPath == assetInfo.SubPath && _createDate == assetInfo._createDate;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[AssetInfo:");
            sb.Append("_subPath:");
            sb.Append(_subPath);
            sb.Append(",_length:");
            sb.Append(_length);
            sb.Append(",_createDate:");
            sb.Append(_createDate);
            sb.Append("]");
            return sb.ToString();
        }
    }

    public class ManifestInfo : System.Object
    {
        public uint version; //本地manifest版本不同于网上的manifest版本的时候，忽略删除本地ab文件夹下所有资源
        public Dictionary<string, AssetInfo> assetDic;

        public ManifestInfo()
        {
            version = 1;
            assetDic = new Dictionary<string, AssetInfo>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ManifestInfo:");
            sb.Append("version:");
            sb.Append(version);
            sb.Append(",assetDic:{");
            foreach (var kv in assetDic)
            {
                sb.Append(kv.Value.ToString());
                sb.Append(";");
            }
               
            sb.Append("}]");
            return sb.ToString();
        }
    }
}
