using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers _instance;
    public static Managers Instance { get { Init(); return _instance; } }

    private readonly BeatManager _beatManager = new();
    public static BeatManager Beat => Instance._beatManager;

    private void Start()
    {
        Init();
    }

    public static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Manager");
            if (go == null)
            {
                go = new GameObject { name = "@Manager" };
                go.AddComponent<Managers>();
            }
            DontDestroyOnLoad(go);
            _instance = go.GetComponent<Managers>();
            _instance._beatManager.Init();
        }
    }

    public static void Clear()
    {
        Beat.Clear();
    }
}
