using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    private static T _inst;

    public static T Inst
    {
        get
        {
            return _inst;
        }
    }

    protected virtual void Awake()
    {
        if (_inst != null &&  _inst != this)
        {
            Destroy(gameObject);
            return;
        }

        _inst = (T)this;
    }
}
