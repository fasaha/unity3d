using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fan.ResMgr.Demo
{
    public class ResMgrDemo : MonoBehaviour
    {
        void Start()
        {
            Fan.ResMgr.ResPreload.instance.checkedVersionHandler += PreLoadCheckedVersionHandler;
            Fan.ResMgr.ResPreload.instance.progressHandler += PreLoadProgressHandler;
            Fan.ResMgr.ResPreload.instance.completeHandler += PreLoadCompleteHandler;

            Fan.ResMgr.ResPreload.instance.CheckVersion(GetExcludeSubpath());
        }

        private List<Regex> GetExcludeSubpath()
        {
            List<Regex> excludeSubpath = new List<Regex>();
            //excludeSubpath.Add(new Regex(@""));
            //excludeSubpath.Add(new Regex(@"activity/"));
            return excludeSubpath;
        }

        private void PreLoadCheckedVersionHandler(Fan.ResMgr.ResPreload target, long totalLength, int totalNum)
        {
            Fan.ResMgr.ResPreload.instance.checkedVersionHandler -= PreLoadCheckedVersionHandler;
            if (totalNum > 0)
            {
                target.Preload();
            }
        }

        private void PreLoadProgressHandler(Fan.ResMgr.ResPreload target, float progress, int totalNum, int currentNum)
        {
            Fan.MDebug.Log(string.Format("预加载totalNum:{0},currentNum:{1},pregress:{2}", totalNum, currentNum, progress));
        }


        private string ConfigABName = string.Format("config.{0}/config.ab", "zh-cn");
        private void PreLoadCompleteHandler(Fan.ResMgr.ResPreload target)
        {
            Fan.MDebug.Log("PreLoadCompleteHandler");
            Fan.ResMgr.ResPreload.instance.checkedVersionHandler -= PreLoadCheckedVersionHandler;
            Fan.ResMgr.ResPreload.instance.completeHandler -= PreLoadCompleteHandler;
            Fan.ResMgr.ResPreload.instance.progressHandler -= PreLoadProgressHandler;
            Fan.ResMgr.ResMgr.instance.RemoveAllRes();

            //Observers.Facade.Instance.RegisterObserver(ConfigABName, this);
            ResMgr.instance.CompleteAction += CompleteAction;
            ResMgr.instance.ProgressAction += ProgressAction;
            ResMgr.instance.ErrorAction += ErrorAction;

            Fan.ResMgr.ResMgr.instance.PushRes(new Fan.ResMgr.ResObj(ConfigABName));
        }

        private void CompleteAction(string subPath, System.Object res)
        {
            if (subPath == ConfigABName)
            {
                if (ResMgr.instance != null)
                {
                    ResMgr.instance.CompleteAction -= CompleteAction;
                    ResMgr.instance.ProgressAction -= ProgressAction;
                    ResMgr.instance.ErrorAction -= ErrorAction;
                }

                TextAsset ta = ResMgr.instance.GetRes(ConfigABName, "diamondcharge_1") as TextAsset;
                Fan.MDebug.Log(ta.text);
            }
        }
        private void ProgressAction(string subPath, float progress)
        {
        }
        private void ErrorAction(string subPath, System.Object res)
        {
        }

        
    }
}
