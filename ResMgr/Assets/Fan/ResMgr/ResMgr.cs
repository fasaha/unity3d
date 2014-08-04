using System.Collections.Generic;
using UnityEngine;

namespace Fan.ResMgr
{
    public sealed class ResMgr : MonoBehaviour
    {
        public const string TYPE_PROGRESS = "RESMGR_PROGRESS";
        public const string TYPE_COMPLETE = "RESMGR_COMPLETE";
        public const string TYPE_ERROR = "RESMGR_ERROR";

        //public System.Action<string, ResObj> CompleteAction;
        public System.Action<string, UnityEngine.Object> CompleteAction;
        public System.Action<string, float> ProgressAction;
        public System.Action<string, ResObj> ErrorAction;
        
        public static ResMgr instance;

        public ManifestInfo remoteManifest;
        public ManifestInfo localManifest;

        public string basePath = "http://kokres.fthgame.com/";

        private byte NUM_LOADING_MAX = 1;//同时可以启用的www下载个数
        private GameObject _loaderRoot;//把加载中的ResLoader放到这个节点下面

        private List<ResLoader> _waitingList;//等待队列
        private List<ResLoader> _loadingList;
        private Dictionary<string, AssetBundle> _resDic;//资源列表

#if UNITY_EDITOR
        public Dictionary<string, AssetBundle> ResDic
        {
            get { return _resDic; }
        }
#endif
        private void Awake()
        {
            if (instance != null)
                throw new System.Exception("ResMgr must be singleton");
            instance = this;
            _loaderRoot = new GameObject("_loaderRoot");
            _loaderRoot.transform.parent = transform;
            _waitingList = new List<ResLoader>();
            _loadingList = new List<ResLoader>();
            _resDic = new Dictionary<string, AssetBundle>();
        }

        void OnDestroy()
        {
            RemoveAllRes();
        }

        #region 公开方法
        //加载一个资源
        public void PushRes(ResObj resObj)
        {
            if (_resDic.ContainsKey(resObj.SubPath))
            {
                Fan.MDebug.Log(string.Format("加载重复：loaded"));
                //Observers.Facade.Instance.SendNotification(resObj.SubPath, GetRes(resObj.SubPath),TYPE_COMPLETE);
                if (CompleteAction != null)
                    CompleteAction(resObj.SubPath, GetRes(resObj.SubPath));
                return;
            }
            ResLoader resLoader;
            int i;
            int count;
            for(i = 0, count = _waitingList.Count; i< count; i++)
            {
                resLoader = _waitingList[i];
                if(resLoader.ResObj.SubPath == resObj.SubPath)
                {
                    Fan.MDebug.Log(string.Format("加载重复：beforeLoad"));
                    return;
                }
            }

            for (i = 0, count = _loadingList.Count; i < count; i++)
            {
                resLoader = _loadingList[i];
                if (resLoader.ResObj.SubPath == resObj.SubPath)
                {
                    Debug.Log(string.Format("加载重复：loading"));
                    return;
                }
            }
            resLoader = _loaderRoot.AddComponent<ResLoader>();
            resLoader.ResObj = resObj;
            _waitingList.Add(resLoader);
            _waitingList.Sort(CompareResLoaderPrior);
            TryLoadRes();
        }

        //获取一个资源
        public AssetBundle GetRes(string subPath)
        {
            AssetBundle result = null;
            if (_resDic.TryGetValue(subPath, out result))
            {
                return result;
            }
            return null;
        }

        public UnityEngine.Object GetRes(string subPath,string resName)
        {
            AssetBundle ab = GetRes(subPath);
            if (ab == null)
                return null;
            return ab.Load(resName, typeof(System.Object));
        }

        #region 异步获取资源
        public void GetResAsync(string subPath, string resName)
        {
            AssetBundle ab = GetRes(subPath);
            if (ab == null || !ab.Contains(resName))
            {
                //Observers.Facade.Instance.SendNotification(string.Format("{0}:{1}", subPath, resName),null,TYPE_COMPLETE);
                if (CompleteAction != null)
                    CompleteAction(string.Format("{0}:{1}", subPath, resName), null);
                return;
            }
           
            AssetBundleRequest abr = ab.LoadAsync(resName, typeof(UnityEngine.Object));
            MAssetBundleRequest mabr = new MAssetBundleRequest(abr, subPath, resName);
            bool isCoroutining;
            if (_getResAsyncMAssetBundleRequestList == null)
            {
                _getResAsyncMAssetBundleRequestList = new List<MAssetBundleRequest>();
                isCoroutining = false;
            }
            else
            {
                isCoroutining = _getResAsyncMAssetBundleRequestList.Count > 0;
            }

            _getResAsyncMAssetBundleRequestList.Add(mabr);
            if (!isCoroutining)
            {
                StartCoroutine("GetResAsyncCoroutine");
            }
        }

        private sealed class MAssetBundleRequest : System.Object
        {
            private AssetBundleRequest _assetBundleRequest;

            public AssetBundleRequest AssetBundleRequest
            {
                get { return _assetBundleRequest; }
            }
            private string _subPath;

            public string SubPath
            {
                get { return _subPath; }
            }
            private string _resName;

            public string ResName
            {
                get { return _resName; }
            }
            public MAssetBundleRequest(AssetBundleRequest abr,string subPath,string resName)
            {
                _assetBundleRequest = abr;
                _subPath = subPath;
                _resName = resName;
            }
        }

        private List<MAssetBundleRequest> _getResAsyncMAssetBundleRequestList = null;

        private System.Collections.IEnumerator GetResAsyncCoroutine()
        {
            while(true)
            {
                if (_getResAsyncMAssetBundleRequestList == null || _getResAsyncMAssetBundleRequestList.Count <= 0)
                    yield break;
                MAssetBundleRequest mabr;
                for (int i = 0, count = _getResAsyncMAssetBundleRequestList.Count; i < count;i++)
                {
                    mabr = _getResAsyncMAssetBundleRequestList[i];
                    if (mabr.AssetBundleRequest.isDone)
                    {
                        //Observers.Facade.Instance.SendNotification(string.Format("{0}:{1}", mabr.SubPath, mabr.ResName), mabr.AssetBundleRequest.asset,TYPE_COMPLETE);
                        if (CompleteAction != null)
                            CompleteAction(string.Format("{0}:{1}", mabr.SubPath, mabr.ResName), mabr.AssetBundleRequest.asset);
                        _getResAsyncMAssetBundleRequestList.RemoveAt(i);
                    }
                    else
                    {
                        Fan.MDebug.Log(mabr.AssetBundleRequest.progress);
                        //Observers.Facade.Instance.SendNotification(string.Format("{0}:{1}", mabr.SubPath, mabr.ResName), mabr.AssetBundleRequest.progress,TYPE_PROGRESS);
                        if (ProgressAction != null)
                            ProgressAction(string.Format("{0}:{1}", mabr.SubPath, mabr.ResName), mabr.AssetBundleRequest.progress);
                    }
                }
                yield return 1;
            }
        }

        #endregion 
        //清理一个资源
        public bool RemoveRes(string subPath)
        {
            AssetBundle ab = null;
            if (_resDic.TryGetValue(subPath, out ab))
            {
                _resDic.Remove(subPath);
                if (ab != null)
                {
                    ab.Unload(true);
                    ab = null;
                    return true;
                }
            }
            ResLoader resLoader;
            int i;
            int count;
            for (i = 0, count = _waitingList.Count; i < count; i++)
            {
                resLoader = _waitingList[i];
                if (resLoader.ResObj.SubPath == subPath)
                {
                    GameObject.DestroyImmediate(resLoader);
                    _waitingList.RemoveAt(i);
                    return true;
                }
            }

            for (i = 0, count = _loadingList.Count; i < count; i++)
            {
                resLoader = _loadingList[i];
                if (resLoader.ResObj.SubPath == subPath)
                {
                    GameObject.DestroyImmediate(resLoader);
                    _loadingList.RemoveAt(i);
                    TryLoadRes();
                    return true;
                }
            }
            return false;
        }

        //清理所有资源
        public void RemoveAllRes()
        {
            AssetBundle ab;
            foreach(var kv in _resDic)
            {
                ab = kv.Value;
                if (ab != null)
                {
                    ab.Unload(true);
                    ab = null;
                }
            }
            _resDic.Clear();

            ResLoader resLoader;
            int i;
            int count;
            for (i = 0, count = _waitingList.Count; i < count; i++)
            {
                resLoader = _waitingList[i];
                GameObject.DestroyImmediate(resLoader);
            }
            _waitingList.Clear();

            for (i = 0, count = _loadingList.Count; i < count; i++)
            {
                resLoader = _loadingList[i];
                GameObject.DestroyImmediate(resLoader);
            }
            _loadingList.Clear();
        }
       
        #endregion

        //尝试加载一个资源列队中的资源
        private void TryLoadRes()
        {
            if(_waitingList.Count <= 0 || _loadingList.Count >= NUM_LOADING_MAX)
                return;
            ResLoader resLoader = _waitingList[0];
            //Fan.MDebug.Log("开始加载一个资源" + resLoader.ResObj.ToString());
            _waitingList.RemoveAt(0);
            _loadingList.Add(resLoader);
            resLoader.startHandler += OneStartHandler;
            resLoader.progressHandler += OneProgressHandler;
            resLoader.completeHandler += OneCompleteHandler;
            resLoader.errorHandler += OneErrorHandler;
            resLoader.Load();
        }

        #region 单个资源的事件处理
        private void OneStartHandler(ResLoader target)
        {
            target.startHandler -= OneStartHandler;
        }

        private void OneProgressHandler(ResLoader target, float progress)
        {
            //Observers.Facade.Instance.SendNotification(target.ResObj.SubPath, progress, TYPE_PROGRESS);
            if (ProgressAction != null)
                ProgressAction(target.ResObj.SubPath, progress);
        }

        private void OneCompleteHandler(ResLoader target)
        {
            //Fan.MDebug.Log(target.AssetBundle + "加载完成一个资源" + target.ResObj.ToString());
            _loadingList.Remove(target);
            _resDic.Add(target.ResObj.SubPath, target.AssetBundle);
            target.progressHandler -= OneProgressHandler;
            target.completeHandler -= OneCompleteHandler;
            target.errorHandler -= OneErrorHandler;
            //Observers.Facade.Instance.SendNotification(target.ResObj.SubPath,target.ResObj,TYPE_COMPLETE);
            if (CompleteAction != null)
                CompleteAction(target.ResObj.SubPath, target.AssetBundle);
            TryLoadRes();
        }

        private void OneErrorHandler(ResLoader target)
        {
            _loadingList.Remove(target);
            target.progressHandler -= OneProgressHandler;
            target.completeHandler -= OneCompleteHandler;
            target.errorHandler -= OneErrorHandler;
            //Observers.Facade.Instance.SendNotification(target.ResObj.SubPath, target.ResObj, TYPE_ERROR);
            if(ErrorAction != null)
                ErrorAction(target.ResObj.SubPath, target.ResObj);
            TryLoadRes();
        }

        #endregion

        private static int CompareResLoaderPrior(ResLoader a, ResLoader b)
        {
            if (a.ResObj.Prior > b.ResObj.Prior) return -1;
            if (a.ResObj.Prior < b.ResObj.Prior) return 1;
            return 0;
        }

    }
}
