using UnityEngine;

/// <summary>
/// On the Subject of Simon Speaks
/// Created by Timwi
/// </summary>
public class SimonSpeaksModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    [UnityEditor.MenuItem("DoStuff/DoStuff")]
    public static void DoStuff()
    {
        var m = FindObjectOfType<SimonSpeaksModule>();
        for (int i = 0; i < 9; i++)
        {
            var par = m.transform.Find("Body" + (i + 1));
            var body = m.transform.Find("Body" + (i + 1)).Find("Body" + (i + 1));
            var outline = m.transform.Find("Outline" + (i + 1)).Find("Outline" + (i + 1));

            outline.parent = par;
            par.gameObject.name = "Bubble" + (i + 1);
        }
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
    }
}
