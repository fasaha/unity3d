
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Fan.ResMgr
{
    public static class ResUtil
    {
        public const string MANIFESTNAME = "manifest.bin";

        public static List<string> RecursiveGetFiles(string path)
        {
            List<string> result = new List<string>();
            if (!System.IO.Directory.Exists(path))
            {
                if (System.IO.File.Exists(path))
                    result.Add(path);
                return result;
            }
            result.AddRange(System.IO.Directory.GetFiles(path));
            foreach (string dir in System.IO.Directory.GetDirectories(path))
            {
                result.AddRange(RecursiveGetFiles(dir));
            }
            return result;
        }
     
        public static string GetDirectoryByPath(string path)
        {
            return path.Substring(0, path.LastIndexOf("/"));
        }

        private static string GetFullPath(string basePath, string subPath)
        {
            return string.Format(@"{0}{1}/ab/{2}", basePath, ResConf.eResPlatform, subPath);
        }

        public static string GetRemotePathRoot(string subPath)
        {
            if (ResMgr.instance == null)
            {
                throw new System.Exception("you must run game first");
            }
            return string.Format(@"{0}{1}", ResMgr.instance.basePath, subPath);
        }

        //本地ab路径
        public static string GetLocalPath(string subPath)
        {
            return GetFullPath(Application.persistentDataPath + "/",subPath);
        }
        //远程ab路径
        public static string GetRemotePath(string subPath)
        {
            if(ResMgr.instance == null)
            {
                throw new System.Exception("you must run game first");
            }
            return GetFullPath(ResMgr.instance.basePath, subPath);
        }
        //本地manifest路径
        public static string GetLocalManifestPath()
        {
            return GetLocalPath(ResUtil.MANIFESTNAME);
        }
        //远程manifest路径
        public static string GetRemoteManifestPath()
        {
            return GetRemotePath(ResUtil.MANIFESTNAME);
        }

        public static bool ExistsInLocal(string subPath, out string localPath)
        {
            localPath = GetLocalPath(subPath);
            if (System.IO.File.Exists(localPath))
            {
                return true;
            }
            localPath = null;
            return false;
        }
       
        public static void Save2Local(string subPath, byte[] bytes)
        {
            string path = GetLocalPath(subPath);
            string dir = ResUtil.GetDirectoryByPath(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
            fs.SetLength(0);
            if (bytes != null)
                fs.Write(bytes, 0, bytes.Length);
            fs.Close();
            fs.Dispose();
        }

        //public static bool DelLocalAbRes(string subPath)
        //{
        //    string path = GetLocalPath(subPath);
        //    if (System.IO.File.Exists(path))
        //    {
        //        System.IO.File.Delete(path);
        //        return true;
        //    }
        //    return false;
        //}

        public static bool DelAllLocalAbRes()
        {
            string s = GetLocalPath(string.Empty);
            if(System.IO.Directory.Exists(s))
            {
                System.IO.Directory.Delete(s, true);
                return true;
            }
            return false;
        }

        #region Manifest文件处理相关

        //获取本地的manifest文件信息
        public static ManifestInfo GetManifestFromLocal()
        {
            string path = GetLocalManifestPath();
            if (!System.IO.File.Exists(path))
                return null;
            return GetManifestFromBytes(System.IO.File.ReadAllBytes(path));
        }
        //把manifest文件信息保存在本地
        public static void SaveManifest2Local(ManifestInfo manifestInfo)
        {
            string path = GetLocalManifestPath();
            string dir = ResUtil.GetDirectoryByPath(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(path, GetBytesFromManifest(manifestInfo));
        }

        //把字节流转为manifest对象
        public static ManifestInfo GetManifestFromBytes(byte[] bytes)
        {
            ManifestInfo manifestInfo = null;
            try
            {
                byte[] inflatBytes = Fan.Utils.ZipUtil.Decompress(bytes);
               
                System.IO.MemoryStream ms = new System.IO.MemoryStream(inflatBytes);
                System.IO.BinaryReader br = new System.IO.BinaryReader(ms);
                br.BaseStream.Position = 0;
                manifestInfo = new ManifestInfo();
                manifestInfo.version = br.ReadUInt32();
                uint num = br.ReadUInt32();
                AssetInfo assetInfo;
                while (num-- > 0)
                {
                    assetInfo = new AssetInfo(br.ReadString(), br.ReadInt64(), br.ReadInt64());
                    manifestInfo.assetDic.Add(assetInfo.SubPath, assetInfo);
                }
                br.Close();
                ms.Close();
                ms.Dispose();
                return manifestInfo;
            }
            catch (Exception e)
            {
                Fan.MDebug.Log("GetManifestFromBytes:" + e);
            }
            finally
            {
            }
            return null;
        }

        //把manifest对象转为字节流
        public static byte[] GetBytesFromManifest(ManifestInfo manifestInfo)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms);
            bw.Write(manifestInfo.version);
            bw.Write(manifestInfo.assetDic.Count);
            foreach (var kv in manifestInfo.assetDic)
            {
                bw.Write(kv.Value.SubPath);
                bw.Write(kv.Value.Length);
                bw.Write(kv.Value.CreateDate);
            }
            ms.Position = 0;
            int len = (int)ms.Length;//manifest文件不能超2GB
            byte[] bytes = new byte[len];
            ms.Read(bytes, 0, len);
            bw.Close();
            ms.Close();
            ms.Dispose();
            return Fan.Utils.ZipUtil.Compress(bytes);
        }
        #endregion


    }
}
