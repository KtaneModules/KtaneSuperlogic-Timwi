using System;
using System.Collections.Generic;
using System.Linq;
using Superlogic;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Superlogic
/// Created by Timwi
/// </summary>
public class SuperlogicModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
    }
}
