using System.Collections.Generic;
using UnityEditor;

namespace Fan.ResMgr.Editor
{
    public class GameResBuilder
    {
        private const string BASE_SOURCE_DIR = "Assets/Fan/ResMgr/Demo/abres/";

        public static string GetSingleABDefaultName(string subPath)
        {
            return subPath.Substring(BASE_SOURCE_DIR.Length, subPath.LastIndexOf(".") - BASE_SOURCE_DIR.Length) + ".ab";
        }

        [MenuItem("ResMgr/打包活动" + Fan.ResMgr.ResConf.E_RES_PLATFORM_NAME, false, 100)]
        public static void BuildActivity()
        {
            string sourceDir = BASE_SOURCE_DIR + "activity/";
            string targetSubPath;
            List<string> files = Fan.ResMgr.ResUtil.RecursiveGetFiles(sourceDir);
            for (int i = 0; i < files.Count; )
            {
                files[i] = files[i].Replace('\\', '/');
                if (files[i].EndsWith(".meta"))
                {
                    files.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            List<string> singleFileList = new List<string>();
            List<string> singleFileNameList = new List<string>();
            foreach (string file in files)
            {
                singleFileList.Clear();
                singleFileNameList.Clear();
                targetSubPath = GetSingleABDefaultName(file);
                singleFileList.Add(file);
                string fileName = targetSubPath.Substring(9, targetSubPath.Length - 12);
                singleFileNameList.Add(fileName);
                //Fan.MDebug.Log(targetSubPath);//比如：activity/Banner022.ab
                //Fan.MDebug.Log(fileName);//比如/Banner022.ab
                Fan.ResMgr.Editor.AssetBundleCommand.ExportAssets(singleFileList, singleFileNameList, targetSubPath, "打包活动:" + file);
            }
        }

        [MenuItem("ResMgr/打包配置" + Fan.ResMgr.ResConf.E_RES_PLATFORM_NAME + "/" + "zh-cn", false, 101)]
        public static void BuildConfigZHCN()
        {
            BuildConfig("zh-cn");
        }

        //[MenuItem("ResMgr/打包配置" + Fan.ResMgr.ResConf.E_RES_PLATFORM_NAME + "/" + "zh-tw", false, 101)]
        //public static void BuildConfigZHTW()
        //{
        //    BuildConfig("zh-tw");
        //}

        public static void BuildConfig(string language)
        {
            string sourceDir = string.Format("{0}config.{1}/", BASE_SOURCE_DIR, language);
            string targetSubPath = string.Format("config.{0}/config.ab", language);
            List<string> files = Fan.ResMgr.ResUtil.RecursiveGetFiles(sourceDir);

            for (int i = 0; i < files.Count; )
            {
                files[i] = files[i].Replace('\\', '/');
                if (files[i].EndsWith(".meta"))
                {
                    files.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            List<string> fileNames = new List<string>();
            int len = BASE_SOURCE_DIR.Length + "config./".Length + language.Length;
            foreach (string file in files)
            {
                string fileName = file.Substring(len, file.LastIndexOf(".") - len);
                fileNames.Add(fileName);
                //Fan.MDebug.Log(fileName);
            }
            Fan.ResMgr.Editor.AssetBundleCommand.ExportAssets(files, fileNames, targetSubPath, "打包配置" + language);
        }
    }
}
