
using System.Collections.Generic;
using UnityEditor;
namespace Fan.ResMgr.Editor
{
    public static class AssetBundleCommand
    {
        public static void ExportAssets(List<string> fileList, List<string> fileNameList, string targetSubPath, string desc)
        {
            if(fileList == null || fileNameList == null || fileList.Count == 0 || fileList.Count != fileNameList.Count)
            {
                string content = string.Format("打包失败1，原因：未传入文件或表格不对 targetSubPath:{0} desc:{1}", targetSubPath, desc);
                Fan.MDebug.Log(content);
                EditorUtility.DisplayDialog("打包失败1", content, "OK");
              
                return;
            }
          
            bool hasChangedFile = SAUtil.HasChangedFile(fileList);
            if (!hasChangedFile)
            {
                string content = string.Format("打包失败2，原因：未更改过文件 targetSubPath:{0} desc:{1}", targetSubPath,desc);
                Fan.MDebug.Log(content);
                EditorUtility.DisplayDialog("打包失败2", content, "OK");
                return;
            }
            //===============================================
            List<UnityEngine.Object> objList = null;
            
            objList = new List<UnityEngine.Object>();
            foreach (string file in fileList)
            {
                UnityEngine.Object o = AssetDatabase.LoadAssetAtPath(file, typeof(System.Object));
                if (o == null)
                {
                    throw new System.Exception("you can not LoadAssetAtPath:" + file);
                }
                objList.Add(o);
            }

            //===============================================
            string exportPath = SAConf.GetExportPath(targetSubPath);
            string targetDir = ResUtil.GetDirectoryByPath(exportPath);
            if (!System.IO.Directory.Exists(targetDir))
                System.IO.Directory.CreateDirectory(targetDir);
            BuildPipeline.BuildAssetBundleExplicitAssetNames(objList.ToArray(),fileNameList.ToArray(), exportPath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, SAConf.buildTarget);
            List<AssetInfo> assetInfoList = new List<AssetInfo>();
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(exportPath);
            assetInfoList.Add(new AssetInfo(targetSubPath, fileInfo.Length,fileInfo.LastWriteTime.Ticks));
            SAUtil.SaveManifestInfo(assetInfoList);
            SAUtil.SaveSAInfo(fileList);

            string strcontent = string.Format("打包成功,targetSubPath:{0} desc:{1}", targetSubPath, desc);
            UnityEngine.Debug.Log(strcontent);
            //EditorUtility.DisplayDialog("打包成功", strcontent, "OK");

        }
    }
}
