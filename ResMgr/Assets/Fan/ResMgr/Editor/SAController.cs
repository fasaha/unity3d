using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Fan.ResMgr.Editor
{
    //SourceAssetInfo原始文件清单
    [XmlRoot("root")]
    public class SAInfo
    {
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public HashSet<SAItem> saItemSet;

        public SAInfo()
        {
            saItemSet = new HashSet<SAItem>();
        }

        public SAItem GetSAItem(string subPath)
        {
            if (saItemSet == null)
                return null;
            foreach(SAItem saItem in saItemSet)
            {
                if (saItem.subPath == subPath)
                    return saItem;
            }
            return null;
        }

        public void AddSAItem(SAItem saItem)
        {
            SAItem existSAItem = GetSAItem(saItem.subPath);
            if (existSAItem != null)
            {
                saItemSet.Remove(existSAItem);
            }
            saItemSet.Add(saItem);
        }
    }
    //本地原始文件条目
    //存储了本地曾经被打包过的原始文件信息
    //1,路径 2,最后更改时间 3,文件大小
    public class SAItem : System.Object
    {
        [XmlAttribute("sub_path")]
        public String subPath;

        //[XmlAttribute("last_write_time")]
        //public long lastWriteTime;

        [XmlAttribute("length")]
        public long length;

        [XmlAttribute("md5_hash")]
        public string md5Hash;

        public SAItem()
        {
        }

        public SAItem(string subPath)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(subPath);
            this.subPath = subPath;
            //this.lastWriteTime = fileInfo.LastWriteTime.Ticks;
            this.length = fileInfo.Length;
            this.md5Hash = SAUtil.EncryptMD5(System.IO.File.ReadAllBytes(subPath));
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[SAItem:");
            sb.Append("subPath:");
            sb.Append(subPath);
            //sb.Append(",lastWriteTime:");
            //sb.Append(lastWriteTime);
            sb.Append(",md5Hash:");
            sb.Append(md5Hash);
            sb.Append(",length:");
            sb.Append(length);
            sb.Append("]");
            return sb.ToString();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();//override　Equals　后必须override GetHashCode
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if(!(obj is SAItem))
                return false;
            SAItem saItemObj = obj as SAItem;
            if (subPath != saItemObj.subPath)
                return false;
            //if(lastWriteTime != saItemObj.lastWriteTime)
            //    return false;
            if (md5Hash != saItemObj.md5Hash)
                return false;
            if(length != saItemObj.length)
                return false;
            return true;
        }
    }

    public static class SAConf
    {
        public static UnityEditor.BuildTarget buildTarget
        {
            get
            {
                UnityEditor.BuildTarget result;
                switch (ResConf.eResPlatform)
                {
                    case EResPlatform.android:
                        result = UnityEditor.BuildTarget.Android;
                        break;
                    case EResPlatform.iphone:
                        result = UnityEditor.BuildTarget.iPhone;
                        break;
                    case EResPlatform.standalonewindows:
                        result = UnityEditor.BuildTarget.StandaloneWindows;
                        break;
                    case EResPlatform.webplayer:
                        result = UnityEditor.BuildTarget.WebPlayer;
                        break;
                    case EResPlatform.unknown:
                        result = UnityEditor.BuildTarget.Android;
                        break;
                    default :
                        result = UnityEditor.BuildTarget.Android;
                        break;
                }
                return result;
            }
        }

        /// <summary>
        /// 本地原始文件清单路径
        /// </summary>
        public static string SA_INFO_PATH
        {
            get
            {
                return string.Format(@"./BuildFan/source_asset.{0}.xml", ResConf.eResPlatform);
            }
        }

        //导出的基础路径
        private static string AB_ROOT
        {
            get
            {
                return string.Format(@"./BuildFan/{0}/ab/",ResConf.eResPlatform);
            }
        }
        //导出manifest的路径
        public static string MANIFEST_PATH
        {
            get
            {
                return string.Format("{0}{1}",SAConf.AB_ROOT, ResUtil.MANIFESTNAME);
            }
        }
        //导出资源的路径
        public static string GetExportPath(string subPath)
        {
            return string.Format("{0}{1}", SAConf.AB_ROOT, subPath);
        }

       
    }

    public static class SAUtil
    {

        public static string EncryptMD5(byte[] bytes)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(bytes);
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    sBuilder.Append(data[i].ToString("x2"));
                return sBuilder.ToString();
            }
        }

        public static ManifestInfo GetManifestInfo()
        {
            if (!System.IO.File.Exists(SAConf.MANIFEST_PATH))
                return null;
            return ResUtil.GetManifestFromBytes(System.IO.File.ReadAllBytes(SAConf.MANIFEST_PATH));
        }
        /// <summary>
        /// 把一组assetbundle写到manifest清单中
        /// </summary>
        /// <param name="abSubPaths"></param>
        public static void SaveManifestInfo(List<AssetInfo> assetInfoList)
        {
            if (assetInfoList == null || assetInfoList.Count == 0)
                return;
            ManifestInfo manifestInfo = SAUtil.GetManifestInfo();
            if (manifestInfo == null)
                manifestInfo = new ManifestInfo();
            foreach (AssetInfo assetInfo in assetInfoList)
            {
                AssetInfo oldAssetInfo;
                if (manifestInfo.assetDic.TryGetValue(assetInfo.SubPath, out oldAssetInfo))
                {
                    oldAssetInfo.CreateDate = assetInfo.CreateDate;
                    oldAssetInfo.Length = assetInfo.Length;
                }
                else
                {
                    manifestInfo.assetDic.Add(assetInfo.SubPath,assetInfo);
                }
            }
            string dir = ResUtil.GetDirectoryByPath(SAConf.MANIFEST_PATH);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(SAConf.MANIFEST_PATH, ResUtil.GetBytesFromManifest(manifestInfo));
        }
       /// <summary>
       /// 对比一组文件是否和原始文件清单一致
       /// </summary>
       /// <param name="fileSubPaths">所到对比的文件列表</param>
       /// <returns></returns>
        public static bool HasChangedFile(List<string> fileSubPaths)
        {
            SAInfo saInfo = SAUtil.GetSAInfo();
            if(saInfo == null)
                return true;
            SAItem saItemNow;
            SAItem saItemHistory;
            string metaFile;
            foreach (string subPath in fileSubPaths)
            {
                saItemNow = new SAItem(subPath);
                saItemHistory = saInfo.GetSAItem(subPath);
                if (!saItemNow.Equals(saItemHistory))
                    return true;
                metaFile = subPath + ".meta";
                saItemNow = new SAItem(metaFile);
                saItemHistory = saInfo.GetSAItem(metaFile);
                if (!saItemNow.Equals(saItemHistory))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 把这组文件存到本地原始文件清单
        /// </summary>
        /// <param name="fileSubPaths">所要存储的文件列表</param>
        public static void SaveSAInfo(List<string> fileSubPaths)
        {
            if (fileSubPaths == null || fileSubPaths.Count == 0)
                return;
            SAInfo saInfo = SAUtil.GetSAInfo();
            if(saInfo == null)
                saInfo = new SAInfo();
            foreach (string fileSubPath in fileSubPaths)
            {
                saInfo.AddSAItem(new SAItem(fileSubPath));
                saInfo.AddSAItem(new SAItem(fileSubPath + ".meta"));
            }

            using (System.IO.StreamWriter streamWriter = System.IO.File.CreateText(SAConf.SA_INFO_PATH))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SAInfo));
                xmlSerializer.Serialize(streamWriter, saInfo);
            }
        }
        /// <summary>
        /// 获取本地原始文件清单
        /// </summary>
        /// <returns></returns>
        private static SAInfo GetSAInfo()
        {
            if (!System.IO.File.Exists(SAConf.SA_INFO_PATH))
            {
                Fan.MDebug.LogError("本地无打包文件的清单列表？");
                return null;
            }
            using ( System.IO.FileStream fs = System.IO.File.OpenRead(SAConf.SA_INFO_PATH))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SAInfo));
                return xmlSerializer.Deserialize(fs) as SAInfo;
            }
        }

    }


}
