using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : SingletonBase<GameDataManager>
{
    protected override void Awake()
    {
        base.Awake();
    }

    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
    }

    private Dictionary<string, object> _dataList = new Dictionary<string, object>();

    private Dictionary<string, T> LoadJsonData<T>(string tableName) where T : GameDataBase
    {
        // Resources/JsonOutput 폴더
        string resourcePath = $"JsonOutput/{tableName}";

        // 리소스 로드
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        // 파일 존재 여부 체크
        if (textAsset == null)
        {
            Debug.LogError($"[Error] 리소스를 찾을 수 없습니다: Resources/{resourcePath}");
            return new Dictionary<string, T>();
        }

        try
        {
            string jsonString = textAsset.text;

            // JsonUtility용 Wrapper 트릭 적용
            string wrappedJson = "{\"items\":" + jsonString + "}";
            SerializationWrapper<T> wrapper = JsonUtility.FromJson<SerializationWrapper<T>>(wrappedJson);

            if (wrapper != null && wrapper.items != null)
            {
                Debug.Log($"{typeof(T).Name} 데이터를 {wrapper.items.Count}개 로드했습니다.");
                // ToDictionary를 사용하려면 각 클래스(T)에 Id 필드가 있어야 합니다.
                return wrapper.items.ToDictionary(item => item.Id.ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{typeof(T).Name} JSON 로드 오류] {ex.Message}");
        }

        return new Dictionary<string, T>();
    }

    public void LoadData<T>() where T : GameDataBase
    {
        string dataName = typeof(T).Name;
        if (_dataList.ContainsKey(dataName) == false)
        {
            _dataList.Add(dataName, new Dictionary<string, T>());
        }
        _dataList[dataName] = LoadJsonData<T>(dataName);
    }


    public T GetData<T>(string id) where T : GameDataBase
    {
        string type = typeof(T).Name;
        object dictObj = null;
        if (_dataList.TryGetValue(type, out dictObj))
        {
            var dict = dictObj as Dictionary<string, T>;
            return dict[id];
        }
        return null;
    }

    public List<string> GetAllDataId<T>() where T : GameDataBase
    {
        string type = typeof(T).Name;
        object dictObj = null;

        if (_dataList.TryGetValue(type, out dictObj))
        {
            var dict = dictObj as Dictionary<string, T>;
            return dict.Keys.ToList();
        }
        return null;
    }

    public List<T> GetAllData<T>() where T : GameDataBase
    {
        string type = typeof(T).Name;
        object dictObj = null;

        if (_dataList.TryGetValue(type, out dictObj))
        {
            var dict = dictObj as Dictionary<string, T>;
            return dict.Values.ToList();
        }
        return null;
    }
}
