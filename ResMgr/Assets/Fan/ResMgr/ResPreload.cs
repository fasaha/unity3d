using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fan.ResMgr
{
    public sealed class ResPreload : MonoBehaviour
    {
        public static ResPreload instance;

        public delegate void CheckedVersionHandler(ResPreload target, long totalLength, int totalNum);
        public event CheckedVersionHandler checkedVersionHandler;

        public delegate void ProgressHandler(ResPreload target, float progress, int totalNum, int currentNum);
        public event ProgressHandler progressHandler;

        public delegate void CompleteHandler(ResPreload target);
        public event CompleteHandler completeHandler;

        private Dictionary<string, AssetInfo> _diffAssetDic;
        private int _totalNum;
        private int _currentNum;
        private long _totalLength;

        public long TotalLength
        {
            get { return _totalLength; }
        }

        private List<Regex> _excludeSubpathRegexList;//被排掉的子路径

        private bool IsExclude(string subpath)
        {
            foreach (Regex regex in _excludeSubpathRegexList)
            {
                if (regex.IsMatch(subpath))
                    return true;
            }
            return false;
        }

        private bool _checkedVersion = false;
        public void CheckVersion(List<Regex> excludeSubpathRegexList)
        {
            if (_checkedVersion)
                return;
            _excludeSubpathRegexList = excludeSubpathRegexList;
            _checkedVersion = true;
            LoadLocalManifest();
            LoadRemoteManifest();
        }

        private void LoadLocalManifest()
        {
            ResMgr.instance.localManifest = ResUtil.GetManifestFromLocal();
            if (ResMgr.instance.localManifest == null)
                ResMgr.instance.localManifest = new ManifestInfo();
        }

        private bool _isPreloading = false;
        public void Preload()
        {
            if (_isPreloading)
                return;
            _isPreloading = true;
            ResMgr.instance.CompleteAction += CompleteAction;
            ResMgr.instance.ProgressAction += ProgressAction;
            ResMgr.instance.ErrorAction += ErrorAction;
            foreach (var kv in _diffAssetDic)
            {
                //Observers.Facade.Instance.RegisterObserver(kv.Value.SubPath, this);
                ResMgr.instance.PushRes(new ResObj(kv.Value.SubPath, 0));
            }
        }

        private void Awake()
        {
            if (instance != null)
                throw new System.Exception("ResPreload must be singleton");
            instance = this;
            //CheckVersion();//外部手动去调用
        }

        private void OnDestroy()
        {
            _diffAssetDic = null;

            progressHandler = null;
            completeHandler = null;
            checkedVersionHandler = null;
            if (ResMgr.instance != null)
            {
                ResMgr.instance.CompleteAction -= CompleteAction;
                ResMgr.instance.ProgressAction -= ProgressAction;
                ResMgr.instance.ErrorAction -= ErrorAction;
            }
            StopAllCoroutines();
            instance = null;
        }

        private void LoadRemoteManifest()
        {
            StopCoroutine("LoadRemoteManifestCoroutine");
            StartCoroutine("LoadRemoteManifestCoroutine");
        }

        //=============================================www加载有bug==========================start
        private float _currentProgress;
        private float _lastProgressTime;
        private const float ERR_TIMEOUT = 5f;//5s秒进度没有变化要重新开始加载
        private bool IsErrorProgress(float value)
        {
            if (_currentProgress != value)
            {
                _lastProgressTime = Time.time;
                _currentProgress = value;
                return false;
            }
            if (Time.time - _lastProgressTime > ERR_TIMEOUT)
            {
                _currentProgress = -1;
                return true;
            }
            return false;
        }
        //=============================================www加载有bug==========================end

        private System.Collections.IEnumerator LoadRemoteManifestCoroutine()
        {
            WWW www = new WWW(ResUtil.GetRemoteManifestPath() + "?v=" + System.DateTime.Now.Ticks);
            while (!www.isDone)
            {
                if (IsErrorProgress(www.progress))
                {
                    Fan.MDebug.Log("manifest加载失败aaa:" + www.url);
                    www.Dispose();
                    www = null;
                    LoadRemoteManifest();
                    yield break;
                }
                yield return 1;
            }
            if (!string.IsNullOrEmpty(www.error))
            {
                Fan.MDebug.Log("manifest加载失败bbb:" + www.url);
                www.Dispose();
                www = null;
                yield return new WaitForSeconds(0.2f);
                LoadRemoteManifest();
            }
            else
            {
                Fan.MDebug.Log("manifest加载成功:" + www.url);
                ResMgr.instance.remoteManifest = ResUtil.GetManifestFromBytes(www.bytes);
                if (ResMgr.instance.remoteManifest == null)
                    ResMgr.instance.remoteManifest = new ManifestInfo();

                www.Dispose();
                www = null;
                CheckDifference();
            }
        }

        private void CheckDifference()
        {
            _totalLength = 0;
            _diffAssetDic = new Dictionary<string, AssetInfo>();
            AssetInfo localAsset;
            if (ResMgr.instance.localManifest.version != ResMgr.instance.remoteManifest.version)
            {
                ResMgr.instance.localManifest.version = ResMgr.instance.remoteManifest.version;
                ResMgr.instance.localManifest.assetDic.Clear();
                ResUtil.DelAllLocalAbRes();
            }

            string tmpStr;
            foreach (var kv in ResMgr.instance.remoteManifest.assetDic)
            {
                //Fan.MDebug.Log("不要预先加载"+ kv.Key);
                if (IsExclude(kv.Key))
                {
                    continue;
                }

                if (ResMgr.instance.localManifest.assetDic.TryGetValue(kv.Key, out localAsset))
                {
                    if (!localAsset.Equals(kv.Value) || !ResUtil.ExistsInLocal(kv.Key, out tmpStr))
                    {
                        _diffAssetDic.Add(kv.Key, kv.Value);
                        _totalLength += kv.Value.Length;
                    }
                }
                else
                {
                    _diffAssetDic.Add(kv.Key, kv.Value);
                    _totalLength += kv.Value.Length;
                }
            }

            _totalNum = _diffAssetDic.Count;
            _currentNum = 1;

            if (checkedVersionHandler != null)
                checkedVersionHandler(this, _totalLength, _totalNum);
            Fan.MDebug.Log(string.Format("检查更新结束_totalLength:{0},_totalNum{1}", _totalLength, _totalNum));
            if (_totalNum == 0)
            {
                Fan.MDebug.Log("资源同步完成");
                if (completeHandler != null)
                    completeHandler(this);
                UnityEngine.Object.Destroy(this);
            }
            else
            {
                //Preload();检查完之后手动去调用
            }
        }

        private void CompleteAction(string subPath, System.Object res)
        {
            ResMgr.instance.RemoveRes(subPath);
            _diffAssetDic.Remove(subPath);
            _currentNum++;
            if (_diffAssetDic.Count == 0)
            {
                if (ResMgr.instance != null)
                {
                    ResMgr.instance.CompleteAction -= CompleteAction;
                    ResMgr.instance.ProgressAction -= ProgressAction;
                    ResMgr.instance.ErrorAction -= ErrorAction;
                }

                Fan.MDebug.Log("资源同步完成");
                if (completeHandler != null)
                    completeHandler(this);
                
                UnityEngine.Object.Destroy(this);
            }
        }
        private void ProgressAction(string subPath, float progress)
        {
            if (progressHandler != null)
                progressHandler(this, progress, _totalNum, _currentNum);
        }
        private void ErrorAction(string subPath, System.Object res)
        {
        }

    }
}
