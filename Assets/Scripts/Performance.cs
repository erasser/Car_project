using UnityEngine;
using UnityEngine.UI;

public static class Performance
{
    static readonly Text PerformanceText;  // UI element
    static int _frames;
    static float _timeSum;

    static Performance()
    {
        PerformanceText = GameObject.Find("FPS").GetComponent<Text>();
    }

    // called from Update()
    public static void ShowFPS()
    {
        if (_timeSum > 1)  // seconds
        {
            PerformanceText.text = Mathf.Floor(_frames / _timeSum).ToString();
            _frames = 0;
            _timeSum = 0;
        }
        else
        {
            _frames++;
            _timeSum += Time.deltaTime;
        }
    }

}