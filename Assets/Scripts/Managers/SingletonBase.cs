using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    private static T _inst;

    public static T Inst => _inst;

    protected virtual void Awake()
    {
        if (_inst != null && _inst != this)
        {
            Destroy(gameObject);
            return;
        }

        _inst = (T)this;
    }

    // 현재 등록된 싱글톤 객체가 제거될 때 정적 참조 해제
    protected virtual void OnDestroy()
    {
        if (_inst != this)
        {
            return;
        }

        _inst = null;
    }
}